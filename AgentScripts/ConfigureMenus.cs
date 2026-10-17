using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class ConfigureMenus
{
    public static object Inspect() => new {
        scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
        dirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,
        mute = EditorUtility.audioMasterMute,
        quality = QualitySettings.names,
        nativeModes = Screen.resolutions.Select(r => r.ToString()).ToArray(),
        lobbyPrefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(UnityEngine.Object.FindFirstObjectByType<MechaLobbyPanel>())
    };
    public static object InspectAuthored()
    {
        var menu = UnityEngine.Object.FindFirstObjectByType<VideoConfigurationMenu>(FindObjectsInactive.Include);
        var serialized = new SerializedObject(menu);
        string[] fields = { "settings", "resolution", "display", "refreshRate", "quality", "sync", "frameLimit", "apply", "defaults", "back", "keep", "revert", "controls", "confirmation", "confirmationText", "status" };
        var audio = UnityEngine.Object.FindFirstObjectByType<AudioConfigurationMenu>();
        var lobby = UnityEngine.Object.FindFirstObjectByType<MechaLobbyPanel>();
        var host = (UnityEngine.UI.Button)new SerializedObject(lobby).FindProperty("localHost").objectReferenceValue;
        return new {
            missingVideoReferences = fields.Where(f => serialized.FindProperty(f).objectReferenceValue == null).ToArray(),
            missingSceneScripts = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
            audioButtons = audio.GetComponentsInChildren<UnityEngine.UI.Button>(true).Select(b => b.name).ToArray(),
            handleSizes = audio.GetComponentsInChildren<UnityEngine.UI.Slider>(true).Select(s => s.handleRect.sizeDelta.ToString()).ToArray(),
            hostText = host.GetComponentInChildren<TMPro.TMP_Text>(true).text,
            hostPrefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(host),
            help = MechaPresentationSetup.Profile<ControllerInterfaceSettings>("ControllerInterface").controllerHelp,
            videoProfile = AssetDatabase.GetAssetPath(MechaPresentationSetup.Profile<VideoConfigurationSettings>("VideoConfiguration")),
            videoLabels = AssetDatabase.GetLabels(MechaPresentationSetup.Profile<VideoConfigurationSettings>("VideoConfiguration")),
            sceneDirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,
            mute = EditorUtility.audioMasterMute
        };
    }
    public static string Apply()
    {
        var profile = MechaPresentationSetup.Profile<VideoConfigurationSettings>("VideoConfiguration");
        string oldPath = AssetDatabase.GetAssetPath(profile), newPath = MechaSettingsSetup.PathFor(typeof(VideoConfigurationSettings), "VideoConfiguration");
        if (oldPath != newPath) { string error = AssetDatabase.MoveAsset(oldPath, newPath); if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error); }
        MechaMenuRefinementSetup.ApplyConfiguration();
        var lobbyRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(UnityEngine.Object.FindFirstObjectByType<MechaLobbyPanel>());
        if (lobbyRoot != null) PrefabUtility.ApplyPrefabInstance(lobbyRoot, InteractionMode.AutomatedAction);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        foreach (var path in new[] { "Assets/_Project/Scripts/Interface/VideoConfigurationMenu.cs", "Assets/_Project/Scripts/Interface/VideoConfigurationSettings.cs", "Assets/_Project/Scripts/Interface/UiSelectionScroll.cs", "Assets/_Project/Scripts/Interface/GameOptionsMenu.cs" })
            AssetDatabase.SetLabels(AssetDatabase.LoadMainAssetAtPath(path), new[] { "Code" });
        foreach (var path in new[] { "Assets/Editor/MechaVideoSetup.cs" }) AssetDatabase.SetLabels(AssetDatabase.LoadMainAssetAtPath(path), new[] { "Others" });
        var settings = MechaPresentationSetup.Profile<VideoConfigurationSettings>("VideoConfiguration");
        AssetDatabase.SetLabels(settings, new[] { "Authored Data" }); AssetDatabase.SaveAssets();
        return AssetDatabase.GetAssetPath(settings);
    }
}
