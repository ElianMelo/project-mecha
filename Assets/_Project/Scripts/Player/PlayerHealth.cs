using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private PlayerHealthSettings settings;
    private readonly SyncVar<float> health = new SyncVar<float>(100f);
    private float soloHealth = 100f, stunnedUntil, invincibleUntil;
    private Vector3 spawnPosition;
    private Rigidbody body;
    private PlayerMovement movement;
    private bool defeated;
    private float respawnAt;
    public float Health => NetworkObject.IsSpawned ? health.Value : soloHealth;
    public bool IsStunned => defeated || Time.time < stunnedUntil;
    public float MaxHealth => settings.maxHealth;
    private void Awake() { body = GetComponent<Rigidbody>(); movement = GetComponent<PlayerMovement>(); spawnPosition = transform.position; soloHealth = settings.maxHealth; }
    public override void OnStartServer() { base.OnStartServer(); health.Value = settings.maxHealth; spawnPosition = transform.position; }
    public override void OnStartClient() { base.OnStartClient(); spawnPosition = transform.position; if (Health <= 0f) ApplyDefeat(); }
    private void Update()
    {
        if (defeated)
        {
            if (Time.time >= respawnAt && (!NetworkObject.IsSpawned || IsServerInitialized)) Respawn();
            return;
        }
        if (transform.position.y < settings.fallLimit && (!NetworkObject.IsSpawned || IsServerInitialized)) Respawn();
    }
    public void TakeHit(float damage, Vector3 knockback)
    {
        if (defeated || (NetworkObject.IsSpawned && !IsServerInitialized) || Time.time < invincibleUntil || movement != null && movement.IsDashing) return;
        if (NetworkObject.IsSpawned) health.Value = Mathf.Max(0f, health.Value - damage);
        else soloHealth = Mathf.Max(0f, soloHealth - damage);
        invincibleUntil = Time.time + settings.invincibility;
        if (Health <= 0f)
        {
            if (NetworkObject.IsSpawned) Defeat(); else ApplyDefeat();
            return;
        }
        if (NetworkObject.IsSpawned) React(knockback); else ApplyReaction(knockback);
    }
    [ObserversRpc(RunLocally = true)]
    private void Defeat() => ApplyDefeat();
    private void ApplyDefeat()
    {
        defeated=true;respawnAt=Time.time+settings.defeatDuration;
        GetComponent<PlayerAttack>().Interrupt();GetComponent<PlayerMovement>().StopDash();
        GetComponent<CharacterAnimator>()?.Death(settings.defeatDuration);
        if(!NetworkObject.IsSpawned || IsOwner)body.linearVelocity=Vector3.zero;
    }
    [ObserversRpc(RunLocally = true)]
    private void React(Vector3 knockback) => ApplyReaction(knockback);
    private void ApplyReaction(Vector3 knockback)
    {
        GetComponent<PlayerCombatVfx>()?.Hit(knockback);
        // Skills have super armor; ordinary attacks can be interrupted.
        if (GetComponent<PlayerAttack>().IsSkillActive) return;
        stunnedUntil = Time.time + settings.hitStun;
        GetComponent<PlayerAttack>().Interrupt(); GetComponent<PlayerMovement>().StopDash();
        GetComponent<CharacterAnimator>()?.Reaction(settings.hitStun);
        if (!NetworkObject.IsSpawned || IsOwner) body.linearVelocity = knockback;
    }
    private void Respawn()
    {
        invincibleUntil = Time.time + settings.respawnInvincibility;
        if (NetworkObject.IsSpawned) { health.Value = settings.maxHealth; ResetAtSpawn(spawnPosition); }
        else { soloHealth = settings.maxHealth; ApplyReset(spawnPosition); }
    }
    public void EnterLevel(Vector3 position)
    {
        if (NetworkObject.IsSpawned && !IsServerInitialized) return;
        spawnPosition = position;
        Respawn();
    }
    [ObserversRpc(RunLocally = true)]
    private void ResetAtSpawn(Vector3 position) => ApplyReset(position);
    private void ApplyReset(Vector3 position)
    {
        defeated = false; stunnedUntil = 0f; spawnPosition = position;
        invincibleUntil = Time.time + settings.respawnInvincibility;
        body.position = position; transform.position = position;
        GetComponent<FishNet.Component.Transforming.NetworkTransform>()?.Teleport();
        if (!body.isKinematic) body.linearVelocity = Vector3.zero;
        GetComponent<PlayerAttack>().Interrupt(); GetComponent<PlayerMovement>().StopDash();
        GetComponent<CharacterAnimator>()?.ResetPresentation();
        movement?.ResetTraversal();
        if (movement != null && movement.HasControl) FindFirstObjectByType<RoomCameraRig>()?.Focus(movement);
    }
}
