using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

// Smooth the bounded scouting target along board depth; Cinemachine owns the output camera.
public sealed class RouteMapCamera : MonoBehaviour
{
    [SerializeField] private Camera view;
    [SerializeField] private CinemachineCamera rig;
    [SerializeField] private CinemachineFollow follow;
    [SerializeField] private Transform trackingTarget, boardSpace;
    private LevelRouteSettings settings;
    private Vector3 targetOrigin;
    private float preferredDepth, farDepth, maximumOffset;
    private float desiredOffset, currentOffset, depthVelocity;
    private bool initialized;

    public void Configure(LevelRouteSettings profile, int steps)
    {
        settings = profile;
        if (!initialized)
        {
            targetOrigin = trackingTarget.position;
            preferredDepth = DepthAtViewport(settings.cameraFocusViewport);
            farDepth = DepthAtViewport(Mathf.Max(settings.cameraFocusViewport + 0.05f, settings.cameraFarViewport));
            initialized = true;
        }
        // The old far-edge cap kept late rows at the top of the board even when
        // focused. Allow extra travel so they can approach the normal focus area.
        maximumOffset = Mathf.Max(0f, (steps - 1) * settings.nodeSpacing.y - farDepth
            + Mathf.Max(0f, settings.cameraFarDepthOffset));
        desiredOffset = Mathf.Clamp(desiredOffset, 0f, maximumOffset);
        var tracker = follow.TrackerSettings;
        tracker.BindingMode = BindingMode.WorldSpace;
        // Cinemachine damps in camera-offset space. Damping only its Z axis leaves
        // part of a world-depth jump undamped on this angled camera. Smooth the
        // proxy instead, with no second filter or camera-height drift.
        tracker.PositionDamping = Vector3.zero;
        follow.TrackerSettings = tracker;
    }
    private float DepthAtViewport(float y)
    {
        var plane = new Plane(boardSpace.up, boardSpace.position);
        var ray = view.ViewportPointToRay(new Vector3(0.5f, y, 0f));
        return plane.Raycast(ray, out float distance) ? boardSpace.InverseTransformPoint(ray.GetPoint(distance)).z : 0f;
    }
    public void Focus(LevelRouteNode node)
    {
        if (!initialized) return;
        desiredOffset = Mathf.Clamp(node.depth * settings.nodeSpacing.y - preferredDepth, 0f, maximumOffset);
    }
    public void Suspend()
    {
        // Remove this virtual camera before gameplay brains resume; their default
        // channel masks can otherwise see the routing rig during scene unloading.
        if (rig != null) rig.gameObject.SetActive(false);
        if (view != null) view.enabled = false;
    }
    private void Update()
    {
        if (!initialized) return;
        // Essential map navigation stays smooth even with decorative UI motion off.
        // Unscaled time also keeps scouting responsive while gameplay is paused.
        currentOffset = Mathf.SmoothDamp(currentOffset, desiredOffset, ref depthVelocity,
            Mathf.Max(0.05f, settings.cameraDepthDamping), Mathf.Infinity,
            Mathf.Min(Time.unscaledDeltaTime, 0.1f));
        trackingTarget.position = targetOrigin + boardSpace.forward * currentOffset;
    }
}
