using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MechaScenarioSetup
{
    public static void Apply()
    {
        var settings = MechaPresentationSetup.Create<ScenarioSettings>("Scenario");
        MechaScenarioArt.Ensure(settings);
        var root = GameObject.Find("Cyberpunk Scenario") ?? new GameObject("Cyberpunk Scenario");
        MechaBackgroundSetup.Apply(root.transform);
        ApplyPlatforms(settings);
        MechaDestructibleSetup.Apply(settings, root.transform);
    }
    public static void ApplyPlatforms(ScenarioSettings settings)
    {
        var ground = GameObject.Find("Ground");
        var bounds = ground.GetComponent<Collider>().bounds;
        ground.GetComponent<Renderer>().enabled = false;
        int tiles = Mathf.CeilToInt(bounds.size.x / settings.floorTileWidth);
        for (int i = 0; i < tiles; i++)
        {
            float width = Mathf.Min(settings.floorTileWidth, bounds.size.x - i * settings.floorTileWidth);
            Face(ground.transform, "Floor Face " + i, settings.floorSprite,
                new Vector3(bounds.min.x + i * settings.floorTileWidth + width * 0.5f, bounds.max.y, -0.02f), width, settings.floorFaceHeight, settings, settings.floorBlockPrefab);
        }
        foreach (var platform in Object.FindObjectsByType<OneWayPlatform>(FindObjectsSortMode.None))
        {
            var surface = platform.GetComponent<Collider>().bounds;
            platform.GetComponent<Renderer>().enabled = false;
            Face(platform.transform, "Floating Face", settings.floatingSprite,
                new Vector3(surface.center.x, surface.max.y, -0.02f), surface.size.x, settings.floatingFaceHeight, settings, settings.floatingBlockPrefab);
        }
    }
    public static void Face(Transform parent, string name, Sprite sprite, Vector3 position, float width, float height, ScenarioSettings settings, GameObject blockPrefab)
    {
        var face = Child(parent, name);
        if (blockPrefab != null)
        {
            var previous = face.GetComponent<SpriteRenderer>();
            if (previous != null) previous.enabled = false;
            var block = face.transform.Find("Block Geometry");
            if (block == null)
            {
                block = ((GameObject)PrefabUtility.InstantiatePrefab(blockPrefab, face.transform)).transform;
                block.name = "Block Geometry";
            }
            block.localPosition = Vector3.zero; block.localRotation = Quaternion.identity; block.localScale = Vector3.one;
            face.transform.SetPositionAndRotation(new Vector3(position.x, position.y - height * .5f,
                settings.platformFrontDepth + settings.platformVisualDepth * .5f), Quaternion.identity);
            var parentScale = parent.lossyScale;
            face.transform.localScale = new Vector3(width / parentScale.x, height / parentScale.y, settings.platformVisualDepth / parentScale.z);
            return;
        }
        face.transform.position = position; face.transform.rotation = Quaternion.identity;
        var scale = parent.lossyScale;
        face.transform.localScale = new Vector3(width / sprite.bounds.size.x / scale.x, height / sprite.bounds.size.y / scale.y, 1f / scale.z);
        var renderer = MechaPresentationSetup.Ensure<SpriteRenderer>(face);
        renderer.sprite = sprite; renderer.sortingOrder = 0; renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.enabled = true;
        if (settings.platformMaterial != null) renderer.sharedMaterial = settings.platformMaterial;
    }
    public static GameObject Child(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(name); go.transform.SetParent(parent, false); return go;
    }
    public static Material Material(string name, Color color)
    {
        string path = "Assets/_Project/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color); EditorUtility.SetDirty(material);
        return material;
    }
    public static GameObject Primitive(Transform parent, string name, Material material)
    {
        var existing = parent.Find(name);
        var go = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetParent(parent, false);
        var collider = go.GetComponent<Collider>(); if (collider != null) Object.DestroyImmediate(collider);
        var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        return go;
    }
}
