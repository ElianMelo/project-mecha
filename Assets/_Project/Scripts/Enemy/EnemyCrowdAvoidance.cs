using System.Collections.Generic;
using UnityEngine;

// Enemy-to-enemy collision stays disabled. Soft spacing permits overlap as crowds grow.
[RequireComponent(typeof(CapsuleCollider), typeof(EnemyController))]
public class EnemyCrowdAvoidance : MonoBehaviour
{
    [SerializeField] private EnemyCrowdSettings settings;
    private static readonly HashSet<EnemyCrowdAvoidance> Members = new HashSet<EnemyCrowdAvoidance>();
    private CapsuleCollider capsule;
    private EnemyController actor;
    private Rigidbody body;
    public float AllowedOverlap { get; private set; }
    private void Awake() { capsule = GetComponent<CapsuleCollider>(); actor = GetComponent<EnemyController>(); body = GetComponent<Rigidbody>(); }
    private void OnEnable() => Members.Add(this);
    private void OnDisable() => Members.Remove(this);

    public float Steer(float desiredVelocity)
    {
        if (settings == null || actor.IsDead) return desiredVelocity;
        int neighbors = 0;
        Vector3 center = capsule.bounds.center;
        foreach (var other in Members)
            if (IsNeighbor(other, center)) neighbors++;
        AllowedOverlap = Mathf.Lerp(settings.sparseOverlap, settings.denseOverlap,
            Mathf.Clamp01((float)neighbors / Mathf.Max(1, settings.denseNeighborCount)));
        float correction = 0f;
        float lowerVelocity = float.NegativeInfinity, upperVelocity = float.PositiveInfinity;
        foreach (var other in Members)
        {
            if (!IsNeighbor(other, center)) continue;
            float dx = center.x - other.capsule.bounds.center.x;
            float spacing = Mathf.Max(settings.minimumSpacing, (capsule.bounds.extents.x + other.capsule.bounds.extents.x) * (1f - AllowedOverlap));
            float side = Mathf.Abs(dx) > 0.001f ? Mathf.Sign(dx) : GetInstanceID() > other.GetInstanceID() ? 1f : -1f;
            float gap = Mathf.Abs(dx) - spacing;
            // Match a nearby actor's velocity before contact, then relax any penetration.
            float approach = gap / Mathf.Max(Time.fixedDeltaTime, settings.anticipation);
            if (side < 0f) upperVelocity = Mathf.Min(upperVelocity, other.body.linearVelocity.x + approach);
            else lowerVelocity = Mathf.Max(lowerVelocity, other.body.linearVelocity.x - approach);
            if (gap < 0f) correction -= side * gap * settings.separationStrength;
        }
        float preferred = desiredVelocity + Mathf.Clamp(correction, -settings.maximumCorrection, settings.maximumCorrection);
        return lowerVelocity <= upperVelocity ? Mathf.Clamp(preferred, lowerVelocity, upperVelocity) : (lowerVelocity + upperVelocity) * 0.5f;
    }
    private bool IsNeighbor(EnemyCrowdAvoidance other, Vector3 center)
    {
        if (other == null || other == this || other.actor.IsDead || !other.capsule.enabled) return false;
        Vector3 delta = other.capsule.bounds.center - center;
        return Mathf.Abs(delta.y) <= settings.verticalTolerance && Mathf.Abs(delta.x) < settings.neighborRadius;
    }
}
