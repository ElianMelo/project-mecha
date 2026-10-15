using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MechaBackgroundSetup
{
    private const string Art = "Assets/_Project/Art/Environment/";
    public static void Apply(Transform root)
    {
        var settings = MechaPresentationSetup.Create<ParallaxSceneSettings>("ParallaxScene");
        if (settings.layers == null || settings.layers.Length == 0)
        {
            settings.layers = new[] {
                Layer("Far Sky", "CyberpunkFarSky", new Vector2(0.08f, 0.04f), new Vector3(-27f, 7f, 20f), new Vector2(36f,24f), -120),
                Layer("Middle Skyline", "CyberpunkMiddleSkyline", new Vector2(0.22f, 0.10f), new Vector3(-27f, 5f, 16f), new Vector2(30f,20f), -110),
                Layer("Near Rooftops", "CyberpunkNearRooftops", new Vector2(0.42f, 0.18f), new Vector3(-27f, 4f, 10f), new Vector2(30f,20f), -100)
            };
            EditorUtility.SetDirty(settings);
        }
        // Replace the old image/primitive combination through Unity's scene APIs.
        foreach (string oldName in new[] { "Distant City", "Industrial Silhouettes" })
        {
            var old = root.Find(oldName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }
        var background = MechaScenarioSetup.Child(root, "Parallax Backgrounds");
        foreach (var old in background.transform.Cast<Transform>().ToArray())
            if (!settings.layers.Any(layer => layer.name == old.name)) Object.DestroyImmediate(old.gameObject);
        foreach (var layer in settings.layers)
        {
            var go = MechaScenarioSetup.Child(background.transform, layer.name);
            go.transform.SetPositionAndRotation(layer.position, Quaternion.identity); go.transform.localScale = Vector3.one;
            MechaPresentationSetup.Assign(MechaPresentationSetup.Ensure<ParallaxLayer>(go), "settings", layer.motion);
            var tileNames = Enumerable.Range(-layer.sideCopies, 2 * layer.sideCopies + 1).Select(i => "Image " + i).ToArray();
            foreach (var old in go.transform.Cast<Transform>().ToArray())
                if (!tileNames.Contains(old.name)) Object.DestroyImmediate(old.gameObject);
            for (int i = -layer.sideCopies; i <= layer.sideCopies; i++)
            {
                var tile = MechaScenarioSetup.Child(go.transform, "Image " + i);
                tile.transform.localPosition = new Vector3(i * layer.size.x, 0f, 0f);
                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale = new Vector3(layer.size.x / layer.sprite.bounds.size.x, layer.size.y / layer.sprite.bounds.size.y, 1f);
                var renderer = MechaPresentationSetup.Ensure<SpriteRenderer>(tile);
                renderer.sprite = layer.sprite; renderer.color = layer.tint; renderer.sortingOrder = layer.sortingOrder;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            }
        }
    }
    private static ParallaxSceneSettings.ImageLayer Layer(string name, string asset, Vector2 movement, Vector3 position, Vector2 size, int order)
    {
        string path = Art + asset + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException("Missing parallax art: " + path);
        importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
        importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + asset + ".asset");
        if (sprite == null)
        {
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f, 256f, 0, SpriteMeshType.FullRect);
            sprite.name = asset; AssetDatabase.CreateAsset(sprite, Art + asset + ".asset");
        }
        var motion = MechaPresentationSetup.Create<ParallaxSettings>(asset + "Parallax");
        motion.movement = movement; EditorUtility.SetDirty(motion);
        return new ParallaxSceneSettings.ImageLayer { name = name, sprite = sprite, motion = motion, position = position, size = size, sortingOrder = order };
    }
}
