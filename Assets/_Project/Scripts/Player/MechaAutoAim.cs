using System.Collections.Generic;
using UnityEngine;

// Native physics discovers target colliders; camera bounds decide what is on screen.
[RequireComponent(typeof(PlayerMovement))]
public sealed class MechaAutoAim : MonoBehaviour
{
    [SerializeField] private SecondaryWeaponSettings settings;
    [SerializeField] private Transform leftArm;
    private PlayerMovement movement;
    private PlayerIdentity identity;
    private ITargetable target;
    private bool hadTarget;
    private Camera viewCamera;
    private readonly Plane[] frustum = new Plane[6];
    private readonly Collider[] candidates = new Collider[128];
    private readonly HashSet<ITargetable> visited = new HashSet<ITargetable>();
    private float angle, angularVelocity, acquireAt, nextScan, remoteAngle;
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
    public ITargetable Target => target is Object native && native == null ? null : target;
    public Vector3 TargetPoint => Target != null ? Point(Target) : Origin;
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
            if (hadTarget) { hadTarget = false; acquireAt = Time.time + settings.acquisitionDelay; }
        }
        if (Time.time >= nextScan && Time.time >= acquireAt)
        {
            nextScan = Time.time + Mathf.Max(0.02f, settings.targetScanInterval);
            var best = FindBest();
            // Retain a valid target, but an enemy always displaces a fallback crate.
            if (best != null && (!valid || best.TargetPriority > target.TargetPriority))
            { target = best; hadTarget = true; }
        }
        Vector3 aim = target != null ? Point(target) - Origin : Vector3.right * movement.Facing;
        float desired = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        angle = Mathf.SmoothDampAngle(angle, desired, ref angularVelocity, settings.aimSmoothTime, settings.aimDegreesPerSecond, Time.deltaTime);
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
            float squared = (Point(candidate) - Origin).sqrMagnitude;
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
        // Platforms do not veto screen acquisition; physical bullets still collide with them.
        return (Point(candidate) - Origin).sqrMagnitude <= settings.range * settings.range;
    }
}
