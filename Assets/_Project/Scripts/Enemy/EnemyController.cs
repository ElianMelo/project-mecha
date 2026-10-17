using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(EnemyMotor))]
[RequireComponent(typeof(EnemyPlatformNavigator), typeof(EnemyStateMachine))]
public class EnemyController : NetworkBehaviour, ICombatTarget, ITargetable
{
    public static readonly System.Collections.Generic.HashSet<EnemyController> ActiveEnemies = new System.Collections.Generic.HashSet<EnemyController>();
    [SerializeField] private EnemySettings settings;
    private readonly SyncVar<float> health = new SyncVar<float>(90f);
    private readonly SyncVar<bool> dead = new SyncVar<bool>();
    private readonly SyncVar<bool> grounded = new SyncVar<bool>();
    private readonly SyncVar<EnemyStateId> state = new SyncVar<EnemyStateId>(EnemyStateId.Wander);
    private readonly SyncVar<EnemyRole> role = new SyncVar<EnemyRole>();
    private EnemyRole initialRole;
    private EnemyLanceAttack lance;
    public EnemyRole Role => NetworkObject.IsSpawned ? role.Value : initialRole;
    public void AssignRole(EnemyRole value) { initialRole = value; if (IsServerInitialized) role.Value = value; }
    private float soloHealth = 90f, stunnedUntil, nextAttack, windupUntil, deathUntil;
    private bool windingUp, soloDead;
    private int facing = -1;
    private Rigidbody body;
    private CapsuleCollider capsule;
    private PlayerHealth target;
    private EnemyPresentation presentation;
    private EnemyMotor motor;
    private EnemyStateMachine brain;
    private bool Authority => !NetworkObject.IsSpawned || IsServerInitialized;
    public float Health => NetworkObject.IsSpawned ? health.Value : soloHealth;
    public float MaxHealth => settings.maxHealth;
    public bool IsDead => NetworkObject.IsSpawned ? dead.Value : soloDead;
    public bool CanTarget => isActiveAndEnabled && !IsDead && capsule != null && capsule.enabled;
    public int TargetPriority => 2;
    public Bounds TargetBounds => capsule.bounds;
    public Vector3 AimPoint => TargetBounds.center;
    public Transform TargetTransform => transform;
    public bool IsJuggling => !IsGrounded;
    public bool PracticePassive { get; set; }
    public float StunRemaining => Mathf.Max(0f, stunnedUntil - Time.time);
    public bool IsStunned => Time.time < stunnedUntil;
    public bool IsGrounded => NetworkObject.IsSpawned && !IsServerInitialized ? grounded.Value : motor != null && motor.IsGrounded;
    public bool IsAttacking => windingUp || lance != null && lance.IsCharging;
    public EnemyStateId CurrentState => NetworkObject.IsSpawned && !IsServerInitialized ? state.Value : brain != null ? brain.CurrentState : EnemyStateId.Wander;
    public float ChaseSpeed => settings.moveSpeed;
    public float StopDistance => settings.stopDistance;
    private void Awake()
    {
        body = GetComponent<Rigidbody>(); capsule = GetComponent<CapsuleCollider>(); presentation = GetComponent<EnemyPresentation>();
        motor = GetComponent<EnemyMotor>(); brain = GetComponent<EnemyStateMachine>();
        lance = GetComponent<EnemyLanceAttack>();
        body.useGravity = false; body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        soloHealth = settings.maxHealth;
    }
    private void OnEnable() { ActiveEnemies.Add(this); OneWayPlatform.Register(GetComponent<CapsuleCollider>()); }
    private void OnDisable() { ActiveEnemies.Remove(this); OneWayPlatform.Unregister(capsule); }
    public override void OnStartServer() { base.OnStartServer(); health.Value = settings.maxHealth; dead.Value = false; role.Value = initialRole; brain.ResetBehavior(); }
    public override void OnStartClient()
    {
        base.OnStartClient(); // NetworkTransform configures replica Rigidbody authority/interpolation.
        if (IsDead) { capsule.enabled = false; presentation?.Death(); }
    }
    private void FixedUpdate()
    {
        if (!Authority) return;
        try
        {
            if (IsDead)
            {
                body.linearVelocity = Vector3.zero;
                if (Time.time >= deathUntil) { if (NetworkObject.IsSpawned) Despawn(); else Destroy(gameObject); }
                return;
            }
            if (transform.position.y < settings.fallLimit) { Die(); return; }
            motor.Tick(IsStunned && IsJuggling ? settings.juggleGravityScale : 1f);
            if (IsServerInitialized) grounded.Value = motor.IsGrounded;
            brain.UpdateAwareness();
            if (IsServerInitialized) state.Value = brain.CurrentState;
            if (IsStunned) { motor.DampImpact(settings.hitDrag); return; }
            if (PracticePassive) { CancelAttack(); motor.Move(0f); return; }
            if (!windingUp && lance != null && lance.Tick()) return;
            if (windingUp)
            {
                motor.Move(0f);
                if (target == null || target.Health <= 0f || Vector3.Distance(target.transform.position, transform.position) > settings.impactRange * settings.abandonWindupDistance)
                { windingUp = false; return; }
                if (Time.time >= windupUntil)
                {
                    windingUp = false; nextAttack = Time.time + settings.attackCooldown;
                    if (NetworkObject.IsSpawned) AttackFeedback(); else presentation?.Attack();
                    if (target != null)
                    {
                        Vector3 difference = target.transform.position - transform.position;
                        if (Mathf.Abs(difference.x) < settings.impactRange && Mathf.Abs(difference.y) < settings.heightTolerance && difference.x * facing >= -0.1f)
                            target.TakeHit(settings.attackDamage, new Vector3(facing * settings.attackKnockback.x, settings.attackKnockback.y, 0f));
                    }
                }
                return;
            }
            brain.TickMovement();
        }
        finally
        {
            // Navigation can travel away from the target (retreats, jumps and routes).
            // Face actual travel after AI decisions, retaining deliberate attack facing.
            if (!IsDead && !IsStunned && !IsAttacking && Mathf.Abs(motor.Velocity.x) > settings.facingSpeedThreshold)
                Face(motor.Velocity.x);
        }
    }
    public void Face(float direction)
    {
        if (Mathf.Abs(direction) < 0.001f) return;
        facing = direction > 0f ? 1 : -1;
        transform.rotation = Quaternion.Euler(0f, facing * 90f, 0f);
    }
    public bool TryStartAttack(PlayerHealth victim)
    {
        if (!Authority || IsDead || IsStunned || PracticePassive || victim == null || victim.Health <= 0f || !motor.IsGrounded || Time.time < nextAttack) return false;
        var difference = victim.transform.position - transform.position;
        if (Mathf.Abs(difference.y) >= settings.heightTolerance || Mathf.Abs(difference.x) >= settings.attackRange) return false;
        target = victim; windingUp = true;
        float startup = Mathf.Max(0.18f, settings.windup);
        windupUntil = Time.time + startup;
        motor.Move(0f);
        if (NetworkObject.IsSpawned) Telegraph(startup); else presentation?.Telegraph(startup);
        return true;
    }
    public void CancelAttack() { windingUp = false; target = null; lance?.Cancel(); }
    [ObserversRpc(RunLocally = true)] public void ShowLanceWindup(float duration) => presentation?.Telegraph(duration);
    public void TakeHit(float damage, Vector3 knockback, float stun)
    {
        if (!Authority || IsDead) return;
        if (NetworkObject.IsSpawned) health.Value = Mathf.Max(0f, health.Value - damage);
        else soloHealth = Mathf.Max(0f, soloHealth - damage);
        windingUp = false; stunnedUntil = Time.time + stun;
        lance?.Cancel();
        brain.AlertFromDamage();
        motor.SetImpact(knockback);
        if (NetworkObject.IsSpawned) HitFeedback(damage, knockback, stun); else presentation?.Hit(damage, knockback, stun);
        if (Health <= 0f) Die();
    }
    public void TakeDamage(float damage, Vector3 knockback, float stun, PlayerHealth attacker)
        => TakeHit(damage, knockback, stun);
    private void Die()
    {
        if (IsDead) return;
        if (NetworkObject.IsSpawned) dead.Value = true; else soloDead = true;
        deathUntil = Time.time + settings.deathDuration; capsule.enabled = false;
        if (NetworkObject.IsSpawned) DeathFeedback(); else presentation?.Death();
    }
    [ObserversRpc(RunLocally = true)] private void Telegraph(float duration) => presentation?.Telegraph(duration);
    [ObserversRpc(RunLocally = true)] private void HitFeedback(float damage, Vector3 direction, float stun) => presentation?.Hit(damage, direction, stun);
    [ObserversRpc(RunLocally = true)] private void DeathFeedback() => presentation?.Death();
    [ObserversRpc(RunLocally = true)] private void AttackFeedback() => presentation?.Attack();
}
