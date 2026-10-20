using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/World/Scenario")]
public class ScenarioSettings : ScriptableObject
{
    public Texture2D platformAtlas;
    public Sprite floorSprite, floatingSprite;
    public Material platformMaterial;
    public GameObject floorBlockPrefab, floatingBlockPrefab;
    [Min(.01f)] public float platformVisualDepth = .6f;
    public float platformFrontDepth = .10f;
    public float floorFaceHeight = 1.5f, floorTileWidth = 6f, floatingFaceHeight = 0.55f;
    public float propDepth = 0.6f;
    public Vector3 boxSize = new Vector3(1f, 1f, 0.7f);
    public Vector3[] boxFeet;
    public Color crateColor = new Color(0.28f, 0.38f, 0.45f);
    public Color crateTrimColor = new Color(0.07f, 0.11f, 0.15f), crateLampColor = new Color(0.2f, 0.85f, 0.95f);
    public Vector3 trimSize = new Vector3(0.12f, 1f, 0.08f), trimOffset = new Vector3(0.36f, 0f, -0.37f);
    public Vector3 lampSize = new Vector3(0.35f, 0.06f, 0.06f), lampOffset = new Vector3(0f, 0.17f, -0.37f);
}
