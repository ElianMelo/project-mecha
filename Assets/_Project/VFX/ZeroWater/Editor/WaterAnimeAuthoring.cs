using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Explicit native authoring. All visual ingredients come from GabrielAguiarProductions.
public static class WaterAnimeAuthoring
{
    const string Root = "Assets/_Project/VFX/ZeroWater";
    const string Source = "Assets/GabrielAguiarProductions";
    static Material body, foam, flow;
    static Mesh sphere, cylinder;
    static readonly string[] Names = { "BubblePreparation", "BubbleDetonation", "FountainPreparation", "RisingFountain", "WaterModeAura" };
    static AnimationCurve Curve(params float[] values) => new AnimationCurve(Enumerable.Range(0, values.Length / 2)
        .Select(i => new Keyframe(values[i * 2], values[i * 2 + 1])).ToArray());
    static Mesh Mesh(string name) => AssetDatabase.LoadAllAssetsAtPath(Source + "/Models/" + name + ".fbx").OfType<Mesh>().First();
    static Material Material(string name, Color blue, Color white, string texture, Vector2 tiling, Vector2 speed)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(Source + "/Shaders/TsunamiShader_lite.shadergraph");
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.shader = shader;
        material.SetColor("_BaseColor", blue); material.SetColor("_MainTexColor", white);
        material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "/Textures/" + texture + ".png"));
        material.SetVector("_MainTexTiling", tiling); material.SetVector("_MainTexSpeed", speed);
        material.SetFloat("_SoftEdgeFactor", 80); material.renderQueue = 3000;
        EditorUtility.SetDirty(material); return material;
    }
    static ParticleSystem Layer(GameObject root, string name, Material material, Mesh mesh, Vector3 point,
        Vector3 size, float life, int count = 1, float delay = 0, float speed = 0, bool loop = false)
    {
        var child = new GameObject(name); child.transform.SetParent(root.transform, false);
        child.layer = CombatVfxRendering.Layer; child.transform.localPosition = point;
        var ps = child.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.duration = 1; main.loop = loop; main.playOnAwake = true; main.startDelay = delay;
        main.startLifetime = life; main.startSpeed = speed; main.startSize3D = true;
        main.startSizeX = size.x; main.startSizeY = size.y; main.startSizeZ = size.z;
        main.startColor = Color.white; main.maxParticles = 64; main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.simulationSpace = loop ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
        var emission = ps.emission; emission.rateOverTime = loop ? count : 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)(loop ? mesh == sphere && speed > 0 ? 4 : 1 : count)) });
        var shape = ps.shape; shape.enabled = false;
        var scale = ps.sizeOverLifetime; scale.enabled = true;
        scale.size = new ParticleSystem.MinMaxCurve(1, Curve(0, .25f, .12f, 1, .72f, 1, 1, 0));
        var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = mesh;
        renderer.alignment = ParticleSystemRenderSpace.Local; renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return ps;
    }
    static void Column(GameObject root, string name, Vector3 point, float width, float height, float life, bool loop = false)
    {
        // Gabriel's cylinder runs from Z=0 to Z=2. Rotate its source axis into world up.
        var ps = Layer(root, name, body, cylinder, point, new Vector3(width * .5f, width * .5f, height * .5f), life, 1, 0, 0, loop);
        ps.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        var scale = ps.sizeOverLifetime; scale.separateAxes = true;
        scale.x = scale.y = new ParticleSystem.MinMaxCurve(1, Curve(0, .45f, .1f, 1, .82f, 1, 1, .05f));
        scale.z = new ParticleSystem.MinMaxCurve(1, Curve(0, .04f, .22f, 1, .75f, 1, 1, .04f));
        if (loop) { var main = ps.main; main.startLifetime = .8f; var emission = ps.emission; emission.rateOverTime = 1.25f; }
    }
    static void Spray(GameObject root, string name, Vector3 point, int count, float life, float speed, float radius,
        float delay = 0, bool loop = false, bool radial = false)
    {
        var ps = Layer(root, name, foam, sphere, point, new Vector3(.11f, .18f, .11f), life, count, delay, speed, loop);
        ps.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        var shape = ps.shape; shape.enabled = true;
        shape.shapeType = radial ? ParticleSystemShapeType.Sphere : ParticleSystemShapeType.Cone;
        shape.radius = radius; shape.angle = 32;
        var main = ps.main; main.gravityModifier = radial ? .65f : .85f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * .65f, speed);
        var noise = ps.noise; noise.enabled = true; noise.strength = .08f; noise.frequency = 1.8f;
    }
    static void Build(GameObject root)
    {
        for (int i = root.transform.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        switch (root.name)
        {
            case "BubblePreparation":
            {
                var ps = Layer(root, "Confined blue water", body, sphere, Vector3.zero, Vector3.one * 1.03f, .32f);
                var scale = ps.sizeOverLifetime; scale.size = new ParticleSystem.MinMaxCurve(1, Curve(0, .05f, .85f, 1, 1, 1.05f));
                var skin = Layer(root, "Emissive foam skin", foam, sphere, new Vector3(0, 0, -.015f), Vector3.one * 1.045f, .32f);
                var size = skin.sizeOverLifetime; size.size = scale.size;
                break;
            }
            case "BubbleDetonation":
            {
                var ps = Layer(root, "Pressure release volume", body, sphere, Vector3.zero, new Vector3(1.6f, 1.35f, .65f), .38f);
                var scale = ps.sizeOverLifetime; scale.size = new ParticleSystem.MinMaxCurve(1, Curve(0, .45f, .28f, 1, .75f, 1.2f, 1, 0));
                Spray(root, "White hydraulic breakup", Vector3.zero, 32, .65f, 4.1f, .45f, .025f, false, true);
                var crest = Layer(root, "Broken cyan pressure front", flow, sphere, Vector3.zero, new Vector3(1.65f, 1.4f, .7f), .43f);
                var size = crest.sizeOverLifetime; size.size = scale.size;
                break;
            }
            case "FountainPreparation":
                for (int i = 0; i < 4; i++)
                {
                    float x = (i - 1.5f) * 1.45f;
                    Column(root, "Pressurized nozzle " + i, new Vector3(x, .02f, 0), .58f, .4f, .3f);
                    Spray(root, "Nozzle foam " + i, new Vector3(x, .08f, -.1f), 8, .28f, 1.1f, .22f);
                }
                break;
            case "RisingFountain":
                for (int i = 0; i < 4; i++)
                {
                    float x = (i - 1.5f) * 1.45f;
                    Column(root, "Solid rising water " + i, new Vector3(x, .02f, .08f), .85f, 2.75f, 1.3f);
                    var crest = Layer(root, "White broken crest " + i, foam, sphere, new Vector3(x, 2.65f, -.06f), new Vector3(.47f, .22f, .35f), 1.05f, 1, .19f);
                    var size = crest.sizeOverLifetime; size.size = new ParticleSystem.MinMaxCurve(1, Curve(0, .4f, .12f, 1, .75f, 1, 1, 0));
                    Spray(root, "Crest droplets " + i, new Vector3(x, 2.65f, -.1f), 8, .45f, 1.8f, .25f, .2f);
                    var drops = root.transform.Find("Crest droplets " + i).GetComponent<ParticleSystem>();
                    var emission = drops.emission; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 8, 4, .22f) });
                    Spray(root, "Base foam " + i, new Vector3(x, .12f, -.1f), 12, .5f, 1.5f, .35f, .05f);
                }
                break;
            case "WaterModeAura":
                for (int i = -1; i <= 1; i += 2)
                {
                    Column(root, "Recirculating water " + i, new Vector3(i * .65f, .05f, .22f), .34f, 1.7f, .8f, true);
                    Spray(root, "Luminous coolant vent " + i, new Vector3(i * .58f, .12f, -.08f), 10, .65f, 1.7f, .1f, 0, true);
                }
                var orbit = Layer(root, "Flowing water envelope", flow, sphere, new Vector3(0, .85f, .3f), new Vector3(.83f, 1.05f, .55f), .8f, 1, 0, 0, true);
                var envelope = orbit.sizeOverLifetime; envelope.size = new ParticleSystem.MinMaxCurve(1, Curve(0, .9f, .5f, 1, 1, .9f));
                var feed = orbit.emission; feed.rateOverTime = 1.25f;
                break;
        }
    }
    public static object ApplyAll()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Author water in Edit Mode.");
        sphere = Mesh("Sphere01"); cylinder = Mesh("Cylinder");
        body = Material("AnimeWaterBody", new Color(.015f, .32f, 1.25f, .85f), new Color(.18f, 1.15f, 1.8f, .15f), "Water01", new Vector2(2, 1), new Vector2(.12f, -1.25f));
        foam = Material("AnimeWaterFoam", Color.clear, new Color(2.4f, 3.0f, 3.2f, 1), "Water02", new Vector2(1.2f, 1), new Vector2(.13f, -.5f));
        flow = Material("AnimeWaterFlow", Color.clear, new Color(.18f, 1.65f, 2.5f, .9f), "Water01", new Vector2(1.5f, 1), new Vector2(.1f, -1.4f));
        foreach (var name in Names)
        {
            string path = Root + "/Prefabs/" + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try { Build(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith(Root + "/Materials/Anime") ||
            p == Root + "/Editor/WaterAnimeAuthoring.cs" || Names.Any(n => p == Root + "/Prefabs/" + n + ".prefab")).ToArray();
        MechaAssetLabels.Apply(paths, 0, paths.Length);
        foreach (var path in paths)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path); if (asset == null) continue;
            var labels = AssetDatabase.GetLabels(asset).Where(l => l != "Keep");
            if (path.EndsWith(".cs")) labels = labels.Where(l => l != "Others").Append("Code");
            AssetDatabase.SetLabels(asset, labels.Append("Replace").Distinct().ToArray());
        }
        foreach (var path in new[] { "Textures/Water01.png", "Textures/Water02.png", "Models/Sphere01.fbx", "Models/Cylinder.fbx", "Shaders/TsunamiShader_lite.shadergraph" })
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(Source + "/" + path);
            AssetDatabase.SetLabels(asset, AssetDatabase.GetLabels(asset).Where(l => l != "Reference").Append("VFXSource").Distinct().ToArray());
        }
        AssetDatabase.SaveAssets();
        return Names.Select(n => new { effect = n, ingredients = AssetDatabase.GetDependencies(Root + "/Prefabs/" + n + ".prefab", true)
            .Where(p => p.EndsWith(".png") || p.EndsWith(".fbx") || p.EndsWith(".shadergraph") || p.EndsWith(".shader")).ToArray() }).ToArray();
    }
}
