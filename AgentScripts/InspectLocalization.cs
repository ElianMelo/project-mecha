using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

// Read-only inspection receipts, not a test suite.
public static class InspectLocalization
{
    public static object Audit()
    {
        var gaps = new List<string>(); var bindings = new List<string>();
        int entries = 0, labels = 0, localized = 0, fonts = 0;
        var collections = LocalizationEditorSettings.GetStringTableCollections().ToArray();
        foreach (var collection in collections)
        {
            foreach (var table in collection.StringTables)
                foreach (var key in collection.SharedData.Entries)
                {
                    entries++;
                    var entry = table.GetEntry(key.Id);
                    if (string.IsNullOrWhiteSpace(entry?.LocalizedValue)) { gaps.Add(collection.TableCollectionName + "/" + key.Key); continue; }
                    var tokens = Regex.Matches(entry.LocalizedValue, @"\{(\d+)(?:[:,][^}]*)?\}").Cast<Match>().ToArray();
                    if (tokens.Length > 0)
                    {
                        int count = tokens.Max(m => int.Parse(m.Groups[1].Value)) + 1;
                        try { entry.GetLocalizedString(Enumerable.Repeat((object)1, count).ToArray()); }
                        catch (Exception error) { gaps.Add(collection.TableCollectionName + "/" + key.Key + ": " + error.Message); }
                    }
                }
        }
        foreach (string file in Directory.GetFiles("Assets/_Project/Scripts", "*.cs", SearchOption.AllDirectories))
            foreach (Match match in Regex.Matches(File.ReadAllText(file), "GameText.Get\\(\\s*\"([^\"]+)\"\\s*,\\s*\"([^\"]+)\""))
            {
                if (match.Groups[2].Value.EndsWith(".", StringComparison.Ordinal)) continue; // Runtime key prefix, inspected through its concrete catalog entries.
                var collection = collections.FirstOrDefault(c => c.TableCollectionName == match.Groups[1].Value);
                if (collection == null || collection.SharedData.GetEntry(match.Groups[2].Value) == null) gaps.Add(file + " => " + match.Value);
            }
        Action<GameObject> inspect = root =>
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                labels++;
                var font = text.GetComponent<LocalizeTmpFontEvent>();
                if (font == null || font.AssetReference.IsEmpty || font.OnUpdateAsset.GetPersistentEventCount() == 0) bindings.Add(text.name + ": font binding missing");
                else fonts++;
                var binding = text.GetComponent<LocalizeStringEvent>();
                if (binding == null) continue;
                localized++;
                if (binding.StringReference.IsEmpty || binding.OnUpdateString.GetPersistentEventCount() != 1 || binding.OnUpdateString.GetPersistentTarget(0) != text || binding.OnUpdateString.GetPersistentMethodName(0) != "set_text" || binding.OnUpdateString.GetPersistentListenerState(0) != UnityEngine.Events.UnityEventCallState.EditorAndRuntime) bindings.Add(text.name + ": string event invalid");
            }
        };
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string path in AssetDatabase.FindAssets("t:Scene", new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))
                foreach (var root in EditorSceneManager.OpenScene(path, OpenSceneMode.Single).GetRootGameObjects()) inspect(root);
            foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[]{"Assets/_Project"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try { inspect(root); } finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        var result = new { locales = LocalizationEditorSettings.GetLocales().Select(l => l.Identifier.Code).ToArray(), entries, labels, localized, fonts, gaps, bindings };
        File.WriteAllLines("Logs/LocalizationAudit.txt", new[]{"Entries: " + entries, "Labels: " + labels, "String events: " + localized, "Font events: " + fonts, "Gaps: " + gaps.Count, "Invalid bindings: " + bindings.Count}.Concat(gaps).Concat(bindings));
        return result;
    }
    public static string BuildContent()
    {
        AddressableAssetSettings.BuildPlayerContent(out var result);
        string message = string.IsNullOrEmpty(result.Error) ? "Addressables content build succeeded" : result.Error;
        File.WriteAllText("Logs/LocalizationContentBuild.txt", message);
        return message;
    }
    public static object Runtime()
    {
        var texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return new {
            frame=Time.frameCount,
            initialized=LocalizationSettings.InitializationOperation.IsDone,
            locale=LocalizationSettings.SelectedLocale.Identifier.Code,
            active=texts.Where(t=>t.gameObject.activeInHierarchy).Select(t=>t.name + " = " + t.text).ToArray(),
            missingFonts=texts.Where(t=>t.font==null).Select(t=>t.name).ToArray(),
            untranslated=texts.Where(t=>t.text.Contains("No translation found")).Select(t=>t.name + ": " + t.text).ToArray(),
            selected=UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject?.name
        };
    }
}
