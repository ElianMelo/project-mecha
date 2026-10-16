using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class EnemyMotor : MonoBehaviour
{
    [SerializeField] private EnemyTraversalSettings settings;
    private Rigidbody body;
    private CapsuleCollider capsule;
    private EnemyCrowdAvoidance crowd;
    private readonly RaycastHit[] hits = new RaycastHit[12];
    private float nextJump;
    public EnemyTraversalSettings Settings => settings;
    public bool IsGrounded { get; private set; }
    public Collider Support { get; private set; }
    public Vector3 Velocity => body.linearVelocity;
    public float FeetY => capsule.bounds.min.y;
    private void Awake() { body = GetComponent<Rigidbody>(); capsule = GetComponent<CapsuleCollider>(); crowd = GetComponent<EnemyCrowdAvoidance>(); }

    public void Tick()
    {
        CheckGround();
        float gravity = body.linearVelocity.y > 0f ? settings.risingGravity : settings.fallingGravity;
        var velocity = body.linearVelocity;
        velocity.y = Mathf.Max(velocity.y - gravity * Time.fixedDeltaTime, -settings.terminalVelocity);
        body.linearVelocity = velocity;
    }
    private void CheckGround()
    {
        IsGrounded = false; Support = null;
        if (body.linearVelocity.y > 0.1f) return;
        var bounds = capsule.bounds;
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + settings.probeInset, bounds.center.z);
        int count = Physics.BoxCastNonAlloc(origin, new Vector3(bounds.extents.x * 0.8f, 0.02f, bounds.extents.z * 0.7f),
            Vector3.down, hits, Quaternion.identity, settings.groundProbe, settings.groundLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (hit.normal.y < 0.5f || hit.distance >= nearest || Physics.GetIgnoreCollision(capsule, hit.collider)) continue;
            Support = hit.collider; nearest = hit.distance; IsGrounded = true;
        }
    }
    public void Move(float desiredSpeed, bool avoidCrowd = true)
    {
        if (avoidCrowd && crowd != null && IsGrounded) desiredSpeed = crowd.Steer(desiredSpeed);
        float acceleration = IsGrounded ? settings.acceleration : settings.airAcceleration;
        body.linearVelocity = new Vector3(Mathf.MoveTowards(body.linearVelocity.x, desiredSpeed, acceleration * Time.fixedDeltaTime), body.linearVelocity.y, 0f);
    }
    public bool Jump(float height = float.PositiveInfinity)
    {
        if (!IsGrounded || Time.time < nextJump) return false;
        nextJump = Time.time + settings.jumpCooldown;
        float speed = Mathf.Min(settings.jumpSpeed, Mathf.Sqrt(2f * settings.risingGravity *
            Mathf.Max(settings.minimumHopHeight, height + settings.landingClearance)));
        body.linearVelocity = new Vector3(body.linearVelocity.x, speed, 0f);
        IsGrounded = false; Support = null;
        return true;
    }
    public bool Drop()
    {
        if (!IsGrounded || Support == null || !Support.TryGetComponent<OneWayPlatform>(out var platform)) return false;
        platform.Drop(capsule);
        body.linearVelocity = new Vector3(body.linearVelocity.x, -settings.dropSpeed, 0f);
        IsGrounded = false; Support = null;
        return true;
    }
}
