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
    private float jumpRequested = -10f, lastGrounded = -10f, dashUntil, nextDash;
    private int dashDirection = 1;
    private int requestedDashDirection;
    private float dashRequestedUntil = -10f;
    private bool airDashUsed, running;
    public enum ImpulseKind { Dash, Rocket, Mushidon }
    private ImpulseKind impulse;
    private float impulseStart, impulseSpeed, impulseDuration, rocketStarted = -10f, stepUntil;
    private float stepSpeed;
    private bool pausedMotion;
    private Vector3 savedMotion;
    public string MovementState => health != null && health.IsStunned ? "Hit stun" : attack != null && attack.IsBusy ? "Action" : IsDashing ? impulse.ToString() : Time.time < nextDash ? "Recovery" :
        !IsGrounded ? "Air" : Mathf.Abs(Velocity.x) < 0.1f ? "Idle" : running ? "Run" : "Walk";
    public float RecoveryRemaining => Mathf.Max(0f, nextDash - Time.time);
    public bool IsPassingThrough => IsDashing || attack != null && attack.IsSideSwitchActive;
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
    public bool IsDashing => HasControl || !IsServerInitialized ? Time.time < dashUntil : Time.time < serverDashUntil;
    private int localFacing = 1;
    public int Facing => NetworkObject.IsSpawned && !IsOwner ? (transform.forward.x >= 0f ? 1 : -1) : localFacing;
    public Collider Support { get; private set; }
    public Vector3 Velocity => HasControl ? body.linearVelocity : observedVelocity;
    public bool HasControl => !NetworkObject.IsSpawned || IsOwner;
    public void ResetTraversal()
    {
        StopDash(); pausedMotion = false; stepUntil = 0f; rocketStarted = -10f; IsGrounded = false; Support = null;
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
    // NetworkTransform's Rigidbody configuration owns network authority and disables
    // Rigidbody interpolation on replicas, including ownership changes.
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
            StopDash(); pausedMotion = false; jumpRequested = dashRequestedUntil = -10f;
            ApplyGravity(); return;
        }
        // Evaluate held input again after support changes and recovery windows open.
        if (attack != null && attack.IsInHitStop)
        {
            if (!pausedMotion) { savedMotion = body.linearVelocity; pausedMotion = true; }
            stepUntil += Time.fixedDeltaTime;
            body.linearVelocity = Vector3.zero; return;
        }
        if (pausedMotion) { body.linearVelocity = savedMotion; pausedMotion = false; }
        if (inputs.DownHeld && (attack == null || !attack.IsBusy) && !IsDashing) ApplyDescent();
        if (Time.time <= dashRequestedUntil) TryStartDash(requestedDashDirection);
        if (Time.time - jumpRequested <= settings.jumpBuffer && Time.time - lastGrounded <= settings.coyoteTime && TryCancel())
        {
            bool rocket = IsDashing && impulse == ImpulseKind.Dash && Time.time - impulseStart <= settings.chordWindow;
            body.linearVelocity = new Vector3(body.linearVelocity.x, settings.jumpSpeed, 0f);
            jumpRequested = lastGrounded = -10f; IsGrounded = false; Support = null;
            attack?.BeginAirCombo();
            if (rocket) BeginRocket();
        }
        float direction = Mathf.Abs(inputs.MoveInput.x) > 0.25f ? Mathf.Sign(inputs.MoveInput.x) : 0f;
        if (attack == null || !attack.IsBusy)
        {
            if (IsDashing)
            {
                float elapsed = Time.time - impulseStart;
                // The first few frames commit to the impulse; braking opens afterward.
                if (elapsed >= settings.brakeLock && direction == -dashDirection)
                {
                    StopDash(); Face((int)direction);
                    if (NetworkObject.IsSpawned) RequestBrake();
                }
                else
                {
                    float speed = Mathf.Lerp(impulseSpeed, impulseSpeed * settings.impulseEndRatio,
                        Mathf.Clamp01(elapsed / Mathf.Max(0.01f, impulseDuration)));
                    body.linearVelocity = new Vector3(dashDirection * speed, body.linearVelocity.y, 0f);
                }
            }
            else
            {
                if (direction != 0f) Face((int)direction);
                if (direction == 0f || direction != dashDirection) running = false;
                float target = direction * (running ? settings.runSpeed : settings.walkSpeed);
                float acceleration = IsGrounded ? settings.groundAcceleration : settings.airAcceleration;
                if (Mathf.Abs(body.linearVelocity.x) > Mathf.Abs(target) && direction == Mathf.Sign(body.linearVelocity.x))
                    acceleration = settings.slideDrag;
                body.linearVelocity = new Vector3(Mathf.MoveTowards(body.linearVelocity.x, target, acceleration * Time.fixedDeltaTime), body.linearVelocity.y, 0f);
            }
        }
        else
        {
            float target = Time.time < stepUntil ? stepSpeed : 0f;
            var action = attack.ActiveData;
            if (action != null && action.sideSwitch)
                body.linearVelocity = new Vector3(target, body.linearVelocity.y, 0f);
            else if (action != null && action.diveSpeed > 0f)
                body.linearVelocity = new Vector3(target, -action.diveSpeed, 0f);
            else
                body.linearVelocity = new Vector3(Mathf.MoveTowards(body.linearVelocity.x, target, settings.attackDrag * Time.fixedDeltaTime), body.linearVelocity.y, 0f);
            if (attack.IsSkillActive) { body.linearVelocity = Vector3.zero; return; }
        }
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
        if (IsDashing && Time.time - impulseStart <= settings.chordWindow && impulse == ImpulseKind.Dash)
        { BeginRocket(); return; }
        jumpRequested = Time.time;
    }
    public void HandleDash(int direction)
    {
        if (!HasControl || (health != null && health.IsStunned)) return;
        if (attack != null && attack.IsBusy)
        {
            attack.TryCriticalBranch(direction == 0 ? Facing : direction);
            return;
        }
        requestedDashDirection = direction;
        dashRequestedUntil = Time.time + settings.dashBuffer;
        TryStartDash(direction);
    }
    private void TryStartDash(int direction)
    {
        // A ground dash may chain into the available air dash after jumping.
        bool airTransition = !IsGrounded && !airDashUsed;
        if ((!airTransition && Time.time < nextDash) || (!IsGrounded && airDashUsed)) return;
        if (attack != null && attack.IsBusy) return;
        dashRequestedUntil = -10f;
        dashDirection = direction == 0 ? Facing : direction; Face(dashDirection);
        if (!IsGrounded) airDashUsed = true;
        impulse = ImpulseKind.Dash; impulseStart = Time.time;
        impulseSpeed = settings.dashSpeed; impulseDuration = settings.dashDuration;
        dashUntil = Time.time + impulseDuration; nextDash = dashUntil + settings.dashCooldown; running = true;
        body.linearVelocity = new Vector3(dashDirection * settings.dashSpeed, body.linearVelocity.y, 0f);
        GetComponent<PlayerCombatVfx>()?.Dash(Facing, true);
        if (NetworkObject.IsSpawned) RequestDash(dashDirection);
        if (Time.time - inputs.LastJumpPressed <= settings.chordWindow) BeginRocket();
    }
    [ServerRpc]
    private void RequestDash(int direction)
    {
        if ((direction != -1 && direction != 1) || (health != null && health.IsStunned) || attack != null && attack.ServerBusy) { RejectDash(Owner); return; }
        bool airTransition = !IsGrounded && !serverAirDashUsed;
        if ((!airTransition && Time.time + settings.dashNetworkGrace < serverNextDash) || (!IsGrounded && serverAirDashUsed))
        { RejectDash(Owner); return; }
        serverImpulse = ImpulseKind.Dash; serverDashStarted = Time.time;
        serverAirDashUsed = !IsGrounded;
        serverDashUntil = Time.time + settings.dashDuration;
        serverNextDash = serverDashUntil + settings.dashCooldown;
        ShowDash(direction);
    }
    [TargetRpc]
    private void RejectDash(FishNet.Connection.NetworkConnection connection) => StopDash();
    [ObserversRpc(ExcludeOwner = true)]
    private void ShowDash(int direction)
    {
        dashUntil = Time.time + settings.dashDuration; dashDirection = direction;
        GetComponent<PlayerCombatVfx>()?.Dash(direction, true);
    }
    private void BeginRocket()
    {
        impulse = ImpulseKind.Rocket; impulseStart = rocketStarted = Time.time;
        impulseSpeed = settings.rocketSpeed; impulseDuration = settings.rocketDuration;
        dashUntil = Time.time + impulseDuration; nextDash = dashUntil + settings.dashCooldown;
        jumpRequested = lastGrounded = -10f; IsGrounded = false; Support = null; airDashUsed = true;
        body.linearVelocity = new Vector3(dashDirection * impulseSpeed, settings.rocketRiseSpeed, 0f);
        attack?.BeginAirCombo();
        if (NetworkObject.IsSpawned) RequestAdvancedImpulse(ImpulseKind.Rocket);
    }
    public void HandleDown()
    {
        if (!HasControl || (health != null && health.IsStunned) || attack != null && attack.IsBusy) return;
        if (IsDashing && impulse == ImpulseKind.Rocket && Time.time - rocketStarted <= settings.mushidonWindow)
        {
            impulse = ImpulseKind.Mushidon; impulseStart = Time.time;
            impulseSpeed = settings.mushidonSpeed; impulseDuration = settings.mushidonDuration;
            dashUntil = Time.time + impulseDuration; nextDash = dashUntil + settings.dashCooldown;
            body.linearVelocity = new Vector3(dashDirection * impulseSpeed, -settings.mushidonFallSpeed, 0f);
            if (NetworkObject.IsSpawned) RequestAdvancedImpulse(ImpulseKind.Mushidon);
            return;
        }
        ApplyDescent();
    }
    private void ApplyDescent()
    {
        if (Support != null && Support.TryGetComponent<OneWayPlatform>(out var platform))
        {
            platform.Drop(capsule); lastGrounded = jumpRequested = -10f; IsGrounded = false; Support = null;
            body.linearVelocity = new Vector3(body.linearVelocity.x, -settings.dropSpeed, 0f);
        }
        else if (!IsGrounded)
            body.linearVelocity = new Vector3(body.linearVelocity.x, Mathf.Min(body.linearVelocity.y, -settings.fastFallSpeed), 0f);
    }
    private ImpulseKind serverImpulse;
    private float serverRocketStarted = -10f, serverDashStarted = -10f;
    [ServerRpc]
    private void RequestAdvancedImpulse(ImpulseKind kind)
    {
        if (health != null && health.IsStunned || attack != null && attack.ServerBusy) return;
        if (kind == ImpulseKind.Rocket)
        {
            if (Time.time - serverDashStarted > settings.chordWindow + settings.dashNetworkGrace || serverImpulse != ImpulseKind.Dash) return;
            serverRocketStarted = Time.time; serverImpulse = kind;
            serverDashUntil = Time.time + settings.rocketDuration;
        }
        else if (kind == ImpulseKind.Mushidon)
        {
            if (serverImpulse != ImpulseKind.Rocket || Time.time - serverRocketStarted > settings.mushidonWindow + settings.dashNetworkGrace) return;
            serverImpulse = kind; serverDashUntil = Time.time + settings.mushidonDuration;
        }
        else return;
        serverDashStarted = Time.time;
        serverAirDashUsed = true; serverNextDash = serverDashUntil + settings.dashCooldown;
        ShowAdvancedImpulse(kind);
    }
    [ObserversRpc(ExcludeOwner = true)]
    private void ShowAdvancedImpulse(ImpulseKind kind)
    {
        impulse = kind;
        dashUntil = Time.time + (kind == ImpulseKind.Rocket ? settings.rocketDuration : settings.mushidonDuration);
    }
    [ServerRpc]
    private void RequestBrake()
    {
        if (Time.time + settings.dashNetworkGrace - serverDashStarted < settings.brakeLock) return;
        serverDashUntil = 0f;
        ShowBrake();
    }
    [ObserversRpc(ExcludeOwner = true)]
    private void ShowBrake() => dashUntil = 0f;
    public void StopDash()
    {
        dashUntil = serverDashUntil = 0f; dashRequestedUntil = -10f; running = false;
    }
    public void BeginAttackMotion(PlayerCombatSettings.ActionData action, int direction)
    {
        StopDash(); stepUntil = 0f;
        if (!HasControl) return;
        pausedMotion = false;
        body.linearVelocity = new Vector3(0f, action.diveSpeed > 0f ? -action.diveSpeed : body.linearVelocity.y, 0f);
        if (PlayerAttack.IsSkill(action.kind)) body.linearVelocity = Vector3.zero;
        if (action.sideSwitch) ApplyAttackStep(action, direction);
    }
    public void ApplyAttackStep(PlayerCombatSettings.ActionData action, int direction)
    {
        if (!HasControl) return;
        stepSpeed = direction * action.stepSpeed; stepUntil = Time.time + action.stepDuration;
        body.linearVelocity = new Vector3(stepSpeed, body.linearVelocity.y, 0f);
    }
    public void Face(int direction)
    {
        localFacing = direction >= 0 ? 1 : -1;
        if (!HasControl) return;
        transform.rotation = Quaternion.Euler(0f, 90f * Facing, 0f);
    }
}
