using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private readonly SyncVar<float> health = new SyncVar<float>(100f);
    private float soloHealth = 100f, stunnedUntil, invincibleUntil;
    private Vector3 spawnPosition;
    private Rigidbody body;
    public float Health => NetworkObject.IsSpawned ? health.Value : soloHealth;
    public bool IsStunned => Time.time < stunnedUntil;
    public float MaxHealth => maxHealth;
    private void Awake() { body = GetComponent<Rigidbody>(); spawnPosition = transform.position; soloHealth = maxHealth; }
    public override void OnStartServer() { base.OnStartServer(); health.Value = maxHealth; spawnPosition = transform.position; }
    public override void OnStartClient() { base.OnStartClient(); spawnPosition = transform.position; }
    private void Update()
    {
        if (transform.position.y < -12f && (!NetworkObject.IsSpawned || IsServerInitialized)) Respawn();
    }
    public void TakeHit(float damage, Vector3 knockback)
    {
        if ((NetworkObject.IsSpawned && !IsServerInitialized) || Time.time < invincibleUntil) return;
        if (NetworkObject.IsSpawned) health.Value = Mathf.Max(0f, health.Value - damage);
        else soloHealth = Mathf.Max(0f, soloHealth - damage);
        invincibleUntil = Time.time + 0.9f;
        if (Health <= 0f) { Respawn(); return; }
        if (NetworkObject.IsSpawned) React(knockback); else ApplyReaction(knockback);
    }
    [ObserversRpc(RunLocally = true)]
    private void React(Vector3 knockback) => ApplyReaction(knockback);
    private void ApplyReaction(Vector3 knockback)
    {
        GetComponent<PlayerCombatVfx>()?.Hit(transform.position + Vector3.up * 0.6f);
        // Skills have super armor; ordinary attacks can be interrupted.
        if (GetComponent<PlayerAttack>().IsSkillActive) return;
        stunnedUntil = Time.time + 0.3f;
        GetComponent<PlayerAttack>().Interrupt(); GetComponent<PlayerMovement>().StopDash();
        if (!NetworkObject.IsSpawned || IsOwner) body.linearVelocity = knockback;
    }
    private void Respawn()
    {
        invincibleUntil = Time.time + 2f;
        if (NetworkObject.IsSpawned) { health.Value = maxHealth; ResetAtSpawn(spawnPosition); }
        else { soloHealth = maxHealth; ApplyReset(spawnPosition); }
    }
    [ObserversRpc(RunLocally = true)]
    private void ResetAtSpawn(Vector3 position) => ApplyReset(position);
    private void ApplyReset(Vector3 position)
    {
        stunnedUntil = 0f; body.position = position;
        if (!body.isKinematic) body.linearVelocity = Vector3.zero;
        GetComponent<PlayerAttack>().Interrupt(); GetComponent<PlayerMovement>().StopDash();
    }
}
