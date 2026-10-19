using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.Rendering;

public static class AuthorDamageSparks
{
    const string Root="Assets/_Project/VFX/DamageSparks";
    static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;string parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));}
    static void Save(UnityEngine.Object o){EditorUtility.SetDirty(o);AssetDatabase.SaveAssetIfDirty(o);}
    static Material Material(string name,string texture,Color color)
    {
        var path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(m,path);}
        m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture>(texture));m.SetColor("_BaseColor",color);m.SetFloat("_Surface",1);m.SetFloat("_Blend",2);m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",1);m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3001;Save(m);return m;
    }
    static ParticleSystem Part(GameObject root,string name,Material material,int count,float minLife,float maxLife,float minSpeed,float maxSpeed,float minSize,float maxSize)
    {
        var go=new GameObject(name);go.layer=CombatVfxRendering.Layer;go.transform.SetParent(root.transform,false);
        var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.loop=false;main.duration=.35f;main.playOnAwake=true;main.startLifetime=new ParticleSystem.MinMaxCurve(minLife,maxLife);main.startSpeed=new ParticleSystem.MinMaxCurve(minSpeed,maxSpeed);main.startSize=new ParticleSystem.MinMaxCurve(minSize,maxSize);main.maxParticles=24;main.simulationSpace=ParticleSystemSimulationSpace.World;
        var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=38;shape.radius=.025f;
        var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
        var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.6f,.12f,.015f),.6f),new GradientColorKey(new Color(.12f,.08f,.04f),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.9f,.35f),new GradientAlphaKey(0,1)});color.color=gradient;
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        return ps;
    }
    public static string Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode");Folder(Root+"/Materials");Folder(Root+"/Prefabs");Folder(Root+"/Animation");
        var spark=Material("MetalSpark","Assets/Hovl Studio/HSFiles/Textures/Point11.png",new Color(3.8f,2.3f,.65f,1));
        var core=Material("ContactCore","Assets/Hovl Studio/HSFiles/Textures/Mask23.png",new Color(2.4f,1.35f,.3f,1));
        var pulse=new AnimationClip{name="DamageLightPulse",frameRate=60};var curve=new AnimationCurve(new Keyframe(0,3.8f),new Keyframe(.018f,3.8f),new Keyframe(.095f,0),new Keyframe(.5f,0));
        for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
        AnimationUtility.SetEditorCurve(pulse,EditorCurveBinding.FloatCurve("Contact Light",typeof(Light),"m_Intensity"),curve);
        string clipPath=Root+"/Animation/DamageLightPulse.anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);if(old==null)AssetDatabase.CreateAsset(pulse,clipPath);else{EditorUtility.CopySerialized(pulse,old);UnityEngine.Object.DestroyImmediate(pulse);pulse=old;Save(old);}
        string controllerPath=Root+"/Animation/DamageLight.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath,pulse);Save(controller);
        var root=new GameObject("Damage Sparks");root.layer=CombatVfxRendering.Layer;
        try
        {
            var timer=root.AddComponent<ParticleSystem>();timer.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var m=timer.main;m.loop=false;m.playOnAwake=true;m.duration=.6f;m.stopAction=ParticleSystemStopAction.Destroy;var e=timer.emission;e.enabled=false;root.GetComponent<ParticleSystemRenderer>().enabled=false;
            var fragments=Part(root,"Rebounding metal",spark,11,.10f,.28f,1.4f,4.2f,.025f,.055f);m=fragments.main;m.gravityModifier=.9f;
            var trails=fragments.trails;trails.enabled=true;trails.lifetime=.035f;trails.widthOverTrail=.25f;trails.ratio=.6f;trails.dieWithParticles=true;fragments.GetComponent<ParticleSystemRenderer>().trailMaterial=spark;
            var collision=fragments.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.World;collision.mode=ParticleSystemCollisionMode.Collision3D;collision.collidesWith=(1<<6)|(1<<7);collision.quality=ParticleSystemCollisionQuality.Low;collision.dampen=.35f;collision.bounce=.2f;collision.lifetimeLoss=.25f;collision.maxCollisionShapes=8;
            var drag=fragments.limitVelocityOverLifetime;drag.enabled=true;drag.drag=.6f;
            Part(root,"Contact ignition",core,1,.045f,.065f,0,0,.22f,.28f);
            var light=new GameObject("Contact Light").AddComponent<Light>();light.transform.SetParent(root.transform,false);light.type=LightType.Point;light.color=new Color(1,.58f,.15f);light.intensity=3.8f;light.range=1.35f;light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForcePixel;
            root.AddComponent<Animator>().runtimeAnimatorController=controller;
            string path=Root+"/Prefabs/DamageSparks.prefab";PrefabUtility.SaveAsPrefabAsset(root,path);
            var settings=AssetDatabase.LoadAssetAtPath<CombatVfxSettings>("Assets/_Project/Settings/Effects/CombatVfx.asset");settings.damageSparkPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);Save(settings);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        return "Native directional metal sparks, contact flash, collision and 95 ms animated point light authored; 600 ms native cleanup";
    }
}
