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
    private static readonly string[] SaveFiles = { "Primary", "Backup" };
    public static string SnapshotVideoPreferences()
    {
        foreach (var name in SaveFiles)
        {
            string path = name == "Primary" ? GameSave.SavePath : GameSave.BackupPath;
            string cache = "Mecha.ManualPrefs." + name;
            SessionState.SetBool(cache + ".Exists", System.IO.File.Exists(path));
            if (System.IO.File.Exists(path)) SessionState.SetString(cache, System.Convert.ToBase64String(System.IO.File.ReadAllBytes(path)));
        }
        SessionState.SetBool("Mecha.ManualPrefs.Snapshot", true);
        return "Shared settings JSON and backup retained for restoration; do not change unrelated settings during this manual inspection";
    }
    public static string RestoreVideoPreferences()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before restoring saved files.");
        if (!SessionState.GetBool("Mecha.ManualPrefs.Snapshot", false)) throw new System.InvalidOperationException("No saved-file snapshot exists.");
        foreach (var name in SaveFiles)
        {
            string path = name == "Primary" ? GameSave.SavePath : GameSave.BackupPath;
            string cache = "Mecha.ManualPrefs." + name;
            if (!SessionState.GetBool(cache + ".Exists", false)) { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
            else
            {
                System.IO.Directory.CreateDirectory(Application.persistentDataPath);
                System.IO.File.WriteAllBytes(path, System.Convert.FromBase64String(SessionState.GetString(cache, "")));
            }
            SessionState.EraseBool(cache + ".Exists");
            SessionState.EraseString(cache);
        }
        SessionState.EraseBool("Mecha.ManualPrefs.Snapshot");
        typeof(GameSave).GetMethod("Reset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
        return "Original shared settings JSON and backup restored";
    }
    public static string Cleanup()
    {
        foreach(var pad in InputSystem.devices.OfType<Gamepad>().Where(d => d.name == "CodexManualController").ToArray()) InputSystem.RemoveDevice(pad);
        Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().moveRepeatDelay = 0.35f;
        return "Manual device removed; native repeat delay restored";
    }
}
