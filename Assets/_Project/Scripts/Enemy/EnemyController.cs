using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class EnemyController : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 90f;
    [SerializeField] private float moveSpeed = 1.7f;
    [SerializeField] private float attackDamage = 12f;
    [SerializeField] private float aggroRange = 7f;
    [SerializeField] private LayerMask groundLayer = (1 << 6) | (1 << 7);
    private readonly SyncVar<float> health = new SyncVar<float>(90f);
    private readonly SyncVar<bool> dead = new SyncVar<bool>();
    private float soloHealth = 90f, stunnedUntil, nextAttack, windupUntil, nextTargetSearch, deathUntil;
    private bool windingUp, soloDead;
    private int facing = -1;
    private Rigidbody body;
    private CapsuleCollider capsule;
    private PlayerHealth target;
    private EnemyPresentation presentation;
    private bool Authority => !NetworkObject.IsSpawned || IsServerInitialized;
    public float Health => NetworkObject.IsSpawned ? health.Value : soloHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => NetworkObject.IsSpawned ? dead.Value : soloDead;
    public bool IsStunned => Time.time < stunnedUntil;
    private void Awake()
    {
        body = GetComponent<Rigidbody>(); capsule = GetComponent<CapsuleCollider>(); presentation = GetComponent<EnemyPresentation>();
        body.useGravity = false; body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        soloHealth = maxHealth;
    }
    private void OnEnable() => OneWayPlatform.Register(GetComponent<CapsuleCollider>());
    private void OnDisable() => OneWayPlatform.Unregister(capsule);
    public override void OnStartServer() { base.OnStartServer(); health.Value = maxHealth; dead.Value = false; }
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
        if (transform.position.y < -12f) { Die(); return; }
        float y = Mathf.Max(body.linearVelocity.y - (body.linearVelocity.y > 0f ? 24f : 40f) * Time.fixedDeltaTime, -20f);
        body.linearVelocity = new Vector3(body.linearVelocity.x, y, 0f);
        if (IsStunned) return;
        if (Time.time >= nextTargetSearch)
        {
            nextTargetSearch = Time.time + 0.35f; target = null; float closest = aggroRange;
            foreach (var player in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (player.Health > 0f && distance < closest) { target = player; closest = distance; }
            }
        }
        if (windingUp)
        {
            body.linearVelocity = new Vector3(0f, y, 0f);
            if (Time.time >= windupUntil)
            {
                windingUp = false; nextAttack = Time.time + 1.2f;
                if (target != null)
                {
                    Vector3 difference = target.transform.position - transform.position;
                    if (Mathf.Abs(difference.x) < 1.25f && Mathf.Abs(difference.y) < 1.1f && difference.x * facing >= -0.1f)
                        target.TakeHit(attackDamage, new Vector3(facing * 5f, 4f, 0f));
                }
            }
            return;
        }
        if (target == null || Mathf.Abs(target.transform.position.y - transform.position.y) > 1.1f)
        {
            body.linearVelocity = new Vector3(0f, y, 0f); return;
        }
        float dx = target.transform.position.x - transform.position.x;
        facing = dx >= 0f ? 1 : -1;
        transform.rotation = Quaternion.Euler(0f, facing == 1 ? 0f : 180f, 0f);
        if (Mathf.Abs(dx) < 1.05f && Time.time >= nextAttack)
        {
            windingUp = true; windupUntil = Time.time + 0.48f;
            if (NetworkObject.IsSpawned) Telegraph(); else presentation?.Telegraph();
        }
        else
        {
            Vector3 probe = new Vector3(capsule.bounds.center.x + facing * 0.45f, capsule.bounds.min.y + 0.15f, transform.position.z);
            bool floorAhead = Physics.Raycast(probe, Vector3.down, 0.45f, groundLayer, QueryTriggerInteraction.Ignore);
            body.linearVelocity = new Vector3(floorAhead && Mathf.Abs(dx) > 0.8f ? facing * moveSpeed : 0f, y, 0f);
        }
    }
    public void TakeHit(float damage, Vector3 knockback, float stun)
    {
        if (!Authority || IsDead) return;
        if (NetworkObject.IsSpawned) health.Value = Mathf.Max(0f, health.Value - damage);
        else soloHealth = Mathf.Max(0f, soloHealth - damage);
        windingUp = false; stunnedUntil = Time.time + stun;
        body.linearVelocity = knockback;
        if (NetworkObject.IsSpawned) HitFeedback(damage); else presentation?.Hit(damage);
        if (Health <= 0f) Die();
    }
    private void Die()
    {
        if (IsDead) return;
        if (NetworkObject.IsSpawned) dead.Value = true; else soloDead = true;
        deathUntil = Time.time + 0.45f; capsule.enabled = false;
        if (NetworkObject.IsSpawned) DeathFeedback(); else presentation?.Death();
    }
    [ObserversRpc(RunLocally = true)] private void Telegraph() => presentation?.Telegraph();
    [ObserversRpc(RunLocally = true)] private void HitFeedback(float damage) => presentation?.Hit(damage);
    [ObserversRpc(RunLocally = true)] private void DeathFeedback() => presentation?.Death();
}
