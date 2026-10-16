using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerInputs))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private PlayerMovementSettings settings;
    [SerializeField] private GameObject visuals;
    private Rigidbody body;
    private CapsuleCollider capsule;
    private PlayerInputs inputs;
    private PlayerAttack attack;
    private PlayerHealth health;
    private PlayerLedgeAssist ledgeAssist;
    private readonly RaycastHit[] groundHits = new RaycastHit[12];
    private float jumpRequested = -10f, lastGrounded = -10f, dashUntil, nextDash, momentumUntil;
    private int dashDirection = 1;
    private int requestedDashDirection;
    private float dashRequestedUntil = -10f;
    private bool airDashUsed, running;
    private float serverDashUntil, serverNextDash;
    private bool serverAirDashUsed;
    private readonly SyncVar<bool> observedGrounded = new SyncVar<bool>();
    private bool locallyGrounded;
    private Vector3 previousObservedPosition, observedVelocity;
    public bool IsGrounded
    {
        get => NetworkObject.IsSpawned && !IsOwner && !IsServerInitialized ? observedGrounded.Value : locallyGrounded;
        private set => locallyGrounded = value;
    }
    public bool IsDashing => Time.time < dashUntil || IsServerInitialized && Time.time < serverDashUntil;
    private int localFacing = 1;
    public int Facing => NetworkObject.IsSpawned && !IsOwner ? (transform.forward.x >= 0f ? 1 : -1) : localFacing;
    public Collider Support { get; private set; }
    public Vector3 Velocity => HasControl ? body.linearVelocity : observedVelocity;
    public bool HasControl => !NetworkObject.IsSpawned || IsOwner;
    public void ResetTraversal()
    {
        StopDash(); IsGrounded = false; Support = null;
        jumpRequested = lastGrounded = dashRequestedUntil = -10f;
        airDashUsed = serverAirDashUsed = false; nextDash = serverNextDash = 0f;
        previousObservedPosition = transform.position; observedVelocity = Vector3.zero;
    }
    private void Awake()
    {
        body = GetComponent<Rigidbody>(); capsule = GetComponent<CapsuleCollider>();
        inputs = GetComponent<PlayerInputs>(); attack = GetComponent<PlayerAttack>(); health = GetComponent<PlayerHealth>();
        ledgeAssist = GetComponent<PlayerLedgeAssist>();
        body.useGravity = false; body.linearDamping = 0f;
        body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        previousObservedPosition = transform.position;
    }
    private void OnEnable()
    {
        OneWayPlatform.Register(GetComponent<CapsuleCollider>());
        inputs.OnJumpInput += HandleJump; inputs.OnDownInput += HandleDown; inputs.OnDashInput += HandleDash;
    }
    private void OnDisable()
    {
        OneWayPlatform.Unregister(capsule);
        inputs.OnJumpInput -= HandleJump; inputs.OnDownInput -= HandleDown; inputs.OnDashInput -= HandleDash;
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        body.isKinematic = !IsOwner;
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        body.isKinematic = !Owner.IsLocalClient;
    }
    private void FixedUpdate()
    {
        if (!HasControl)
        {
            observedVelocity = Vector3.Lerp(observedVelocity, Vector3.ClampMagnitude((transform.position - previousObservedPosition) / Time.fixedDeltaTime, settings.terminalVelocity), 0.4f);
            previousObservedPosition = transform.position;
            if (IsServerInitialized) { CheckGround(); observedGrounded.Value = IsGrounded; if (IsGrounded) serverAirDashUsed = false; }
            return;
        }
        bool wasGrounded = IsGrounded;
        if ((health == null || !health.IsStunned) && (attack == null || !attack.IsBusy)) ledgeAssist?.TryAssist(settings, inputs);
        CheckGround();
        if (IsServerInitialized) observedGrounded.Value = IsGrounded;
        if (IsGrounded) { lastGrounded = Time.time; airDashUsed = serverAirDashUsed = false; }
        if (!wasGrounded && IsGrounded) attack?.HandleLanding();
        if (health != null && health.IsStunned)
        {
            dashUntil = 0f; running = false; jumpRequested = dashRequestedUntil = -10f;
            ApplyGravity(); return;
        }
        // Evaluate held input again after support changes and recovery windows open.
        if (inputs.DownHeld) HandleDown();
        if (Time.time <= dashRequestedUntil) TryStartDash(requestedDashDirection);
        if (Time.time - jumpRequested <= settings.jumpBuffer && Time.time - lastGrounded <= settings.coyoteTime && TryCancel())
        {
            bool rocket = IsDashing || Time.time < momentumUntil;
            body.linearVelocity = new Vector3(rocket ? dashDirection * settings.dashSpeed : body.linearVelocity.x, settings.jumpSpeed, 0f);
            jumpRequested = lastGrounded = -10f; IsGrounded = false; Support = null;
            attack?.BeginAirCombo();
            if (rocket) momentumUntil = Time.time + settings.rocketMomentum;
        }
        float direction = Mathf.Abs(inputs.MoveInput.x) > 0.25f ? Mathf.Sign(inputs.MoveInput.x) : 0f;
        if (attack == null || !attack.IsBusy)
        {
            if (direction != 0f) Face((int)direction);
            if (direction == 0f) running = false;
            float target = IsDashing ? dashDirection * settings.dashSpeed :
                Time.time < momentumUntil ? dashDirection * settings.dashSpeed : direction * (running ? settings.runSpeed : settings.walkSpeed);
            float acceleration = IsGrounded ? settings.groundAcceleration : settings.airAcceleration;
            body.linearVelocity = new Vector3(IsDashing ? target : Mathf.MoveTowards(body.linearVelocity.x, target, acceleration * Time.fixedDeltaTime), body.linearVelocity.y, 0f);
        }
        else if (!IsGrounded && attack.CanAirSteer)
            body.linearVelocity = new Vector3(direction == 0f ? body.linearVelocity.x * 0.98f : Mathf.MoveTowards(body.linearVelocity.x, direction * settings.walkSpeed, settings.airAttackAcceleration * Time.fixedDeltaTime), body.linearVelocity.y, 0f);
        else body.linearVelocity = new Vector3(body.linearVelocity.x * 0.85f, body.linearVelocity.y, 0f);
        ApplyGravity();
    }
    private void ApplyGravity()
    {
        float y = body.linearVelocity.y;
        float gravity = y < -0.4f ? settings.fallingGravity : Mathf.Abs(y) < settings.apexSpeed ? settings.apexGravity : settings.risingGravity;
        body.linearVelocity = new Vector3(body.linearVelocity.x, Mathf.Max(y - gravity * Time.fixedDeltaTime, -settings.terminalVelocity), 0f);
    }
    private void CheckGround()
    {
        IsGrounded = false; Support = null;
        if ((HasControl ? body.linearVelocity.y : observedVelocity.y) > 0.1f) return;
        Bounds b = capsule.bounds;
        Vector3 origin = new Vector3(b.center.x, b.min.y + settings.groundProbeInset, b.center.z);
        int count = Physics.BoxCastNonAlloc(origin, new Vector3(b.extents.x * 0.8f, 0.02f, b.extents.z * 0.75f), Vector3.down, groundHits, Quaternion.identity, settings.groundProbe, settings.groundLayers, QueryTriggerInteraction.Ignore);
        float closest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            var h = groundHits[i];
            if (h.normal.y < 0.5f || Physics.GetIgnoreCollision(capsule, h.collider) || h.distance >= closest) continue;
            closest = h.distance; Support = h.collider; IsGrounded = true;
        }
    }
    private bool TryCancel() => attack == null || attack.TryMovementCancel();
    private void OnCollisionEnter(Collision collision) => CaptureLanding(collision);
    private void OnCollisionStay(Collision collision) => CaptureLanding(collision);
    private void CaptureLanding(Collision collision)
    {
        if (!HasControl || body.linearVelocity.y > 0.1f || (settings.groundLayers.value & (1 << collision.gameObject.layer)) == 0) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y < 0.5f) continue;
            bool landed = !IsGrounded;
            IsGrounded = true; Support = collision.collider; lastGrounded = Time.time;
            if (landed) attack?.HandleLanding();
            break;
        }
    }
    public void HandleJump()
    {
        if (!HasControl || (health != null && health.IsStunned)) return;
        if (attack != null && attack.TryRisingBranch()) return;
        jumpRequested = Time.time;
    }
    public void HandleDash(int direction)
    {
        if (!HasControl || (health != null && health.IsStunned)) return;
        requestedDashDirection = direction;
        dashRequestedUntil = Time.time + settings.dashBuffer;
        TryStartDash(direction);
    }
    private void TryStartDash(int direction)
    {
        // A ground dash may chain into the available air dash after jumping.
        bool airTransition = !IsGrounded && !airDashUsed;
        if ((!airTransition && Time.time < nextDash) || (!IsGrounded && airDashUsed)) return;
        attack?.CancelForDash();
        dashRequestedUntil = -10f;
        dashDirection = direction == 0 ? Facing : direction; Face(dashDirection);
        if (!IsGrounded) airDashUsed = true;
        dashUntil = Time.time + settings.dashDuration; nextDash = dashUntil + settings.dashCooldown; running = true;
        body.linearVelocity = new Vector3(dashDirection * settings.dashSpeed, body.linearVelocity.y, 0f);
        GetComponent<PlayerCombatVfx>()?.Dash(Facing, true);
        if (NetworkObject.IsSpawned) RequestDash(dashDirection);
    }
    [ServerRpc]
    private void RequestDash(int direction)
    {
        if ((direction != -1 && direction != 1) || (health != null && health.IsStunned)) { RejectDash(Owner); return; }
        bool airTransition = !IsGrounded && !serverAirDashUsed;
        if ((!airTransition && Time.time + settings.dashNetworkGrace < serverNextDash) || (!IsGrounded && serverAirDashUsed))
        { RejectDash(Owner); return; }
        serverAirDashUsed = !IsGrounded;
        serverDashUntil = Time.time + settings.dashDuration;
        serverNextDash = serverDashUntil + settings.dashCooldown;
        attack?.CancelForDashOnServer();
        ShowDash(direction);
    }
    [TargetRpc]
    private void RejectDash(FishNet.Connection.NetworkConnection connection) => StopDash();
    [ObserversRpc(ExcludeOwner = true)]
    private void ShowDash(int direction)
    {
        dashUntil = Time.time + settings.dashDuration; dashDirection = direction;
        attack?.CancelForDash();
        GetComponent<PlayerCombatVfx>()?.Dash(direction, true);
    }
    public void HandleDown()
    {
        if (!HasControl || (health != null && health.IsStunned) || !TryCancel()) return;
        if (Support != null && Support.TryGetComponent<OneWayPlatform>(out var platform))
        {
            platform.Drop(capsule); lastGrounded = jumpRequested = -10f; IsGrounded = false; Support = null;
            body.linearVelocity = new Vector3(body.linearVelocity.x, -settings.dropSpeed, 0f);
        }
        else if (!IsGrounded)
            body.linearVelocity = new Vector3(body.linearVelocity.x, Mathf.Min(body.linearVelocity.y, -settings.fastFallSpeed), 0f);
    }
    public void StopDash() { dashUntil = serverDashUntil = momentumUntil = 0f; dashRequestedUntil = -10f; running = false; }
    public void Face(int direction)
    {
        localFacing = direction >= 0 ? 1 : -1;
        transform.rotation = Quaternion.Euler(0f, 90f * Facing, 0f);
    }
}
