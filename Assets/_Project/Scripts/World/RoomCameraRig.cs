using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(CinemachineCamera), typeof(CinemachinePositionComposer))]
public class RoomCameraRig : MonoBehaviour
{
    [SerializeField] private RoomCameraSettings settings;
    private CinemachineCamera virtualCamera;
    private CinemachinePositionComposer composer;
    private PlayerMovement player;
    private Camera output;
    private Transform trackingPoint;
    private Rect roomBounds;
    private float nextSearch, cameraHeight, verticalVelocity;
    private Vector3 roomStart;
    private bool hasRoom, needsInitialPose;
    private void Awake()
    {
        output = Camera.main;
        Configure();
        if (!hasRoom) { roomBounds = settings.viewBounds; roomStart = settings.initialPosition; }
    }
    public void Configure()
    {
        if (settings == null) return;
        virtualCamera = GetComponent<CinemachineCamera>(); composer = GetComponent<CinemachinePositionComposer>();
        virtualCamera.Lens.OrthographicSize = settings.orthographicSize;
        virtualCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
        composer.CameraDistance = settings.cameraDistance; composer.TargetOffset = Vector3.up * settings.targetHeight;
        composer.CenterOnActivate = false; composer.Lookahead.Enabled = false;
        composer.Composition.ScreenPosition = settings.screenPosition;
        composer.Composition.DeadZone = new ScreenComposerSettings.DeadZoneSettings {Enabled = true, Size = new Vector2(settings.deadZone.x, 0f)};
        composer.Composition.HardLimits = new ScreenComposerSettings.HardLimitSettings {Enabled = true, Size = new Vector2(settings.hardLimits.x, 1f)};
        composer.Damping = new Vector3(settings.centerDamping, 0f, 0f);
    }
    public void SetRoom(Rect bounds, Vector3 start)
    {
        roomBounds = bounds; roomStart = start; hasRoom = true;
        if (player != null) Focus(player);
    }
    private float ClampHeight(float height)
    {
        float half = virtualCamera.Lens.OrthographicSize;
        if (roomBounds.height <= half * 2f) return roomBounds.center.y;
        return Mathf.Clamp(height, roomBounds.yMin + half, roomBounds.yMax - half);
    }
    private void Update()
    {
        if (settings == null) return;
        if (player == null && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + settings.searchInterval;
            foreach (var candidate in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
                if (candidate.HasControl) { Focus(candidate); break; }
        }
        if (player == null || output == null) return;
        if (needsInitialPose)
        {
            float foot = hasRoom ? roomStart.y : player.transform.position.y;
            cameraHeight = ClampHeight(foot + (0.5f - settings.groundedFeetViewport) * 2f * GetComponent<RoomCameraZoom>().BaseSize);
            virtualCamera.ForceCameraPosition(new Vector3(player.transform.position.x, cameraHeight, -settings.cameraDistance), Quaternion.identity);
            virtualCamera.PreviousStateIsValid = false;
            needsInitialPose = false;
        }
        float lensHeight = 2f * virtualCamera.Lens.OrthographicSize;
        float desired = cameraHeight;
        if (player.IsGrounded)
        {
            float foot = player.Support != null ? player.Support.bounds.max.y : player.transform.position.y;
            desired = foot + (0.5f - settings.groundedFeetViewport) * 2f * GetComponent<RoomCameraZoom>().BaseSize;
        }
        else
        {
            // A soft vertical band absorbs small jumps, then follows continued ascent/descent.
            float feetViewport = 0.5f + (player.transform.position.y - cameraHeight) / lensHeight;
            if (feetViewport > settings.airborneFeetBand.y)
                desired += (feetViewport - settings.airborneFeetBand.y) * lensHeight;
            else if (feetViewport < settings.airborneFeetBand.x)
                desired += (feetViewport - settings.airborneFeetBand.x) * lensHeight;
        }
        desired = ClampHeight(desired);
        cameraHeight = Mathf.SmoothDamp(cameraHeight, desired, ref verticalVelocity,
            desired > cameraHeight ? settings.verticalRiseSmoothTime : settings.verticalFallSmoothTime);
        if (!player.IsGrounded)
            cameraHeight = ClampHeight(Mathf.Clamp(cameraHeight,
                player.transform.position.y + (0.5f - settings.verticalSafetyBand.y) * lensHeight,
                player.transform.position.y + (0.5f - settings.verticalSafetyBand.x) * lensHeight));
        float trackedY = cameraHeight - settings.screenPosition.y * lensHeight - settings.targetHeight;
        trackingPoint.position = new Vector3(player.transform.position.x, trackedY, player.transform.position.z);
        float x = Mathf.Abs(output.WorldToViewportPoint(player.transform.position).x - 0.5f) * 2f;
        float pressure = Mathf.InverseLerp(settings.deadZone.x, settings.hardLimits.x, x);
        composer.Damping = new Vector3(Mathf.Lerp(settings.centerDamping, settings.edgeDamping, pressure * pressure), 0f, 0f);
    }
    public void Focus(PlayerMovement target)
    {
        if (settings == null) return;
        if (virtualCamera == null) Configure();
        if (output == null) output = Camera.main;
        if (!hasRoom) roomBounds = settings.viewBounds;
        player = target; verticalVelocity = 0f;
        GetComponent<RoomCameraZoom>()?.Focus(target);
        if (trackingPoint == null) trackingPoint = new GameObject("Room Camera Tracking Point") {hideFlags = HideFlags.DontSave}.transform;
        trackingPoint.position = target.transform.position;
        virtualCamera.Follow = trackingPoint; needsInitialPose = true;
    }
    private void OnDestroy() { if (trackingPoint != null) Destroy(trackingPoint.gameObject); }
}
