using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerHealth : NetworkBehaviour, IDamageable, ITargetable
{
    [SerializeField] private PlayerHealthSettings settings;
    private readonly SyncVar<float> health = new SyncVar<float>(100f);
    private readonly SyncVar<int> escapeCharges = new SyncVar<int>(3);
    private int soloEscapeCharges;
    private float nextEscapeRefill;
    private float soloHealth = 100f, stunnedUntil, invincibleUntil;
    private Vector3 spawnPosition;
    private Rigidbody body;
    private PlayerMovement movement;
    private CapsuleCollider capsule;
    public bool IsDead => defeated || Health <= 0f;
    public bool CanTarget => PvpMatchDirector.Active && !IsDead;
    public int TargetPriority => 2;
    public Bounds TargetBounds => capsule != null ? capsule.bounds : new Bounds(transform.position, Vector3.one);
    public Vector3 AimPoint => TargetBounds.center;
    public Transform TargetTransform => transform;
    public float RespawnRemaining => defeated ? Mathf.Max(0f, respawnAt - Time.time) : 0f;
    private float DefeatDuration => PvpMatchDirector.Active ? PvpMatchDirector.Instance.Settings.respawnSeconds : settings.defeatDuration;
    private bool defeated;
    private float respawnAt;
    public float Health => NetworkObject.IsSpawned ? health.Value : soloHealth;
    public bool IsStunned => defeated || Time.time < stunnedUntil;
    public int EscapeCharges => NetworkObject.IsSpawned ? escapeCharges.Value : soloEscapeCharges;
    public int EscapeCapacity => settings.escapeCapacity;
    public float EscapeRefillRemaining => EscapeCharges < EscapeCapacity ? Mathf.Max(0f, nextEscapeRefill - Time.time) : 0f;
    public float StaggerDrag => settings.staggerDrag;
    public float MaxHealth => settings.maxHealth;
    private void Awake() { body = GetComponent<Rigidbody>(); capsule = GetComponent<CapsuleCollider>(); movement = GetComponent<PlayerMovement>(); spawnPosition = transform.position; soloHealth = settings.maxHealth; soloEscapeCharges = EscapeCapacity; }
    public override void OnStartServer() { base.OnStartServer(); health.Value = settings.maxHealth; ResetEscapeResources(); spawnPosition = transform.position; }
    public override void OnStartClient() { base.OnStartClient(); spawnPosition = transform.position; if (Health <= 0f) ApplyDefeat(); }
    private void Update()
    {
        if ((!NetworkObject.IsSpawned || IsServerInitialized) && !PvpMatchDirector.BlocksCombat && !defeated && EscapeCharges < EscapeCapacity && Time.time >= nextEscapeRefill)
        {
            if (NetworkObject.IsSpawned) escapeCharges.Value++; else soloEscapeCharges++;
            nextEscapeRefill = Time.time + settings.escapeRefillSeconds;
        }
        if (defeated)
        {
            if (!PvpMatchDirector.BlocksCombat && Time.time >= respawnAt && (!NetworkObject.IsSpawned || IsServerInitialized)) Respawn();
            return;
        }
        if (transform.position.y < settings.fallLimit && (!NetworkObject.IsSpawned || IsServerInitialized))
        {
            if (PvpMatchDirector.Active)
            {
                if (PvpMatchDirector.BlocksCombat) return;
                health.Value = 0f; PvpMatchDirector.Instance.RecordDeath(this, null); Defeat();
            }
            else Respawn();
        }
    }
    public void TakeHit(float damage, Vector3 knockback)
        => ReceiveHit(damage, knockback, settings.hitStun, null);
    public void TakeDamage(float damage, Vector3 knockback, float stun, PlayerHealth attacker)
    {
        if (!CombatTargetRules.CanHit(attacker, this)) return;
        ReceiveHit(damage, knockback, stun, attacker);
    }
    private void ReceiveHit(float damage, Vector3 knockback, float stun, PlayerHealth attacker)
    {
        if (!float.IsFinite(damage) || damage <= 0f || !float.IsFinite(knockback.x) || !float.IsFinite(knockback.y) || !float.IsFinite(knockback.z) ||
            defeated || (NetworkObject.IsSpawned && !IsServerInitialized) || Time.time < invincibleUntil || movement != null && movement.IsDashing ||
            PvpMatchDirector.Active && (attacker == null || !CombatTargetRules.CanHit(attacker, this))) return;
        float removed = Mathf.Min(Health, damage);
        if (NetworkObject.IsSpawned) health.Value = Mathf.Max(0f, health.Value - damage);
        else soloHealth = Mathf.Max(0f, soloHealth - damage);
        invincibleUntil = Time.time + settings.invincibility;
        if (PvpMatchDirector.Active) PvpMatchDirector.Instance.RecordDamage(this, attacker, removed);
        if (Health <= 0f)
        {
            if (PvpMatchDirector.Active) PvpMatchDirector.Instance.RecordDeath(this, attacker);
            if (NetworkObject.IsSpawned) Defeat(); else ApplyDefeat();
            return;
        }
        float duration = float.IsFinite(stun) ? Mathf.Max(settings.hitStun, stun) : settings.hitStun;
        if (NetworkObject.IsSpawned) React(knockback, duration); else ApplyReaction(knockback, duration);
    }
    public void Heal(float amount)
    {
        if (amount <= 0f || defeated || Health <= 0f || NetworkObject.IsSpawned && !IsServerInitialized) return;
        float previous = Health;
        if (NetworkObject.IsSpawned) health.Value = Mathf.Min(settings.maxHealth, health.Value + amount);
        else soloHealth = Mathf.Min(settings.maxHealth, soloHealth + amount);
        if (Health > previous) { if (NetworkObject.IsSpawned) HealAudio(); else GameAudio.Play(GameAudioCue.Heal, transform.position); }
    }
    [ObserversRpc(RunLocally = true)] private void HealAudio() => GameAudio.Play(GameAudioCue.Heal, transform.position);
    [ObserversRpc(RunLocally = true)]
    private void Defeat() => ApplyDefeat();
    private void ApplyDefeat()
    {
        if (!defeated) GameAudio.Play(GameAudioCue.PlayerDeath, transform.position);
        defeated=true;respawnAt=Time.time+DefeatDuration;
        GetComponent<CharacterSkills>()?.ClearModifier();
        GetComponent<PlayerAttack>().Interrupt();GetComponent<PlayerMovement>().StopDash();
        GetComponent<CharacterAnimator>()?.Death(DefeatDuration);
        if(!NetworkObject.IsSpawned || IsOwner)body.linearVelocity=Vector3.zero;
    }
    [ObserversRpc(RunLocally = true)]
    private void React(Vector3 knockback, float stun) => ApplyReaction(knockback, stun);
    private void ApplyReaction(Vector3 knockback, float stun)
    {
        GetComponent<PlayerCombatVfx>()?.Hit(knockback);
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + stun);
        GetComponent<PlayerAttack>().Interrupt(); GetComponent<PlayerMovement>().StopDash();
        GetComponent<CharacterAnimator>()?.Reaction(stun);
        if (!NetworkObject.IsSpawned || IsOwner) body.linearVelocity = knockback;
    }
    private void Respawn()
    {
        ResetEscapeResources();
        if (PvpMatchDirector.Active) spawnPosition = PvpMatchDirector.Instance.SpawnPoint(this);
        invincibleUntil = Time.time + settings.respawnInvincibility;
        if (NetworkObject.IsSpawned) { health.Value = settings.maxHealth; ResetAtSpawn(spawnPosition); }
        else { soloHealth = settings.maxHealth; ApplyReset(spawnPosition); }
        if (PvpMatchDirector.Active) PvpMatchDirector.Instance.ResetLife(this);
    }
    public void ResetEscapeResources()
    {
        if (NetworkObject.IsSpawned && !IsServerInitialized) return;
        if (NetworkObject.IsSpawned) escapeCharges.Value = EscapeCapacity; else soloEscapeCharges = EscapeCapacity;
        nextEscapeRefill = 0f;
    }
    // Called only by solo gameplay or the server, never by owner prediction.
    public bool TryConsumeEscape()
    {
        if (NetworkObject.IsSpawned && !IsServerInitialized || IsDead || !IsStunned || EscapeCharges <= 0) return false;
        bool full = EscapeCharges == EscapeCapacity;
        if (NetworkObject.IsSpawned) escapeCharges.Value--; else soloEscapeCharges--;
        if (full) nextEscapeRefill = Time.time + settings.escapeRefillSeconds;
        return true;
    }
    public void ReleaseStaggerForEscape()
    {
        stunnedUntil = 0f;
        invincibleUntil = Mathf.Max(invincibleUntil, Time.time + settings.escapeInvincibility);
        GetComponent<CharacterAnimator>()?.ReleaseAction();
    }
    public void CorrectPvpPosition(Vector3 position)
    {
        if (!IsServerInitialized) return;
        CorrectPosition(position);
    }
    [ObserversRpc(RunLocally = true)]
    private void CorrectPosition(Vector3 position)
    {
        body.position = transform.position = position;
        GetComponent<FishNet.Component.Transforming.NetworkTransform>()?.Teleport();
        if (!body.isKinematic) body.linearVelocity = Vector3.zero;
        movement?.ResetTraversal();
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
        if (defeated) GameAudio.Play(GameAudioCue.Revive, position);
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
