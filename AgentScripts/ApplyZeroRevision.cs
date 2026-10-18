using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;

public static class ApplyZeroRevision
{
    public static string RetuneTraversal()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode.");
        var zero=AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/_Project/Settings/Characters/Zero.asset");
        var motion=zero.movement;
        motion.walkSpeed=motion.runSpeed=7.225f;
        motion.dashDuration=.2f;
        zero.animation.runReferenceSpeed=motion.runSpeed;
        EditorUtility.SetDirty(motion);AssetDatabase.SaveAssetIfDirty(motion);
        EditorUtility.SetDirty(zero.animation);AssetDatabase.SaveAssetIfDirty(zero.animation);
        foreach(var item in new[]{("Input","zero.traversal"),("Combat","zero.overview")})
        {
            var table=UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollection(item.Item1).StringTables.Single(t=>t.LocaleIdentifier.Code=="en");
            var entry=((UnityEngine.Localization.Tables.StringTable)table).GetEntry(item.Item2);
            entry.Value=entry.Value.Replace("long directional dash","directional dash").Replace("long eight-direction dash","eight-direction dash");
            EditorUtility.SetDirty(table);AssetDatabase.SaveAssetIfDirty(table);
        }
        return "Zero speed="+motion.runSpeed+"; dash speed="+motion.dashSpeed+"; duration="+motion.dashDuration+"; nominal dash distance="+(motion.dashSpeed*motion.dashDuration)+"; Animator run reference="+zero.animation.runReferenceSpeed;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in stopped Edit Mode.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/MainScene.unity"||scene.isDirty)throw new InvalidOperationException("MainScene must be open and clean; preserve unsaved scene changes first.");
        var settings=AssetDatabase.LoadAssetAtPath<WingZeroRigSettings>("Assets/_Project/Settings/Characters/ZeroRig.asset");
        var zero=AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/_Project/Settings/Characters/Zero.asset");zero.visualScale=settings.visualScale;EditorUtility.SetDirty(zero);
        zero.animation.runReferenceSpeed=zero.movement.runSpeed;EditorUtility.SetDirty(zero.animation);
        const string player="Assets/_Project/Prefabs/MechaPlayer.prefab";var root=PrefabUtility.LoadPrefabContents(player);
        try{root.transform.Find("Zero Visual").localScale=Vector3.one*settings.visualScale;PrefabUtility.SaveAsPrefabAsset(root,player);}finally{PrefabUtility.UnloadPrefabContents(root);}
        MechaRenderSetup.ConfigureWorldEffects(Camera.main);
        int renderers=0, prefabs=0;string skipped="";
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            bool Relevant(Renderer r)=>r is ParticleSystemRenderer||r is TrailRenderer||r.GetType().Name=="VFXRenderer"||r.sharedMaterials.Any(m=>m!=null&&m.shader.name.Contains("/Particles/"));
            if(!asset.GetComponentsInChildren<Renderer>(true).Any(Relevant))continue;
            var contents=PrefabUtility.LoadPrefabContents(path);bool changed=false;
            try
            {
                foreach(var r in contents.GetComponentsInChildren<Renderer>(true).Where(Relevant))
                {
                    if(r.GetComponent<Collider>()!=null){skipped+=path+":"+r.name+" ";continue;}
                    if(r.gameObject.layer!=CombatVfxRendering.Layer){r.gameObject.layer=CombatVfxRendering.Layer;renderers++;changed=true;}
                }
                if(changed){PrefabUtility.SaveAsPrefabAsset(contents,path);prefabs++;}
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Zero model scale="+zero.visualScale+" wing scale="+settings.wingScale+"; foreground renderers="+renderers+" prefabs="+prefabs+"; collider roots skipped="+skipped;
    }
    public static string OtherScenes()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode.");
        var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string report="";
        foreach(var path in new[]{"Assets/Scenes/ComboConfigurator.unity","Assets/Scenes/LevelRouting.unity"})
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool loaded=scene.IsValid()&&scene.isLoaded;
            if(loaded&&scene.isDirty)throw new InvalidOperationException("Preserve unsaved scene changes first: "+path);
            if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var cameras=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true))
                    .Where(c=>c.GetUniversalAdditionalCameraData().renderType==CameraRenderType.Base).ToArray();
                foreach(var camera in cameras)MechaRenderSetup.ConfigureWorldEffects(camera);
                if(cameras.Length>0){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                report+=path+": "+cameras.Length+" base cameras configured. ";
            }
            finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
        }
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);
        return report;
    }
}

