using System;
using UnityEngine;
using UnityEditor;

public static class AuthorBladeContactBindings
{
    public static string Weapon(string color)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode");
        string settingsPath="Assets/_Project/Settings/Attacks/MeleeBlade.asset";
        var settings=AssetDatabase.LoadAssetAtPath<MeleeBladeSettings>(settingsPath);
        if(settings==null){settings=ScriptableObject.CreateInstance<MeleeBladeSettings>();AssetDatabase.CreateAsset(settings,settingsPath);}
        string path="Assets/_Project/Prefabs/Weapons/Sword7"+color+".prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var edge=root.transform.Find("Blade Edge Base");
            if(edge==null){edge=new GameObject("Blade Edge Base").transform;edge.SetParent(root.transform,false);}
            edge.localPosition=new Vector3(0,.3f,0);
            var blade=root.GetComponent<MeleeBlade>();if(blade==null)blade=root.AddComponent<MeleeBlade>();
            blade.edgeBase=edge;blade.edgeTip=root.transform.Find("Blade Tip");blade.settings=settings;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        return path;
    }
    public static string BindRigs()
    {
        string path="Assets/_Project/Prefabs/MechaPlayer.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);string result="";
        try
        {
            foreach(var rig in root.GetComponentsInChildren<CharacterRigBinding>(true))
            {rig.blade=rig.GetComponentInChildren<MeleeBlade>(true);result+=rig.name+" blade="+(rig.blade!=null)+"\n";}
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        string[] paths={"Assets/_Project/Scripts/Combat/DamageContact.cs","Assets/_Project/Scripts/Combat/MeleeBlade.cs","Assets/_Project/Scripts/Combat/MeleeBladeSettings.cs","Assets/_Project/Scripts/Combat/MeleeBladeSweep.cs","Assets/_Project/Settings/Attacks/MeleeBlade.asset"};
        return result+MechaAssetLabels.Apply(paths,0,paths.Length);
    }
}
