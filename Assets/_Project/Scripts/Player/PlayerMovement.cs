using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerInputs))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float movementSpeed = 4.5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private GameObject visuals;
    [Header("Dash and chaining")]
    [SerializeField] private float dashSpeed = 11.5f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.12f;
    [SerializeField] private float dashBuffer = 0.14f;
    [Header("Jump feel")]
    [SerializeField] private float risingGravity = 24f;
    [SerializeField] private float fallingGravity = 40f;
    [SerializeField] private float jumpCutGravity = 52f;
    [SerializeField] private float apexGravity = 14f;
    [SerializeField] private float terminalVelocity = 20f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBuffer = 0.12f;
    [SerializeField] private LayerMask groundLayer = (1 << 6) | (1 << 7);
    private Rigidbody body;
    private CapsuleCollider capsule;
    private PlayerInputs inputs;
    private PlayerAttack attack;
    private PlayerHealth health;
    private Animator animator;
    private readonly RaycastHit[] groundHits = new RaycastHit[12];
    private float jumpRequested = -10f, lastGrounded = -10f, dashUntil, nextDash, momentumUntil;
    private int dashDirection = 1;
    private int requestedDashDirection;
    private float dashRequestedUntil = -10f;
    private bool airDashUsed, running;
    public bool IsGrounded { get; private set; }
    public bool IsDashing => Time.time < dashUntil;
    public int Facing { get; private set; } = 1;
    public Collider Support { get; private set; }
    public Vector3 Velocity => body.linearVelocity;
    public bool HasControl => !NetworkObject.IsSpawned || IsOwner;
    private void Awake()
    {
        body = GetComponent<Rigidbody>(); capsule = GetComponent<CapsuleCollider>();
        inputs = GetComponent<PlayerInputs>(); attack = GetComponent<PlayerAttack>(); health = GetComponent<PlayerHealth>();
        animator = GetComponentInChildren<Animator>();
        body.useGravity = false; body.linearDamping = 0f;
        body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
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
        if (!HasControl) return;
        bool wasGrounded = IsGrounded;
        CheckGround();
        if (IsGrounded) { lastGrounded = Time.time; airDashUsed = false; }
        if (!wasGrounded && IsGrounded) attack?.HandleLanding();
        if (health != null && health.IsStunned)
        {
            dashUntil = 0f; running = false; jumpRequested = dashRequestedUntil = -10f;
            ApplyGravity(); UpdateAnimation(); return;
        }
        // Evaluate held input again after support changes and recovery windows open.
        if (inputs.DownHeld) HandleDown();
        if (Time.time <= dashRequestedUntil) TryStartDash(requestedDashDirection);
        if (Time.time - jumpRequested <= jumpBuffer && Time.time - lastGrounded <= coyoteTime && TryCancel())
        {
            bool rocket = IsDashing || Time.time < momentumUntil;
            body.linearVelocity = new Vector3(rocket ? dashDirection * dashSpeed : body.linearVelocity.x, jumpForce, 0f);
            jumpRequested = lastGrounded = -10f; IsGrounded = false; Support = null;
            if (rocket) momentumUntil = Time.time + 0.28f;
        }
        float direction = Mathf.Abs(inputs.MoveInput.x) > 0.25f ? Mathf.Sign(inputs.MoveInput.x) : 0f;
        if (attack == null || !attack.IsBusy)
        {
            if (direction != 0f) Face((int)direction);
            if (direction == 0f) running = false;
            float target = IsDashing ? dashDirection * dashSpeed :
                Time.time < momentumUntil ? dashDirection * dashSpeed : direction * (running ? runSpeed : movementSpeed);
            float acceleration = IsGrounded ? 90f : 45f;
            body.linearVelocity = new Vector3(IsDashing ? target : Mathf.MoveTowards(body.linearVelocity.x, target, acceleration * Time.fixedDeltaTime), body.linearVelocity.y, 0f);
        }
        else if (!IsGrounded && attack.CanAirSteer)
            body.linearVelocity = new Vector3(direction == 0f ? body.linearVelocity.x * 0.98f : Mathf.MoveTowards(body.linearVelocity.x, direction * movementSpeed, 20f * Time.fixedDeltaTime), body.linearVelocity.y, 0f);
        else body.linearVelocity = new Vector3(body.linearVelocity.x * 0.85f, body.linearVelocity.y, 0f);
        ApplyGravity(); UpdateAnimation();
    }
    private void ApplyGravity()
    {
        float y = body.linearVelocity.y;
        float gravity = y < -0.4f ? fallingGravity : !inputs.JumpHeld && y > 0f ? jumpCutGravity : Mathf.Abs(y) < 1.5f ? apexGravity : risingGravity;
        body.linearVelocity = new Vector3(body.linearVelocity.x, Mathf.Max(y - gravity * Time.fixedDeltaTime, -terminalVelocity), 0f);
    }
    private void CheckGround()
    {
        IsGrounded = false; Support = null;
        if (body.linearVelocity.y > 0.1f) return;
        Bounds b = capsule.bounds;
        Vector3 origin = new Vector3(b.center.x, b.min.y + 0.08f, b.center.z);
        int count = Physics.BoxCastNonAlloc(origin, new Vector3(b.extents.x * 0.8f, 0.02f, b.extents.z * 0.75f), Vector3.down, groundHits, Quaternion.identity, 0.15f, groundLayer, QueryTriggerInteraction.Ignore);
        float closest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            var h = groundHits[i];
            if (h.normal.y < 0.5f || Physics.GetIgnoreCollision(capsule, h.collider) || h.distance >= closest) continue;
            closest = h.distance; Support = h.collider; IsGrounded = true;
        }
    }
    private bool TryCancel() => attack == null || attack.TryMovementCancel();
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
        dashRequestedUntil = Time.time + dashBuffer;
        TryStartDash(direction);
    }
    private void TryStartDash(int direction)
    {
        // A ground dash may chain into the available air dash after jumping.
        bool airTransition = !IsGrounded && !airDashUsed;
        if ((!airTransition && Time.time < nextDash) || (!IsGrounded && airDashUsed) || !TryCancel()) return;
        dashRequestedUntil = -10f;
        dashDirection = direction == 0 ? Facing : direction; Face(dashDirection);
        if (!IsGrounded) airDashUsed = true;
        dashUntil = Time.time + dashDuration; nextDash = dashUntil + dashCooldown; running = true;
        body.linearVelocity = new Vector3(dashDirection * dashSpeed, body.linearVelocity.y, 0f);
        GetComponent<PlayerCombatVfx>()?.Dash(Facing);
    }
    public void HandleDown()
    {
        if (!HasControl || (health != null && health.IsStunned) || !TryCancel()) return;
        if (Support != null && Support.TryGetComponent<OneWayPlatform>(out var platform))
        {
            platform.Drop(capsule); lastGrounded = jumpRequested = -10f; IsGrounded = false; Support = null;
            body.linearVelocity = new Vector3(body.linearVelocity.x, -4f, 0f);
        }
        else if (!IsGrounded)
            body.linearVelocity = new Vector3(body.linearVelocity.x, Mathf.Min(body.linearVelocity.y, -6f), 0f);
    }
    public void StopDash() { dashUntil = momentumUntil = 0f; dashRequestedUntil = -10f; running = false; }
    public void Face(int direction)
    {
        Facing = direction >= 0 ? 1 : -1;
        transform.rotation = Quaternion.Euler(0f, 90f * Facing, 0f);
    }
    private void UpdateAnimation()
    {
        if (animator == null) return;
        animator.SetBool("Running", IsGrounded && Mathf.Abs(body.linearVelocity.x) > 0.4f && (attack == null || !attack.IsBusy));
        animator.SetBool("Grounded", IsGrounded);
        animator.SetFloat("VerticalSpeed", body.linearVelocity.y);
        animator.SetBool("Dashing", IsDashing);
    }
}
