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
    private int localPiece, serverPiece;
    private readonly ComboSequencePlayer sequence = new ComboSequencePlayer();
    private ComboLoadout previewLoadout;
    public ComboLoadout Loadout => previewLoadout != null ? previewLoadout : settings.combos;
    public PlayerCombatSettings Settings => settings;
    public void UseLoadout(ComboLoadout loadout) { Interrupt(); previewLoadout = loadout; }
    public void ResetPracticeResources()
    {
        if (NetworkObject.IsSpawned) return;
        soloMana = settings.maxMana;
        System.Array.Clear(localCooldowns, 0, localCooldowns.Length);
    }
    public void BeginAirCombo() => sequence.Reset();
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
    private float Duration(int piece) => settings.At(piece).duration;
    private int Pulses(int piece) => settings.At(piece).pulses;
    private float ImpactTime(int piece, int pulse) => settings.At(piece).impact + pulse * settings.At(piece).pulseInterval;
    private float CancelTime(int piece) => settings.At(piece).cancel;
    private void Awake()
    {
        inputs = GetComponent<PlayerInputs>(); movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>(); vfx = GetComponent<PlayerCombatVfx>(); animationView = GetComponent<CharacterAnimator>();
        soloMana = settings.maxMana;
    }
    private void OnEnable() { inputs.OnAttackInput += HandleAttack; inputs.OnSkillInput += HandleSkill; }
    private void OnDisable() { inputs.OnAttackInput -= HandleAttack; inputs.OnSkillInput -= HandleSkill; localActive = serverActive = false; ClearBuffers(); sequence.Reset(); }
    public override void OnStartServer() { base.OnStartServer(); mana.Value = settings.maxMana; }
    private void Update()
    {
        bool solo = !NetworkObject.IsSpawned;
        if (solo) soloMana = Mathf.Min(settings.maxMana, soloMana + settings.manaRegen * Time.deltaTime);
        else if (IsServerInitialized) mana.Value = Mathf.Min(settings.maxMana, mana.Value + settings.manaRegen * Time.deltaTime);
        if (localActive)
        {
            float elapsed = Time.time - localStart;
            while (localPulse < Pulses(localPiece) && elapsed >= ImpactTime(localPiece, localPulse))
            {
                vfx?.Strike(settings.At(localPiece), localDirection, localPulse);
                if (solo) ApplyHits(localPiece, localDirection, localPulse);
                localPulse++;
            }
            if (elapsed >= Duration(localPiece)) EndPresentation();
            else if (landedDuringAirSlash && localPulse > 0) FinishLanding();
        }
        if (IsServerInitialized && serverActive)
        {
            float elapsed = Time.time - serverStart;
            while (serverPulse < Pulses(serverPiece) && elapsed >= ImpactTime(serverPiece, serverPulse))
                ApplyHits(serverPiece, serverDirection, serverPulse++);
            if (elapsed >= Duration(serverPiece)) serverActive = false;
        }
        bool canChain = HasControl && (!localActive || Time.time - localStart >= CancelTime(localPiece));
        if (canChain && Time.time <= risingBufferedUntil)
        {
            risingBufferedUntil = attackBuffered = -10f; combo = 0;
            sequence.Reset();
            TryBeginPiece(Loadout != null ? settings.IndexOf(Loadout.risingBranch) : settings.IndexOf(ActionKind.RisingSlash));
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
        sequence.Reset();
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
        if (localActive && Time.time - localStart < CancelTime(localPiece)) { attackBuffered = Time.time; return; }
        if (Loadout != null)
        {
            AttackPiece piece;
            if (movement.IsDashing) { sequence.Reset(); piece = Loadout.dashAttack; }
            else piece = sequence.Consume(movement.IsGrounded ? Loadout.groundCombo : Loadout.airCombo, Time.time);
            if (!TryBeginPiece(settings.IndexOf(piece))) sequence.Reset();
            return;
        }
        if (Time.time > comboExpires) combo = 0;
        ActionKind kind = !movement.IsGrounded ? ActionKind.AirSlash : movement.IsDashing ? ActionKind.DashSlash : (ActionKind)combo;
        combo = kind <= ActionKind.Slash3 ? (combo + 1) % 3 : 0;
        comboExpires = Time.time + settings.comboWindow;
        TryBeginPiece(settings.IndexOf(kind));
    }
    public void HandleSkill(int skill)
    {
        if (!HasControl || skill < 0 || skill > 2 || (health != null && health.IsStunned) || Cooldown(skill) > 0f || Mana < Cost(skill)) return;
        if (localActive && Time.time - localStart < CancelTime(localPiece))
        {
            bufferedSkill = skill; skillBufferedUntil = Time.time + settings.skillBuffer; return;
        }
        bufferedSkill = -1; attackBuffered = risingBufferedUntil = -10f;
        sequence.Reset(); TryBeginPiece(settings.IndexOf((ActionKind)(skill + 5)));
    }
    private bool TryBeginPiece(int piece)
    {
        if (!settings.IsValidPiece(piece)) return false;
        var data = settings.At(piece);
        if (IsSkill(data.kind))
        {
            int skill = (int)data.kind - 5;
            if (Cooldown(skill) > 0f || Mana < data.manaCost) return false;
            localCooldowns[skill] = Time.time + data.cooldown;
            if (!NetworkObject.IsSpawned) soloMana -= data.manaCost;
        }
        StartPresentation(piece, movement.Facing);
        if (NetworkObject.IsSpawned) RequestAction(piece, movement.Facing);
        return true;
    }
    private void StartPresentation(int piece, int direction)
    {
        localPiece = piece;
        ActionKind kind = settings.At(piece).kind;
        localKind = kind; localStart = Time.time; localPulse = 0; localDirection = direction; localActive = true;
        landedDuringAirSlash = false;
        movement.StopDash();
        animationView?.Action(kind, Duration(piece));
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
        if (Time.time - localStart < CancelTime(localPiece)) return false;
        EndPresentation(); ClearBuffers(); combo = 0; sequence.Reset();
        if (NetworkObject.IsSpawned && IsOwner) RequestCancel(false);
        return true;
    }
    // Dash cancels startup, active frames and recovery. Spent skill costs stay spent.
    public void CancelForDash()
    {
        EndPresentation(); ClearBuffers(); combo = 0; comboExpires = 0f; sequence.Reset();
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
        EndPresentation(); ClearBuffers(); combo = 0; sequence.Reset();
        serverActive = false;
    }
    private void ClearBuffers() { attackBuffered = risingBufferedUntil = skillBufferedUntil = -10f; bufferedSkill = -1; }
    [ServerRpc]
    private void RequestAction(int piece, int direction)
    {
        if (!settings.IsValidPiece(piece) || (direction != -1 && direction != 1) || (health != null && health.IsStunned)) { RejectAction(Owner); return; }
        var action = settings.At(piece);
        ActionKind kind = action.kind;
        // RPCs begin on a network tick, after the owner's presentation starts.
        // A small grace covers that tick boundary while preserving startup locks.
        if (serverActive && Time.time - serverStart + settings.networkRecoveryGrace < CancelTime(serverPiece)) { RejectAction(Owner); return; }
        if (IsSkill(kind))
        {
            int skill = (int)kind - 5;
            if (Time.time < serverCooldowns[skill] || mana.Value < action.manaCost) { RejectAction(Owner); return; }
            mana.Value -= action.manaCost; serverCooldowns[skill] = Time.time + action.cooldown;
        }
        movement.StopDash();
        serverPiece = piece; serverKind = kind; serverDirection = direction; serverStart = Time.time; serverPulse = 0; serverActive = true;
        ShowAction(piece, direction);
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
        bool validLanding = landing && serverKind == ActionKind.AirSlash && elapsed >= ImpactTime(serverPiece, 0) &&
            Physics.Raycast(transform.position + Vector3.up * 0.15f, Vector3.down, 0.5f, (1 << 6) | (1 << 7), QueryTriggerInteraction.Ignore);
        if (!validLanding && elapsed + settings.networkRecoveryGrace < CancelTime(serverPiece)) return;
        serverActive = false; ShowCancel();
    }
    [ObserversRpc(ExcludeOwner = true)]
    private void ShowAction(int piece, int direction) => StartPresentation(piece, direction);
    [ObserversRpc(ExcludeOwner = true)]
    private void ShowCancel() => EndPresentation();
    private void ApplyHits(int piece, int direction, int pulse)
    {
        if (NetworkObject.IsSpawned && !IsServerInitialized) return;
        var action = settings.At(piece);
        ActionKind kind = action.kind;
        float reach = action.reach + pulse * action.pulseAdvance;
        Vector3 center = transform.position + new Vector3(direction * reach, action.height, 0f);
        int count = Physics.OverlapBoxNonAlloc(center, action.hitHalfSize, hitBuffer, Quaternion.identity, settings.enemyLayers, QueryTriggerInteraction.Ignore);
        hitTargets.Clear();
        for (int i = 0; i < count; i++)
        {
            var enemy = hitBuffer[i].GetComponentInParent<ICombatTarget>();
            if (enemy == null || !hitTargets.Add(enemy) || enemy.IsDead) continue;
            float damage = (pulse == action.pulses - 1 ? action.finalDamage : action.damage) * settings.damageMultiplier;
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
