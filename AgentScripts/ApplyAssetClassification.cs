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
    private static readonly string[] ManagedLabels = { "AI Generated", "Placeholder", "Others" };

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
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.EndsWith(".meta", StringComparison.Ordinal)) continue;
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
