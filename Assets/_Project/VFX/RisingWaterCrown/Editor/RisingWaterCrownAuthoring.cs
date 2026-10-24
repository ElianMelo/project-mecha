using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Independent authoring from raw Gabriel ingredients. Does not inspect another project effect.
public static class RisingWaterCrownAuthoring
{
    public const string Root = "Assets/_Project/VFX/RisingWaterCrown";
    private const string Source = "Assets/GabrielAguiarProductions";

    [MenuItem("Mecha/VFX/Build rising water crown")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Author the crown in Edit Mode.");
        EnsureFolder(Root + "/Materials");
        EnsureFolder(Root + "/Animation");
        var profile = AssetDatabase.LoadAssetAtPath<RisingWaterCrownSettings>(Root + "/RisingWaterCrown.asset");
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<RisingWaterCrownSettings>();
            AssetDatabase.CreateAsset(profile, Root + "/RisingWaterCrown.asset");
        }
        // Vendor liquid shaders use a radial splash mask and coupled color/opacity. The crown
        // needs independently readable sheets, scrolling bands and crest-only foam instead.
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/CrownWater.shader");
        if (shader == null || !shader.isSupported) throw new InvalidOperationException("Crown water shader unavailable.");
        var body = Material("DeepWater", shader, profile.bodyColor, 0);
        body.SetColor("_FlowColor", profile.flowColor);
        body.SetColor("_FoamColor", profile.foamColor);
        var foam = Material("BrokenFoam", shader, profile.foamColor, 3);
        var drops = Material("Droplet", shader, profile.flowColor * .8f, 3);
        var gatheringMaterial = Material("Gathering", shader, profile.flowColor * .55f, 3);
        var meshes = new[] { Mesh("TsunamiWave01"), Mesh("TsunamiWave02"), Mesh("TsunamiWave03") };
        var dropletMesh = Mesh("Sphere02");
        var gatherMesh = Mesh("PlaneTrail01");
        var root = new GameObject("RisingWaterCrown");
        try
        {
            root.layer = LayerMask.NameToLayer("TransparentFX");
            var effect = root.AddComponent<RisingWaterCrownEffect>();
            var animator = root.AddComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var rise = new AnimationClip { name = "CrownRise", frameRate = 60f };
            var drain = new AnimationClip { name = "CrownDrain", frameRate = 60f };
            var startup = new List<ParticleSystem>();
            var pressure = new List<ParticleSystem>();
            float[] angles = { -117f, -59f, 3f, 64f, 123f };
            float[] heights = { .82f, 1.02f, .92f, 1f, .77f };
            float[] widths = { .96f, 1.02f, 1.14f, 1.05f, .88f };
            for (int i = 0; i < angles.Length; i++)
            {
                float radians = angles[i] * Mathf.Deg2Rad;
                var sheet = Child(root.transform, "Sheet" + (i + 1));
                sheet.localPosition = new Vector3(Mathf.Sin(radians) * profile.radius.x, 0f, Mathf.Cos(radians) * profile.radius.y);
                sheet.localRotation = Quaternion.Euler(0f, angles[i], 0f);
                var mesh = meshes[i % meshes.Length];
                float scaleY = profile.crestHeight * heights[i] / mesh.bounds.size.y;
                float scaleX = widths[i] / mesh.bounds.extents.x;
                var layers = new[] { body };
                var names = new[] { "Body" };
                for (int layer = 0; layer < layers.Length; layer++)
                {
                    var ps = MeshSystem(sheet, names[layer], mesh, layers[layer], 1, 1.45f);
                    startup.Add(ps);
                    var path = AnimationUtility.CalculateTransformPath(ps.transform, root.transform);
                    float inset = 1f - layer * .018f;
                    float delay = i * .009f;
                    var growth = Curve(0f, .025f, .10f + delay, .08f, .28f + delay, .28f,
                        .50f + delay, 1f, .72f, 1.05f, .94f, .86f, 1.12f, .46f, 1.25f, .001f, 1.6f, .001f);
                    Float(rise, path, typeof(Transform), "m_LocalScale.x", Scaled(growth, scaleX * inset));
                    Float(rise, path, typeof(Transform), "m_LocalScale.y", Scaled(growth, scaleY * inset));
                    Float(rise, path, typeof(Transform), "m_LocalScale.z", Curve(0f, .12f, .28f, .25f,
                        .50f, .55f + layer * .015f, .72f, .82f + layer * .015f, .94f, 1f, 1.25f, .001f, 1.6f, .001f));
                    Float(rise, path, typeof(Transform), "m_LocalPosition.y", Scaled(growth, -mesh.bounds.min.y * scaleY * inset));
                    Float(rise, path, typeof(Transform), "m_LocalPosition.z", AnimationCurve.Constant(0f, 1.6f, -layer * .012f));
                    Float(rise, path, typeof(Transform), "localEulerAnglesRaw.x", Curve(0f, 0f, .5f, 0f, .94f, -12f, 1.25f, -24f, 1.6f, -24f));
                    Float(rise, path, typeof(ParticleSystemRenderer), "material._Coverage", Curve(0f, 0f,
                        .08f + delay, .08f, .24f + delay, .38f, .5f, 1f,
                        .94f, .9f, 1.12f, .5f, 1.25f, 0f, 1.6f, 0f));
                    Float(drain, path, typeof(ParticleSystemRenderer), "material._Coverage", Curve(0f, .65f, .16f, 0f, .22f, 0f));
                }
                var spray = Spray(sheet, "CrestSpray", dropletMesh, foam, .38f, 2.6f);
                spray.transform.localRotation = Quaternion.Euler(-52f, 0f, 0f);
                pressure.Add(spray);
                var sprayPath = AnimationUtility.CalculateTransformPath(spray.transform, root.transform);
                Float(rise, sprayPath, typeof(Transform), "m_LocalPosition.y", Curve(0f, .15f, .28f, .7f,
                    .5f, profile.crestHeight * heights[i], .72f, profile.crestHeight * heights[i],
                    .94f, profile.crestHeight * heights[i] * .72f, 1.25f, .1f, 1.6f, .1f));
                Float(rise, sprayPath, typeof(Transform), "m_LocalPosition.z", Curve(0f, 0f, .5f, .3f, .94f, .65f, 1.25f, .7f, 1.6f, .7f));
            }
            var gathering = MeshSystem(root.transform, "Gathering", gatherMesh, gatheringMaterial, 22, .28f);
            var gatherMain = gathering.main;
            gatherMain.startLifetime = new ParticleSystem.MinMaxCurve(.12f, .27f);
            gatherMain.startSize3D = true;
            gatherMain.startSizeX = .10f; gatherMain.startSizeY = .28f; gatherMain.startSizeZ = .10f;
            var gatherShape = gathering.shape;
            gatherShape.enabled = true; gatherShape.shapeType = ParticleSystemShapeType.Circle;
            gatherShape.radius = profile.radius.x * .7f; gatherShape.arc = 240f;
            gatherShape.rotation = new Vector3(90f, 0f, -30f);
            var gatherVelocity = gathering.velocityOverLifetime;
            gatherVelocity.enabled = true; gatherVelocity.radial = -.8f;
            var gatherSize = gathering.sizeOverLifetime;
            gatherSize.enabled = true; gatherSize.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, .2f, .25f, 1f, 1f, 0f));
            startup.Add(gathering);
            var surface = Spray(root.transform, "SurfaceSpray", dropletMesh, drops, .4f, 1.4f);
            surface.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var surfaceShape = surface.shape; surfaceShape.radius = .9f; surfaceShape.angle = 70f;
            var surfaceEmission = surface.emission;
            surfaceEmission.enabled = true;
            surfaceEmission.SetBursts(new[] { new ParticleSystem.Burst(.18f, 24) });
            for (int i = 0; i < 2; i++)
            {
                var lightTransform = Child(root.transform, "WaterLight" + (i + 1));
                lightTransform.localPosition = new Vector3(i == 0 ? -.85f : .85f, .95f, -.15f);
                var light = lightTransform.gameObject.AddComponent<Light>();
                light.type = LightType.Point; light.color = profile.lightColor;
                light.range = profile.lightRange; light.intensity = 0f;
                light.shadows = LightShadows.None; light.renderMode = LightRenderMode.ForcePixel;
                var lightPath = AnimationUtility.CalculateTransformPath(lightTransform, root.transform);
                Float(rise, lightPath, typeof(Light), "m_Intensity", Scaled(Curve(0f, 0f, .1f, .08f,
                    .28f, .7f, .36f, .28f, .5f, 1f, .59f, .38f, .72f, .9f, .81f, .32f,
                    .94f, .75f, 1.08f, .25f, 1.25f, 0f, 1.6f, 0f), profile.lightIntensity));
                Float(drain, lightPath, typeof(Light), "m_Intensity", Curve(0f, profile.lightIntensity * .3f, .12f, 0f, .22f, 0f));
            }
            var riseAsset = SaveClip(rise, Root + "/Animation/CrownRise.anim");
            var drainAsset = SaveClip(drain, Root + "/Animation/CrownDrain.anim");
            animator.runtimeAnimatorController = Controller(riseAsset, drainAsset);
            var serialized = new SerializedObject(effect);
            SetArray(serialized.FindProperty("startupSystems"), startup);
            SetArray(serialized.FindProperty("pressureSpray"), pressure);
            serialized.FindProperty("surfaceSpray").objectReferenceValue = surface;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/RisingWaterCrown.prefab");
            profile.prefab = prefab.GetComponent<RisingWaterCrownEffect>();
            EditorUtility.SetDirty(profile);
            var zero = AssetDatabase.FindAssets("t:CharacterDefinition", new[] { "Assets/_Project" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<CharacterDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .Single(character => character != null && character.id == "zero");
            if (zero.skills.Length < 2 || zero.skills[1] == null) throw new InvalidOperationException("Zero S slot unavailable.");
            zero.skills[1].presentationOverride = profile;
            EditorUtility.SetDirty(zero.skills[1]);
            foreach (var guid in AssetDatabase.FindAssets("", new[] { Root }))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null) continue;
                bool code = asset is MonoScript || asset is Shader;
                string category = code ? "Code" : asset is ScriptableObject ? "Authored Data" : asset is GameObject ? "Prefabs" : "Placeholder";
                string readiness = code || asset is ScriptableObject ? "Keep" : "Replace";
                var previousLabels = AssetDatabase.GetLabels(asset).Where(label => label != "Keep" && label != "Replace" && label != "Placeholder" && label != "Code" && label != "Prefabs" && label != "Authored Data");
                AssetDatabase.SetLabels(asset, previousLabels.Concat(new[] { readiness, category }).Distinct().ToArray());
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Rising water crown authored from Gabriel sources; assigned only to Zero S presentation.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static ParticleSystem MeshSystem(Transform parent, string name, Mesh mesh, Material material, int count, float lifetime)
    {
        var go = Child(parent, name).gameObject;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false; main.loop = false; main.duration = 1.6f;
        main.startLifetime = lifetime; main.startSpeed = 0f; main.startSize = 1f;
        main.maxParticles = Mathf.Max(count, 1); main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        var emission = ps.emission;
        emission.rateOverTime = 0f; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        var shape = ps.shape; shape.enabled = false;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = mesh;
        renderer.alignment = ParticleSystemRenderSpace.Local;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = 0f;
        return ps;
    }

    private static ParticleSystem Spray(Transform parent, string name, Mesh mesh, Material material, float lifetime, float speed)
    {
        var ps = MeshSystem(parent, name, mesh, material, 48, lifetime);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * .55f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * .65f, speed);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(.025f, .06f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(.06f, .13f);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(.025f, .06f);
        main.gravityModifier = .85f; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var shape = ps.shape;
        shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 23f; shape.radius = .22f;
        var emission = ps.emission; emission.SetBursts(Array.Empty<ParticleSystem.Burst>());
        var size = ps.sizeOverLifetime;
        size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, .7f, .15f, 1f, .75f, .75f, 1f, 0f));
        return ps;
    }

    private static Material Material(string name, Shader shader, Color color, int layer)
    {
        var path = Root + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.shader = shader;
        material.SetColor("_Color", color); material.SetFloat("_Coverage", 1f); material.SetFloat("_Layer", layer);
        material.SetTexture("_FlowMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "/Textures/BrokenNoise02.png"));
        material.SetTexture("_NoiseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "/Textures/Noise01_v2.png"));
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh Mesh(string name)
    {
        var mesh = AssetDatabase.LoadAllAssetsAtPath(Source + "/Models/" + name + ".fbx").OfType<Mesh>().FirstOrDefault();
        if (mesh == null) throw new InvalidOperationException("Gabriel source mesh missing: " + name);
        return mesh;
    }

    private static AnimatorController Controller(AnimationClip rise, AnimationClip drain)
    {
        string path = Root + "/Animation/WaterCrown.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.parameters = new[] { new AnimatorControllerParameter { name = "Drain", type = AnimatorControllerParameterType.Trigger } };
        var machine = controller.layers[0].stateMachine;
        foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
        foreach (var state in machine.states) machine.RemoveState(state.state);
        var riseState = machine.AddState("Rise"); riseState.motion = rise; riseState.writeDefaultValues = false;
        var drainState = machine.AddState("Drain"); drainState.motion = drain; drainState.writeDefaultValues = false;
        machine.defaultState = riseState;
        var stop = machine.AddAnyStateTransition(drainState);
        stop.hasExitTime = false; stop.duration = .03f; stop.canTransitionToSelf = false;
        stop.AddCondition(AnimatorConditionMode.If, 0f, "Drain");
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip SaveClip(AnimationClip clip, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null) { AssetDatabase.CreateAsset(clip, path); return clip; }
        EditorUtility.CopySerialized(clip, existing);
        UnityEngine.Object.DestroyImmediate(clip);
        EditorUtility.SetDirty(existing);
        return existing;
    }
    private static Transform Child(Transform parent, string name)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false); child.gameObject.layer = parent.gameObject.layer;
        return child;
    }
    private static void SetArray(SerializedProperty property, List<ParticleSystem> values)
    { property.arraySize = values.Count; for (int i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
    private static void Float(AnimationClip clip, string path, Type type, string property, AnimationCurve curve)
        => AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, property), curve);
    private static AnimationCurve Scaled(AnimationCurve curve, float scale)
    { return new AnimationCurve(curve.keys.Select(key => new Keyframe(key.time, key.value * scale)).ToArray()); }
    private static AnimationCurve Curve(params float[] pairs)
    {
        var keys = new Keyframe[pairs.Length / 2];
        for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(pairs[i * 2], pairs[i * 2 + 1]);
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < keys.Length; i++)
        { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto); }
        return curve;
    }
    private static void EnsureFolder(string path)
    { if (AssetDatabase.IsValidFolder(path)) return; var parent = path.Substring(0, path.LastIndexOf('/')); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1)); }
}
