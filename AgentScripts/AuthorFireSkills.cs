using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

// Targeted native authoring. Rebuilds only the new FireSkills assets and three skill envelopes.
public static class AuthorFireSkills
{
    private const string Root = "Assets/_Project/VFX/FireSkills";
    private static Material flame, core, smoke, spark, flash;
    private static AnimatorController lightController;

    public static object Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Author in stopped Edit Mode.");
        foreach (var folder in new[] { "Textures", "Materials", "Profiles", "Prefabs", "Animations" }) Folder(Root + "/" + folder);
        Texture2D cells = CopyTexture("Assets/Hovl Studio/HSFiles/Textures/Fire3.png", "FireCells");
        Texture2D soot = CopyTexture("Assets/Hovl Studio/HSFiles/Textures/Smoke17.png", "SootMask");
        Texture2D noise = CopyTexture("Assets/Hovl Studio/HSFiles/Textures/Noise35.png", "ErosionNoise");
        Texture2D streak = CopyTexture("Assets/Hovl Studio/HSFiles/Textures/Trail21.png", "ThermalStreak");
        Texture2D flare = CopyTexture("Assets/Hovl Studio/HSFiles/Textures/Flare17.png", "IgnitionMask");
        var shader = Shader.Find("Mecha/VFX/CellThermal");
        if (shader == null) throw new InvalidOperationException("Compile CellThermal.shader first.");
        flame = Thermal("FlameEnvelope", shader, cells, noise, 0.075f, 0.10f, 0.8f);
        core = Thermal("HotCore", shader, cells, noise, 0.14f, 0.04f, 1.6f);
        smoke = Thermal("CharcoalSoot", shader, soot, noise, 0.15f, 0.12f, 0f);
        spark = Thermal("MetalSparks", shader, streak, noise, 0.08f, 0f, 0.1f);
        flash = Thermal("Ignition", shader, flare, noise, 0.15f, 0f, 0.5f);
        lightController = LightAnimation();
        CastAnimation();
        var hit = BuildHit();
        var skills = new FireSkillSettings[3];
        for (int i = 0; i < skills.Length; i++)
        {
            var kind = (PlayerAttack.ActionKind)(i + 5);
            var piece = AssetDatabase.LoadAssetAtPath<AttackPiece>("Assets/_Project/Settings/Skills/" + kind + ".asset");
            var action = piece.action;
            action.hitActiveTime = i == 1 ? 0.28f : 0.24f;
            action.hitTravelTime = i == 1 ? 0.21f : 0.17f;
            action.hitLength = i == 0 ? 2.6f : i == 1 ? 3.5f : 3.2f;
            action.hitRadius = i == 1 ? 0.34f : 0.32f;
            action.hitFromHand = i == 1;
            action.hitFirstAngle = i == 0 ? -24f : i == 1 ? 0f : -22.5f;
            action.hitAngleStep = i == 0 ? 16f : i == 1 ? 0f : 45f;
            action.hitOrigin = i == 2 ? new Vector2(0.12f, 1.38f) : new Vector2(0.14f, 0.82f);
            action.hitPulseHeight = i == 2 ? -1.22f : 0f;
            action.duration = i == 0 ? 0.78f : i == 1 ? 0.85f : 0.88f;
            action.cancel = action.duration;
            action.impact = 0.18f;
            action.pulseInterval = i == 0 ? 0.08f : i == 1 ? 0.11f : 0.16f;
            EditorUtility.SetDirty(piece); AssetDatabase.SaveAssetIfDirty(piece);
            var settings = Asset<FireSkillSettings>(Root + "/Profiles/" + kind + "Fire.asset");
            settings.kind = kind; settings.attack = piece; settings.hit = hit;
            settings.emissionTime = i == 1 ? 0.14f : 0.1f;
            settings.cleanupTime = 1.25f; settings.finalScale = 1.07f;
            settings.burst = BuildBurst(settings);
            EditorUtility.SetDirty(settings); AssetDatabase.SaveAssetIfDirty(settings);
            skills[i] = settings;
        }
        const string playerPath = "Assets/_Project/Prefabs/MechaPlayer.prefab";
        var player = PrefabUtility.LoadPrefabContents(playerPath);
        try
        {
            var mount = player.GetComponent<CombatEmissionOrigin>() ?? player.AddComponent<CombatEmissionOrigin>();
            var serialized = new SerializedObject(mount);
            serialized.FindProperty("skillHand").objectReferenceValue = player.GetComponentsInChildren<Transform>(true)
                .Single(t => t.name == "Bip001 R Hand");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var presentation = player.GetComponent<FireSkillPresentation>() ?? player.AddComponent<FireSkillPresentation>();
            serialized = new SerializedObject(presentation);
            var array = serialized.FindProperty("skills"); array.arraySize = 3;
            for (int i = 0; i < 3; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(player, playerPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
        return JsonConvert.SerializeObject(new { skills = skills.Select(s => new { kind = s.kind.ToString(),
            profile = AssetDatabase.GetAssetPath(s), prefab = AssetDatabase.GetAssetPath(s.burst),
            s.attack.action.hitLength, s.attack.action.hitActiveTime,
            nativeSystems=s.burst.GetComponentsInChildren<ParticleSystem>(true).Length }).ToArray() });
    }

    private static FireSkillBurst BuildBurst(FireSkillSettings settings)
    {
        var root = new GameObject(settings.kind + " Thermal Burst");
        root.SetActive(false);
        try
        {
            root.AddComponent<FireSkillBurst>();
            if (settings.kind == PlayerAttack.ActionKind.SwordFire) root.AddComponent<HandFlameVfx>();
            float speed = settings.attack.action.hitLength / settings.attack.action.hitTravelTime;
            var envelope = Layer(root, "Flame Envelope", flame, 0.22f, speed * 0.88f, 0.42f, new Color(1.0f, 0.44f, 0.045f), 155f, 0);
            Cone(envelope, settings.kind == PlayerAttack.ActionKind.SwordFire ? 8f : 5f, 0.045f);
            Sheet(envelope); Growth(envelope, 0.65f, 1.45f); WorldCollision(envelope, false);
            var inner = Layer(root, "Flame Core", core, 0.19f, speed, 0.23f, new Color(1f, 0.88f, 0.45f), 120f, 0);
            Cone(inner, 3f, 0.025f); Sheet(inner); Growth(inner, 0.7f, 1.1f); WorldCollision(inner, false);
            var edges = Layer(root, "Shear Streaks", spark, 0.15f, speed * 1.05f, 0.11f, new Color(1.65f, 0.4f, 0.045f), 0f, 5);
            Cone(edges, 7f, 0.02f); Stretch(edges, 3.0f); WorldCollision(edges, false);
            if (settings.kind != PlayerAttack.ActionKind.SwordFire)
            {
                var cut = Layer(root, "Cut Residue", spark, 0.38f, 0f, 1f, new Color(1f,0.72f,0.30f),0,1,0.08f);
                cut.transform.localPosition = Vector3.forward * settings.attack.action.hitLength * 0.48f;
                var cm=cut.main;cm.startSize3D=true;cm.startSizeX=settings.attack.action.hitLength;cm.startSizeY=0.075f;cm.startSizeZ=1f;
            }
            var ignition = Layer(root, "Hand Ignition", flash, 0.055f, 0f, 0.24f, new Color(1.6f, 0.8f, 0.25f), 0f, 2);
            Growth(ignition, 0.5f, 1.1f);
            var soot = Layer(root, "Cooling Soot", smoke, 0.65f, speed * 0.14f, 0.25f, new Color(0.08f, 0.055f, 0.045f, 0.65f), 35f, 0, 0.055f);
            Cone(soot, 12f, 0.06f); Growth(soot, 0.6f, 2.0f);
            var force = soot.forceOverLifetime; force.enabled = true; force.y = 0.75f; force.space = ParticleSystemSimulationSpace.World;
            var embers = Layer(root, "Cooling Embers", spark, 0.44f, speed * 0.40f, 0.035f, new Color(1.2f, 0.28f, 0.015f), 0f, 11, 0.02f);
            Cone(embers, 15f, 0.04f); Stretch(embers, 1.3f); WorldCollision(embers, true);
            var haze = Layer(root, "Residual Heat Breakup", smoke, 0.35f, speed * 0.26f, 0.15f, new Color(0.28f, 0.09f, 0.018f, 0.20f), 25f, 0, 0.02f);
            Cone(haze, 8f, 0.045f); Growth(haze, 0.8f, 1.6f);
            var lightObject = new GameObject("Thermal Light"); lightObject.transform.SetParent(root.transform, false);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1f, 0.29f, 0.055f);
            light.range = 3.5f; light.intensity = 0f; light.shadows = LightShadows.None;
            root.AddComponent<Animator>().runtimeAnimatorController = lightController;
            string path = Root + "/Prefabs/" + settings.kind + "ThermalBurst.prefab";
            return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<FireSkillBurst>();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static FireSkillHitVfx BuildHit()
    {
        var root = new GameObject("Thermal Armor Contact"); root.SetActive(false);
        try
        {
            root.AddComponent<FireSkillHitVfx>();
            var flare = Layer(root, "Contact Flash", flash, 0.055f, 0f, 0.28f, new Color(1.7f, 0.8f, 0.2f), 0, 1);
            Growth(flare, 0.6f, 1f);
            var flameHit = Layer(root, "Flame Surface Break", flame, 0.20f, 2.2f, 0.20f, new Color(1.2f, 0.24f, 0.015f), 0, 7);
            Cone(flameHit, 55f, 0.06f); Sheet(flameHit); Growth(flameHit, 0.6f, 1.2f);
            var sparks = Layer(root, "Incoming Sparks", spark, 0.32f, 5.5f, 0.04f, new Color(1.8f, 0.58f, 0.08f), 0, 9);
            Cone(sparks, 28f, 0.05f); Stretch(sparks, 2f); WorldCollision(sparks, true);
            var soot = Layer(root, "Armor Soot", smoke, 0.55f, 0.75f, 0.20f, new Color(0.075f, 0.05f, 0.04f, 0.7f), 0, 3, 0.04f);
            Cone(soot, 50f, 0.06f); Growth(soot, 0.7f, 1.7f);
            return PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/ThermalArmorContact.prefab").GetComponent<FireSkillHitVfx>();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static ParticleSystem Layer(GameObject root, string name, Material material, float life, float speed, float size, Color tint, float rate, int count, float delay = 0f)
    {
        var go = new GameObject(name); go.transform.SetParent(root.transform, false);
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = false; main.playOnAwake = false; main.duration = 0.14f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.82f, life); main.startDelay = delay;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.88f, speed); main.startSize = new ParticleSystem.MinMaxCurve(size * 0.8f, size * 1.2f);
        main.startColor = tint; main.maxParticles = 96; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape; main.stopAction = ParticleSystemStopAction.None;
        var emission = ps.emission; emission.rateOverTime = rate; if (count > 0) emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
        var shape = ps.shape; shape.enabled = false;
        var color = ps.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient(); gradient.SetKeys(new[] {new GradientColorKey(Color.white,0f),new GradientColorKey(new Color(0.85f,0.65f,0.35f),0.65f),new GradientColorKey(new Color(0.2f,0.12f,0.07f),1f)},
            new[] {new GradientAlphaKey(0f,0f),new GradientAlphaKey(1f,0.08f),new GradientAlphaKey(0.75f,0.55f),new GradientAlphaKey(0f,1f)}); color.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; renderer.sortingFudge = 0f;
        renderer.SetActiveVertexStreams(new System.Collections.Generic.List<ParticleSystemVertexStream> {ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV});
        return ps;
    }
    private static void Cone(ParticleSystem ps, float angle, float radius) { var s=ps.shape;s.enabled=true;s.shapeType=ParticleSystemShapeType.Cone;s.angle=angle;s.radius=radius;s.arc=360f; }
    private static void Sheet(ParticleSystem ps) { var s=ps.textureSheetAnimation;s.enabled=true;s.numTilesX=2;s.numTilesY=2;s.frameOverTime=new ParticleSystem.MinMaxCurve(1f,AnimationCurve.Linear(0f,0f,1f,1f));s.startFrame=new ParticleSystem.MinMaxCurve(0f,0.75f);s.cycleCount=1; }
    private static void Growth(ParticleSystem ps,float from,float to) {var s=ps.sizeOverLifetime;s.enabled=true;s.size=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0f,from),new Keyframe(0.25f,1f),new Keyframe(0.75f,to),new Keyframe(1f,to*0.65f)));}
    private static void Stretch(ParticleSystem ps,float length) {var r=ps.GetComponent<ParticleSystemRenderer>();r.renderMode=ParticleSystemRenderMode.Stretch;r.lengthScale=length;r.velocityScale=0.025f;}
    private static void WorldCollision(ParticleSystem ps,bool bounce) {var c=ps.collision;c.enabled=true;c.type=ParticleSystemCollisionType.World;c.mode=ParticleSystemCollisionMode.Collision3D;c.collidesWith=(1<<6)|(1<<7);c.quality=ParticleSystemCollisionQuality.Medium;c.maxCollisionShapes=16;c.radiusScale=0.1f;c.bounce=bounce?0.4f:0f;c.dampen=0.45f;c.lifetimeLoss=bounce?0.25f:1f;var m=ps.main;m.gravityModifier=bounce?0.35f:0f;}
    private static Material Thermal(string name, Shader shader, Texture2D mask, Texture2D noise, float cutoff, float erosion, float hot)
    {
        string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;
        m.SetTexture("_BaseMap",mask);m.SetTexture("_NoiseMap",noise);m.SetFloat("_Cutoff",cutoff);m.SetFloat("_NoiseStrength",erosion);m.SetFloat("_CoreStrength",hot);
        m.SetFloat("_CoreThreshold",0.32f);m.SetFloat("_Feather",0.055f);
        EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);return m;
    }
    private static AnimatorController LightAnimation()
    {
        string clipPath=Root+"/Animations/ThermalLight.anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}
        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("Thermal Light",typeof(Light),"m_Intensity"),new AnimationCurve(new Keyframe(0,0),new Keyframe(0.02f,2.4f),new Keyframe(0.07f,0.8f),new Keyframe(0.18f,0)));
        EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
        string path=Root+"/Animations/ThermalLight.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(controller==null){controller=AnimatorController.CreateAnimatorControllerAtPath(path);var state=controller.layers[0].stateMachine.AddState("ThermalPulse");state.motion=clip;controller.layers[0].stateMachine.defaultState=state;}
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);return controller;
    }
    private static void CastAnimation()
    {
        var settings=AssetDatabase.LoadAssetAtPath<CharacterAnimationSettings>("Assets/_Project/Settings/Animation/PlayerAnimation.asset");
        var player=PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/MechaPlayer.prefab");
        try
        {
            player.transform.rotation=Quaternion.Euler(0f,90f,0f);
            var animator=player.GetComponentInChildren<Animator>();
            var idle=settings.Clip("Idle");idle.SampleAnimation(animator.gameObject,0f);
            var upper=player.GetComponentsInChildren<Transform>().Single(t=>t.name=="Bip001 R UpperArm");
            var fore=player.GetComponentsInChildren<Transform>().Single(t=>t.name=="Bip001 R Forearm");
            var hand=player.GetComponentsInChildren<Transform>().Single(t=>t.name=="Bip001 R Hand");
            Quaternion upperRest=upper.localRotation,foreRest=fore.localRotation;
            Vector3 aim=new Vector3(1f,-0.08f,0f).normalized;
            upper.rotation=Quaternion.FromToRotation(fore.position-upper.position,aim)*upper.rotation;
            fore.rotation=Quaternion.FromToRotation(hand.position-fore.position,Vector3.right)*fore.rotation;
            string path=Root+"/Animations/SwordFireCast.anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
            foreach(var binding in AnimationUtility.GetCurveBindings(idle))
            {
                var source=AnimationUtility.GetEditorCurve(idle,binding);
                var keys=source.keys;for(int i=0;i<keys.Length;i++){var k=keys[i];k.time=k.time/Mathf.Max(idle.length,0.01f)*0.85f;keys[i]=k;}
                AnimationUtility.SetEditorCurve(clip,binding,new AnimationCurve(keys));
            }
            ArmCurve(clip,animator.transform,upper,upperRest,upper.localRotation);
            ArmCurve(clip,animator.transform,fore,foreRest,fore.localRotation);
            clip.EnsureQuaternionContinuity();
            var clipSettings=AnimationUtility.GetAnimationClipSettings(clip);clipSettings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,clipSettings);
            EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
            var controller=(AnimatorController)settings.controller;
            var castState=controller.layers[0].stateMachine.states.Single(s=>s.state.name=="SwordFire").state;
            castState.motion=clip;EditorUtility.SetDirty(castState);
            for(int i=0;i<settings.clips.Length;i++) if(settings.clips[i].state=="SwordFire") settings.clips[i].clip=clip;
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);
        }
        finally {PrefabUtility.UnloadPrefabContents(player);}
    }
    private static void ArmCurve(AnimationClip clip,Transform root,Transform bone,Quaternion rest,Quaternion aim)
    {
        string path=AnimationUtility.CalculateTransformPath(bone,root);
        for(int i=0;i<4;i++)
        {
            string channel="xyzw"[i].ToString();
            var curve=new AnimationCurve(new Keyframe(0,rest[i]),new Keyframe(0.14f,aim[i]),new Keyframe(0.66f,aim[i]),new Keyframe(0.85f,rest[i]));
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+channel),curve);
        }
    }
    private static T Asset<T>(string path) where T:ScriptableObject {var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a==null){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);}return a;}
    private static Texture2D CopyTexture(string source,string name)
    {
        string path=Root+"/Textures/"+name+".png";if(AssetDatabase.LoadAssetAtPath<Texture2D>(path)==null && !AssetDatabase.CopyAsset(source,path))throw new InvalidOperationException("Could not copy "+source);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=name=="ErosionNoise"?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.sRGBTexture=false;importer.textureCompression=TextureImporterCompression.Compressed;importer.mipmapEnabled=true;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static void Folder(string path) {if(AssetDatabase.IsValidFolder(path))return;Folder(System.IO.Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path).Replace('\\','/'),System.IO.Path.GetFileName(path));}
}
