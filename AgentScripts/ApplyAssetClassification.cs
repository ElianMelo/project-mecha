using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Run through official Pipeline run_script after updating the file inventory.
// Unity's built-in labels remain editable in the Project window.
public static class ApplyAssetClassification
{
    // Localization also bundles Newtonsoft in its editor assembly. Select the public
    // package assembly explicitly to keep this external inspection tool unambiguous.
    private static string SerializeJson(object value)
    {
        var json = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
        return (string)json.GetMethod("SerializeObject", new[]{typeof(object)}).Invoke(null, new[]{value});
    }
    private static readonly string[] ManagedLabels = { "AI Generated", "Placeholder", "Others", "Code", "Prefabs", "Scenes", "Authored Data", "Reference", "VFXSource" };

    public static object Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before labeling assets.");
        var lines = File.ReadAllLines("Docs/ProjectFileClassification.csv");
        int labeled = 0, changed = 0;
        // SetLabels can reimport code and models. Batch the edits so the Editor
        // refreshes once instead of compiling/importing between individual files.
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var line in lines.Skip(1))
            {
                var fields = ReadCsvRow(line);
                string path = fields[0];
                if (!path.StartsWith("Assets/", StringComparison.Ordinal)) continue;
                if (path.EndsWith(".meta", StringComparison.Ordinal))
                {
                    path = path.Substring(0, path.Length - 5);
                    if (!AssetDatabase.IsValidFolder(path) ||
                        !fields[5].Split(';').Any(label => label == "Reference" || label == "VFXSource")) continue;
                }
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null) continue;
                var before = AssetDatabase.GetLabels(asset);
                var after = before.Where(label => !ManagedLabels.Contains(label))
                    .Concat(fields[5].Split(';')).Distinct().OrderBy(label => label, StringComparer.Ordinal).ToArray();
                if (!before.OrderBy(label => label, StringComparer.Ordinal).SequenceEqual(after))
                {
                    AssetDatabase.SetLabels(asset, after);
                    changed++;
                }
                labeled++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        // SetLabels persists importer metadata. Avoid SaveAssets, which could
        // also flush unrelated dirty scenes, profiles or graph upgrades.
        return new { labeledAssets = labeled, changedAssets = changed };
    }

    // Bounded batches keep large imported libraries below Pipeline's response timeout.
    // Folder metadata is represented by the folder asset; ordinary .meta files are not assets.
    public static object ApplyVfxLibraries(int offset, int count)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before labeling assets.");
        if (offset < 0 || count < 1) throw new ArgumentOutOfRangeException("Expected a nonnegative offset and positive count.");
        var entries = VfxEntries();
        int changed = 0, labeled = 0;
        var skipped = new List<string>();
        var batch = entries.Skip(offset).Take(count).ToArray();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var entry in batch)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(entry.Key);
                if (asset == null) { skipped.Add(entry.Key); continue; }
                var before = AssetDatabase.GetLabels(asset);
                var after = before.Where(label => !ManagedLabels.Contains(label)).Concat(entry.Value)
                    .Distinct().OrderBy(label => label, StringComparer.Ordinal).ToArray();
                if (!before.OrderBy(label => label, StringComparer.Ordinal).SequenceEqual(after))
                { AssetDatabase.SetLabels(asset, after); changed++; }
                labeled++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        return SerializeJson(new { total = entries.Count, offset, processed = batch.Length, labeled, changed,
            nextOffset = offset + batch.Length, complete = offset + batch.Length >= entries.Count, skipped });
    }

    public static object InspectVfxLibraries()
    {
        var entries = VfxEntries();
        int reference = 0, source = 0;
        var mismatches = new List<string>();
        foreach (var entry in entries)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(entry.Key);
            if (asset == null) { mismatches.Add("Not imported: " + entry.Key); continue; }
            var labels = AssetDatabase.GetLabels(asset);
            if (labels.Contains("Reference")) reference++;
            if (labels.Contains("VFXSource")) source++;
            if (entry.Value.Except(labels).Any() || labels.Contains("Reference") && labels.Contains("VFXSource"))
                mismatches.Add(entry.Key);
        }
        return SerializeJson(new { assets = entries.Count, reference, source, mismatches,
            playMode = EditorApplication.isPlaying, compiling = EditorApplication.isCompiling });
    }

    public static object ApplyPathPrefix(string prefix)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Label in stopped Edit Mode.");
        if (!prefix.StartsWith("Assets/_Project/", StringComparison.Ordinal)) throw new ArgumentException("Expected a project-owned asset prefix.");
        int changed = 0, labeled = 0;
        foreach (var line in File.ReadAllLines("Docs/ProjectFileClassification.csv").Skip(1))
        {
            var fields = ReadCsvRow(line);
            string path = fields[0];
            if (!path.StartsWith(prefix, StringComparison.Ordinal)) continue;
            if (path.EndsWith(".meta", StringComparison.Ordinal))
            {
                path = path.Substring(0,path.Length-5);
                if (!AssetDatabase.IsValidFolder(path)) continue;
            }
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null) continue;
            var before = AssetDatabase.GetLabels(asset);
            var after = before.Where(label=>!ManagedLabels.Contains(label)).Concat(fields[5].Split(';'))
                .Distinct().OrderBy(label=>label,StringComparer.Ordinal).ToArray();
            if (!before.OrderBy(label=>label,StringComparer.Ordinal).SequenceEqual(after)) {AssetDatabase.SetLabels(asset,after);changed++;}
            labeled++;
        }
        return SerializeJson(new{prefix,labeled,changed});
    }

    public static object ApplyFireSkills()
    {
        AssetDatabase.StartAssetEditing();
        try
        {
            return SerializeJson(new[] {
                ApplyPathPrefix("Assets/_Project/VFX/FireSkills"),
                ApplyPathPrefix("Assets/_Project/VFX/SceneSupport"),
                ApplyPathPrefix("Assets/_Project/Scripts/Combat/CombatEmissionOrigin"),
                ApplyPathPrefix("Assets/_Project/Scripts/Combat/SkillContactWindow") });
        }
        finally { AssetDatabase.StopAssetEditing(); }
    }

    private static SortedDictionary<string, string[]> VfxEntries()
    {
        var entries = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines("Docs/ProjectFileClassification.csv").Skip(1))
        {
            var fields = ReadCsvRow(line);
            var labels = fields[5].Split(';');
            if (!labels.Contains("Reference") && !labels.Contains("VFXSource")) continue;
            string path = fields[0];
            if (path.EndsWith(".meta", StringComparison.Ordinal))
            {
                path = path.Substring(0, path.Length - 5);
                if (!AssetDatabase.IsValidFolder(path)) continue;
            }
            if (path.StartsWith("Assets/", StringComparison.Ordinal)) entries[path] = labels;
        }
        return entries;
    }

    private static string[] ReadCsvRow(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted) { fields.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        fields.Add(current.ToString());
        if (fields.Count != 7) throw new InvalidDataException("Expected seven inventory columns.");
        return fields.ToArray();
    }
}
