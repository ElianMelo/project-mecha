using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public static class ConfigureWorldVfx
{
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scene edits first.");
        const string path="Assets/_Project/VFX/Rendering/Profiles/SurfaceDepth.asset";
        MechaPresentationSetup.Folder("Assets/_Project/VFX/Rendering/Profiles");
        var profile=AssetDatabase.LoadAssetAtPath<VfxSurfaceDepthSettings>(path);
        if(profile==null){profile=ScriptableObject.CreateInstance<VfxSurfaceDepthSettings>();AssetDatabase.CreateAsset(profile,path);}
        int prefabs=0;
        foreach(var assetPath in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project/VFX/ZeroWater/Prefabs","Assets/_Project/VFX/FireSkills/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var contents=PrefabUtility.LoadPrefabContents(assetPath);
            try{MechaRenderSetup.ConfigureSurfaceDepth(contents);PrefabUtility.SaveAsPrefabAsset(contents,assetPath);prefabs++;}
            finally{PrefabUtility.UnloadPrefabContents(contents);}
        }
        var original=SceneManager.GetActiveScene();int cameras=0;
        foreach(var scenePath in new[]{"Assets/Scenes/MainScene.unity","Assets/Scenes/ComboConfigurator.unity","Assets/Scenes/LevelRouting.unity"})
        {
            var scene=SceneManager.GetSceneByPath(scenePath);bool loaded=scene.IsValid()&&scene.isLoaded;
            if(!loaded)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            try
            {
                foreach(var camera in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Where(c=>c.GetUniversalAdditionalCameraData().renderType==CameraRenderType.Base).ToArray())
                {MechaRenderSetup.ConfigureWorldEffects(camera);cameras++;}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
        }
        SceneManager.SetActiveScene(original);
        return "World cameras="+cameras+"; optional cosmetic placement prefabs="+prefabs+"; clearance="+profile.surfaceClearance+"; maximum shift="+profile.maximumShift;
    }
    public static string Inspect()
    {
        var camera=Camera.main;var data=camera.GetUniversalAdditionalCameraData();string text="Main camera mask="+camera.cullingMask+" stack="+data.cameraStack.Count+"\n";
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project/VFX/FireSkills/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            text+=path+" lights="+prefab.GetComponentsInChildren<Light>(true).Length+" native collision="+string.Join(",",prefab.GetComponentsInChildren<ParticleSystem>(true).Where(p=>p.collision.enabled).Select(p=>p.name))+"\n";
        }
        return text;
    }
}
