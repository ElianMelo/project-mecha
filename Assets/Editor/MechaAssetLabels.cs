using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

// Native labels are the classification record. No generated inventory or import callback.
public static class MechaAssetLabels
{
    static readonly string[] Categories={"Placeholder","Others","Code","Prefabs","Scenes","Authored Data"};
    [MenuItem("Tools/Mecha/Content/Refresh Project Labels")]
    public static void RefreshProject()=>Debug.Log(ApplyPrefix("Assets/_Project"));
    public static string ApplyPrefix(string prefix)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Apply labels in Edit Mode");
        var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p==prefix||p.StartsWith(prefix+"/",StringComparison.Ordinal)).ToArray();
        return Apply(paths,0,paths.Length);
    }
    public static string Apply(string[] paths,int offset,int count)
    {
        int changed=0,processed=0;AssetDatabase.StartAssetEditing();
        try
        {
            foreach(string path in paths.Skip(offset).Take(count))
            {
                if(!path.StartsWith("Assets/"))continue;
                var asset=AssetDatabase.LoadMainAssetAtPath(path);if(asset==null)continue;
                var before=AssetDatabase.GetLabels(asset);string category=before.FirstOrDefault(Categories.Contains)??Category(path,asset,before);
                var decisions=before.Where(x=>x=="Keep"||x=="Replace").ToArray();if(decisions.Length==0)decisions=new[]{category=="Placeholder"?"Replace":"Keep"};
                var after=before.Where(x=>!Categories.Contains(x)&&x!="Keep"&&x!="Replace").Concat(new[]{category}).Concat(decisions).Distinct().OrderBy(x=>x,StringComparer.Ordinal).ToArray();
                if(!before.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(after)){AssetDatabase.SetLabels(asset,after);changed++;}processed++;
            }
        }
        finally{AssetDatabase.StopAssetEditing();}
        return "Native labels: "+processed+" assets, "+changed+" updated; explicit decisions and provenance preserved";
    }
    static string Category(string path,UnityEngine.Object asset,string[] labels)
    {
        if(AssetDatabase.IsValidFolder(path)||labels.Contains("Reference"))return "Others";
        string ext=Path.GetExtension(path).ToLowerInvariant();
        if(ext==".unity")return "Scenes";if(ext==".prefab")return "Prefabs";
        if(ext==".cs")return path.StartsWith("Assets/_Project/")&&!path.Contains("/Editor/")?"Code":"Others";
        if(asset is Texture||asset is Sprite||asset is Material||asset is Shader||asset is Mesh||asset is AnimationClip||asset is Font||asset is TMPro.TMP_FontAsset||ext==".fbx"||ext==".obj"||ext==".shadergraph"||ext==".shadersubgraph")return "Placeholder";
        if(asset is ScriptableObject||asset is RuntimeAnimatorController||asset is Avatar||ext==".mixer")return "Authored Data";
        return "Others";
    }
}
