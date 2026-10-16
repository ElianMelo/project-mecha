using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// VFX Graph's graph-authoring model is internal in 17.3. Use its native model/slot
// APIs through this version-specific adapter; never edit serialized graph text.
public static class MechaFireVfxSetup
{
    private const string GraphPath = "Assets/_Project/VFX/Graphs/SwordFire.vfx";
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static PropertyInfo Property(object obj, string name)
    {
        for (var type = obj.GetType(); type != null; type = type.BaseType)
        {
            var property = type.GetProperty(name, Flags | BindingFlags.DeclaredOnly);
            if (property != null) return property;
        }
        return null;
    }
    private static object Get(object obj, string name) => Property(obj, name)?.GetValue(obj);
    private static object[] Items(object obj, string name) => ((IEnumerable)Get(obj, name)).Cast<object>().ToArray();
    private static object Resource(string path) => FindType("UnityEditor.VFX.VisualEffectResource").GetMethod("GetResourceAtPath").Invoke(null, new object[] {path});
    private static object Graph(string path) => FindType("UnityEditor.VFX.VisualEffectResourceExtensions").GetMethod("GetOrCreateGraph").Invoke(null, new[] {Resource(path)});
    private static object Describe(object obj) => new {
        type = obj.GetType().Name,
        slots = Property(obj, "inputSlots") == null ? null : Items(obj, "inputSlots").Select(s => new {
            name = Get(s, "name"), value = Get(s, "value")?.ToString(), type = Get(s, "value")?.GetType().FullName,
            children = Items(s, "children").Select(c => new {name = Get(c, "name"), value = Get(c, "value")?.ToString()}).ToArray()
        }).ToArray()
    };
    public static object Inspect()
    {
        return Items(Graph(GraphPath), "children").Select(c => new {context = Describe(c), blocks = Items(c, "children").Select(Describe).ToArray()}).ToArray();
    }

    private static object Call(object obj, string name, params object[] args)
    {
        var method = obj.GetType().GetMethods(Flags).First(m => m.Name == name && m.GetParameters().Length == args.Length);
        return method.Invoke(obj, args);
    }
    private static void Setting(object obj, string name, object value)
    {
        var current = Call(obj, "GetSettingValue", name);
        if (current != null && current.GetType().IsEnum && value is string text) value = Enum.Parse(current.GetType(), text);
        Call(obj, "SetSettingValue", name, value);
    }
    private static object Slot(object node, string path)
    {
        var names = path.Split('/'); var slot = Items(node, "inputSlots").First(s => (string)Get(s, "name") == names[0]);
        foreach (string name in names.Skip(1)) slot = Items(slot, "children").First(s => (string)Get(s, "name") == name);
        return slot;
    }
    private static void Value(object node, string path, object value)
    {
        var slot = Slot(node, path);
        // Position/Vector slots wrap a float3 in distinct VFX space-aware structs.
        // Write their native child slot rather than assigning an incompatible Vector3.
        if (value is Vector3 && Get(slot, "value")?.GetType() != typeof(Vector3))
            slot = Items(slot, "children").First(s => Get(s, "value") is Vector3);
        Property(slot, "value").SetValue(slot, value);
    }
    private static object Add(object parent, string type)
    {
        var node = ScriptableObject.CreateInstance(FindType((type.StartsWith("VFX") ? "UnityEditor.VFX." : "UnityEditor.VFX.Block.") + type));
        Call(parent, "AddChild", node, -1, true); return node;
    }
    private static object Attribute(object context, string attribute, object min, object max = null)
    {
        var block = Add(context, "SetAttribute"); Setting(block, "attribute", attribute);
        if (max != null)
        {
            Setting(block, "Random", min is Vector3 ? "PerComponent" : "Uniform");
            Value(block, "A", min); Value(block, "B", max);
        }
        else Property(Items(block, "inputSlots")[0], "value").SetValue(Items(block, "inputSlots")[0], min);
        return block;
    }
    private static void Expose(object graph, object destination, string slotName, string name, Type type, object value, Vector2 position)
    {
        var parameter = ScriptableObject.CreateInstance(FindType("UnityEditor.VFX.VFXParameter"));
        Call(parameter, "Init", type); Setting(parameter, "m_ExposedName", name); Setting(parameter, "m_Exposed", true);
        Property(parameter, "value").SetValue(parameter, value); Property(parameter, "position").SetValue(parameter, position);
        Call(graph, "AddChild", parameter, -1, true);
        Call(Slot(destination, slotName), "Link", Items(parameter, "outputSlots")[0], true);
    }

    [MenuItem("Tools/Mecha/Apply Sword Fire VFX Graph")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        MechaPresentationSetup.Folder("Assets/_Project/VFX/Graphs");
        MechaPresentationSetup.Folder("Assets/_Project/VFX/Textures");
        MechaPresentationSetup.Folder("Assets/_Project/VFX/Prefabs");
        var profile = MechaPresentationSetup.Create<SwordFireVfxSettings>("SwordFireVfx");
        if (profile.flameColors == null || profile.flameColors.colorKeys.Length < 3)
        {
            profile.flameColors = Gradient(new Color(2.4f, 1.45f, 0.35f), new Color(1.7f, 0.28f, 0.015f), new Color(0.25f, 0.01f, 0f), 0.62f);
            profile.emberColors = Gradient(new Color(3f, 1.5f, 0.2f), new Color(2.5f, 0.3f, 0.01f), new Color(0.6f, 0.015f, 0f), 1f);
        }
        var texture = FlameTexture();
        ConfigureGraph(GraphPath, false, texture, profile.flameColors);
        const string emberPath = "Assets/_Project/VFX/Graphs/SwordFireEmbers.vfx";
        ConfigureGraph(emberPath, true, null, profile.emberColors);
        const string prefabPath = "Assets/_Project/VFX/Prefabs/SwordFireBurst.prefab";
        var root = new GameObject("Sword Fire Burst"); root.SetActive(false);
        try
        {
            root.AddComponent<UnityEngine.VFX.VisualEffect>().visualEffectAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.VFX.VisualEffectAsset>(GraphPath);
            var embers = new GameObject("Embers"); embers.transform.SetParent(root.transform, false);
            embers.AddComponent<UnityEngine.VFX.VisualEffect>().visualEffectAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.VFX.VisualEffectAsset>(emberPath);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        profile.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath); EditorUtility.SetDirty(profile);
        var combat = MechaPresentationSetup.Profile<CombatVfxSettings>("CombatVfx"); combat.swordFire = profile; EditorUtility.SetDirty(combat);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Mecha/Apply A and D Fire VFX Graphs")]
    public static void ApplyOtherSkills()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var combat = MechaPresentationSetup.Profile<CombatVfxSettings>("CombatVfx");
        combat.megaSlashFire = BuildSkill("MegaSlash", "Mega Slash Fire Sweep", 1, -18f, 12f);
        combat.criticalXFire = BuildSkill("CriticalX", "Critical X Fire Cut", 2, 45f, -90f);
        EditorUtility.SetDirty(combat); AssetDatabase.SaveAssets();
    }

    private static SwordFireVfxSettings BuildSkill(string name, string title, int shape, float angle, float step)
    {
        MechaPresentationSetup.Folder("Assets/_Project/VFX/Graphs");
        MechaPresentationSetup.Folder("Assets/_Project/VFX/Prefabs");
        var profile = MechaPresentationSetup.Profile<SwordFireVfxSettings>(name + "FireVfx");
        if (profile == null)
        {
            profile = MechaPresentationSetup.Create<SwordFireVfxSettings>(name + "FireVfx");
            profile.flameCount = shape == 1 ? 56 : 64; profile.emberCount = 24;
            profile.firstPulseAngle = angle; profile.pulseAngleStep = step;
            profile.lifetime = 1.1f;
            profile.flameColors = Gradient(new Color(2.8f, 1.8f, 0.45f), new Color(2f, 0.32f, 0.015f), new Color(0.3f, 0.015f, 0f), 0.65f);
            profile.emberColors = Gradient(new Color(3f, 1.8f, 0.35f), new Color(2.5f, 0.4f, 0.015f), new Color(0.5f, 0.02f, 0f), 1f);
        }
        string flamePath = "Assets/_Project/VFX/Graphs/" + name + "Fire.vfx";
        string emberPath = "Assets/_Project/VFX/Graphs/" + name + "FireEmbers.vfx";
        ConfigureGraph(flamePath, false, FlameTexture(), profile.flameColors, shape);
        ConfigureGraph(emberPath, true, null, profile.emberColors, shape);
        string prefabPath = "Assets/_Project/VFX/Prefabs/" + name + "FireBurst.prefab";
        var root = new GameObject(title); root.SetActive(false);
        try
        {
            root.AddComponent<UnityEngine.VFX.VisualEffect>().visualEffectAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.VFX.VisualEffectAsset>(flamePath);
            var embers = new GameObject("Embers"); embers.transform.SetParent(root.transform, false);
            embers.AddComponent<UnityEngine.VFX.VisualEffect>().visualEffectAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.VFX.VisualEffectAsset>(emberPath);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        profile.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath); EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void ConfigureGraph(string path, bool embers, Texture2D texture, Gradient gradient, int shape = 0)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.VFX.VisualEffectAsset>(path) == null)
            AssetDatabase.CopyAsset("Packages/com.unity.visualeffectgraph/Editor/Templates/03_Simple_Burst.vfx", path);
        var graph = Graph(path); var contexts = Items(graph, "children").Where(c => c.GetType().Name.StartsWith("VFX") && Property(c, "inputSlots") != null).ToArray();
        var spawn = contexts.First(c => c.GetType().Name == "VFXBasicSpawner");
        var initialize = contexts.First(c => c.GetType().Name == "VFXBasicInitialize");
        var update = contexts.First(c => c.GetType().Name == "VFXBasicUpdate");
        var output = contexts.First(c => c.GetType().Name == "VFXPlanarPrimitiveOutput");
        // Remove only authored blackboard parameters and per-context blocks before rebuilding.
        foreach (var parameter in Items(graph, "children").Where(c => c.GetType().Name == "VFXParameter")) Call(graph, "RemoveChild", parameter, true);
        foreach (var context in contexts)
            foreach (var block in Items(context, "children")) Call(context, "RemoveChild", block, true);
        var data = Call(initialize, "GetData"); Setting(data, "capacity", 256u);
        Setting(spawn, "loopCount", "Constant"); Value(spawn, "LoopCount", 1);
        Setting(data, "title", System.IO.Path.GetFileNameWithoutExtension(path));
        Value(initialize, "bounds/center", new Vector3(1.5f, 0.6f, 0f)); Value(initialize, "bounds/size", new Vector3(8f, 5f, 3f));
        var burst = Add(spawn, "VFXSpawnerBurst"); Value(burst, "Count", embers ? 20f : 48f); Value(burst, "Delay", 0f);
        Expose(graph, burst, "Count", embers ? "EmberCount" : "FlameCount", typeof(float), embers ? 20f : 48f, new Vector2(-180f, 40f));
        Attribute(initialize, "lifetime", embers ? 0.3f : 0.24f, embers ? 0.75f : 0.5f);
        if (shape == 0)
            Attribute(initialize, "position", new Vector3(-0.3f, -0.3f, -0.08f), new Vector3(0.25f, 0.3f, 0.08f));
        else
        {
            var position = Add(initialize, "PositionShape");
            Setting(position, "shape", shape == 1 ? "Circle" : "Line");
            Setting(position, "applyOrientation", "None");
            if (shape == 1)
            {
                Setting(position, "positionMode", "ThicknessRelative"); Value(position, "Thickness", 0.18f);
                Value(position, "arcCircle/circle/radius", 1.05f);
                Value(position, "arcCircle/arc", Mathf.PI);
            }
            else
            {
                Value(position, "line/start", new Vector3(-1.25f, 0f, 0f));
                Value(position, "line/end", new Vector3(1.25f, 0f, 0f));
            }
        }
        Attribute(initialize, "velocity", shape == 0 ? new Vector3(embers ? 1.7f : 1.2f, 0.1f, -0.08f) : new Vector3(-0.15f, 0.2f, -0.05f),
            shape == 0 ? new Vector3(embers ? 4.2f : 2.8f, embers ? 2f : 1.1f, 0.08f) : new Vector3(0.3f, embers ? 2.2f : 1.1f, 0.05f));
        Attribute(initialize, "size", embers ? 0.025f : shape == 0 ? 0.4f : 0.25f, embers ? 0.065f : shape == 0 ? 0.8f : 0.5f);
        Attribute(initialize, "scaleY", embers ? 2.5f : 1.75f);
        Attribute(initialize, "angleZ", embers ? -80f : -65f, embers ? -40f : -20f);
        var gravity = Add(update, "Gravity"); Value(gravity, "Force/vector", new Vector3(0f, embers ? -2.8f : 2f, 0f));
        var drag = Add(update, "Drag"); Value(drag, "dragCoefficient", embers ? 0.4f : 1.2f);
        var turbulence = Add(update, "Turbulence"); Value(turbulence, "Intensity", embers ? 1.4f : 2.2f);
        Setting(output, "blendMode", "Additive");
        var orient = Add(output, "Orient");
        Setting(orient, "mode", "FaceCameraPlane");
        var size = Add(output, "AttributeFromCurve"); Setting(size, "attribute", "size"); Setting(size, "Composition", "Multiply");
        Value(size, "Size", new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(0.18f, 1.1f), new Keyframe(0.65f, 0.8f), new Keyframe(1f, 0f)));
        var color = Add(output, "AttributeFromCurve"); Setting(color, "attribute", "color"); Setting(color, "ColorMode", "ColorAndAlpha");
        Value(color, "Color", gradient);
        Expose(graph, color, "Color", embers ? "EmberGradient" : "FlameGradient", typeof(Gradient), gradient, new Vector2(-180f, 210f));
        if (texture != null) Value(output, "mainTexture", texture);
        var resource = Resource(path);
        FindType("UnityEditor.VFX.VisualEffectResourceExtensions").GetMethod("WriteAssetWithSubAssets").Invoke(null, new[] {resource});
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
    private static Gradient Gradient(Color start, Color middle, Color end, float alpha)
    {
        var gradient = new Gradient();
        gradient.SetKeys(new[] {new GradientColorKey(start, 0f), new GradientColorKey(middle, 0.35f), new GradientColorKey(end, 1f)},
            new[] {new GradientAlphaKey(0f, 0f), new GradientAlphaKey(alpha, 0.08f), new GradientAlphaKey(alpha * 0.65f, 0.55f), new GradientAlphaKey(0f, 1f)});
        return gradient;
    }
    private static Texture2D FlameTexture()
    {
        const string path = "Assets/_Project/VFX/Textures/SwordFireFlame.asset";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (texture != null) return texture;
        const int size = 128;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true) {name = "Sword Fire Flame", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear};
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float height = y / (size - 1f), u = x / (size - 1f) * 2f - 1f;
            float center = 0.18f * Mathf.Sin(height * 8f) + 0.07f * Mathf.Sin(height * 23f);
            float width = 0.62f * Mathf.Pow(1f - height, 0.8f) + 0.015f;
            float edge = Mathf.Clamp01(1f - Mathf.Abs(u - center) / width);
            float body = Mathf.Pow(edge, 0.75f) * Mathf.SmoothStep(0f, 1f, height / 0.06f) * Mathf.SmoothStep(0f, 1f, (1f - height) / 0.08f);
            float noise = Mathf.PerlinNoise(x * 0.065f + height * 6f, y * 0.06f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, body * Mathf.Lerp(0.35f, 1f, noise));
        }
        texture.SetPixels(pixels); texture.Apply(false, true); AssetDatabase.CreateAsset(texture, path); return texture;
    }
}
