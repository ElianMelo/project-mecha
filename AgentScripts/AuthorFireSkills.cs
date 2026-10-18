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
    private static Material flame, core, smoke, wisps, rearSmoke, ember, flash, ignition, distortion;
    private static AnimatorController lightController;
    private static Texture2D heatNoise;

    public static object Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Author in stopped Edit Mode.");
        foreach (var folder in new[] { "Textures", "Materials", "Profiles", "Prefabs", "Animations" }) Folder(Root + "/" + folder);
        Texture2D cells = CopyTexture("Assets/Hovl Studio/HSFiles/Textures/Fire3.png", "FireCells");
        Texture2D soot = CopyTexture("Assets/Hovl Studio/HSFiles/Textures/Smoke17.png", "SootMask");
        Texture2D noise = CopyTexture("Assets/Hovl Studio/HSFiles/Textures/Noise35.png", "ErosionNoise");
        Texture2D billow = CopyTexture("Assets/Hovl Studio/HSFiles/Textures/Smoke3.png", "BillowMask");
        Texture2D flow = CopyTexture("Assets/Hovl Studio/HSFiles/Textures/Noise43.png", "FlowNoise");
        Texture2D flare = CopyTexture("Assets/Hovl Studio/HSFiles/Textures/Flare17.png", "IgnitionMask");
        heatNoise = CopyTexture("Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr uv distortion fire perlin.png", "CombustionNoise");
        var shader = Shader.Find("Mecha/VFX/CellThermal");
        if (shader == null) throw new InvalidOperationException("Compile CellThermal.shader first.");
        flame = Thermal("FlameEnvelope", shader, cells, noise, 0.16f, 0.18f, 0.45f);
        core = Thermal("HotCore", shader, cells, flow, 0.24f, 0.12f, 0.85f);
        wisps = Thermal("VolumeFlame", shader, billow, flow, 0.055f, 0.18f, 0.30f, 0.045f);
        smoke = Thermal("CharcoalSoot", shader, soot, noise, 0.07f, 0.14f, 0f, 0.015f);
        rearSmoke = Thermal("GrayBillows", shader, billow, flow, 0.035f, 0.14f, 0f, 0.020f);
        ember = Thermal("CoalFragments", shader, billow, noise, 0.48f, 0.35f, 0.1f);
        flash = Thermal("Ignition", shader, flare, flow, 0.20f, 0.15f, 0.3f);
        if(AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/SourceIgnition.mat")==null &&
            AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/GroundIgnition.mat")!=null)
            AssetDatabase.MoveAsset(Root+"/Materials/GroundIgnition.mat",Root+"/Materials/SourceIgnition.mat");
        ignition = Thermal("SourceIgnition", shader, billow, noise, 0.25f, 0.3f, 0.40f);
        string distortionPath=Root+"/Materials/TerrainHeatDistortion.mat";
        distortion=AssetDatabase.LoadAssetAtPath<Material>(distortionPath);
        var distortionShader=Shader.Find("Mecha/VFX/TerrainHeatDistortion");
        if(distortionShader==null)throw new InvalidOperationException("Compile TerrainHeatDistortion first.");
        if(distortion==null){distortion=new Material(distortionShader);AssetDatabase.CreateAsset(distortion,distortionPath);}
        distortion.SetTexture("_BaseMap",billow);distortion.SetTexture("_NoiseMap",heatNoise);distortion.SetFloat("_Strength",0.004f);
        EditorUtility.SetDirty(distortion);AssetDatabase.SaveAssetIfDirty(distortion);
        var preparation=BuildWarmup();
        lightController = LightAnimation();
        CastAnimation();
        var hit = BuildHit();
        var skills = new FireSkillSettings[3];
        for (int i = 0; i < skills.Length; i++)
        {
            var kind = (PlayerAttack.ActionKind)(i + 5);
            var piece = AssetDatabase.LoadAssetAtPath<AttackPiece>("Assets/_Project/Settings/Skills/" + kind + ".asset");
            var action = piece.action;
            action.hitActiveTime = i == 1 ? 0.40f : i == 2 ? 0.42f : 0.24f;
            action.hitTravelTime = i == 1 || i == 2 ? 0.30f : 0f;
            action.hitLength = i == 0 ? 3f : i == 1 ? 4f : 4.7f;
            action.hitRadius = i == 0 ? 0.78f : i == 1 ? 0.48f : 0.62f;
            action.hitFromHand = i == 1;
            action.hitFirstAngle = i == 0 ? 35f : i == 1 ? 0f : 22f;
            action.hitAngleStep = i == 0 ? 12f : i == 1 ? 0f : -44f;
            action.hitOrigin = i == 0 ? new Vector2(0.65f,0.28f) : i == 2 ? new Vector2(0.20f, 0.24f) : new Vector2(0.14f, 0.82f);
            action.hitPulseHeight = i == 2 ? 1.15f : 0f;
            action.duration = i == 0 ? 0.95f : i == 1 ? 1.30f : 1.05f;
            action.cancel = action.duration;
            action.impact = i == 1 ? 0.30f : 0.28f;
            action.pulseInterval = i == 0 ? 0.045f : i == 1 ? 0.18f : 0.22f;
            EditorUtility.SetDirty(piece); AssetDatabase.SaveAssetIfDirty(piece);
            var settings = Asset<FireSkillSettings>(Root + "/Profiles/" + kind + "Fire.asset");
            settings.kind = kind; settings.attack = piece; settings.hit = hit;
            settings.warmup=preparation;settings.warmupScale=i==1?0.70f:0.85f;
            settings.warmupOffset=i==0?new Vector2(0.4f,0):i==2?new Vector2(0.14f,0):Vector2.zero;
            settings.emissionTime = i == 1 ? 0.18f : 0.06f;
            settings.cleanupTime = i==2?2.8f:2.2f; settings.finalScale = 1.04f;
            settings.firstPulseOnly = i != 1;
            settings.alignToFacing = i != 1;
            settings.facingAngle = i == 0 ? 65f : 0f;
            settings.motionVariation = 0.18f;
            settings.burstOffset = i == 0 ? new Vector2(0.4f,0) : i == 2 ? new Vector2(0,0.575f) : Vector2.zero;
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
        return settings.kind == PlayerAttack.ActionKind.SwordFire ? BuildSurge(settings) : BuildExplosion(settings);
    }

    private static FireSkillBurst BuildSurge(FireSkillSettings settings)
    {
        var root = new GameObject(settings.kind + " Thermal Burst");
        root.SetActive(false);
        try
        {
            root.AddComponent<FireSkillBurst>();
            if (settings.kind == PlayerAttack.ActionKind.SwordFire) root.AddComponent<HandFlameVfx>();
            bool surge = settings.kind == PlayerAttack.ActionKind.SwordFire;
            float speed = settings.attack.action.hitLength / settings.attack.action.hitTravelTime;
            // Sparse large bodies, compact cores and broken curls have different masks,
            // depth, motion and opacity. More systems alone do not create visible layers.
            var envelope = Layer(root, "Flame Body", flame, 0.48f, speed, 0.88f, new Color(1f,0.27f,0.025f,0.92f), 0, 3);
            Waves(envelope,3,2);
            Cone(envelope, surge ? 13 : 24, 0.11f); Sheet(envelope); Growth(envelope,0.48f,1.45f);
            FireColor(envelope, new Color(1f,0.55f,0.18f),new Color(0.55f,0.065f,0.015f),1f);
            Turbulence(envelope,0.38f,2.4f,0.18f); Slow(envelope); WorldCollision(envelope,false);
            var inner = Layer(root, "Flame Hot Pockets", core,0.34f,speed*1.08f,0.38f,new Color(1.05f,0.80f,0.38f,0.9f),0,3);
            Waves(inner,3,2);
            Cone(inner,7f,0.055f);Sheet(inner);Growth(inner,0.4f,0.8f);Slow(inner);
            FireColor(inner,Color.white,new Color(1f,0.32f,0.055f),0.95f);Turbulence(inner,0.20f,4.5f,-0.12f);WorldCollision(inner,false);
            inner.transform.localPosition = Vector3.up * -0.10f;
            var tongues = Layer(root,"Flame Rolling Tongues",wisps,0.60f,speed*0.82f,1.05f,new Color(1f,0.39f,0.045f,0.64f),0,3,0.025f);
            Waves(tongues,3,2);
            Cone(tongues,surge?18:30,0.16f);Growth(tongues,0.55f,1.65f);Slow(tongues);
            FireColor(tongues,new Color(1f,0.8f,0.4f),new Color(0.42f,0.10f,0.045f),0.70f);
            Turbulence(tongues,0.62f,2.9f,-0.28f);WorldCollision(tongues,false);
            tongues.transform.localPosition=Vector3.up*0.10f;
            var ignition=Layer(root,"Hand Ignition",flash,0.13f,0,0.60f,new Color(1.3f,0.82f,0.40f,0.85f),0,2);
            Growth(ignition,0.4f,1.2f);
            BuildWake(envelope,tongues);
            var embers=Layer(root,"Cooling Coals",ember,0.95f,speed*0.33f,0.075f,new Color(1.3f,0.30f,0.025f,0.8f),0,8,0.08f);
            Cone(embers,32,0.14f);Growth(embers,1,0.2f);FireColor(embers,Color.white,new Color(0.32f,0.065f,0.025f),0.8f);Turbulence(embers,0.20f,4f,0.12f);WorldCollision(embers,true);
            var lightObject = new GameObject("Thermal Light"); lightObject.transform.SetParent(root.transform, false);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1f, 0.29f, 0.055f);
            lightObject.transform.localPosition=Vector3.forward*1.1f+Vector3.up*-0.9f;
            light.range = 4.5f; light.intensity = 0f; light.shadows = LightShadows.None;light.renderMode=LightRenderMode.ForcePixel;
            root.AddComponent<Animator>().runtimeAnimatorController = lightController;
            string path = Root + "/Prefabs/" + settings.kind + "ThermalBurst.prefab";
            MechaRenderSetup.ConfigureSurfaceDepth(root);
            return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<FireSkillBurst>();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static FireSkillBurst BuildExplosion(FireSkillSettings settings)
    {
        bool cross=settings.kind==PlayerAttack.ActionKind.CriticalX;
        var root=new GameObject(settings.kind+" Instant Fire Burst");root.SetActive(false);
        try
        {
            root.AddComponent<FireSkillBurst>();
            // Native box volumes reveal the silhouette at once instead of feeding a jet.
            int arms=cross?2:1;
            for(int i=0;i<arms;i++)
            {
                var arm=new GameObject(cross?(i==0?"Rising Blast":"Falling Blast"):"Upper Blast");
                arm.transform.SetParent(root.transform,false);
                if(cross)
                {
                    arm.transform.localPosition=Vector3.forward*0.14f+Vector3.right*(i==0?-0.575f:0.575f);
                    arm.transform.localRotation=Quaternion.Euler(0,i==0?22:-22,0);
                }
                var body=Layer(arm,"Flame Explosion Body",flame,cross?0.56f:0.43f,cross?16f:2.8f,cross?0.66f:0.82f,new Color(1f,0.36f,0.035f,0.92f),0,cross?6:13);
                if(cross){Cone(body,5,0.10f);Waves(body,6,3);}else Volume(body,1.30f,2.35f,0.68f);
                Sheet(body);Growth(body,0.82f,1.1f);FireColor(body,Color.white,new Color(0.68f,0.14f,0.035f),1f);
                Turbulence(body,0.30f,3f,0.10f);Slow(body);WorldCollision(body,false);
                var hot=Layer(arm,"Flame Explosion Pockets",core,cross?0.42f:0.24f,cross?17f:3.3f,0.33f,new Color(1.2f,0.94f,0.55f,0.95f),0,cross?4:7);
                if(cross){Cone(hot,3,0.06f);Waves(hot,4,2);}else Volume(hot,1.1f,1.65f,0.6f);hot.transform.localPosition=Vector3.up*-0.10f;
                Sheet(hot);Growth(hot,0.9f,0.65f);FireColor(hot,Color.white,new Color(1,0.4f,0.08f),0.9f);Turbulence(hot,0.16f,4.5f,-0.1f);Slow(hot);WorldCollision(hot,false);
                var curls=Layer(arm,"Flame Explosion Curls",wisps,cross?0.64f:0.52f,cross?13.5f:2.1f,cross?0.76f:0.8f,new Color(1f,0.46f,0.055f,0.64f),0,cross?4:8,0.015f);
                if(cross){Cone(curls,7,0.12f);Waves(curls,4,2);}else Volume(curls,1.35f,2.4f,0.70f);
                Growth(curls,0.7f,1.15f);FireColor(curls,Color.white,new Color(0.65f,0.15f,0.04f),0.7f);Turbulence(curls,0.35f,2.9f,-0.16f);Slow(curls);WorldCollision(curls,false);
                if(cross)
                {
                    LingeringFire(body,0.56f,1.05f,1f);
                    LingeringFire(hot,0.42f,0.78f,0.90f);
                    LingeringFire(curls,0.64f,1.18f,0.70f);
                }
                if(cross)
                    foreach(var ps in arm.GetComponentsInChildren<ParticleSystem>())
                    {var main=ps.main;main.startDelay=main.startDelay.constantMax+i*0.22f;}
                BuildWake(body,curls);
            }
            var ignition=Layer(root,"Blast Ignition",flash,0.10f,0,0.50f,new Color(1.1f,0.75f,0.30f,0.8f),0,1);
            ignition.transform.localPosition=Vector3.forward*(cross?1.25f:0.40f);Growth(ignition,1,0.6f);
            var coals=Layer(root,"Burst Coals",ember,0.62f,3f,0.055f,new Color(1.1f,0.25f,0.025f,0.85f),0,7,0.04f);
            coals.transform.localPosition=Vector3.forward*(cross?1.25f:0.75f);Cone(coals,45,0.2f);Growth(coals,1,0.2f);Turbulence(coals,0.20f,3,0.1f);WorldCollision(coals,true);
            var lightObject=new GameObject("Thermal Light");lightObject.transform.SetParent(root.transform,false);
            lightObject.transform.localPosition=Vector3.forward*(cross?2f:0.6f)+Vector3.up*-0.9f;
            var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,0.42f,0.10f);light.range=4.5f;light.intensity=0;light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForcePixel;
            root.AddComponent<Animator>().runtimeAnimatorController=lightController;
            MechaRenderSetup.ConfigureSurfaceDepth(root);
            return PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+settings.kind+"ThermalBurst.prefab").GetComponent<FireSkillBurst>();
        }
        finally {UnityEngine.Object.DestroyImmediate(root);}
    }

    private static void BuildWake(ParticleSystem body,ParticleSystem outer)
    {
        Wake(body,smoke,ParticleSystemSubEmitterType.Birth,0.40f,0.70f,0.38f,new Color(0.24f,0.23f,0.22f,0.30f));
        Wake(body,rearSmoke,ParticleSystemSubEmitterType.Death,0.75f,0.90f,0.48f,new Color(0.44f,0.45f,0.47f,0.42f));
        Wake(outer,rearSmoke,ParticleSystemSubEmitterType.Death,0.55f,0.85f,0.52f,new Color(0.47f,0.48f,0.50f,0.35f));
    }

    private static void Wake(ParticleSystem parent,Material material,ParticleSystemSubEmitterType trigger,float probability,float life,float size,Color tint)
    {
        var wake=Layer(parent.gameObject,"Rear Wake "+trigger,material,life,0.12f,size,tint,0,1);
        var shape=wake.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;
        shape.scale=new Vector3(0.12f,0.04f,0.12f);shape.position=new Vector3(0,0.72f,-0.03f);
        var inherit=wake.inheritVelocity;inherit.enabled=true;inherit.mode=ParticleSystemInheritVelocityMode.Initial;inherit.curve=0.16f;
        Growth(wake,0.7f,1.6f);SmokeColor(wake);Rise(wake,0.23f);Turbulence(wake,0.10f,1.3f,0);Spin(wake,0.2f);
        var children=parent.subEmitters;children.enabled=true;children.AddSubEmitter(wake,trigger,ParticleSystemSubEmitterProperties.InheritNothing,probability);
    }

    private static FireSourceWarmup BuildWarmup()
    {
        var root=new GameObject("Source Heat Preparation");root.SetActive(false);
        try
        {
            root.AddComponent<FireSourceWarmup>();
            var heat=Layer(root,"Warmup Source Distortion",distortion,0.40f,0.10f,0.70f,new Color(1,1,1,0.55f),0,2);
            Cone(heat,15,0.06f);Growth(heat,0.35f,1.1f);
            var glow=Layer(root,"Warmup Source Ignition",ignition,0.36f,0.28f,0.50f,new Color(1f,0.52f,0.055f,0.90f),0,2);
            Cone(glow,18,0.05f);Growth(glow,0.25f,1.15f);Turbulence(glow,0.08f,3,0.12f);
            var emission=glow.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(0,2),new ParticleSystem.Burst(0.12f,3)});
            var charge=new Gradient();charge.SetKeys(new[]{new GradientColorKey(new Color(0.8f,0.25f,0.03f),0),new GradientColorKey(new Color(1.3f,1.1f,0.35f),0.65f),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(0.25f,0),new GradientAlphaKey(0.95f,0.65f),new GradientAlphaKey(0.5f,1)});
            var color=glow.colorOverLifetime;color.color=charge;
            foreach(var ps in root.GetComponentsInChildren<ParticleSystem>())
            {var main=ps.main;main.simulationSpace=ParticleSystemSimulationSpace.Local;}
            MechaRenderSetup.ConfigureSurfaceDepth(root);
            return PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/SourceHeatPreparation.prefab").GetComponent<FireSourceWarmup>();
        }
        finally {UnityEngine.Object.DestroyImmediate(root);}
    }

    private static FireSkillHitVfx BuildHit()
    {
        var root = new GameObject("Thermal Armor Contact"); root.SetActive(false);
        try
        {
            root.AddComponent<FireSkillHitVfx>();
            var flare = Layer(root, "Contact Flash", flash, 0.10f, 0f, 0.48f, new Color(1.5f, 0.8f, 0.3f), 0, 1);
            Growth(flare, 0.6f, 1f);
            var flameHit=Layer(root,"Flame Surface Break",flame,0.32f,2.5f,0.34f,new Color(1.05f,0.30f,0.025f,0.85f),0,3);
            Cone(flameHit,55,0.10f);Sheet(flameHit);Growth(flameHit,0.5f,1.45f);FireColor(flameHit,Color.white,new Color(0.6f,0.07f,0.015f),0.85f);Turbulence(flameHit,0.3f,3,0.15f);
            var rolling=Layer(root,"Flame Contact Curls",wisps,0.42f,1.8f,0.42f,new Color(1f,0.4f,0.06f,0.55f),0,2,0.03f);
            Cone(rolling,60,0.14f);Growth(rolling,0.45f,1.5f);Turbulence(rolling,0.4f,3,-0.15f);FireColor(rolling,Color.white,new Color(0.5f,0.13f,0.045f),0.7f);
            var coals=Layer(root,"Incoming Sparks",ember,0.75f,3.8f,0.065f,new Color(1.4f,0.50f,0.055f),0,7);
            Cone(coals,35,0.06f);WorldCollision(coals,true);Turbulence(coals,0.18f,3,0.1f);Growth(coals,1,0.2f);
            BuildWake(flameHit,rolling);
            MechaRenderSetup.ConfigureSurfaceDepth(root);
            return PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/ThermalArmorContact.prefab").GetComponent<FireSkillHitVfx>();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static ParticleSystem Layer(GameObject root, string name, Material material, float life, float speed, float size, Color tint, float rate, int count, float delay = 0f)
    {
        var go = new GameObject(name); go.transform.SetParent(root.transform, false);
        go.layer = CombatVfxRendering.Layer;
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = false; main.playOnAwake = false; main.duration = 0.30f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.82f, life); main.startDelay = delay;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.88f, speed); main.startSize = new ParticleSystem.MinMaxCurve(size * 0.8f, size * 1.2f);
        main.startColor = tint; main.maxParticles = 96; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);
        main.scalingMode = ParticleSystemScalingMode.Shape; main.stopAction = ParticleSystemStopAction.None;
        var emission = ps.emission; emission.rateOverTime = rate; if (count > 0) emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
        var shape = ps.shape; shape.enabled = false;
        var color = ps.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient(); gradient.SetKeys(new[] {new GradientColorKey(Color.white,0f),new GradientColorKey(new Color(0.85f,0.65f,0.35f),0.65f),new GradientColorKey(new Color(0.2f,0.12f,0.07f),1f)},
            new[] {new GradientAlphaKey(0f,0f),new GradientAlphaKey(1f,0.08f),new GradientAlphaKey(0.75f,0.55f),new GradientAlphaKey(0f,1f)}); color.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; renderer.sortingFudge = 0f;
        renderer.SetActiveVertexStreams(new System.Collections.Generic.List<ParticleSystemVertexStream> {ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV,ParticleSystemVertexStream.StableRandomX});
        return ps;
    }
    private static void Cone(ParticleSystem ps, float angle, float radius) { var s=ps.shape;s.enabled=true;s.shapeType=ParticleSystemShapeType.Cone;s.angle=angle;s.radius=radius;s.arc=360f; }
    private static void Volume(ParticleSystem ps,float length,float width,float center) {var s=ps.shape;s.enabled=true;s.shapeType=ParticleSystemShapeType.Box;s.scale=new Vector3(width,0.08f,length);s.position=Vector3.forward*center;}
    private static void Waves(ParticleSystem ps,short first,short second) {var e=ps.emission;e.SetBursts(new[]{new ParticleSystem.Burst(0,first),new ParticleSystem.Burst(0.075f,second)});}
    private static void Sheet(ParticleSystem ps,int tiles=2) { var s=ps.textureSheetAnimation;s.enabled=true;s.numTilesX=tiles;s.numTilesY=tiles;s.frameOverTime=new ParticleSystem.MinMaxCurve(1f,AnimationCurve.Linear(0f,0f,1f,1f));s.startFrame=new ParticleSystem.MinMaxCurve(0f,0.75f);s.cycleCount=1; }
    private static void Growth(ParticleSystem ps,float from,float to) {var s=ps.sizeOverLifetime;s.enabled=true;s.size=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0f,from),new Keyframe(0.25f,1f),new Keyframe(0.75f,to),new Keyframe(1f,to*0.65f)));}
    private static void FireColor(ParticleSystem ps,Color peak,Color cooled,float opacity)
    {
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(peak,0),new GradientColorKey(Color.white,0.20f),new GradientColorKey(new Color(1f,0.78f,0.40f),0.62f),new GradientColorKey(cooled,1)},
            new[]{new GradientAlphaKey(0.45f*opacity,0),new GradientAlphaKey(opacity,0.08f),new GradientAlphaKey(opacity*0.65f,0.48f),new GradientAlphaKey(0,1)});
        var alternate=new Gradient();alternate.SetKeys(new[]{new GradientColorKey(new Color(1.08f,0.95f,0.75f),0),new GradientColorKey(new Color(1.05f,0.88f,0.58f),0.35f),new GradientColorKey(new Color(1f,0.62f,0.22f),0.70f),new GradientColorKey(cooled*0.85f,1)},gradient.alphaKeys);
        var module=ps.colorOverLifetime;module.enabled=true;module.color=new ParticleSystem.MinMaxGradient(gradient,alternate);
        var rotation=ps.rotationOverLifetime;rotation.enabled=true;rotation.z=new ParticleSystem.MinMaxCurve(-0.65f,0.65f);
    }
    private static void SmokeColor(ParticleSystem ps)
    {
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(0.52f,0.45f,0.40f),0),new GradientColorKey(Color.white,0.40f),new GradientColorKey(new Color(0.72f,0.75f,0.80f),1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(0.75f,0.16f),new GradientAlphaKey(0.30f,0.48f),new GradientAlphaKey(0,1)});
        var module=ps.colorOverLifetime;module.enabled=true;module.color=gradient;
    }
    private static void Turbulence(ParticleSystem ps,float amplitude,float frequency,float orbit)
    {
        var n=ps.noise;n.enabled=true;n.separateAxes=true;n.strengthX=amplitude;n.strengthY=amplitude*0.20f;n.strengthZ=amplitude*0.65f;
        n.frequency=frequency;n.scrollSpeed=new ParticleSystem.MinMaxCurve(0.9f,2.5f);n.octaveCount=2;n.damping=true;n.quality=ParticleSystemNoiseQuality.Medium;
        var v=ps.velocityOverLifetime;v.enabled=true;v.space=ParticleSystemSimulationSpace.Local;v.orbitalY=orbit;
    }
    private static void Slow(ParticleSystem ps)
    {
        var v=ps.velocityOverLifetime;v.enabled=true;
        v.speedModifier=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,1),new Keyframe(0.25f,0.85f),new Keyframe(0.7f,0.18f),new Keyframe(1,0.03f)));
    }
    private static void LingeringFire(ParticleSystem ps,float previousLife,float life,float opacity)
    {
        var main=ps.main;main.startLifetime=new ParticleSystem.MinMaxCurve(life*0.82f,life);
        // Keep the original travel interval, then cool in place instead of extending reach.
        float ratio=previousLife/life;
        var velocity=ps.velocityOverLifetime;velocity.speedModifier=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(
            new Keyframe(0,1),new Keyframe(0.25f*ratio,0.85f),new Keyframe(0.7f*ratio,0.18f),new Keyframe(ratio,0),new Keyframe(1,0)));
        var color=ps.colorOverLifetime;var colors=color.color;
        var gradient=colors.gradientMin;var alternate=colors.gradientMax;
        var alpha=new[]{new GradientAlphaKey(opacity*0.45f,0),new GradientAlphaKey(opacity,0.06f),new GradientAlphaKey(opacity*0.9f,0.38f),
            new GradientAlphaKey(opacity*0.65f,0.68f),new GradientAlphaKey(opacity*0.20f,0.9f),new GradientAlphaKey(0,1)};
        gradient.alphaKeys=alpha;alternate.alphaKeys=alpha;color.color=new ParticleSystem.MinMaxGradient(gradient,alternate);
    }
    private static void Rise(ParticleSystem ps,float lift) {var f=ps.forceOverLifetime;f.enabled=true;f.space=ParticleSystemSimulationSpace.World;f.y=lift;}
    private static void Spin(ParticleSystem ps,float rate) {var r=ps.rotationOverLifetime;r.enabled=true;r.z=new ParticleSystem.MinMaxCurve(-rate,rate);}
    private static void WorldCollision(ParticleSystem ps,bool bounce) {var c=ps.collision;c.enabled=true;c.type=ParticleSystemCollisionType.World;c.mode=ParticleSystemCollisionMode.Collision3D;c.collidesWith=(1<<6)|(1<<7);c.quality=ParticleSystemCollisionQuality.Medium;c.maxCollisionShapes=16;c.radiusScale=0.1f;c.bounce=bounce?0.4f:0f;c.dampen=0.45f;c.lifetimeLoss=bounce?0.25f:1f;var m=ps.main;m.gravityModifier=bounce?0.35f:0f;}
    private static Material Thermal(string name, Shader shader, Texture2D mask, Texture2D noise, float cutoff, float erosion, float hot,float flow=0)
    {
        string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;
        m.SetTexture("_BaseMap",mask);m.SetTexture("_NoiseMap",noise);m.SetFloat("_Cutoff",cutoff);m.SetFloat("_NoiseStrength",erosion);m.SetFloat("_CoreStrength",hot);
        m.SetTexture("_HeatMap",heatNoise);m.SetFloat("_EmissionGain",hot==0?1f:name=="HotCore"?2.0f:1.45f);
        m.SetFloat("_DensityBoost",hot==0?1.8f:2.4f);m.SetFloat("_OpacityPower",hot==0?0.95f:0.70f);
        m.SetFloat("_CoreThreshold",0.52f);m.SetFloat("_Feather",hot==0?0.065f:0.012f);
        m.SetFloat("_FadeErosion",hot==0?0.10f:0.26f);m.SetFloat("_FlowStrength",flow);m.SetFloat("_DensityDetail",hot==0?0.16f:0.22f);
        m.SetFloat("_ShadowLevel",hot==0?1f:0.42f);m.SetFloat("_CelBlend",hot==0?0f:0.65f);m.SetColor("_CoreColor",new Color(1.9f,1.45f,0.25f));
        m.renderQueue=hot==0?2980:name=="HotCore"?3002:3000;
        EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);return m;
    }
    private static AnimatorController LightAnimation()
    {
        string clipPath=Root+"/Animations/ThermalLight.anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}
        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("Thermal Light",typeof(Light),"m_Intensity"),new AnimationCurve(new Keyframe(0,0),new Keyframe(0.015f,12f),new Keyframe(0.10f,8f),new Keyframe(0.22f,5f),new Keyframe(0.28f,10f),new Keyframe(0.38f,3f),new Keyframe(0.48f,0)));
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
                var keys=source.keys;for(int i=0;i<keys.Length;i++){var k=keys[i];k.time=k.time/Mathf.Max(idle.length,0.01f)*1.30f;keys[i]=k;}
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
            var curve=new AnimationCurve(new Keyframe(0,rest[i]),new Keyframe(0.06f,aim[i]),new Keyframe(1.05f,aim[i]),new Keyframe(1.30f,rest[i]));
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+channel),curve);
        }
    }
    private static T Asset<T>(string path) where T:ScriptableObject {var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a==null){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);}return a;}
    private static Texture2D CopyTexture(string source,string name)
    {
        string path=Root+"/Textures/"+name+".png";if(AssetDatabase.LoadAssetAtPath<Texture2D>(path)==null && !AssetDatabase.CopyAsset(source,path))throw new InvalidOperationException("Could not copy "+source);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=name.EndsWith("Noise")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.sRGBTexture=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=!name.EndsWith("Cells");importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static void Folder(string path) {if(AssetDatabase.IsValidFolder(path))return;Folder(System.IO.Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path).Replace('\\','/'),System.IO.Path.GetFileName(path));}
}
