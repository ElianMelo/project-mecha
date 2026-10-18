using System.Collections.Generic;
using UnityEngine;

// Native physics discovers target colliders; camera bounds decide what is on screen.
[RequireComponent(typeof(PlayerMovement))]
public sealed class MechaAutoAim : MonoBehaviour
{
    [SerializeField] private SecondaryWeaponSettings settings;
    [SerializeField] private Transform leftArm;
    public void Bind(Transform arm) => leftArm = arm;
    private PlayerMovement movement;
    private PlayerIdentity identity;
    private ITargetable target;
    private bool hadTarget, capturing;
    private Camera viewCamera;
    private readonly Plane[] frustum = new Plane[6];
    private readonly Collider[] candidates = new Collider[128];
    private readonly HashSet<ITargetable> visited = new HashSet<ITargetable>();
    private float angle, angularVelocity, acquireAt, nextScan, remoteAngle, switchAt;
    private Vector3 trackedPoint, trackedVelocity;
    public Vector3 Origin
    {
        get
        {
            Vector3 position = leftArm != null ? leftArm.TransformPoint(settings.armMountOffset) : transform.position + settings.mountOffset;
            position.z = transform.position.z;
            return position;
        }
    }
    public Vector3 Direction => Quaternion.Euler(0f, 0f, angle) * Vector3.right;
    public float Angle => angle;
    public bool IsLocked { get; private set; }
    public ITargetable Target => target is Object native && native == null ? null : target;
    public Vector3 TargetPoint => Target != null ? (IsLocked ? trackedPoint : Point(Target)) : Origin;
    private Vector3 Point(ITargetable candidate)
    {
        Vector3 point = candidate.AimPoint;
        point.z = transform.position.z;
        return point;
    }
    private void Awake()
    {
        movement = GetComponent<PlayerMovement>(); identity = GetComponent<PlayerIdentity>();
        angle = remoteAngle = movement.Facing > 0 ? 0f : 180f;
    }
    public void PresentRemote(float value) => remoteAngle = value;
    private void Update()
    {
        if (identity != null && identity.IsSpawned && !identity.IsOwner)
        {
            angle = Mathf.SmoothDampAngle(angle, remoteAngle, ref angularVelocity, settings.aimSmoothTime, settings.aimDegreesPerSecond, Time.deltaTime);
            return;
        }
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera == null) return;
        GeometryUtility.CalculateFrustumPlanes(viewCamera, frustum);
        bool valid = Valid(target);
        if (!valid)
        {
            target = null;
            IsLocked = false; capturing = false;
            if (hadTarget) { hadTarget = false; acquireAt = Time.time + settings.acquisitionDelay; }
        }
        if (Time.time >= nextScan && Time.time >= acquireAt)
        {
            nextScan = Time.time + Mathf.Max(0.02f, settings.targetScanInterval);
            var best = FindBest();
            // Reconsider proximity even while locked, but hold each selection long
            // enough to avoid bouncing between crossing or equally close enemies.
            if (best != null && best != target && (!valid || Time.time >= switchAt && Prefer(best)))
            {
                target = best; hadTarget = true; IsLocked = false; capturing = false;
                trackedPoint = Point(best); trackedVelocity = Vector3.zero;
                switchAt = Time.time + settings.targetSwitchDelay;
            }
        }
        if (target != null)
        {
            if (IsLocked) trackedPoint = Vector3.SmoothDamp(trackedPoint, Point(target), ref trackedVelocity,
                settings.lockedTargetSmoothTime, Mathf.Infinity, Time.deltaTime);
            else { trackedPoint = Point(target); trackedVelocity = Vector3.zero; }
        }
        Vector3 aim = target != null ? trackedPoint - Origin : Vector3.right * movement.Facing;
        float desired = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        float error = Mathf.Abs(Mathf.DeltaAngle(angle, desired));
        if (target != null && error <= settings.lockAngle) capturing = true;
        if (target != null && IsLocked)
        {
            // Retain capture through ordinary movement. Releasing on angular error
            // repeatedly replayed the bounce when a target crossed close to us.
            angle = Mathf.SmoothDampAngle(angle, desired, ref angularVelocity, settings.lockedAimSmoothTime,
                settings.lockedDegreesPerSecond, Time.deltaTime);
        }
        else if (target != null && capturing)
        {
            // The scan has inertia; capture has a definite arrival, with no long
            // easing tail. Native Animator adds the visual overshoot separately.
            angularVelocity = 0f;
            angle = Mathf.MoveTowardsAngle(angle, desired, settings.captureDegreesPerSecond * Time.deltaTime);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, desired)) <= 0.1f)
            { angle = desired; IsLocked = true; }
        }
        else angle = Mathf.SmoothDampAngle(angle, desired, ref angularVelocity, settings.aimSmoothTime, settings.aimDegreesPerSecond, Time.deltaTime);
    }
    private bool Prefer(ITargetable candidate)
    {
        if (candidate.TargetPriority != target.TargetPriority) return candidate.TargetPriority > target.TargetPriority;
        float currentDistance = Mathf.Sqrt(SelectionDistanceSquared(target));
        float challengerDistance = Mathf.Sqrt(SelectionDistanceSquared(candidate));
        return challengerDistance + settings.targetSwitchDistanceMargin < currentDistance;
    }
    private float SelectionDistanceSquared(ITargetable candidate)
    {
        // Rank from the pilot, independent of animated forearm/socket motion.
        Vector3 origin = transform.position + Vector3.up * settings.mountOffset.y;
        return (Point(candidate) - origin).sqrMagnitude;
    }
    private ITargetable FindBest()
    {
        Physics.SyncTransforms();
        int count = Physics.OverlapSphereNonAlloc(Origin, settings.range, candidates, settings.targetLayers, QueryTriggerInteraction.Ignore);
        visited.Clear();
        ITargetable best = null;
        float distance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            var candidate = candidates[i].GetComponentInParent<ITargetable>();
            if (candidate == null || !visited.Add(candidate) || !Valid(candidate)) continue;
            float squared = SelectionDistanceSquared(candidate);
            if (best == null || candidate.TargetPriority > best.TargetPriority || candidate.TargetPriority == best.TargetPriority && squared < distance)
            { best = candidate; distance = squared; }
        }
        return best;
    }
    private bool Valid(ITargetable candidate)
    {
        if (candidate == null || candidate is Object native && native == null || !candidate.CanTarget) return false;
        if (!GeometryUtility.TestPlanesAABB(frustum, candidate.TargetBounds)) return false;
        // Full XY acquisition includes enemies above/below and behind the pilot.
        // Platforms do not veto screen acquisition or physical bullet travel.
        return (Point(candidate) - Origin).sqrMagnitude <= settings.range * settings.range;
    }
}
