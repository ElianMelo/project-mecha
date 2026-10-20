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
    private CombatReaction reactions;
    private float recoveryUntil, yieldUntil, nextSpecial;
    private int attackCount;
    private bool specialAttack;
    public bool EngagementBusy => IsAttacking || Time.time < recoveryUntil || lance != null && lance.HasProjectileInFlight;
    public bool WantsEngagement => Authority && isActiveAndEnabled && !IsDead && !IsStunned && !PracticePassive &&
        brain != null && brain.Target != null && brain.Target.IsAlive && Time.time >= yieldUntil &&
        (EngagementBusy || Time.time >= nextAttack) &&
        (lance == null || lance.ReadyToEngage) &&
        (brain.CurrentState == EnemyStateId.Alert || brain.CurrentState == EnemyStateId.Prediction);
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
    public bool IsStunned => Time.time < stunnedUntil || reactions != null && reactions.BlocksControl;
    public bool IsGrounded => NetworkObject.IsSpawned && !IsServerInitialized ? grounded.Value : motor != null && motor.IsGrounded;
    public bool IsAttacking => windingUp || lance != null && lance.IsCharging;
    public EnemyStateId CurrentState => NetworkObject.IsSpawned && !IsServerInitialized ? state.Value : brain != null ? brain.CurrentState : EnemyStateId.Wander;
    public float ChaseSpeed => settings.moveSpeed;
    public float StopDistance => settings.stopDistance;
    public float AttackRecovery(bool special) => special ? settings.specialRecovery : settings.attackRecovery;
    private void Awake()
    {
        body = GetComponent<Rigidbody>(); capsule = GetComponent<CapsuleCollider>(); presentation = GetComponent<EnemyPresentation>();
        motor = GetComponent<EnemyMotor>(); brain = GetComponent<EnemyStateMachine>();
        lance = GetComponent<EnemyLanceAttack>();
        reactions = GetComponent<CombatReaction>();
        body.useGravity = false; body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        soloHealth = settings.maxHealth;
    }
    private void OnEnable() { ActiveEnemies.Add(this); OneWayPlatform.Register(GetComponent<CapsuleCollider>()); }
    private void OnDisable() { EnemyAttackTokens.Release(this); ActiveEnemies.Remove(this); OneWayPlatform.Unregister(capsule); }
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
            if (IsStunned) { EnemyAttackTokens.Release(this); motor.DampImpact(settings.hitDrag); return; }
            if (PracticePassive) { CancelAttack(); motor.Move(0f); return; }
            if (Time.time < recoveryUntil) { motor.Move(0f); return; }
            if (!EngagementBusy && recoveryUntil > 0f)
            { recoveryUntil = 0f; YieldEngagement(); }
            if (settings.engagement == null || !EnemyAttackTokens.Request(this, settings.engagement))
            {
                if (IsAttacking) CancelAttack();
                ObserveCombat(); return;
            }
            if (!windingUp && lance != null && lance.Tick()) return;
            if (windingUp)
            {
                motor.Move(0f);
                if (target == null || target.IsDead || target.IsStunned || Vector3.Distance(target.transform.position, transform.position) > settings.impactRange * settings.abandonWindupDistance)
                { CancelAttack(); YieldEngagement(); return; }
                if (Time.time >= windupUntil)
                {
                    windingUp = false; nextAttack = Time.time + settings.attackCooldown;
                    recoveryUntil = Time.time + (specialAttack ? settings.specialRecovery : settings.attackRecovery);
                    if (NetworkObject.IsSpawned) AttackFeedback(); else presentation?.Attack();
                    if (target != null)
                    {
                        Vector3 difference = target.transform.position - transform.position;
                        if (Mathf.Abs(difference.x) < settings.impactRange && Mathf.Abs(difference.y) < settings.heightTolerance && difference.x * facing >= -0.1f)
                        {
                            if (specialAttack) target.TakeSpecialHit(settings.attackDamage * settings.specialDamageMultiplier,
                                new Vector3(facing * settings.specialKnockback.x, settings.specialKnockback.y, 0f), settings.specialStun);
                            else target.TakeHit(settings.attackDamage, new Vector3(facing * settings.attackKnockback.x, settings.attackKnockback.y, 0f));
                        }
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
        if (!Authority || IsAttacking || IsDead || IsStunned || PracticePassive || victim == null || victim.IsDead ||
            victim.IsStunned || !motor.IsGrounded || Time.time < nextAttack || Time.time < recoveryUntil || settings.engagement == null) return false;
        var difference = victim.transform.position - transform.position;
        if (Mathf.Abs(difference.y) >= settings.heightTolerance || Mathf.Abs(difference.x) >= settings.attackRange) return false;
        if (!EnemyAttackTokens.Request(this, settings.engagement) || !TryCommitAttack()) return false;
        target = victim; windingUp = true;
        Face(difference.x);
        attackCount++;
        specialAttack = attackCount % Mathf.Max(1, settings.specialEveryAttacks) == 0 && Time.time >= nextSpecial;
        if (specialAttack) nextSpecial = Time.time + settings.specialCooldown;
        float startup = Mathf.Max(0.18f, specialAttack ? settings.specialWindup : settings.windup);
        windupUntil = Time.time + startup;
        motor.Move(0f);
        if (NetworkObject.IsSpawned) Telegraph(startup, specialAttack); else presentation?.Telegraph(startup, specialAttack);
        return true;
    }
    public bool TryCommitAttack() => settings.engagement != null && EnemyAttackTokens.TryBeginAttack(this, settings.engagement);
    public void YieldEngagement()
    { EnemyAttackTokens.Release(this); yieldUntil = Time.time + (settings.engagement != null ? settings.engagement.handoffDelay : .2f); }
    public void CancelAttack() { windingUp = false; target = null; lance?.Cancel(); EnemyAttackTokens.Release(this); }
    public void ShowLanceWindup(float duration) { if (NetworkObject.IsSpawned) Telegraph(duration, false); else presentation?.Telegraph(duration); }
    private void ObserveCombat()
    {
        if (brain.CurrentState == EnemyStateId.Wander || brain.CurrentState == EnemyStateId.Suspicious)
        { brain.TickMovement(); return; }
        var victim = brain.Target;
        var profile = settings.engagement;
        var support = motor.Support;
        if (victim == null || profile == null || support == null) { motor.Move(0f); return; }
        float side = transform.position.x >= victim.transform.position.x ? 1f : -1f;
        float patrol = Mathf.Sin(Time.time / Mathf.Max(.1f, profile.patrolPeriod) * Mathf.PI * 2f + GetInstanceID()) * profile.patrolRadius;
        float x = Mathf.Clamp(victim.transform.position.x + side * (profile.observeDistance + Mathf.Abs(patrol)),
            support.bounds.min.x + .5f, support.bounds.max.x - .5f);
        brain.Navigation.MoveTo(new Vector3(x, support.bounds.max.y, transform.position.z), support, brain.Settings.wanderSpeed, .35f);
        Face(victim.transform.position.x - transform.position.x);
    }
    public void TakeHit(float damage, Vector3 knockback, float stun)
        => ReceiveHit(damage, knockback, stun, default);
    private void ReceiveHit(float damage, Vector3 knockback, float stun, DamageContact contact)
    {
        if (!Authority || IsDead || reactions != null && reactions.ProtectsRecovery) return;
        if (NetworkObject.IsSpawned) health.Value = Mathf.Max(0f, health.Value - damage);
        else soloHealth = Mathf.Max(0f, soloHealth - damage);
        CancelAttack(); stunnedUntil = Mathf.Max(stunnedUntil, Time.time + stun);
        brain.AlertFromDamage();
        motor.SetImpact(knockback);
        if (NetworkObject.IsSpawned) HitFeedback(damage, knockback, stun, contact); else presentation?.Hit(damage, knockback, stun, contact);
        if (Health <= 0f) Die();
    }
    public void TakeDamage(float damage, Vector3 knockback, float stun, PlayerHealth attacker, DamageContact contact = default)
        => ReceiveHit(damage, knockback, stun, contact);
    private void Die()
    {
        if (IsDead) return;
        CancelAttack();
        if (NetworkObject.IsSpawned) dead.Value = true; else soloDead = true;
        deathUntil = Time.time + settings.deathDuration; capsule.enabled = false;
        if (NetworkObject.IsSpawned) DeathFeedback(); else presentation?.Death();
    }
    [ObserversRpc(RunLocally = true)] private void Telegraph(float duration, bool special) => presentation?.Telegraph(duration, special);
    [ObserversRpc(RunLocally = true)] private void HitFeedback(float damage, Vector3 direction, float stun, DamageContact contact) => presentation?.Hit(damage, direction, stun, contact);
    [ObserversRpc(RunLocally = true)] private void DeathFeedback() => presentation?.Death();
    [ObserversRpc(RunLocally = true)] private void AttackFeedback() => presentation?.Attack();
}
