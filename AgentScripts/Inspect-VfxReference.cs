using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Read-only prefab analysis through official Pipeline run_script. No instantiation,
// vendor behaviour execution, scene changes, or asset saves.
public static class InspectVfxReference
{
    public static object Inspect(string path)
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null) throw new ArgumentException("Expected an imported prefab asset: " + path);
        return Serialize(new
        {
            path,
            labels = AssetDatabase.GetLabels(root),
            scripts = root.GetComponentsInChildren<MonoBehaviour>(true)
                .Select(b => b == null ? "MISSING SCRIPT" : b.GetType().FullName).Distinct().ToArray(),
            layers = root.GetComponentsInChildren<ParticleSystem>(true).Select(ps =>
            {
                var main = ps.main;
                var emission = ps.emission;
                var bursts = new ParticleSystem.Burst[emission.burstCount];
                emission.GetBursts(bursts);
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                var streams = new List<ParticleSystemVertexStream>();
                if (renderer != null) renderer.GetActiveVertexStreams(streams);
                var sub = ps.subEmitters;
                var collision = ps.collision;
                return new
                {
                    hierarchy = Hierarchy(ps.transform, root.transform),
                    active = ps.gameObject.activeSelf,
                    localPosition = V(ps.transform.localPosition),
                    localRotation = V(ps.transform.localEulerAngles),
                    localScale = V(ps.transform.localScale),
                    main = new { main.duration, main.loop, startDelay = Curve(main.startDelay), lifetime = Curve(main.startLifetime),
                        speed = Curve(main.startSpeed), size = Curve(main.startSize), main.maxParticles,
                        space = main.simulationSpace.ToString(), scaling = main.scalingMode.ToString(), stop = main.stopAction.ToString() },
                    emission = new { emission.enabled, rate = Curve(emission.rateOverTime),
                        bursts = bursts.Select(b => new { b.time, count = Curve(b.count), b.cycleCount, b.repeatInterval, b.probability }).ToArray() },
                    shape = new { ps.shape.enabled, type = ps.shape.shapeType.ToString(), ps.shape.angle, ps.shape.radius },
                    collision = new { collision.enabled, type = collision.type.ToString(), mode = collision.mode.ToString(),
                        mask = collision.collidesWith.value, collision.sendCollisionMessages, bounce = Curve(collision.bounce),
                        dampen = Curve(collision.dampen), lifetimeLoss = Curve(collision.lifetimeLoss) },
                    subemitters = Enumerable.Range(0, sub.subEmittersCount).Select(i => new {
                        path = sub.GetSubEmitterSystem(i) == null ? null : Hierarchy(sub.GetSubEmitterSystem(i).transform, root.transform),
                        type = sub.GetSubEmitterType(i).ToString(), properties = sub.GetSubEmitterProperties(i).ToString() }).ToArray(),
                    textureSheet = new { ps.textureSheetAnimation.enabled, ps.textureSheetAnimation.numTilesX, ps.textureSheetAnimation.numTilesY,
                        mode = ps.textureSheetAnimation.mode.ToString(), frame = Curve(ps.textureSheetAnimation.frameOverTime) },
                    modules = new { velocity = ps.velocityOverLifetime.enabled, color = ps.colorOverLifetime.enabled,
                        size = ps.sizeOverLifetime.enabled, noise = ps.noise.enabled, trails = ps.trails.enabled,
                        lights = ps.lights.enabled, ps.lights.maxLights, ps.lights.ratio, customData = ps.customData.enabled },
                    renderer = renderer == null ? null : new { mode = renderer.renderMode.ToString(),
                        alignment = renderer.alignment.ToString(), renderer.sortingLayerName, renderer.sortingOrder,
                        mesh = AssetDatabase.GetAssetPath(renderer.mesh), streams = streams.Select(s => s.ToString()).ToArray(),
                        materials = renderer.sharedMaterials.Where(m => m != null).Select(MaterialInfo).ToArray(),
                        trailMaterial = renderer.trailMaterial == null ? null : AssetDatabase.GetAssetPath(renderer.trailMaterial) }
                };
            }).ToArray(),
            lights = root.GetComponentsInChildren<Light>(true).Select(l => new {
                path = Hierarchy(l.transform, root.transform), l.enabled, type = l.type.ToString(), l.intensity, l.range,
                shadows = l.shadows.ToString() }).ToArray(),
            dependencies = AssetDatabase.GetDependencies(path, true),
            note = "For exact curves/gradients, inspect serialized native components read-only. Summary is structural, not visual approval."
        });
    }
    static string Serialize(object value)
    {
        // Pipeline also references Localization's embedded JSON types; select the public package explicitly.
        var type = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
        return (string)type.GetMethod("SerializeObject", new[]{typeof(object)}).Invoke(null, new[]{value});
    }

    private static object Curve(ParticleSystem.MinMaxCurve c) => new
    {
        mode = c.mode.ToString(), c.constantMin, c.constantMax, c.curveMultiplier,
        minKeys = c.curveMin == null ? null : c.curveMin.keys,
        maxKeys = c.curveMax == null ? null : c.curveMax.keys
    };

    private static object MaterialInfo(Material m) => new
    {
        path = AssetDatabase.GetAssetPath(m), shader = m.shader == null ? null : m.shader.name,
        m.renderQueue, keywords = m.shaderKeywords,
        textures = m.GetTexturePropertyNames().Select(name => new {
            property = name, asset = AssetDatabase.GetAssetPath(m.GetTexture(name)),
            scale = V(m.GetTextureScale(name)), offset = V(m.GetTextureOffset(name)) }).ToArray()
    };

    private static float[] V(Vector3 v) => new[] { v.x, v.y, v.z };
    private static float[] V(Vector2 v) => new[] { v.x, v.y };

    private static string Hierarchy(Transform t, Transform root)
    {
        var names = new List<string>();
        while (t != null) { names.Add(t.name); if (t == root) break; t = t.parent; }
        names.Reverse();
        return string.Join("/", names);
    }
}
