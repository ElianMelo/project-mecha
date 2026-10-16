using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/World/Room Camera")]
public class RoomCameraSettings : ScriptableObject
{
    [Range(0f, 1f)] public float groundedFeetViewport = 0.32f;
    public Vector2 airborneFeetBand = new Vector2(0.22f, 0.55f);
    public Vector2 verticalSafetyBand = new Vector2(0.08f, 0.65f);
    [Min(0.01f)] public float verticalRiseSmoothTime = 0.24f, verticalFallSmoothTime = 0.18f;
    public Rect viewBounds = new Rect(-12.5f, -5.6f, 25f, 14.4f);
    public Vector2 physicalWalls = new Vector2(-12f, 12f);
    public float wallThickness = 0.4f, wallHeight = 24f, wallDepth = 6f;
    public float orthographicSize = 3.485f, cameraDistance = 10f, targetHeight = 0.9f;
    public Vector2 deadZone = new Vector2(0.12f, 0.6f), hardLimits = new Vector2(0.74f, 0.72f), screenPosition = new Vector2(0f, -0.06f);
    public float centerDamping = 1.1f, edgeDamping = 0.04f, searchInterval = 0.25f;
    public float framingFloorHeight = -3.5f;
    public float GroundedHeightAtSupport(float height) => height + (0.5f - groundedFeetViewport) * 2f * orthographicSize;
    public float FeetViewportAtHeight(float height) => 0.5f + (height - GroundedHeightAtSupport(height)) / (2f * orthographicSize);
    public Vector3 initialPosition = new Vector3(-3f, -1.5f, -10f);
}
