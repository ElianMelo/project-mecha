using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

// Unity tables remain authoritative: capture only new text; never overwrite existing entries.
[InitializeOnLoad]
public static class MechaLocalizationAuthoring
{
    public const string Root = "Assets/_Project/Localization";
    private static bool busy;
    private static double due;
    private static readonly Dictionary<string, StringTableCollection> collections = new Dictionary<string, StringTableCollection>();
    static MechaLocalizationAuthoring()
    {
        EditorApplication.hierarchyChanged += () => due = EditorApplication.timeSinceStartup + 0.8;
        ObjectChangeEvents.changesPublished += (ref ObjectChangeEventStream changes) => due = EditorApplication.timeSinceStartup + 0.8;
        EditorApplication.update += Update;
        EditorSceneManager.sceneSaving += (scene, path) => { if (!busy && path.StartsWith("Assets/Scenes/")) LocalizeScene(scene); };
        EditorSceneManager.sceneSaved += scene =>
        {
            if (scene.path.StartsWith("Assets/Scenes/")) EditorApplication.delayCall += AssetDatabase.SaveAssets;
        };
    }
    private static void Update()
    {
        if (busy || due == 0 || EditorApplication.timeSinceStartup < due || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || LocalizationEditorSettings.ActiveLocalizationSettings == null) return;
        due = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.path.StartsWith("Assets/Scenes/")) LocalizeScene(scene);
        }
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.assetPath.StartsWith("Assets/_Project/")) LocalizeRoot(stage.prefabContentsRoot);
    }
    public static LocalizedString Capture(string category, string key, string english)
    {
        if (string.IsNullOrWhiteSpace(english)) return new LocalizedString();
        if (!collections.TryGetValue(category, out var collection) || collection == null)
        {
            collection = LocalizationEditorSettings.GetStringTableCollection(category) ?? LocalizationEditorSettings.CreateStringTableCollection(category, Root + "/Strings/" + category);
            collections[category] = collection;
        }
        var table = collection.StringTables.Single(t => t.LocaleIdentifier.Code == "en");
        if (table.GetEntry(key) == null)
        {
            table.AddEntry(key, english);
            EditorUtility.SetDirty(table); EditorUtility.SetDirty(collection.SharedData); EditorUtility.SetDirty(collection);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
        }
        return new LocalizedString(collection.TableCollectionName, table.GetEntry(key).KeyId);
    }
    public static string Category(string path)
    {
        string value = path.ToLowerInvariant();
        if (value.Contains("combo") || value.Contains("attack") || value.Contains("skill")) return "Combat";
        if (value.Contains("lobby")) return "Lobby";
        if (value.Contains("routing") || value.Contains("route")) return "Routing";
        if (value.Contains("chat")) return "Chat";
        if (value.Contains("keyboard") || value.Contains("controller")) return "Input";
        if (value.Contains("diagnostic")) return "Diagnostics";
        if (value.Contains("audio") || value.Contains("video") || value.Contains("options")) return "Settings";
        return "HUD";
    }
    // Automatic static-label capture reuses existing wording within its category.
    // Explicit Capture calls retain their requested keys for runtime lookup contracts.
    private static LocalizedString CaptureStatic(string category, string hint, string english)
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(category);
        var table = collection?.StringTables.Single(t => t.LocaleIdentifier.Code == "en");
        var match = table?.Values.FirstOrDefault(e => e.LocalizedValue == english);
        return match != null ? new LocalizedString(category, match.KeyId) : Capture(category, NewKey(category, hint), english);
    }
    // Keep new keys readable: at most three meaningful words, with a short number
    // on collisions. Full hierarchies, sentences and format tokens never become keys.
    public static string NewKey(string category, string hint)
    {
        string expanded = Regex.Replace(hint, "(?<=[a-z0-9])(?=[A-Z])", " ");
        var wrappers = new HashSet<string> { "label", "text", "tmp", "ui", "canvas", "panel", "viewport", "card", "interface", "settings", "home", "mecha", "gameplay", "configuration", "configurator", "presentation", "display" };
        var words = Regex.Matches(expanded.ToLowerInvariant(), "[a-z0-9]+")
            .Cast<Match>().Select(match => match.Value).Where(word => !wrappers.Contains(word)).ToArray();
        string stem = string.Join(".", words.Reverse().Take(3).Reverse());
        if (string.IsNullOrEmpty(stem)) stem = "entry";
        var shared = LocalizationEditorSettings.GetStringTableCollection(category)?.SharedData;
        string key = stem; int suffix = 2;
        while (shared != null && shared.GetEntry(key) != null) key = stem + suffix++;
        return key;
    }
    public static int LocalizeScene(Scene scene)
    {
        if (!scene.isLoaded) return 0;
        int count = scene.GetRootGameObjects().Sum(LocalizeRoot);
        if (count > 0) EditorSceneManager.MarkSceneDirty(scene);
        return count;
    }
    public static int LocalizeRoot(GameObject root)
    {
        int count = 0;
        var dynamic = new HashSet<UnityEngine.Object>();
        // Presentation scripts reference their dynamic labels; native input/dropdown own their text.
        foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || behaviour is TMP_Text || behaviour is LocalizeStringEvent || behaviour is LocalizeTmpFontEvent) continue;
            if (behaviour is TMP_InputField input) dynamic.Add(input.textComponent);
            if (behaviour is TMP_Dropdown dropdown) { dynamic.Add(dropdown.captionText); dynamic.Add(dropdown.itemText); }
            if (!behaviour.GetType().Assembly.GetName().Name.StartsWith("Assembly-CSharp")) continue;
            var properties = new SerializedObject(behaviour).GetIterator();
            while (properties.Next(true))
                if (properties.propertyType == SerializedPropertyType.ObjectReference && properties.objectReferenceValue is TMP_Text text) dynamic.Add(text);
        }
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            string path = text.name;
            for (var parent = text.transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
            var font = text.GetComponent<LocalizeTmpFontEvent>();
            if (font == null)
            {
                font = text.gameObject.AddComponent<LocalizeTmpFontEvent>();
                font.AssetReference = new LocalizedTmpFont { TableReference = "Fonts", TableEntryReference = "Default" };
                var setFont = (UnityAction<TMP_FontAsset>)Delegate.CreateDelegate(typeof(UnityAction<TMP_FontAsset>), text, "set_font");
                UnityEventTools.AddPersistentListener(font.OnUpdateAsset, setFont);
                font.OnUpdateAsset.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
                EditorUtility.SetDirty(font); count++;
            }
            if (dynamic.Contains(text) || string.IsNullOrWhiteSpace(text.text) || text.GetComponent<LocalizeStringEvent>() != null) continue;
            var binding = text.gameObject.AddComponent<LocalizeStringEvent>();
            string category = Category(path);
            binding.StringReference = CaptureStatic(category, path, text.text);
            var setText = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), text, "set_text");
            UnityEventTools.AddPersistentListener(binding.OnUpdateString, setText);
            binding.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            EditorUtility.SetDirty(binding); count++;
        }
        return count;
    }

    [MenuItem("Tools/Mecha/Localization/Localize Game Content")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring localization.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save open scene changes before the localization migration.");
        busy = true;
        try
        {
            foreach (string path in AssetDatabase.FindAssets("t:Scene", new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                LocalizeScene(scene); EditorSceneManager.SaveScene(scene);
            }
            foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[]{"Assets/_Project"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try { if (LocalizeRoot(prefab) > 0) PrefabUtility.SaveAsPrefabAsset(prefab, path); }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            CaptureData();
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections()) collection.SetPreloadTableFlag(true);
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); busy = false; }
    }
    public static void CaptureData()
    {
        foreach (string path in AssetDatabase.FindAssets("t:ScriptableObject", new[]{"Assets/_Project"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            string[] names = asset is RouteEncounterDefinition ? new[]{"displayName","description","rewardPreview"} : asset is AttackPiece || asset is ComboDefinition ? new[]{"displayName"} : asset is GameplayHudLayoutSettings ? new[]{"tutorialText"} : asset is ControllerInterfaceSettings ? new[]{"controllerHelp"} : asset is SkillPresentationSettings ? new[]{"skillPrefix"} : asset is LobbySettings ? new[]{"defaultLobbyName","defaultPlayerName"} : Array.Empty<string>();
            var data = new SerializedObject(asset);
            foreach (string name in names)
            {
                var source = data.FindProperty(name); var target = data.FindProperty(name + "Localized");
                if (source == null || target == null) continue;
                var value = asset.GetType().GetField(name + "Localized").GetValue(asset) as LocalizedString;
                if (value != null && !value.IsEmpty) continue;
                string english = string.IsNullOrWhiteSpace(source.stringValue) && name == "displayName" ? asset.name : source.stringValue;
                string category = Category(path);
                asset.GetType().GetField(name + "Localized").SetValue(asset, Capture(category, NewKey(category, asset.name + " " + name), english));
                EditorUtility.SetDirty(asset);
            }
        }
    }
}

public sealed class MechaLocalizationImports : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] from)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || LocalizationEditorSettings.ActiveLocalizationSettings == null) return;
        string[] prefabs = imported.Where(p => p.StartsWith("Assets/_Project/") && p.EndsWith(".prefab")).ToArray();
        bool authoredData = imported.Any(p => p.StartsWith("Assets/_Project/") && !p.StartsWith(MechaLocalizationAuthoring.Root + "/") && p.EndsWith(".asset"));
        if (prefabs.Length == 0 && !authoredData) return;
        EditorApplication.delayCall += () =>
        {
            foreach (string path in prefabs)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try { if (MechaLocalizationAuthoring.LocalizeRoot(root) > 0) PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            if (authoredData) MechaLocalizationAuthoring.CaptureData();
            AssetDatabase.SaveAssets();
        };
    }
}
