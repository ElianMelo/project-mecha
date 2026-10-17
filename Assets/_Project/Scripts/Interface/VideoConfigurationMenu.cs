using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Native TMP dropdowns own selection and scrolling. Values are staged until Apply.
public sealed class VideoConfigurationMenu : MonoBehaviour
{
    [SerializeField] private VideoConfigurationSettings settings;
    [SerializeField] private TMP_Dropdown resolution, display, refreshRate, quality, sync, frameLimit;
    [SerializeField] private UnityEngine.UI.Button apply, defaults, back, keep, revert;
    [SerializeField] private CanvasGroup controls;
    [SerializeField] private GameObject confirmation;
    [SerializeField] private TMP_Text confirmationText, status;
    private struct Values
    {
        public int width, height, quality, vsync, limit;
        public FullScreenMode mode;
        public RefreshRate rate;
    }
    private Values current, draft, original;
    private readonly List<Vector2Int> sizes = new List<Vector2Int>();
    private readonly List<RefreshRate> rates = new List<RefreshRate>();
    private readonly List<FullScreenMode> modes = new List<FullScreenMode>();
    private bool initialized, confirming;
    private float confirmationDeadline;
    private TMP_Dropdown[] selectors;
    private const string Prefix = "Mecha.Video.";

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;
        selectors = new[] { display, resolution, refreshRate, quality, sync, frameLimit };
        original = Capture(); current = original;
        modes.Add(FullScreenMode.FullScreenWindow); modes.Add(FullScreenMode.Windowed);
        if ((Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) && Screen.resolutions.Length > 0)
            modes.Add(FullScreenMode.ExclusiveFullScreen);
        string qualityName = PlayerPrefs.GetString(Prefix + "QualityName", QualitySettings.names[current.quality]);
        int qualityIndex = Array.IndexOf(QualitySettings.names, qualityName);
        if (qualityIndex >= 0) current.quality = qualityIndex;
        current.vsync = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "VSync", settings.defaultVSync ? 1 : 0), 0, 1);
        current.limit = PlayerPrefs.GetInt(Prefix + "FrameLimit", settings.defaultFrameLimit);
        if (!settings.frameLimits.Contains(current.limit)) current.limit = settings.defaultFrameLimit;
        // Migrate the previous Windowed preference; unavailable display modes fall back to borderless.
        int oldMode = PlayerPrefs.GetInt(Prefix + "Windowed", current.mode == FullScreenMode.Windowed ? 1 : 0) == 1 ? (int)FullScreenMode.Windowed : (int)FullScreenMode.FullScreenWindow;
        current.mode = (FullScreenMode)PlayerPrefs.GetInt(Prefix + "Mode", oldMode);
        if (!modes.Contains(current.mode)) current.mode = FullScreenMode.FullScreenWindow;
        int width = PlayerPrefs.GetInt(Prefix + "Width", current.width), height = PlayerPrefs.GetInt(Prefix + "Height", current.height);
        if (Screen.resolutions.Any(r => r.width == width && r.height == height) || current.mode == FullScreenMode.Windowed && width > 0 && height > 0)
        { current.width = width; current.height = height; }
        current.rate = new RefreshRate { numerator = (uint)Math.Max(1, PlayerPrefs.GetInt(Prefix + "RefreshNumerator", (int)current.rate.numerator)), denominator = (uint)Math.Max(1, PlayerPrefs.GetInt(Prefix + "RefreshDenominator", (int)current.rate.denominator)) };
        draft = current; BuildModes(); current = draft;
        ApplyNative(current, PlayerPrefs.HasKey(Prefix + "Width") || PlayerPrefs.HasKey(Prefix + "Windowed") || PlayerPrefs.HasKey(Prefix + "Mode"));
        display.onValueChanged.AddListener(i => { draft.mode = modes[i]; BuildModes(); ShowValues(); });
        resolution.onValueChanged.AddListener(i => { draft.width = sizes[i].x; draft.height = sizes[i].y; BuildRates(); ShowValues(); });
        refreshRate.onValueChanged.AddListener(i => { draft.rate = rates[i]; ShowValues(); });
        quality.onValueChanged.AddListener(i => { draft.quality = i; ShowValues(); });
        sync.onValueChanged.AddListener(i => { draft.vsync = i; ShowValues(); });
        frameLimit.onValueChanged.AddListener(i => { draft.limit = settings.frameLimits[i]; ShowValues(); });
        apply.onClick.AddListener(Apply); defaults.onClick.AddListener(Defaults);
        keep.onClick.AddListener(Keep); revert.onClick.AddListener(Revert);
    }
    private static Values Capture() => new Values { width = Screen.width, height = Screen.height, mode = Screen.fullScreenMode,
        rate = Screen.currentResolution.refreshRateRatio, quality = QualitySettings.GetQualityLevel(), vsync = Mathf.Clamp(QualitySettings.vSyncCount, 0, 1), limit = Application.targetFrameRate };
    private void BuildModes()
    {
        sizes.Clear();
        sizes.AddRange(Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).Distinct().OrderBy(s => s.x).ThenBy(s => s.y));
        var size = new Vector2Int(draft.width, draft.height);
        if (draft.mode != FullScreenMode.ExclusiveFullScreen && !sizes.Contains(size)) sizes.Add(size);
        sizes.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        if (sizes.Count == 0) sizes.Add(new Vector2Int(Screen.width, Screen.height));
        if (!sizes.Contains(size)) { draft.width = sizes[sizes.Count - 1].x; draft.height = sizes[sizes.Count - 1].y; }
        SetOptions(resolution, sizes.Select(s => s.x + " × " + s.y));
        SetOptions(display, modes.Select(m => m == FullScreenMode.Windowed ? "WINDOWED" : m == FullScreenMode.ExclusiveFullScreen ? "FULLSCREEN (EXCLUSIVE)" : "FULLSCREEN (BORDERLESS)"));
        SetOptions(quality, QualitySettings.names.Select(n => n.ToUpperInvariant()));
        SetOptions(sync, new[] { "OFF", "ON" });
        SetOptions(frameLimit, settings.frameLimits.Select(n => n < 0 ? "UNLIMITED" : n + " FPS"));
        BuildRates();
    }
    private void BuildRates()
    {
        rates.Clear();
        foreach (var mode in Screen.resolutions.Where(r => r.width == draft.width && r.height == draft.height).OrderBy(r => r.refreshRateRatio.value))
            if (!rates.Any(r => Math.Abs(r.value - mode.refreshRateRatio.value) < 0.01)) rates.Add(mode.refreshRateRatio);
        if (rates.Count == 0) rates.Add(Screen.currentResolution.refreshRateRatio);
        if (!rates.Any(r => Math.Abs(r.value - draft.rate.value) < 0.01)) draft.rate = rates[rates.Count - 1];
        SetOptions(refreshRate, rates.Select(r => r.value.ToString("0.##") + " HZ"));
    }
    private static void SetOptions(TMP_Dropdown dropdown, IEnumerable<string> values)
    { dropdown.ClearOptions(); dropdown.AddOptions(values.ToList()); }
    public void Refresh(bool selectFirst = true)
    {
        Initialize(); draft = current; BuildModes(); ShowValues();
        status.text = Application.isEditor ? "Display changes preview in a desktop build. Other settings apply here." : "Changes take effect when you choose Apply.";
        if (selectFirst) EventSystem.current?.SetSelectedGameObject(display.gameObject);
    }
    private void ShowValues()
    {
        display.SetValueWithoutNotify(modes.IndexOf(draft.mode));
        resolution.SetValueWithoutNotify(sizes.IndexOf(new Vector2Int(draft.width, draft.height)));
        refreshRate.SetValueWithoutNotify(Math.Max(0, rates.FindIndex(r => Math.Abs(r.value - draft.rate.value) < 0.01)));
        quality.SetValueWithoutNotify(draft.quality); sync.SetValueWithoutNotify(draft.vsync);
        frameLimit.SetValueWithoutNotify(Math.Max(0, Array.IndexOf(settings.frameLimits, draft.limit)));
        resolution.interactable = sizes.Count > 1; quality.interactable = QualitySettings.names.Length > 1;
        refreshRate.interactable = draft.mode == FullScreenMode.ExclusiveFullScreen && rates.Count > 1;
        frameLimit.interactable = draft.vsync == 0;
        RefreshCaptions();
        Navigation();
    }
    // TMP Start refreshes the caption on the first visible frame, after this screen opens.
    private void LateUpdate() { if (initialized) RefreshCaptions(); }
    private void RefreshCaptions()
    {
        if (draft.mode != FullScreenMode.ExclusiveFullScreen) refreshRate.captionText.text = "DESKTOP (" + Screen.currentResolution.refreshRateRatio.value.ToString("0.##") + " HZ)";
        if (draft.vsync != 0) frameLimit.captionText.text = "CONTROLLED BY V-SYNC";
    }
    private void Navigation()
    {
        var sequence = selectors.Cast<UnityEngine.UI.Selectable>().Concat(new UnityEngine.UI.Selectable[] { apply, defaults, back }).Where(s => s.IsInteractable()).ToArray();
        for (int i = 0; i < sequence.Length; i++) sequence[i].navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
            selectOnUp = sequence[(i + sequence.Length - 1) % sequence.Length], selectOnDown = sequence[(i + 1) % sequence.Length] };
        var footer = new[] { apply, defaults, back };
        for (int i = 0; i < footer.Length; i++)
        {
            var links = footer[i].navigation;
            links.selectOnLeft = footer[(i + footer.Length - 1) % footer.Length];
            links.selectOnRight = footer[(i + 1) % footer.Length]; footer[i].navigation = links;
        }
    }
    private void Defaults()
    {
        var desktop = Screen.currentResolution;
        draft = new Values { width = desktop.width, height = desktop.height, rate = desktop.refreshRateRatio, mode = FullScreenMode.FullScreenWindow,
            quality = original.quality, vsync = settings.defaultVSync ? 1 : 0, limit = settings.defaultFrameLimit };
        BuildModes(); ShowValues(); status.text = "Defaults selected. Choose Apply to save.";
    }
    private static bool DisplayChanged(Values a, Values b) => a.width != b.width || a.height != b.height || a.mode != b.mode ||
        (a.mode == FullScreenMode.ExclusiveFullScreen && Math.Abs(a.rate.value - b.rate.value) > 0.01);
    private static void ApplyNative(Values values, bool displayChanged)
    {
        QualitySettings.SetQualityLevel(values.quality, true);
        // Presets can reset v-sync, so frame pacing is applied afterwards.
        QualitySettings.vSyncCount = values.vsync;
        Application.targetFrameRate = values.vsync > 0 ? -1 : values.limit;
        if (displayChanged && !Application.isEditor) Screen.SetResolution(values.width, values.height, values.mode, values.rate);
    }
    private void Apply()
    {
        if (confirming) return;
        bool changed = DisplayChanged(draft, current);
        ApplyNative(draft, changed);
        if (!changed) { current = draft; Save(); status.text = "Video settings saved."; return; }
        confirming = true; confirmationDeadline = Time.realtimeSinceStartup + settings.displayConfirmationSeconds;
        controls.interactable = controls.blocksRaycasts = false; confirmation.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(revert.gameObject); UpdateConfirmation();
    }
    private void Update() { if (!confirming) return; if (Time.realtimeSinceStartup >= confirmationDeadline) Revert(); else UpdateConfirmation(); }
    private void UpdateConfirmation() => confirmationText.text = "KEEP THESE DISPLAY SETTINGS?\nReverting in " + Mathf.CeilToInt(confirmationDeadline - Time.realtimeSinceStartup) + " seconds";
    private void Keep()
    {
        if (!confirming) return;
        current = draft;
        if (!Application.isEditor) { var actual = Capture(); current.width = actual.width; current.height = actual.height; current.mode = actual.mode; current.rate = actual.rate; }
        Save(); FinishConfirmation(); status.text = "Video settings saved.";
    }
    private void Revert()
    {
        if (!confirming) return;
        ApplyNative(current, true); draft = current; FinishConfirmation(); status.text = "Previous video settings restored.";
    }
    private void FinishConfirmation()
    {
        confirming = false; confirmation.SetActive(false); controls.interactable = controls.blocksRaycasts = true;
        BuildModes(); ShowValues(); if (isActiveAndEnabled) EventSystem.current?.SetSelectedGameObject(apply.gameObject);
    }
    public bool ConsumeBack()
    {
        foreach (var selector in selectors) if (selector.IsExpanded) { selector.Hide(); selector.Select(); return true; }
        if (!confirming) return false;
        Revert(); return true;
    }
    private void Save()
    {
        PlayerPrefs.SetString(Prefix + "QualityName", QualitySettings.names[current.quality]); PlayerPrefs.SetInt(Prefix + "VSync", current.vsync); PlayerPrefs.SetInt(Prefix + "FrameLimit", current.limit);
        PlayerPrefs.SetInt(Prefix + "Width", current.width); PlayerPrefs.SetInt(Prefix + "Height", current.height); PlayerPrefs.SetInt(Prefix + "Mode", (int)current.mode);
        PlayerPrefs.SetInt(Prefix + "Windowed", current.mode == FullScreenMode.Windowed ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "RefreshNumerator", (int)current.rate.numerator); PlayerPrefs.SetInt(Prefix + "RefreshDenominator", (int)current.rate.denominator); PlayerPrefs.Save();
    }
    private void OnDisable() { if (!initialized) return; foreach (var selector in selectors) if (selector != null) selector.Hide(); Revert(); }
    private void OnDestroy() { if (initialized && Application.isEditor) ApplyNative(original, false); }
}
