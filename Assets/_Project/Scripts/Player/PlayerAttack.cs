using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(PlayerInputs), typeof(PlayerMovement))]
public class PlayerAttack : NetworkBehaviour
{
    public enum ActionKind { Slash1, Slash2, Slash3, AirSlash, DashSlash, MegaSlash, SwordFire, CriticalX, RisingSlash }
    [SerializeField] private PlayerCombatSettings settings;
    private readonly SyncVar<float> mana = new SyncVar<float>(100f);
    private float soloMana = 100f;
    private readonly float[] localCooldowns = new float[3], serverCooldowns = new float[3];
    private readonly Collider[] hitBuffer = new Collider[48];
    private readonly HashSet<ICombatTarget> hitTargets = new HashSet<ICombatTarget>();
    private PlayerInputs inputs;
    private PlayerMovement movement;
    private PlayerHealth health;
    private PlayerCombatVfx vfx;
    private CharacterAnimator animationView;
    private ActionKind localKind, serverKind;
    private float localStart, serverStart, comboExpires, attackBuffered = -10f;
    private int localPulse, serverPulse, localDirection, serverDirection, combo;
    private bool localActive, serverActive;
    private float risingBufferedUntil = -10f, skillBufferedUntil = -10f;
    private int bufferedSkill = -1;
    private bool landedDuringAirSlash;
    public bool IsBusy => localActive;
    public bool IsSkillActive => (localActive && IsSkill(localKind)) || (IsServerInitialized && serverActive && IsSkill(serverKind));
    public bool CanAirSteer => localActive && localKind == ActionKind.AirSlash;
    public float Mana => NetworkObject.IsSpawned ? mana.Value : soloMana;
    public string CurrentAction => localActive ? localKind.ToString() : "Ready";
    public float Cooldown(int skill) => Mathf.Max(0f, localCooldowns[skill] - Time.time);
    public float MaxMana => settings.maxMana;
    public float Cost(int skill) => settings.Get((ActionKind)(skill + 5)).manaCost;
    private bool HasControl => !NetworkObject.IsSpawned || IsOwner;
    public static bool IsSkill(ActionKind kind) => kind >= ActionKind.MegaSlash && kind <= ActionKind.CriticalX;
    private float Duration(ActionKind kind) => settings.Get(kind).duration;
    private int Pulses(ActionKind kind) => settings.Get(kind).pulses;
    private float ImpactTime(ActionKind kind, int pulse) => settings.Get(kind).impact + pulse * settings.Get(kind).pulseInterval;
    private float CancelTime(ActionKind kind) => settings.Get(kind).cancel;
    private void Awake()
    {
        inputs = GetComponent<PlayerInputs>(); movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>(); vfx = GetComponent<PlayerCombatVfx>(); animationView = GetComponent<CharacterAnimator>();
        soloMana = settings.maxMana;
    }
    private void OnEnable() { inputs.OnAttackInput += HandleAttack; inputs.OnSkillInput += HandleSkill; }
    private void OnDisable() { inputs.OnAttackInput -= HandleAttack; inputs.OnSkillInput -= HandleSkill; localActive = serverActive = false; ClearBuffers(); }
    public override void OnStartServer() { base.OnStartServer(); mana.Value = settings.maxMana; }
    private void Update()
    {
        bool solo = !NetworkObject.IsSpawned;
        if (solo) soloMana = Mathf.Min(settings.maxMana, soloMana + settings.manaRegen * Time.deltaTime);
        else if (IsServerInitialized) mana.Value = Mathf.Min(settings.maxMana, mana.Value + settings.manaRegen * Time.deltaTime);
        if (localActive)
        {
            float elapsed = Time.time - localStart;
            while (localPulse < Pulses(localKind) && elapsed >= ImpactTime(localKind, localPulse))
            {
                vfx?.Strike(localKind, localDirection, localPulse);
                if (solo) ApplyHits(localKind, localDirection, localPulse);
                localPulse++;
            }
            if (elapsed >= Duration(localKind)) EndPresentation();
            else if (landedDuringAirSlash && localPulse > 0) FinishLanding();
        }
        if (IsServerInitialized && serverActive)
        {
            float elapsed = Time.time - serverStart;
            while (serverPulse < Pulses(serverKind) && elapsed >= ImpactTime(serverKind, serverPulse))
                ApplyHits(serverKind, serverDirection, serverPulse++);
            if (elapsed >= Duration(serverKind)) serverActive = false;
        }
        bool canChain = HasControl && (!localActive || Time.time - localStart >= CancelTime(localKind));
        if (canChain && Time.time <= risingBufferedUntil)
        {
            risingBufferedUntil = attackBuffered = -10f; combo = 0;
            StartPresentation(ActionKind.RisingSlash, movement.Facing);
            if (NetworkObject.IsSpawned) RequestAction(ActionKind.RisingSlash, movement.Facing);
        }
        else if (canChain && bufferedSkill >= 0 && Time.time <= skillBufferedUntil)
        {
            int skill = bufferedSkill; bufferedSkill = -1; HandleSkill(skill);
        }
        else if (canChain && Time.time - attackBuffered <= settings.attackBuffer)
        {
            attackBuffered = -10f; HandleAttack();
        }
    }
    public bool TryRisingBranch()
    {
        if (!HasControl || !movement.IsGrounded || !localActive || localKind > ActionKind.Slash3 || (health != null && health.IsStunned)) return false;
        risingBufferedUntil = Time.time + settings.attackBuffer;
        return true;
    }
    public void HandleLanding()
    {
        if (!localActive || localKind != ActionKind.AirSlash) return;
        landedDuringAirSlash = true;
        if (localPulse > 0) FinishLanding();
    }
    private void FinishLanding()
    {
        EndPresentation();
        if (NetworkObject.IsSpawned && IsOwner) RequestCancel(true);
    }
    public void HandleAttack()
    {
        if (!HasControl || (health != null && health.IsStunned)) return;
        if (localActive && Time.time - localStart < CancelTime(localKind)) { attackBuffered = Time.time; return; }
        if (Time.time > comboExpires) combo = 0;
        ActionKind kind = !movement.IsGrounded ? ActionKind.AirSlash : movement.IsDashing ? ActionKind.DashSlash : (ActionKind)combo;
        combo = kind <= ActionKind.Slash3 ? (combo + 1) % 3 : 0;
        comboExpires = Time.time + settings.comboWindow;
        StartPresentation(kind, movement.Facing);
        if (NetworkObject.IsSpawned) RequestAction(kind, movement.Facing);
    }
    public void HandleSkill(int skill)
    {
        if (!HasControl || skill < 0 || skill > 2 || (health != null && health.IsStunned) || Cooldown(skill) > 0f || Mana < Cost(skill)) return;
        if (localActive && Time.time - localStart < CancelTime(localKind))
        {
            bufferedSkill = skill; skillBufferedUntil = Time.time + settings.skillBuffer; return;
        }
        bufferedSkill = -1; attackBuffered = risingBufferedUntil = -10f;
        localCooldowns[skill] = Time.time + SkillCooldown(skill);
        if (!NetworkObject.IsSpawned) soloMana -= Cost(skill);
        StartPresentation((ActionKind)(skill + 5), movement.Facing);
        if (NetworkObject.IsSpawned) RequestAction((ActionKind)(skill + 5), movement.Facing);
    }
    private float SkillCooldown(int skill) => settings.Get((ActionKind)(skill + 5)).cooldown;
    private void StartPresentation(ActionKind kind, int direction)
    {
        localKind = kind; localStart = Time.time; localPulse = 0; localDirection = direction; localActive = true;
        landedDuringAirSlash = false;
        movement.StopDash();
        animationView?.Action(kind, Duration(kind));
    }
    private void EndPresentation()
    {
        localActive = false;
        landedDuringAirSlash = false;
        animationView?.ReleaseAction();
    }
    public bool TryMovementCancel()
    {
        if (!localActive) return true;
        if (Time.time - localStart < CancelTime(localKind)) return false;
        EndPresentation(); ClearBuffers(); combo = 0;
        if (NetworkObject.IsSpawned && IsOwner) RequestCancel(false);
        return true;
    }
    // Dash cancels startup, active frames and recovery. Spent skill costs stay spent.
    public void CancelForDash()
    {
        EndPresentation(); ClearBuffers(); combo = 0; comboExpires = 0f;
        if (!NetworkObject.IsSpawned || IsServerInitialized) serverActive = false;
    }
    public void CancelForDashOnServer()
    {
        if (!IsServerInitialized) return;
        serverActive = false;
        ShowCancel();
    }
    public void Interrupt()
    {
        EndPresentation(); ClearBuffers(); combo = 0;
        serverActive = false;
    }
    private void ClearBuffers() { attackBuffered = risingBufferedUntil = skillBufferedUntil = -10f; bufferedSkill = -1; }
    [ServerRpc]
    private void RequestAction(ActionKind kind, int direction)
    {
        if (!System.Enum.IsDefined(typeof(ActionKind),kind) || (direction != -1 && direction != 1) || (health != null && health.IsStunned)) { RejectAction(Owner); return; }
        // RPCs begin on a network tick, after the owner's presentation starts.
        // A small grace covers that tick boundary while preserving startup locks.
        if (serverActive && Time.time - serverStart + settings.networkRecoveryGrace < CancelTime(serverKind)) { RejectAction(Owner); return; }
        if (IsSkill(kind))
        {
            int skill = (int)kind - 5;
            if (Time.time < serverCooldowns[skill] || mana.Value < Cost(skill)) { RejectAction(Owner); return; }
            mana.Value -= Cost(skill); serverCooldowns[skill] = Time.time + SkillCooldown(skill);
        }
        movement.StopDash();
        serverKind = kind; serverDirection = direction; serverStart = Time.time; serverPulse = 0; serverActive = true;
        ShowAction(kind, direction);
    }
    [TargetRpc]
    private void RejectAction(FishNet.Connection.NetworkConnection connection)
    {
        if (IsSkill(localKind)) localCooldowns[(int)localKind - 5] = Time.time;
        EndPresentation();
    }
    [ServerRpc]
    private void RequestCancel(bool landing)
    {
        if (!serverActive) return;
        float elapsed = Time.time - serverStart;
        bool validLanding = landing && serverKind == ActionKind.AirSlash && elapsed >= ImpactTime(serverKind, 0) &&
            Physics.Raycast(transform.position + Vector3.up * 0.15f, Vector3.down, 0.5f, (1 << 6) | (1 << 7), QueryTriggerInteraction.Ignore);
        if (!validLanding && elapsed + settings.networkRecoveryGrace < CancelTime(serverKind)) return;
        serverActive = false; ShowCancel();
    }
    [ObserversRpc(ExcludeOwner = true)]
    private void ShowAction(ActionKind kind, int direction) => StartPresentation(kind, direction);
    [ObserversRpc(ExcludeOwner = true)]
    private void ShowCancel() => EndPresentation();
    private void ApplyHits(ActionKind kind, int direction, int pulse)
    {
        if (NetworkObject.IsSpawned && !IsServerInitialized) return;
        var action = settings.Get(kind);
        float reach = action.reach + pulse * action.pulseAdvance;
        Vector3 center = transform.position + new Vector3(direction * reach, action.height, 0f);
        int count = Physics.OverlapBoxNonAlloc(center, action.hitHalfSize, hitBuffer, Quaternion.identity, settings.enemyLayers, QueryTriggerInteraction.Ignore);
        hitTargets.Clear();
        for (int i = 0; i < count; i++)
        {
            var enemy = hitBuffer[i].GetComponentInParent<ICombatTarget>();
            if (enemy == null || !hitTargets.Add(enemy) || enemy.IsDead) continue;
            float damage = pulse == action.pulses - 1 ? action.finalDamage : action.damage;
            bool launcher = action.launcher || action.finalPulseLaunch && pulse == action.pulses - 1;
            enemy.TakeHit(damage, new Vector3(direction * action.push, launcher ? action.lift : 1f, 0f), action.stun);
            if (!IsSkill(kind))
            {
                if (NetworkObject.IsSpawned) mana.Value = Mathf.Min(settings.maxMana, mana.Value + settings.hitMana);
                else soloMana = Mathf.Min(settings.maxMana, soloMana + settings.hitMana);
            }
        }
    }
}
