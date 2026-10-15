using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/World/Room Camera")]
public class RoomCameraSettings : ScriptableObject
{
    public Rect viewBounds = new Rect(-12.5f, -5.6f, 25f, 14.4f);
    public Vector2 physicalWalls = new Vector2(-12f,12f);
    public float wallThickness = 0.4f, wallHeight = 24f, wallDepth = 6f;
    public float orthographicSize = 3.485f, cameraDistance = 10f, targetHeight = 0.9f;
    public Vector2 deadZone = new Vector2(0.12f,0.6f), hardLimits = new Vector2(0.74f,0.72f), screenPosition = new Vector2(0f,-0.06f);
    public float centerDamping = 1.1f, edgeDamping = 0.04f, searchInterval = 0.25f;
    [Min(0f)] public float landingHeightTolerance = 0.06f;
    [Min(0.01f), Tooltip("Duration of the grounded height adjustment; airborne movement never advances it.")]
    public float landingBlendDuration = 0.96f;
    public AnimationCurve landingBlend = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Range(0f, 1f)] public float airborneFollowWeight = 0.18f;
    [Min(0f)] public float maximumAirborneOffset = 0.55f;
    [Min(0.01f)] public float airborneSmoothTime = 0.35f, airborneMaximumSpeed = 1.2f;
    public float framingFloorHeight = -3.5f;
    [Range(0f, 1f)] public float floorFeetViewport = 0.1f;
    [Tooltip("Support elevation above the floor → camera rise. The flat first segment keeps the low deck in the floor's frame.")]
    public AnimationCurve groundedRiseByElevation = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(1.9f, 0f),
        new Keyframe(3.66f, 1.569f), new Keyframe(5.51f, 2.8614f));
    public float GroundedHeightAtSupport(float height)
    {
        float floorCenter = framingFloorHeight - (floorFeetViewport - 0.5f) * 2f * orthographicSize;
        return floorCenter + Mathf.Max(0f, groundedRiseByElevation.Evaluate(Mathf.Max(0f, height - framingFloorHeight)));
    }
    public float FeetViewportAtHeight(float height)
    {
        return 0.5f + (height - GroundedHeightAtSupport(height)) / (2f * orthographicSize);
    }
    public Vector3 initialPosition = new Vector3(-3f,-1.5f,-10f);
}
