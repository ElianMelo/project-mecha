using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(EnemyMotor))]
[RequireComponent(typeof(EnemyPlatformNavigator), typeof(EnemyStateMachine))]
public class EnemyController : NetworkBehaviour, ICombatTarget
{
    [SerializeField] private EnemySettings settings;
    private readonly SyncVar<float> health = new SyncVar<float>(90f);
    private readonly SyncVar<bool> dead = new SyncVar<bool>();
    private readonly SyncVar<bool> grounded = new SyncVar<bool>();
    private readonly SyncVar<EnemyStateId> state = new SyncVar<EnemyStateId>(EnemyStateId.Wander);
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
    public bool IsStunned => Time.time < stunnedUntil;
    public bool IsGrounded => NetworkObject.IsSpawned && !IsServerInitialized ? grounded.Value : motor != null && motor.IsGrounded;
    public bool IsAttacking => windingUp;
    public EnemyStateId CurrentState => NetworkObject.IsSpawned && !IsServerInitialized ? state.Value : brain != null ? brain.CurrentState : EnemyStateId.Wander;
    public float ChaseSpeed => settings.moveSpeed;
    public float StopDistance => settings.stopDistance;
    private void Awake()
    {
        body = GetComponent<Rigidbody>(); capsule = GetComponent<CapsuleCollider>(); presentation = GetComponent<EnemyPresentation>();
        motor = GetComponent<EnemyMotor>(); brain = GetComponent<EnemyStateMachine>();
        body.useGravity = false; body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        soloHealth = settings.maxHealth;
    }
    private void OnEnable() => OneWayPlatform.Register(GetComponent<CapsuleCollider>());
    private void OnDisable() => OneWayPlatform.Unregister(capsule);
    public override void OnStartServer() { base.OnStartServer(); health.Value = settings.maxHealth; dead.Value = false; brain.ResetBehavior(); }
    public override void OnStartClient() { base.OnStartClient(); if (!IsServerInitialized) body.isKinematic = true; }
    private void FixedUpdate()
    {
        if (!Authority) return;
        if (IsDead)
        {
            body.linearVelocity = Vector3.zero;
            if (Time.time >= deathUntil) { if (NetworkObject.IsSpawned) Despawn(); else Destroy(gameObject); }
            return;
        }
        if (transform.position.y < settings.fallLimit) { Die(); return; }
        motor.Tick();
        if (IsServerInitialized) grounded.Value = motor.IsGrounded;
        brain.UpdateAwareness();
        if (IsServerInitialized) state.Value = brain.CurrentState;
        if (IsStunned) return;
        if (windingUp)
        {
            motor.Move(0f);
            if (target == null || target.Health <= 0f || Vector3.Distance(target.transform.position, transform.position) > settings.impactRange * settings.abandonWindupDistance)
            { windingUp = false; return; }
            if (Time.time >= windupUntil)
            {
                windingUp = false; nextAttack = Time.time + settings.attackCooldown;
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
    public void Face(float direction)
    {
        if (Mathf.Abs(direction) < 0.001f) return;
        facing = direction > 0f ? 1 : -1;
        transform.rotation = Quaternion.Euler(0f, facing * 90f, 0f);
    }
    public bool TryStartAttack(PlayerHealth victim)
    {
        if (!Authority || IsDead || IsStunned || victim == null || victim.Health <= 0f || !motor.IsGrounded || Time.time < nextAttack) return false;
        var difference = victim.transform.position - transform.position;
        if (Mathf.Abs(difference.y) >= settings.heightTolerance || Mathf.Abs(difference.x) >= settings.attackRange) return false;
        target = victim; windingUp = true; windupUntil = Time.time + settings.windup;
        motor.Move(0f);
        if (NetworkObject.IsSpawned) Telegraph(); else presentation?.Telegraph();
        return true;
    }
    public void CancelAttack() { windingUp = false; target = null; }
    public void TakeHit(float damage, Vector3 knockback, float stun)
    {
        if (!Authority || IsDead) return;
        if (NetworkObject.IsSpawned) health.Value = Mathf.Max(0f, health.Value - damage);
        else soloHealth = Mathf.Max(0f, soloHealth - damage);
        windingUp = false; stunnedUntil = Time.time + stun;
        brain.AlertFromDamage();
        body.linearVelocity = knockback;
        if (NetworkObject.IsSpawned) HitFeedback(damage, knockback, stun); else presentation?.Hit(damage, knockback, stun);
        if (Health <= 0f) Die();
    }
    private void Die()
    {
        if (IsDead) return;
        if (NetworkObject.IsSpawned) dead.Value = true; else soloDead = true;
        deathUntil = Time.time + settings.deathDuration; capsule.enabled = false;
        if (NetworkObject.IsSpawned) DeathFeedback(); else presentation?.Death();
    }
    [ObserversRpc(RunLocally = true)] private void Telegraph() => presentation?.Telegraph();
    [ObserversRpc(RunLocally = true)] private void HitFeedback(float damage, Vector3 direction, float stun) => presentation?.Hit(damage, direction, stun);
    [ObserversRpc(RunLocally = true)] private void DeathFeedback() => presentation?.Death();
}
