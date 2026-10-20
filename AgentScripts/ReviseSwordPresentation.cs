using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ReviseSwordPresentation
{
    const string Root="Assets/_Project/VFX/Melee";
    static void Save(UnityEngine.Object o){EditorUtility.SetDirty(o);AssetDatabase.SaveAssetIfDirty(o);}
    static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;var parent=System.IO.Path.GetDirectoryName(p).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(p));}
    public static string Glow()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode");
        foreach(string color in new[]{"Red","Blue"})
        {
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Blink/Art/Weapons/LowPoly/FreeSwords/Sword7/Sword7_Material_"+color+".mat");
            var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Sword7"+color+".mat");
            m.SetTexture("_Emissive_Tex",source.GetTexture("_EmissionMap"));m.SetColor("_Emissive_Color",source.GetColor("_EmissionColor")*1.8f);m.SetFloat("_EMISSIVE",0);
            m.EnableKeyword("_EMISSIVE_SIMPLE");m.DisableKeyword("_EMISSIVE_ANIMATION");Save(m);
            string path="Assets/_Project/Prefabs/Weapons/Sword7"+color+".prefab";
            var go=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var t=go.transform.Find("Blade Light");if(t==null){t=new GameObject("Blade Light").transform;t.SetParent(go.transform,false);}t.localPosition=new Vector3(0,.85f,-.1f);
                var light=t.GetComponent<Light>();if(light==null)light=t.gameObject.AddComponent<Light>();light.type=LightType.Point;light.shadows=LightShadows.None;light.range=2.6f;light.intensity=2.8f;light.color=color=="Blue"?new Color(.04f,.35f,1f):new Color(1f,.035f,.012f);light.renderMode=LightRenderMode.ForcePixel;
                PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/VFX/SceneSupport/Profiles/CombatPostProcessing.asset");
        if(profile==null)profile=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).First(v=>v.isGlobal).sharedProfile;
        if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.7f);bloom.threshold.Override(.95f);bloom.scatter.Override(.65f);Save(bloom);Save(profile);}
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None)){camera.allowHDR=true;var data=camera.GetComponent<UniversalAdditionalCameraData>();if(data!=null)data.renderPostProcessing=true;}
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        return "Native toon emission mask + original red/blue HDR color restored; blade point lights and Bloom .7 authored";
    }
    public static string Sources()
    {
        Folder(Root+"/Sources");
        foreach(string name in new[]{"Slash01.fbx","BrightSlash01.png","DarkSlash01.png"})
        {
            string path=Root+"/Sources/"+name;
            if(!System.IO.File.Exists(path))System.IO.File.Copy("Temp/VfxRefinery/SwordRevision/"+name,path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            if(name.EndsWith(".png")){var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.sRGBTexture=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.mipmapEnabled=false;imp.wrapMode=TextureWrapMode.Clamp;imp.SaveAndReimport();}
            else {var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.importAnimation=false;imp.SaveAndReimport();}
        }
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Sources/Slash01.fbx");
        return string.Join("\n",model.GetComponentsInChildren<MeshFilter>().Select(f=>f.name+" bounds="+f.sharedMesh.bounds+" transform="+f.transform.localEulerAngles+" scale="+f.transform.localScale));
    }
    static Material ParticleMaterial(string name,Texture texture,Color color,bool additive)
    {
        string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(m,path);}
        m.SetTexture("_BaseMap",texture);m.SetColor("_BaseColor",color);m.SetFloat("_Surface",1);m.SetFloat("_Blend",additive?2:0);m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",additive?1:10);m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.DisableKeyword("_ALPHAPREMULTIPLY_ON");m.DisableKeyword("_ALPHAMODULATE_ON");m.renderQueue=additive?3002:3001;Save(m);return m;
    }
    static ParticleSystem Layer(GameObject root,string name,Mesh mesh,Material material,float delay,float lifetime,float size,float alpha)
    {
        var child=new GameObject(name);child.layer=CombatVfxRendering.Layer;child.transform.SetParent(root.transform,false);
        var ps=child.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.loop=false;main.playOnAwake=false;main.duration=.4f;main.startDelay=delay;main.startLifetime=lifetime;main.startSpeed=0;main.startSize=size;main.maxParticles=2;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.simulationSpace=ParticleSystemSimulationSpace.Local;
        var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,1)});var shape=ps.shape;shape.enabled=false;
        var color=ps.colorOverLifetime;color.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(alpha,.08f),new GradientAlphaKey(alpha*.7f,.4f),new GradientAlphaKey(0,1)});color.color=g;
        var scale=ps.sizeOverLifetime;scale.enabled=true;scale.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.8f),new Keyframe(.35f,1),new Keyframe(1,1.08f)));
        var rotation=ps.rotationOverLifetime;rotation.enabled=true;rotation.z=new ParticleSystem.MinMaxCurve(.6f);
        var r=ps.GetComponent<ParticleSystemRenderer>();r.renderMode=ParticleSystemRenderMode.Mesh;r.mesh=mesh;r.sharedMaterial=material;r.alignment=ParticleSystemRenderSpace.Local;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.maxParticleSize=1f;
        return ps;
    }
    public static string Effects()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode");
        Folder(Root+"/Prefabs");
        foreach(string name in new[]{"BrightSlash01","DarkSlash01"}){var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/Sources/"+name+".png");importer.alphaSource=TextureImporterAlphaSource.FromGrayScale;importer.alphaIsTransparency=false;importer.SaveAndReimport();}
        // Adapt the supplied isolated mesh into a half sweep; all silhouette/UV geometry comes from that source.
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Sources/Slash01.fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
        var mesh=UnityEngine.Object.Instantiate(source);mesh.name="SwordHalfSweep";var triangles=source.triangles;var uv=source.uv;var keep=new System.Collections.Generic.List<int>();
        for(int i=0;i<triangles.Length;i+=3)if((uv[triangles[i]].x+uv[triangles[i+1]].x+uv[triangles[i+2]].x)/3f<=.5f){keep.Add(triangles[i]);keep.Add(triangles[i+1]);keep.Add(triangles[i+2]);}
        mesh.triangles=keep.ToArray();mesh.vertices=mesh.vertices.Select(v=>v/source.bounds.extents.y).ToArray();mesh.RecalculateBounds();
        string meshPath=Root+"/Sources/SwordHalfSweep.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(old!=null){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;Save(mesh);}else AssetDatabase.CreateAsset(mesh,meshPath);
        var settings=AssetDatabase.LoadAssetAtPath<SwordSwingSettings>(Root+"/Profiles/SwordSwing.asset");settings.lightWidth=.18f;settings.heavyWidth=.3f;settings.lightTail=.10f;settings.heavyTail=.16f;Save(settings);
        foreach(string color in new[]{"Red","Blue"})
        {
            bool blue=color=="Blue";Color hue=blue?new Color(.04f,.65f,2.4f,1):new Color(2.4f,.1f,.025f,1);
            var band=ParticleMaterial("CutBand"+color,AssetDatabase.LoadAssetAtPath<Texture>(Root+"/Sources/BrightSlash01.png"),hue,true);
            var rim=ParticleMaterial("CutRim"+color,AssetDatabase.LoadAssetAtPath<Texture>(Root+"/Sources/BrightSlash01.png"),blue?new Color(.7f,1.8f,3f,1):new Color(3f,.65f,.35f,1),true);rim.SetTextureScale("_BaseMap",new Vector2(1,2.6f));rim.SetTextureOffset("_BaseMap",new Vector2(0,-.8f));Save(rim);
            var wake=ParticleMaterial("CutWake"+color,AssetDatabase.LoadAssetAtPath<Texture>(Root+"/Sources/DarkSlash01.png"),new Color(.018f,.025f,.04f,1),false);
            var peel=ParticleMaterial("BladePeel"+color,AssetDatabase.LoadAssetAtPath<Texture>("Assets/Hovl Studio/HSFiles/Textures/Point11.png"),hue,true);
            var root=new GameObject("SwordCut"+color);root.layer=CombatVfxRendering.Layer;
            string fxPath=Root+"/Prefabs/SwordCut"+color+".prefab";
            try
            {
                var body=Layer(root,"Charcoal displacement",mesh,wake,0,.25f,1.05f,.32f);body.transform.localPosition=Vector3.forward*.05f;
                Layer(root,"Ionized cut band",mesh,band,.008f,.17f,1,.65f);
                var edge=Layer(root,"Hot cutting rim",mesh,rim,.012f,.105f,1.025f,.85f);edge.transform.localPosition=Vector3.back*.015f;
                PrefabUtility.SaveAsPrefabAsset(root,fxPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            string path="Assets/_Project/Prefabs/Weapons/Sword7"+color+".prefab";var go=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var t=go.transform.Find("Blade Peel");if(t==null){t=new GameObject("Blade Peel").transform;t.SetParent(go.transform,false);}t.localPosition=new Vector3(0,.95f,0);
                var ps=t.GetComponent<ParticleSystem>();if(ps==null)ps=t.gameObject.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.loop=true;main.playOnAwake=false;main.duration=1;main.startLifetime=new ParticleSystem.MinMaxCurve(.1f,.24f);main.startSize=new ParticleSystem.MinMaxCurve(.035f,.075f);main.startSpeed=new ParticleSystem.MinMaxCurve(.1f,.6f);main.maxParticles=18;main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.gravityModifier=.2f;
                var emission=ps.emission;emission.rateOverTime=38;var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(.08f,.8f,.05f);
                var col=ps.colorOverLifetime;col.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.2f,.3f,.45f),1)},new[]{new GradientAlphaKey(.85f,0),new GradientAlphaKey(.65f,.3f),new GradientAlphaKey(0,1)});col.color=gradient;
                var noise=ps.noise;noise.enabled=true;noise.strength=.13f;noise.frequency=2.2f;noise.scrollSpeed=.5f;noise.quality=ParticleSystemNoiseQuality.Low;
                var render=ps.GetComponent<ParticleSystemRenderer>();render.sharedMaterial=peel;render.renderMode=ParticleSystemRenderMode.Billboard;render.shadowCastingMode=ShadowCastingMode.Off;render.receiveShadows=false;t.gameObject.layer=CombatVfxRendering.Layer;
                var presentation=go.GetComponent<SwordSwingPresentation>();var so=new SerializedObject(presentation);so.FindProperty("bladeLight").objectReferenceValue=go.transform.Find("Blade Light").GetComponent<Light>();so.FindProperty("bladePeel").objectReferenceValue=ps;so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        foreach(string path in new[]{"Assets/_Project/Settings/Animation/PlayerAnimation.asset","Assets/_Project/Settings/Animation/WingZeroAnimation.asset"}){var p=AssetDatabase.LoadAssetAtPath<CharacterAnimationSettings>(path);p.meleeCrossFade=.012f;Save(p);}
        return "Original native half-sweep + dark wake + hot rim, blade peel and owned light envelope authored; source triangles="+mesh.triangles.Length/3;
    }
}

