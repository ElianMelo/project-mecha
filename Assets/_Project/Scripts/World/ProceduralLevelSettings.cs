using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/World/Procedural Levels")]
public class ProceduralLevelSettings : ScriptableObject
{
    public int firstRoomSeed = 104729;
    public Vector2 longWidth = new Vector2(30f, 38f), bigWidth = new Vector2(23f, 29f);
    public Vector2Int risingTiers = new Vector2Int(7, 9), bigTiers = new Vector2Int(5, 7);
    public float firstRoomWidth = 22f, floorHeight = -3.5f, tierHeight = 1.6f;
    public int firstRoomTiers = 3;
    public ArenaLayoutSettings.PlatformData[] firstRoomPlatforms;
    public Vector2Int enemyCount = new Vector2Int(7, 11);
    public float minimumSpawnDistance = 3.2f, platformThickness = 0.22f;
    public float horizontalCellWidth = 5f, edgeInset = 0.6f, platformJitter = 0.45f;
    public Vector2 platformWidthFraction = new Vector2(0.76f, 0.88f);
    public float risingGap = 0.7f, risingStepOffset = 0.18f, minimumEnemySpacing = 2.25f;
    public float playerSpawnSpacing = 0.85f;
    public float portalInteractionRange = 1.8f, pointerInset = 48f;
    public Color enemyPointerColor = new Color(1f, 0.78f, 0.16f), portalColor = new Color(0.2f, 0.95f, 1f);
    public ScenarioSettings scenario;
    public OneWayPlatformSettings oneWay;
    public RoomCameraSettings cameraSettings;
    public Material portalMaterial;
}
