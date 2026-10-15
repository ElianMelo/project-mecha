using UnityEditor;
using UnityEngine;

public static class MechaScenarioArt
{
    private const string Folder = "Assets/_Project/Art/Environment";
    public static void Ensure(ScenarioSettings settings)
    {
        settings.platformAtlas = Import(Folder + "/IndustrialPlatforms.png");
        if (settings.platformAtlas == null) return;
        settings.floorSprite = SpriteAsset("FloorPlatform", settings.platformAtlas, new Rect(0, 532, 1536, 394), new Vector2(0.5f, 1f));
        settings.floatingSprite = SpriteAsset("FloatingPlatform", settings.platformAtlas, new Rect(0, 80, 1536, 358), new Vector2(0.5f, 1f));
        EditorUtility.SetDirty(settings);
    }
    private static Texture2D Import(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static Sprite SpriteAsset(string name, Texture2D texture, Rect rect, Vector2 pivot)
    {
        string path = Folder + "/" + name + ".asset";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        sprite = Sprite.Create(texture, rect, pivot, 256f, 0, SpriteMeshType.FullRect);
        sprite.name = name; AssetDatabase.CreateAsset(sprite, path); return sprite;
    }
}
