using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class MechaToonSetup
{
    public static void Apply()
    {
        var look=MechaPresentationSetup.Profile<ToonLookSettings>("ToonLook");
        foreach(string path in new[]{"Assets/_Project/Models/Enemies/Eva/EvaMaterial.mat","Assets/_Project/Models/Enemy/EvaEnemy.mat","Assets/_Project/Models/Weapon/Material.mat","Assets/_Project/Models/Weapon/Material.001.mat"})
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat!=null)Convert(mat,look);
        }
        var body=Create("MechaArmor",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Models/Player/MechaAlbedo.png"),Color.white,look);
        var blade=Create("MechaBlade",null,new Color(0.68f,0.86f,0.93f),look);
        const string playerPath="Assets/_Project/Prefabs/MechaPlayer.prefab";
        var player=PrefabUtility.LoadPrefabContents(playerPath);
        try
        {
            foreach(var r in player.GetComponentsInChildren<Renderer>(true))
            {
                var materials=r.sharedMaterials;for(int i=0;i<materials.Length;i++)materials[i]=i==0?body:blade;r.sharedMaterials=materials;
                r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
                ConfigureOutline(r,look);
            }
            PrefabUtility.SaveAsPrefabAsset(player,playerPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(player);}
        var enemy=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EvaEnemy.prefab");
        foreach(var renderer in enemy.GetComponentsInChildren<Renderer>(true))ConfigureOutline(renderer,look);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=look.ambient;
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if(light.type!=LightType.Directional)continue;
            light.transform.rotation=Quaternion.Euler(look.keyRotation);light.color=look.keyColor;light.intensity=look.keyIntensity;
            light.shadows=LightShadows.Hard;light.shadowStrength=1f;light.shadowBias=look.shadowBias;light.shadowNormalBias=look.shadowNormalBias;
        }
        var rp=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if(rp!=null)
        {
            var serialized=new SerializedObject(rp);serialized.FindProperty("m_SoftShadowsSupported").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();
            rp.shadowDistance=look.shadowDistance;EditorUtility.SetDirty(rp);
        }
        if(Camera.main!=null){Camera.main.backgroundColor=look.background;Camera.main.clearFlags=CameraClearFlags.SolidColor;}
        // Crisp material bands should survive post processing without bloom or motion blur washing them out.
        foreach(var volume in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            var profile=volume.sharedProfile;if(profile==null)continue;
            // Effect-owned scene profiles retain their authored glow/contrast tuning.
            if(AssetDatabase.GetAssetPath(profile).StartsWith("Assets/_Project/VFX/",System.StringComparison.Ordinal))continue;
            if(profile.TryGet<MotionBlur>(out var blur))blur.active=false;
            if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(0.08f);bloom.threshold.Override(1.2f);}
            if(profile.TryGet<Tonemapping>(out var tone))tone.mode.Override(TonemappingMode.Neutral);
            EditorUtility.SetDirty(profile);
        }
    }
    private static void ConfigureOutline(Renderer renderer,ToonLookSettings look)
    {
        float scale=Mathf.Max(0.00001f,Mathf.Abs(renderer.transform.lossyScale.x));
        foreach(var material in renderer.sharedMaterials)
        {
            if(material==null || !material.HasProperty("_Outline_Width"))continue;
            material.SetFloat("_Outline_Width",look.outlineWidth*1000f/scale);EditorUtility.SetDirty(material);
        }
    }
    private static Material Create(string name,Texture texture,Color tint,ToonLookSettings look)
    {
        string path="Assets/_Project/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Toon/Toon")){name=name};mat.SetTexture("_BaseMap",texture);mat.SetTexture("_MainTex",texture);mat.SetColor("_BaseColor",tint);AssetDatabase.CreateAsset(mat,path);}
        Convert(mat,look);return mat;
    }
    private static void Convert(Material mat,ToonLookSettings look)
    {
        var tex=mat.HasProperty("_BaseMap")?mat.GetTexture("_BaseMap"):mat.mainTexture;
        var color=mat.HasProperty("_BaseColor")?mat.GetColor("_BaseColor"):Color.white;
        mat.shader=Shader.Find("Toon/Toon");mat.shaderKeywords=new string[0];
        mat.SetTexture("_MainTex",tex);mat.SetTexture("_BaseMap",tex);mat.SetColor("_BaseColor",color);mat.SetColor("_Color",color);
        mat.SetFloat("_Use_BaseAs1st",1f);mat.SetFloat("_Use_1stAs2nd",1f);
        mat.SetColor("_1st_ShadeColor",look.firstShade*color);mat.SetColor("_2nd_ShadeColor",look.secondShade*color);
        mat.SetFloat("_BaseColor_Step",look.baseStep);mat.SetFloat("_ShadeColor_Step",look.shadeStep);
        mat.SetFloat("_BaseShade_Feather",look.feather);mat.SetFloat("_1st2nd_Shades_Feather",look.feather);
        mat.SetFloat("_Set_SystemShadowsToBase",1f);mat.SetFloat("_Is_LightColor_1st_Shade",0f);mat.SetFloat("_Is_LightColor_2nd_Shade",0f);
        mat.SetFloat("_GI_Intensity",0f);mat.SetFloat("_Unlit_Intensity",0f);
        mat.SetColor("_HighColor",look.highlight);mat.SetFloat("_HighColor_Power",look.highlightPower);
        mat.SetFloat("_Is_SpecularToHighColor",0f);mat.SetFloat("_Is_BlendAddToHiColor",1f);
        mat.SetFloat("_RimLight",0f);mat.SetFloat("_MatCap",0f);mat.SetFloat("_Outline_Width",look.outlineWidth);
        mat.SetColor("_Outline_Color",look.outline);mat.SetFloat("_Is_LightColor_Outline",0f);mat.SetFloat("_Is_BlendBaseColor",0f);
        mat.EnableKeyword("_OUTLINE_NML");mat.EnableKeyword("_IS_CLIPPING_OFF");mat.EnableKeyword("_EMISSIVE_SIMPLE");
        mat.SetShaderPassEnabled("SRPDefaultUnlit",true);mat.SetOverrideTag("RenderType","Opaque");mat.renderQueue=2000;
        EditorUtility.SetDirty(mat);
    }
}
