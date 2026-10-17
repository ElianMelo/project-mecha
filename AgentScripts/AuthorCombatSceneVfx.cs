using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Explicit scene support requested for combat VFX; native URP assets/components only.
public static class AuthorCombatSceneVfx
{
    private const string Root="Assets/_Project/VFX/SceneSupport";
    public static object RepairReceiver()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Repair in Edit Mode.");
        var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/IndustrialThermalReceiver.mat");
        material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
        material.EnableKeyword("_EMISSION");EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
        return Inspect();
    }
    public static object RepairProfile()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Repair in Edit Mode.");
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Profiles/CombatPostProcessing.asset");
        profile.components.RemoveAll(c=>c==null);
        var color=Get<ColorAdjustments>(profile);color.active=true;color.contrast.Override(5);color.saturation.Override(3);color.postExposure.Override(0);
        foreach(var component in profile.components)
        {if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,profile);EditorUtility.SetDirty(component);}
        EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(profile),ImportAssetOptions.ForceUpdate);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path=="Assets/Scenes/MainScene.unity")EditorSceneManager.SaveScene(scene);
        return Inspect();
    }
    public static object Inspect()
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/IndustrialThermalReceiver.mat");
        var camera=Camera.main;var data=camera.GetComponent<UniversalAdditionalCameraData>();
        var volume=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Single(v=>v.name=="Global Volume");
        volume.sharedProfile.TryGet<Bloom>(out var bloom);
        return new {receiver=new{material.shaderKeywords,flags=material.globalIlluminationFlags.ToString(),emission=material.GetColor("_EmissionColor").ToString()},profile=AssetDatabase.GetAssetPath(volume.sharedProfile),camera.allowHDR,data.renderPostProcessing,
            opaque=data.requiresColorTexture,depth=data.requiresDepthTexture,maskIncludesVolume=(data.volumeLayerMask.value&(1<<volume.gameObject.layer))!=0,
            bloom=new{bloom.active,intensity=bloom.intensity.value,threshold=bloom.threshold.value,overrides=bloom.intensity.overrideState&&bloom.threshold.overrideState},
            shaderErrors=new[]{"Mecha/VFX/CellThermal","Mecha/VFX/TerrainHeatDistortion"}.Select(n=>new{name=n,errors=ShaderUtil.GetShaderMessages(Shader.Find(n)).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray()}).ToArray(),
            sceneDirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty};
    }
    public static object Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/MainScene.unity" || scene.isDirty)
            throw new InvalidOperationException("Expected clean MainScene; preserve unrelated scene edits.");
        Folder(Root+"/Profiles");Folder(Root+"/Materials");
        var volume=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Single(v=>v.name=="Global Volume");
        const string profilePath=Root+"/Profiles/CombatPostProcessing.asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if(profile==null)
        {
            if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(volume.sharedProfile),profilePath))throw new InvalidOperationException("Could not copy current look.");
            profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        }
        var bloom=Get<Bloom>(profile);bloom.active=true;bloom.intensity.Override(0.45f);bloom.threshold.Override(0.95f);bloom.scatter.Override(0.60f);bloom.clamp.Override(12f);bloom.highQualityFiltering.Override(true);
        var tone=Get<Tonemapping>(profile);tone.active=true;tone.mode.Override(TonemappingMode.Neutral);
        var color=Get<ColorAdjustments>(profile);color.active=true;color.contrast.Override(5f);color.saturation.Override(3f);color.postExposure.Override(0f);
        if(profile.TryGet<MotionBlur>(out var blur))blur.active=false;
        foreach(var component in profile.components)EditorUtility.SetDirty(component);
        EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
        Undo.RecordObject(volume,"Combat VFX post processing");volume.sharedProfile=profile;volume.isGlobal=true;volume.enabled=true;volume.weight=1;
        var camera=Camera.main;var data=camera.GetComponent<UniversalAdditionalCameraData>();
        Undo.RecordObjects(new UnityEngine.Object[]{camera,data},"Enable combat HDR and heat sampling");
        camera.allowHDR=true;data.renderPostProcessing=true;data.volumeLayerMask|=1<<volume.gameObject.layer;
        data.requiresColorOption=CameraOverrideOption.On;data.requiresDepthOption=CameraOverrideOption.On;
        var rp=UniversalRenderPipeline.asset;
        if(!rp.supportsHDR || !rp.supportsCameraOpaqueTexture)throw new InvalidOperationException("Expected existing HDR/opaque support.");
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Environment/IndustrialPlatforms.png");
        const string materialPath=Root+"/Materials/IndustrialThermalReceiver.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));AssetDatabase.CreateAsset(material,materialPath);}
        material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",new Color(0.28f,0.28f,0.28f,1));
        material.SetTexture("_EmissionMap",texture);material.SetColor("_EmissionColor",new Color(0.65f,0.65f,0.65f,1));material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;material.EnableKeyword("_EMISSION");
        material.SetFloat("_Surface",0);material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",0.1f);material.SetFloat("_Cull",0);material.EnableKeyword("_ALPHATEST_ON");
        material.SetFloat("_Smoothness",0);if(material.HasProperty("_SpecColor"))material.SetColor("_SpecColor",Color.black);
        material.SetFloat("_ReceiveShadows",0);material.EnableKeyword("_RECEIVE_SHADOWS_OFF");material.renderQueue=2450;
        EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
        const string platformsPath="Assets/_Project/Prefabs/Assemblies/ArenaPlatforms.prefab";
        var platforms=PrefabUtility.LoadPrefabContents(platformsPath);
        int authored=0;
        try
        {
            foreach(var sprite in platforms.GetComponentsInChildren<SpriteRenderer>(true))
                if(IsTerrain(sprite,texture)){sprite.sharedMaterial=material;sprite.shadowCastingMode=ShadowCastingMode.Off;authored++;}
            PrefabUtility.SaveAsPrefabAsset(platforms,platformsPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(platforms);}
        int sceneReceivers=0;
        foreach(var sprite in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            if(IsTerrain(sprite,texture))
            {Undo.RecordObject(sprite,"Enable native thermal light reception");sprite.sharedMaterial=material;sprite.shadowCastingMode=ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(sprite);sceneReceivers++;}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return new {profile=profilePath,bloom=bloom.intensity.value,threshold=bloom.threshold.value,sceneReceivers,authored,material=materialPath};
    }
    private static bool IsTerrain(SpriteRenderer sprite,Texture2D texture)=>sprite.sprite!=null && sprite.sprite.texture==texture && sprite.GetComponentInParent<Collider>()!=null;
    private static T Get<T>(VolumeProfile profile) where T:VolumeComponent
    {
        if(profile.TryGet<T>(out var component))return component;
        component=profile.Add<T>(true);
        AssetDatabase.AddObjectToAsset(component,profile);
        return component;
    }
    private static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;string parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));}
}

