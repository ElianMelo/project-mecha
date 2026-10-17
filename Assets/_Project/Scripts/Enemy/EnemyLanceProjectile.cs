using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class EnemyLanceProjectile : NetworkBehaviour
{
    [SerializeField] private EnemyLanceSettings settings;
    private Vector3 velocity, flightPosition, landingPoint;
    private float expires, remainingFlight;
    private Rigidbody body;
    private Collider landingSurface;
    private bool embedded;
    private static readonly IComparer<RaycastHit> ByDistance = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));
    private readonly RaycastHit[] hits = new RaycastHit[16];
    public bool IsEmbedded => embedded;
    public Collider PlannedSurface => landingSurface;
    public Vector3 PlannedLanding => landingPoint;
    public Vector3 Velocity => velocity;
    private void Awake() { body = GetComponent<Rigidbody>(); body.useGravity = false; body.isKinematic = true; }
    public override void OnStartClient()
    {
        base.OnStartClient();
        // The server uses kinematic MovePosition; observers use NetworkTransform alone.
        if (!IsServerInitialized) body.interpolation = RigidbodyInterpolation.None;
    }
    public void Launch(EnemyLanceSettings profile, Vector3 aimPoint, Collider support)
    {
        settings = profile; embedded = false; expires = Time.time + settings.lifetime;
        PlanLanding(aimPoint, support);
        flightPosition = body.position;
        velocity = PlanVelocity(flightPosition, aimPoint);
        Vector3 direction = velocity.normalized;
        body.rotation = Facing(direction);
        transform.SetPositionAndRotation(body.position, body.rotation);
    }
    private void PlanLanding(Vector3 aimPoint, Collider support)
    {
        landingSurface = null; landingPoint = aimPoint;
        if (support == null || !support.enabled || !support.gameObject.activeInHierarchy)
        {
            support = null;
            if (Physics.Raycast(aimPoint, Vector3.down, out var below, settings.landingSearchDepth,
                settings.landingLayers, QueryTriggerInteraction.Ignore)) support = below.collider;
        }
        if (support == null) return;
        var bounds = support.bounds;
        float inset = Mathf.Min(settings.landingEdgeInset, bounds.extents.x);
        float horizontal = aimPoint.x - body.position.x;
        float followThrough = Mathf.Abs(horizontal) > 0.1f ? Mathf.Sign(horizontal) * settings.landingFollowThrough : 0f;
        float x = Mathf.Clamp(aimPoint.x + followThrough,
            bounds.min.x + inset, bounds.max.x - inset);
        var ray = new Ray(new Vector3(x, bounds.max.y + 1f, Mathf.Clamp(aimPoint.z, bounds.min.z, bounds.max.z)), Vector3.down);
        if (!support.Raycast(ray, out var hit, bounds.size.y + 2f)) return;
        landingSurface = support; landingPoint = hit.point;
    }
    private Vector3 PlanVelocity(Vector3 start, Vector3 aimPoint)
    {
        float gravity = Mathf.Max(0.01f, settings.gravity);
        Vector3 delta = landingPoint - start;
        remainingFlight = Mathf.Max(settings.minimumFlightTime, delta.magnitude / Mathf.Max(1f, settings.throwSpeed));
        // A shallow ballistic curve passes the captured player position before
        // landing a little beyond it. This works for airborne targets too.
        float fraction = Mathf.Abs(delta.x) > 0.01f ? (aimPoint.x - start.x) / delta.x : 0f;
        if (landingSurface != null && fraction > 0.05f && fraction < 0.95f)
        {
            float clearance = aimPoint.y - Mathf.Lerp(start.y, landingPoint.y, fraction);
            float aimedTime = Mathf.Sqrt(2f * Mathf.Max(0f, clearance) / (gravity * fraction * (1f - fraction)));
            remainingFlight = Mathf.Max(remainingFlight, aimedTime);
        }
        else if (landingSurface != null && aimPoint.y + settings.arcHeight >= start.y)
        {
            // Nearly vertical throws still rise above the intended ledge before
            // landing. Lower targets need no forced upward lob.
            float apex = Mathf.Max(start.y, aimPoint.y) + settings.arcHeight;
            float clearanceTime = Mathf.Sqrt(2f * (apex - start.y) / gravity)
                + Mathf.Sqrt(2f * Mathf.Max(0f, apex - landingPoint.y) / gravity);
            remainingFlight = Mathf.Max(remainingFlight, clearanceTime);
        }
        return delta / remainingFlight + Vector3.up * (0.5f * gravity * remainingFlight);
    }
    private void FixedUpdate()
    {
        if (!IsServerInitialized || settings == null) return;
        if (Time.time >= expires) { Despawn(); return; }
        if (embedded) return;
        bool planned = landingSurface != null && landingSurface.enabled && landingSurface.gameObject.activeInHierarchy;
        if (!planned) landingSurface = null;
        float step = planned ? Mathf.Min(Time.fixedDeltaTime, remainingFlight) : Time.fixedDeltaTime;
        Vector3 gravity = Vector3.down * Mathf.Max(0.01f, settings.gravity);
        Vector3 displacement = velocity * step + gravity * (0.5f * step * step);
        Vector3 nextVelocity = velocity + gravity * step;
        Vector3 direction = displacement.sqrMagnitude > 0.000001f ? displacement.normalized : velocity.normalized;
        Vector3 tipPosition = flightPosition + body.rotation * Vector3.right * settings.tipOffset;
        Vector3 tipTravel = flightPosition + displacement + direction * settings.tipOffset - tipPosition;
        int count = Physics.SphereCastNonAlloc(tipPosition, settings.hitRadius, tipTravel.normalized, hits,
            tipTravel.magnitude, settings.collisionLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, 0, count, ByDistance);
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            bool platform = (settings.landingLayers.value & (1 << hit.collider.gameObject.layer)) != 0
                || hit.collider.GetComponentInParent<OneWayPlatform>() != null;
            // The destination is an intent, not a platform-count budget. Cross
            // intervening decks and the destination's underside on upward throws.
            if (platform && planned && (hit.collider != landingSurface || velocity.y > 0f)) continue;
            if (platform && !planned && velocity.y > 0f) continue;
            var victim = hit.collider.GetComponentInParent<PlayerHealth>();
            if (victim != null)
            {
                victim.TakeHit(settings.damage, new Vector3(Mathf.Sign(velocity.x) * settings.knockback.x, settings.knockback.y, 0f));
                Despawn(); return;
            }
            Embed(hit.point, hit.normal, direction); return;
        }
        flightPosition += displacement; velocity = nextVelocity; remainingFlight -= step;
        // Resolve the final fraction at the captured surface point. This also
        // avoids native sweeps missing a surface when their origin overlaps it.
        if (planned && remainingFlight <= 0.0001f)
        { Embed(landingPoint, Vector3.up, direction); return; }
        // The shaft's center follows the ballistic path. Rotating about the tip
        // would add sideways center motion, especially around the top of an arc.
        body.MovePosition(flightPosition);
        body.MoveRotation(Facing(direction));
    }
    private static Quaternion Facing(Vector3 direction) => Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    private void Embed(Vector3 point, Vector3 normal, Vector3 direction)
    {
        velocity = Vector3.zero; expires = Time.time + settings.embeddedLifetime;
        Pin(point - direction * (settings.tipOffset - settings.embedDepth), Facing(direction));
        Impact(point, normal);
    }
    [ObserversRpc(RunLocally = true)]
    private void Impact(Vector3 position, Vector3 normal)
    {
        if (settings != null) ParticleBurstEmitter.Emit(settings.impactParticles, position, normal);
    }
    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void Pin(Vector3 position, Quaternion rotation)
    {
        embedded = true;
        // Stop interpolation too: otherwise observers can visibly creep toward the
        // last airborne packet after the impact. The buffered pose serves late joins.
        var sync = GetComponent<FishNet.Component.Transforming.NetworkTransform>();
        if (sync != null) sync.enabled = false;
        body.interpolation = RigidbodyInterpolation.None;
        body.position = position; body.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
    }
}
