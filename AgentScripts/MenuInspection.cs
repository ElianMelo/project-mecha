using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
public static class MenuInspection
{
    public static object State()
    {
        var video = Object.FindFirstObjectByType<VideoConfigurationMenu>(FindObjectsInactive.Include);
        return new { frame = Time.frameCount, selected = EventSystem.current?.currentSelectedGameObject?.name,
            options = GameOptionsMenu.Instance.IsVisible, audio = Object.FindFirstObjectByType<AudioConfigurationMenu>().IsVisible,
            devices = InputSystem.devices.Select(d => d.name).ToArray(),
            videoActive = video.gameObject.activeSelf,
            fields = video.GetComponentsInChildren<TMP_Dropdown>(true).Select(d => new { d.name, d.value, d.IsExpanded, d.interactable, caption = d.captionText.text, count = d.options.Count,
                up = d.navigation.selectOnUp?.name, down = d.navigation.selectOnDown?.name }).ToArray(),
            confirm = video.transform.Find("Display Confirmation").gameObject.activeSelf,
            status = video.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Status").text,
            sync = QualitySettings.vSyncCount, target = Application.targetFrameRate };
    }
    public static string Prepare()
    {
        if (!InputSystem.devices.Any(d => d.name == "CodexManualController")) InputSystem.AddDevice("Gamepad", "CodexManualController");
        Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().moveRepeatDelay = 20f;
        return "Manual controller ready";
    }
    public static string Focus(string name)
    {
        var control = Object.FindObjectsByType<UnityEngine.UI.Selectable>(FindObjectsSortMode.None).Single(c => c.name == name);
        EventSystem.current.SetSelectedGameObject(control.gameObject); return control.name;
    }
    public static object Audio()
    {
        var menu = Object.FindFirstObjectByType<AudioConfigurationMenu>();
        return menu.GetComponentsInChildren<UnityEngine.UI.Selectable>(true).Select(c => new { c.name,
            selected = EventSystem.current.currentSelectedGameObject == c.gameObject,
            border = c.transform.Find("Selection Border")?.GetComponent<UnityEngine.UI.Image>().enabled,
            bounds = c.transform.Find("Selection Border")?.GetComponent<RectTransform>().rect.ToString(),
            up = c.navigation.selectOnUp?.name, down = c.navigation.selectOnDown?.name }).ToArray();
    }
    public static string Change(string name, int value)
    {
        var dropdown = Object.FindObjectsByType<TMP_Dropdown>(FindObjectsSortMode.None).Single(d => d.name == name);
        dropdown.value = value; return name + " = " + dropdown.captionText.text;
    }
    private static readonly string[] VideoKeys = { "QualityName", "Quality", "VSync", "FrameLimit", "Width", "Height", "Mode", "Windowed", "RefreshNumerator", "RefreshDenominator" };
    public static string SnapshotVideoPreferences()
    {
        foreach (var key in VideoKeys)
        {
            string full = "Mecha.Video." + key, cache = "Mecha.ManualPrefs." + key;
            SessionState.SetBool(cache + ".Exists", PlayerPrefs.HasKey(full));
            if (key == "QualityName") SessionState.SetString(cache, PlayerPrefs.GetString(full));
            else SessionState.SetInt(cache, PlayerPrefs.GetInt(full));
        }
        return "Video preference snapshot retained for restoration";
    }
    public static string RestoreVideoPreferences()
    {
        foreach (var key in VideoKeys)
        {
            string full = "Mecha.Video." + key, cache = "Mecha.ManualPrefs." + key;
            if (!SessionState.GetBool(cache + ".Exists", false)) PlayerPrefs.DeleteKey(full);
            else if (key == "QualityName") PlayerPrefs.SetString(full, SessionState.GetString(cache, ""));
            else PlayerPrefs.SetInt(full, SessionState.GetInt(cache, 0));
            SessionState.EraseBool(cache + ".Exists");
            if (key == "QualityName") SessionState.EraseString(cache); else SessionState.EraseInt(cache);
        }
        PlayerPrefs.Save(); return "Original video preferences restored";
    }
    public static string Cleanup()
    {
        foreach(var pad in InputSystem.devices.OfType<Gamepad>().Where(d => d.name == "CodexManualController").ToArray()) InputSystem.RemoveDevice(pad);
        Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().moveRepeatDelay = 0.35f;
        return "Manual device removed; native repeat delay restored";
    }
}
