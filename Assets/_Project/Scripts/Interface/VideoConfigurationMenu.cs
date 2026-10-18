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
    [SerializeField] private TMP_Dropdown resolution, display, antialiasing, textureQuality, sync, frameLimit;
    [SerializeField] private UnityEngine.UI.Button apply, defaults, back, keep, revert;
    [SerializeField] private CanvasGroup controls;
    [SerializeField] private GameObject confirmation;
    [SerializeField] private TMP_Text confirmationText, status;
    private struct Values
    {
        public int width, height, textureLimit, vsync, limit;
        public VideoAntialiasing antialiasing;
        public FullScreenMode mode;
        public RefreshRate rate;
    }
    private Values current, draft, original;
    private readonly List<Vector2Int> sizes = new List<Vector2Int>();
    private readonly List<FullScreenMode> modes = new List<FullScreenMode>();
    private bool initialized, confirming;
    private float confirmationDeadline;
    private TMP_Dropdown[] selectors;
    private VideoQualityRuntime nativeQuality;
    private const string Prefix = GameSaveKeys.VideoPrefix;

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;
        selectors = new[] { display, resolution, antialiasing, textureQuality, sync, frameLimit };
        nativeQuality = new VideoQualityRuntime();
        original = Capture(); current = original;
        original.antialiasing = current.antialiasing = nativeQuality.Original;
        modes.Add(FullScreenMode.FullScreenWindow); modes.Add(FullScreenMode.Windowed);
        if ((Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) && Screen.resolutions.Length > 0)
            modes.Add(FullScreenMode.ExclusiveFullScreen);
        current.antialiasing = (VideoAntialiasing)Mathf.Clamp(GameSave.GetInt(Prefix + "Antialiasing", (int)settings.defaultAntialiasing), 0, 5);
        current.textureLimit = Mathf.Clamp(GameSave.GetInt(Prefix + "TextureLimit", settings.defaultTextureLimit), 0, 3);
        current.vsync = Mathf.Clamp(GameSave.GetInt(Prefix + "Vsync", settings.defaultVSync ? 1 : 0), 0, 1);
        current.limit = GameSave.GetInt(Prefix + "FrameLimit", settings.defaultFrameLimit);
        if (!settings.frameLimits.Contains(current.limit)) current.limit = settings.defaultFrameLimit;
        // Migrate the previous Windowed preference; unavailable display modes fall back to borderless.
        int oldMode = GameSave.GetInt(Prefix + "Windowed", current.mode == FullScreenMode.Windowed ? 1 : 0) == 1 ? (int)FullScreenMode.Windowed : (int)FullScreenMode.FullScreenWindow;
        current.mode = (FullScreenMode)GameSave.GetInt(Prefix + "Mode", oldMode);
        if (!modes.Contains(current.mode)) current.mode = FullScreenMode.FullScreenWindow;
        int width = GameSave.GetInt(Prefix + "Width", current.width), height = GameSave.GetInt(Prefix + "Height", current.height);
        if (Screen.resolutions.Any(r => r.width == width && r.height == height) || current.mode == FullScreenMode.Windowed && width > 0 && height > 0)
        { current.width = width; current.height = height; }
        draft = current; BuildModes(); current = draft;
        ApplyNative(current, GameSave.HasKey(Prefix + "Width") || GameSave.HasKey(Prefix + "Windowed") || GameSave.HasKey(Prefix + "Mode"));
        display.onValueChanged.AddListener(i => { draft.mode = modes[i]; BuildModes(); ShowValues(); });
        resolution.onValueChanged.AddListener(i => { draft.width = sizes[i].x; draft.height = sizes[i].y; ShowValues(); });
        antialiasing.onValueChanged.AddListener(i => { draft.antialiasing = (VideoAntialiasing)i; ShowValues(); });
        textureQuality.onValueChanged.AddListener(i => { draft.textureLimit = i; ShowValues(); });
        sync.onValueChanged.AddListener(i => { draft.vsync = i; ShowValues(); });
        frameLimit.onValueChanged.AddListener(i => { draft.limit = settings.frameLimits[i]; ShowValues(); });
        apply.onClick.AddListener(Apply); defaults.onClick.AddListener(Defaults);
        keep.onClick.AddListener(Keep); revert.onClick.AddListener(Revert);
    }
    private static Values Capture() => new Values { width = Screen.width, height = Screen.height, mode = Screen.fullScreenMode,
        rate = Screen.currentResolution.refreshRateRatio, textureLimit = QualitySettings.globalTextureMipmapLimit, vsync = Mathf.Clamp(QualitySettings.vSyncCount, 0, 1), limit = Application.targetFrameRate };
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
        SetOptions(display, modes.Select(m => m == FullScreenMode.Windowed ? GameText.Get("Settings", "mode.windowed") : m == FullScreenMode.ExclusiveFullScreen ? GameText.Get("Settings", "mode.exclusive") : GameText.Get("Settings", "mode.borderless")));
        SetOptions(antialiasing, new[] { GameText.Get("Settings", "option.off"), GameText.Get("Settings", "antialiasing.fxaa"), GameText.Get("Settings", "antialiasing.smaa"),
            GameText.Get("Settings", "antialiasing.msaa2"), GameText.Get("Settings", "antialiasing.msaa4"), GameText.Get("Settings", "antialiasing.msaa8") });
        SetOptions(textureQuality, new[] { GameText.Get("Settings", "texture.full"), GameText.Get("Settings", "texture.half"), GameText.Get("Settings", "texture.quarter"), GameText.Get("Settings", "texture.eighth") });
        SetOptions(sync, new[] { GameText.Get("Settings", "option.off"), GameText.Get("Settings", "sync.on") });
        SetOptions(frameLimit, settings.frameLimits.Select(n => n < 0 ? GameText.Get("Settings", "frames.unlimited") : n + GameText.Get("Settings", "units.fps")));
    }
    private static void SetOptions(TMP_Dropdown dropdown, IEnumerable<string> values)
    { dropdown.ClearOptions(); dropdown.AddOptions(values.ToList()); }
    public void Refresh(bool selectFirst = true)
    {
        Initialize(); draft = current; BuildModes(); ShowValues();
        status.text = Application.isEditor ? GameText.Get("Settings", "video.editorPreview") : GameText.Get("Settings", "video.applyHint");
        if (selectFirst) EventSystem.current?.SetSelectedGameObject(display.gameObject);
    }
    private void ShowValues()
    {
        display.SetValueWithoutNotify(modes.IndexOf(draft.mode));
        resolution.SetValueWithoutNotify(sizes.IndexOf(new Vector2Int(draft.width, draft.height)));
        antialiasing.SetValueWithoutNotify((int)draft.antialiasing); textureQuality.SetValueWithoutNotify(draft.textureLimit); sync.SetValueWithoutNotify(draft.vsync);
        frameLimit.SetValueWithoutNotify(Math.Max(0, Array.IndexOf(settings.frameLimits, draft.limit)));
        resolution.interactable = sizes.Count > 1;
        frameLimit.interactable = draft.vsync == 0;
        RefreshCaptions();
        Navigation();
    }
    // TMP Start refreshes the caption on the first visible frame, after this screen opens.
    private void LateUpdate() { if (initialized) RefreshCaptions(); }
    private void RefreshCaptions()
    {
        if (draft.vsync != 0) frameLimit.captionText.text = GameText.Get("Settings", "frames.vsync");
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
            antialiasing = settings.defaultAntialiasing, textureLimit = settings.defaultTextureLimit, vsync = settings.defaultVSync ? 1 : 0, limit = settings.defaultFrameLimit };
        BuildModes(); ShowValues(); status.text = GameText.Get("Settings", "video.defaults");
    }
    private static bool DisplayChanged(Values a, Values b) => a.width != b.width || a.height != b.height || a.mode != b.mode ||
        (a.mode == FullScreenMode.ExclusiveFullScreen && Math.Abs(a.rate.value - b.rate.value) > 0.01);
    private void ApplyNative(Values values, bool displayChanged)
    {
        nativeQuality.Apply(values.antialiasing);
        QualitySettings.globalTextureMipmapLimit = values.textureLimit;
        QualitySettings.vSyncCount = values.vsync;
        Application.targetFrameRate = values.vsync > 0 ? -1 : values.limit;
        if (displayChanged && !Application.isEditor) Screen.SetResolution(values.width, values.height, values.mode, values.rate);
    }
    private void Apply()
    {
        if (confirming) return;
        bool changed = DisplayChanged(draft, current);
        ApplyNative(draft, changed);
        if (!changed) { current = draft; status.text = GameText.Get("Settings", Save() ? "video.saved" : "save.failed"); return; }
        confirming = true; confirmationDeadline = Time.realtimeSinceStartup + settings.displayConfirmationSeconds;
        controls.interactable = controls.blocksRaycasts = false; confirmation.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(revert.gameObject); UpdateConfirmation();
    }
    private void Update() { if (!confirming) return; if (Time.realtimeSinceStartup >= confirmationDeadline) Revert(); else UpdateConfirmation(); }
    private void UpdateConfirmation() => confirmationText.text = GameText.Get("Settings", "display.confirmation") + Mathf.CeilToInt(confirmationDeadline - Time.realtimeSinceStartup) + GameText.Get("Settings", "display.seconds");
    private void Keep()
    {
        if (!confirming) return;
        current = draft;
        if (!Application.isEditor) { var actual = Capture(); current.width = actual.width; current.height = actual.height; current.mode = actual.mode; current.rate = actual.rate; }
        bool saved = Save(); FinishConfirmation(); status.text = GameText.Get("Settings", saved ? "video.saved" : "save.failed");
    }
    private void Revert()
    {
        if (!confirming) return;
        ApplyNative(current, true); draft = current; FinishConfirmation(); status.text = GameText.Get("Settings", "video.restored");
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
    private bool Save()
    {
        GameSave.SetInt(Prefix + "Antialiasing", (int)current.antialiasing); GameSave.SetInt(Prefix + "TextureLimit", current.textureLimit);
        GameSave.SetInt(Prefix + "Vsync", current.vsync); GameSave.SetInt(Prefix + "FrameLimit", current.limit);
        GameSave.SetInt(Prefix + "Width", current.width); GameSave.SetInt(Prefix + "Height", current.height); GameSave.SetInt(Prefix + "Mode", (int)current.mode);
        GameSave.SetInt(Prefix + "Windowed", current.mode == FullScreenMode.Windowed ? 1 : 0);
        return GameSave.Save();
    }
    private void OnDisable() { if (!initialized) return; foreach (var selector in selectors) if (selector != null) selector.Hide(); Revert(); }
    private void OnDestroy()
    {
        if (!initialized) return;
        if (Application.isEditor)
        {
            QualitySettings.globalTextureMipmapLimit = original.textureLimit;
            QualitySettings.vSyncCount = original.vsync; Application.targetFrameRate = original.limit;
        }
        nativeQuality.Dispose();
    }
}
