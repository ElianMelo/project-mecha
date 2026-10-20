using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

// Focused asset authoring through Unity APIs, invoked explicitly in Edit Mode.
public static class AuthorDirectionalSparksAndBlade
{
    const string SparkRoot="Assets/_Project/VFX/DamageSparks";
    static void Save(UnityEngine.Object asset){EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
    static Gradient Cooling(float alpha)
    {
        var g=new Gradient();
        g.SetKeys(new[]{new GradientColorKey(new Color(1,.93f,.6f),0),new GradientColorKey(new Color(1,.56f,.1f),.28f),new GradientColorKey(new Color(.6f,.12f,.012f),.8f),new GradientColorKey(new Color(.15f,.025f,.004f),1)},
            new[]{new GradientAlphaKey(alpha,0),new GradientAlphaKey(alpha,.45f),new GradientAlphaKey(0,1)});
        return g;
    }
    static void Sparks(ParticleSystem ps, bool cooling)
    {
        ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var m=ps.main;m.duration=.55f;m.loop=false;m.playOnAwake=true;m.startDelay=cooling?.018f:0;
        m.startLifetime=new ParticleSystem.MinMaxCurve(cooling?.24f:.14f,cooling?.43f:.32f);
        m.startSpeed=new ParticleSystem.MinMaxCurve(cooling?1.3f:2.6f,cooling?3.1f:5.6f);
        m.startSize=new ParticleSystem.MinMaxCurve(cooling?.012f:.016f,cooling?.024f:.035f);
        m.startColor=Color.white;m.maxParticles=cooling?16:40;m.gravityModifier=cooling?1.2f:.65f;
        m.simulationSpace=ParticleSystemSimulationSpace.World;m.scalingMode=ParticleSystemScalingMode.Hierarchy;
        var e=ps.emission;e.enabled=true;e.rateOverTime=0;e.rateOverDistance=0;
        e.SetBursts(cooling?new[]{new ParticleSystem.Burst(0,9)}:new[]{new ParticleSystem.Burst(0,23),new ParticleSystem.Burst(.018f,7)});
        var s=ps.shape;s.enabled=true;s.shapeType=ParticleSystemShapeType.Cone;s.angle=cooling?52:36;s.radius=.018f;s.length=.01f;
        var color=ps.colorOverLifetime;color.enabled=true;color.color=Cooling(cooling?.65f:1f);
        var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,.18f));
        var drag=ps.limitVelocityOverLifetime;drag.enabled=true;drag.limit=100;drag.drag=cooling?1.1f:.55f;drag.multiplyDragByParticleSize=false;drag.multiplyDragByParticleVelocity=true;
        var noise=ps.noise;noise.enabled=true;noise.strength=cooling?.18f:.1f;noise.frequency=2.7f;noise.scrollSpeed=.7f;noise.quality=ParticleSystemNoiseQuality.Low;
        var trails=ps.trails;trails.enabled=true;trails.ratio=1;trails.lifetime=cooling?.23f:.36f;trails.minVertexDistance=.015f;
        trails.worldSpace=true;trails.dieWithParticles=true;trails.sizeAffectsWidth=true;trails.sizeAffectsLifetime=false;
        trails.inheritParticleColor=true;trails.widthOverTrail=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.7f,1,0));
        var collision=ps.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.World;collision.mode=ParticleSystemCollisionMode.Collision3D;
        collision.collidesWith=(1<<6)|(1<<7);collision.bounce=.25f;collision.dampen=.55f;collision.lifetimeLoss=.3f;collision.sendCollisionMessages=false;collision.maxCollisionShapes=16;
        var r=ps.GetComponent<ParticleSystemRenderer>();r.renderMode=ParticleSystemRenderMode.Billboard;
        r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(SparkRoot+"/Materials/MetalSpark.mat");r.trailMaterial=r.sharedMaterial;
        r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.maxParticleSize=.15f;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode");
        string path=SparkRoot+"/Prefabs/DamageSparks.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var rootPs=root.GetComponent<ParticleSystem>();rootPs.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=rootPs.main;m.duration=.9f;m.startLifetime=.01f;m.startSpeed=0;m.maxParticles=1;m.loop=false;m.stopAction=ParticleSystemStopAction.Destroy;
            var shape=rootPs.shape;shape.enabled=false;var em=rootPs.emission;em.enabled=false;
            Sparks(root.transform.Find("Rebounding metal").GetComponent<ParticleSystem>(),false);
            var cooling=root.transform.Find("Cooling fragments");
            if(cooling==null){cooling=new GameObject("Cooling fragments").transform;cooling.SetParent(root.transform,false);cooling.gameObject.AddComponent<ParticleSystem>();}
            cooling.gameObject.layer=root.layer;Sparks(cooling.GetComponent<ParticleSystem>(),true);
            var core=root.transform.Find("Contact ignition").GetComponent<ParticleSystem>();
            var cm=core.main;cm.startSize=new ParticleSystem.MinMaxCurve(.12f,.20f);cm.startLifetime=new ParticleSystem.MinMaxCurve(.035f,.055f);cm.maxParticles=2;
            var light=root.GetComponentInChildren<Light>();light.range=1.45f;light.intensity=4.2f;light.color=new Color(1,.56f,.16f);light.shadows=LightShadows.None;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(SparkRoot+"/Animation/DamageLightPulse.anim");
        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("Contact Light",typeof(Light),"m_Intensity"),
            new AnimationCurve(new Keyframe(0,4.2f),new Keyframe(.018f,4.2f),new Keyframe(.06f,2.2f),new Keyframe(.15f,0),new Keyframe(.5f,0)));
        Save(clip);
        foreach(string color in new[]{"Blue","Red"})
        {
            path="Assets/_Project/Prefabs/Weapons/Sword7"+color+".prefab";
            var go=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var presentation=go.GetComponent<SwordSwingPresentation>();var so=new SerializedObject(presentation);
                var tip=go.transform.Find("Blade Tip").GetComponent<TrailRenderer>();
                tip.minVertexDistance=.012f;tip.numCornerVertices=3;tip.numCapVertices=2;tip.emitting=false;tip.autodestruct=false;
                var trails=so.FindProperty("bladeBodyTrails");trails.arraySize=2;
                for(int i=0;i<2;i++)
                {
                    string name=i==0?"Blade Middle Trail":"Blade Inner Trail";
                    var t=go.transform.Find(name);if(t==null){t=new GameObject(name).transform;t.SetParent(go.transform,false);}
                    t.localPosition=new Vector3(0,i==0?1.02f:.63f,0);t.gameObject.layer=tip.gameObject.layer;
                    var trail=t.GetComponent<TrailRenderer>();if(trail==null)trail=t.gameObject.AddComponent<TrailRenderer>();
                    EditorUtility.CopySerialized(tip,trail);trail.emitting=false;trail.autodestruct=false;
                    var gradient=tip.colorGradient;var keys=gradient.alphaKeys;for(int k=0;k<keys.Length;k++)keys[k].alpha*=i==0?.65f:.4f;gradient.alphaKeys=keys;trail.colorGradient=gradient;
                    trails.GetArrayElementAtIndex(i).objectReferenceValue=trail;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        var settings=AssetDatabase.LoadAssetAtPath<SwordSwingSettings>("Assets/_Project/VFX/Melee/Profiles/SwordSwing.asset");
        settings.bodyTrailWidth=.65f;Save(settings);
        var feedback=AssetDatabase.LoadAssetAtPath<CombatVfxSettings>("Assets/_Project/Settings/Effects/CombatVfx.asset");
        feedback.flashDuration=.035f;Save(feedback);
        // Match the start of the native source clip's fast cut; duration, recovery,
        // damage and the existing forward capsule stay authored as before.
        string[] names={"Slash1","Slash2","Slash3","Heavy1","Heavy2","Heavy3"};
        float[] impacts={.17f,.26f,.30f,.41f,.33f,.57f};
        for(int i=0;i<names.Length;i++)
        {var piece=AssetDatabase.LoadAssetAtPath<AttackPiece>("Assets/_Project/Settings/Attacks/"+names[i]+".asset");piece.action.impact=impacts[i];Save(piece);}
        return MechaAssetLabels.Apply(new[]{SparkRoot+"/Prefabs/DamageSparks.prefab",SparkRoot+"/Animation/DamageLightPulse.anim","Assets/_Project/Prefabs/Weapons/Sword7Blue.prefab","Assets/_Project/Prefabs/Weapons/Sword7Red.prefab","Assets/_Project/VFX/Melee/Profiles/SwordSwing.asset"},0,5);
    }
}
