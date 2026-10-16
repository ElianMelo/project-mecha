using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class EnemyLanceProjectile : NetworkBehaviour
{
    [SerializeField] private EnemyLanceSettings settings;
    private Vector3 velocity;
    private float expires;
    private Rigidbody body;
    private bool embedded;
    private readonly RaycastHit[] hits = new RaycastHit[16];
    private void Awake() { body = GetComponent<Rigidbody>(); body.useGravity = false; body.isKinematic = true; }
    public override void OnStartClient()
    {
        base.OnStartClient();
        // The server uses kinematic MovePosition; observers use NetworkTransform alone.
        if (!IsServerInitialized) body.interpolation = RigidbodyInterpolation.None;
    }
    public void Launch(EnemyLanceSettings profile, Vector3 initialVelocity)
    { settings = profile; velocity = initialVelocity; embedded = false; expires = Time.time + settings.lifetime; }
    private void FixedUpdate()
    {
        if (!IsServerInitialized || settings == null) return;
        if (Time.time >= expires) { Despawn(); return; }
        if (embedded) return;
        Vector3 displacement = velocity * Time.fixedDeltaTime;
        Vector3 direction = velocity.normalized;
        Vector3 tip = body.position + body.rotation * Vector3.right * settings.tipOffset;
        Vector3 tipTravel = body.position + displacement + direction * settings.tipOffset - tip;
        int count = Physics.SphereCastNonAlloc(tip, settings.hitRadius, tipTravel.normalized, hits,
            tipTravel.magnitude, settings.collisionLayers, QueryTriggerInteraction.Ignore);
        int nearest = -1;
        for (int i = 0; i < count; i++)
        {
            if (velocity.y > 0f && hits[i].collider.GetComponent<OneWayPlatform>() != null) continue;
            if (nearest < 0 || hits[i].distance < hits[nearest].distance) nearest = i;
        }
        if (nearest >= 0)
        {
            var victim = hits[nearest].collider.GetComponentInParent<PlayerHealth>();
            if (victim != null) victim.TakeHit(settings.damage, new Vector3(Mathf.Sign(velocity.x) * settings.knockback.x, settings.knockback.y, 0f));
            if (victim != null) { Despawn(); return; }
            // Only scenery retains the lance. The tip penetrates slightly; the shaft
            // remains visible and cannot continue damaging anything after impact.
            velocity = Vector3.zero;
            expires = Time.time + settings.embeddedLifetime;
            Pin(hits[nearest].point - direction * (settings.tipOffset - settings.embedDepth),
                Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
            Impact(hits[nearest].point, hits[nearest].normal);
            return;
        }
        body.MovePosition(body.position + displacement);
        // A Transform write here cancels the kinematic MovePosition target before
        // physics simulates. Keep both movement and facing on the Rigidbody.
        body.MoveRotation(Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg));
        velocity += Vector3.down * settings.gravity * Time.fixedDeltaTime;
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
