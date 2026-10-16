using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class EnemyLanceProjectile : NetworkBehaviour
{
    private EnemyLanceSettings settings;
    private Vector3 velocity;
    private float expires;
    private Rigidbody body;
    private readonly RaycastHit[] hits = new RaycastHit[16];
    private void Awake() { body = GetComponent<Rigidbody>(); body.useGravity = false; body.isKinematic = true; }
    public void Launch(EnemyLanceSettings profile, Vector3 initialVelocity)
    { settings = profile; velocity = initialVelocity; expires = Time.time + settings.lifetime; }
    private void FixedUpdate()
    {
        if (!IsServerInitialized || settings == null) return;
        if (Time.time >= expires) { Despawn(); return; }
        Vector3 displacement = velocity * Time.fixedDeltaTime;
        int count = Physics.SphereCastNonAlloc(body.position, settings.hitRadius, displacement.normalized, hits,
            displacement.magnitude, settings.collisionLayers, QueryTriggerInteraction.Ignore);
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
            Despawn(); return;
        }
        body.MovePosition(body.position + displacement);
        // A Transform write here cancels the kinematic MovePosition target before
        // physics simulates. Keep both movement and facing on the Rigidbody.
        body.MoveRotation(Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg));
        velocity += Vector3.down * settings.gravity * Time.fixedDeltaTime;
    }
}
