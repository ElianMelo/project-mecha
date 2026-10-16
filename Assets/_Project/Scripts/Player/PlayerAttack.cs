using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(PlayerInputs), typeof(PlayerMovement))]
public class PlayerAttack : NetworkBehaviour
{
    public enum ActionKind { Slash1, Slash2, Slash3, AirSlash, DashSlash, MegaSlash, SwordFire, CriticalX, RisingSlash, DiveStrike, CriticalStrike, MagneticPull }
    [SerializeField] private PlayerCombatSettings settings;
    private readonly SyncVar<float> mana = new SyncVar<float>(100f);
    private float soloMana = 100f;
    private readonly float[] localCooldowns = new float[3], serverCooldowns = new float[3];
    private readonly Collider[] hitBuffer = new Collider[48];
    private readonly HashSet<ICombatTarget> hitTargets = new HashSet<ICombatTarget>();
    private readonly HashSet<ICombatTarget> diveTargets = new HashSet<ICombatTarget>();
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
    private bool bufferedDive;
    private float pauseUntil;
    private float branchUntil = -10f;
    private int bufferedBranch = -1, branchDirection;
    public bool ServerBusy => serverActive;
    public bool IsInHitStop => Time.time < pauseUntil;
    public PlayerCombatSettings.ActionData ActiveData => localActive ? settings.At(localPiece) : null;
    public float ActionElapsed => localActive ? Mathf.Max(0f, Time.time - localStart) : 0f;
    public float ActionDuration => localActive ? Duration(localPiece) : 0f;
    public float CancelRemaining => localActive ? Mathf.Max(0f, CancelTime(localPiece) - ActionElapsed) : 0f;
    public string Phase => !localActive ? "NEUTRAL" : IsInHitStop ? "HIT STOP" :
        ActionElapsed < ImpactTime(localPiece, 0) ? "STARTUP" :
        ActionElapsed < ImpactTime(localPiece, Pulses(localPiece) - 1) + 0.05f ? "ACTIVE" : "RECOVERY";
    public bool IsSideSwitchActive => localActive && settings.At(localPiece).sideSwitch && ActionElapsed < settings.At(localPiece).stepDuration;
    public bool ComboVisible => Time.time - LastHitTime < settings.comboDisplayWindow;
    public int ComboHits { get; private set; }
    public float ComboDamage { get; private set; }
    public float TotalDamage { get; private set; }
    public float LastDamage { get; private set; }
    public float LastHitTime { get; private set; } = -10f;
    public float PeakComboDamage { get; private set; }
    public event System.Action<float> HitConfirmed;
    public static readonly HashSet<PlayerAttack> ActiveActors = new HashSet<PlayerAttack>();
    public static float EnemyTempoAt(Vector3 position)
    {
        float tempo = 1f;
        foreach (var actor in ActiveActors)
            if (actor != null && actor.IsSkillActive && actor.ActionElapsed < actor.settings.skillFieldDuration &&
                (position - actor.transform.position).sqrMagnitude < actor.settings.skillFieldRadius * actor.settings.skillFieldRadius)
                tempo = Mathf.Min(tempo, actor.settings.skillEnemyTempo);
        return tempo;
    }
    public void ResetDamageReadout() { ComboHits = 0; ComboDamage = TotalDamage = LastDamage = PeakComboDamage = 0f; LastHitTime = -10f; }
    public bool IsBusy => localActive;
    public bool IsSkillActive => (localActive && IsSkill(localKind)) || (IsServerInitialized && serverActive && IsSkill(serverKind));
    public bool CanAirSteer => false;
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
    private float CancelTime(int piece) => IsSkill(settings.At(piece).kind) ? Duration(piece) : settings.At(piece).cancel;
    private void Awake()
    {
        inputs = GetComponent<PlayerInputs>(); movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>(); vfx = GetComponent<PlayerCombatVfx>(); animationView = GetComponent<CharacterAnimator>();
        soloMana = settings.maxMana;
    }
    private void OnEnable() { inputs.OnAttackInput += HandleAttack; inputs.OnSkillInput += HandleSkill; inputs.OnTechInput += HandleTech; ActiveActors.Add(this); }
    private void OnDisable() { inputs.OnAttackInput -= HandleAttack; inputs.OnSkillInput -= HandleSkill; inputs.OnTechInput -= HandleTech; ActiveActors.Remove(this); localActive = serverActive = false; ClearBuffers(); sequence.Reset(); }
    public override void OnStartServer() { base.OnStartServer(); mana.Value = settings.maxMana; }
    private void Update()
    {
        bool solo = !NetworkObject.IsSpawned;
        if (IsInHitStop)
        {
            if (localActive) localStart += Time.deltaTime;
            if (serverActive) serverStart += Time.deltaTime;
        }
        if (solo) soloMana = Mathf.Min(settings.maxMana, soloMana + settings.manaRegen * Time.deltaTime);
        else if (IsServerInitialized) mana.Value = Mathf.Min(settings.maxMana, mana.Value + settings.manaRegen * Time.deltaTime);
        if (localActive && !IsInHitStop)
        {
            float elapsed = Time.time - localStart;
            while (localPulse < Pulses(localPiece) && elapsed >= ImpactTime(localPiece, localPulse))
            {
                vfx?.Strike(settings.At(localPiece), localDirection, localPulse);
                if (!settings.At(localPiece).sideSwitch) movement.ApplyAttackStep(settings.At(localPiece), localDirection);
                if (solo) ApplyHits(localPiece, localDirection, localPulse);
                localPulse++;
            }
            if (elapsed >= Duration(localPiece)) EndPresentation();
        }
        if (IsServerInitialized && serverActive && !IsInHitStop)
        {
            float elapsed = Time.time - serverStart;
            while (serverPulse < Pulses(serverPiece) && elapsed >= ImpactTime(serverPiece, serverPulse))
                ApplyHits(serverPiece, serverDirection, serverPulse++);
            if (elapsed >= Duration(serverPiece)) serverActive = false;
        }
        bool canChain = HasControl && !IsInHitStop && (!localActive || Time.time - localStart >= CancelTime(localPiece));
        if (canChain && bufferedBranch >= 0 && Time.time <= branchUntil)
        {
            int piece = bufferedBranch; bufferedBranch = -1; sequence.Reset(); ClearBuffers();
            TryBeginPiece(piece, branchDirection); return;
        }
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
            bool dive = bufferedDive; bufferedDive = false;
            attackBuffered = -10f; HandleAttack(dive);
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
        if (!localActive || !IsAirAttack(localKind)) return;
        EndPresentation(); ClearBuffers();
        if (!NetworkObject.IsSpawned || IsServerInitialized) serverActive = false;
        if (NetworkObject.IsSpawned && IsOwner) RequestCancel(true);
    }
    private static bool IsAirAttack(ActionKind kind) => kind == ActionKind.AirSlash || kind == ActionKind.DiveStrike;
    public bool TryCriticalBranch(int direction)
    {
        if (!HasControl || !localActive || localKind > ActionKind.Slash3 || health != null && health.IsStunned) return false;
        bufferedBranch = Loadout != null ? settings.IndexOf(Loadout.criticalBranch) : settings.IndexOf(ActionKind.CriticalStrike);
        branchDirection = direction; branchUntil = Time.time + settings.attackBuffer;
        return bufferedBranch >= 0;
    }
    public void HandleTech()
    {
        if (!HasControl || health != null && health.IsStunned || IsSkillActive) return;
        int piece = Loadout != null ? settings.IndexOf(Loadout.techBranch) : settings.IndexOf(ActionKind.MagneticPull);
        if (piece < 0) return;
        if (localActive)
        {
            bufferedBranch = piece; branchDirection = movement.Facing; branchUntil = Time.time + settings.attackBuffer;
        }
        else TryBeginPiece(piece);
    }
    public void HandleAttack()
        => HandleAttack(false);
    private void HandleAttack(bool bufferedAirDive)
    {
        if (!HasControl || (health != null && health.IsStunned)) return;
        if (localActive && IsSkill(localKind)) return;
        bool wantsDive = bufferedAirDive || !movement.IsGrounded && inputs.DownHeld;
        if (localActive && Time.time - localStart < CancelTime(localPiece)) { attackBuffered = Time.time; bufferedDive = wantsDive; return; }
        if (Loadout != null)
        {
            AttackPiece piece;
            if (!movement.IsGrounded && wantsDive && Loadout.diveAttack != null) { sequence.Reset(); piece = Loadout.diveAttack; }
            else if (movement.IsDashing) { sequence.Reset(); piece = Loadout.dashAttack; }
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
        if (localActive && IsSkill(localKind)) return;
        if (localActive && Time.time - localStart < CancelTime(localPiece))
        {
            bufferedSkill = skill; skillBufferedUntil = Time.time + settings.skillBuffer; return;
        }
        bufferedSkill = -1; attackBuffered = risingBufferedUntil = -10f;
        sequence.Reset(); TryBeginPiece(settings.IndexOf((ActionKind)(skill + 5)));
    }
    private bool TryBeginPiece(int piece, int direction = 0)
    {
        if (!settings.IsValidPiece(piece)) return false;
        var data = settings.At(piece);
        if (data.airOnly && movement.IsGrounded || data.requiresGround && !movement.IsGrounded) return false;
        if (IsSkill(data.kind))
        {
            int skill = (int)data.kind - 5;
            if (Cooldown(skill) > 0f || Mana < data.manaCost) return false;
            localCooldowns[skill] = Time.time + data.cooldown;
            if (!NetworkObject.IsSpawned) soloMana -= data.manaCost;
        }
        direction = direction == 0 ? movement.Facing : direction;
        StartPresentation(piece, direction);
        if (NetworkObject.IsSpawned) RequestAction(piece, direction);
        return true;
    }
    private void StartPresentation(int piece, int direction)
    {
        diveTargets.Clear();
        localPiece = piece;
        ActionKind kind = settings.At(piece).kind;
        localKind = kind; localStart = Time.time; localPulse = 0; localDirection = direction; localActive = true;
        movement.Face(direction);
        movement.BeginAttackMotion(settings.At(piece), direction);
        vfx?.BeginAction(settings.At(piece), direction);
        animationView?.Action(kind, Duration(piece));
    }
    private void EndPresentation()
    {
        localActive = false;
        pauseUntil = 0f;
        animationView?.ReleaseAction();
    }
    public bool TryMovementCancel()
    {
        if (!localActive) return true;
        if (IsSkill(localKind) || IsAirAttack(localKind) || IsInHitStop || Time.time - localStart < CancelTime(localPiece)) return false;
        EndPresentation(); ClearBuffers(); combo = 0; sequence.Reset();
        if (NetworkObject.IsSpawned && IsOwner) RequestCancel(false);
        return true;
    }
    public void Interrupt()
    {
        EndPresentation(); ClearBuffers(); combo = 0; sequence.Reset();
        serverActive = false;
    }
    private void ClearBuffers() { bufferedDive = false; bufferedBranch = -1; branchUntil = -10f; attackBuffered = risingBufferedUntil = skillBufferedUntil = -10f; bufferedSkill = -1; }
    [ServerRpc]
    private void RequestAction(int piece, int direction)
    {
        if (!settings.IsValidPiece(piece) || (direction != -1 && direction != 1) || (health != null && health.IsStunned)) { RejectAction(Owner); return; }
        var action = settings.At(piece);
        ActionKind kind = action.kind;
        if (action.airOnly && movement.IsGrounded || action.requiresGround && !movement.IsGrounded) { RejectAction(Owner); return; }
        // RPCs begin on a network tick, after the owner's presentation starts.
        // A small grace covers that tick boundary while preserving startup locks.
        if (serverActive && (IsSkill(serverKind) || Time.time - serverStart + settings.networkRecoveryGrace < CancelTime(serverPiece))) { RejectAction(Owner); return; }
        if (IsSkill(kind))
        {
            int skill = (int)kind - 5;
            if (Time.time < serverCooldowns[skill] || mana.Value < action.manaCost) { RejectAction(Owner); return; }
            mana.Value -= action.manaCost; serverCooldowns[skill] = Time.time + action.cooldown;
        }
        movement.StopDash();
        diveTargets.Clear();
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
        bool validLanding = landing && IsAirAttack(serverKind) &&
            Physics.Raycast(transform.position + Vector3.up * 0.15f, Vector3.down, 0.5f, (1 << 6) | (1 << 7), QueryTriggerInteraction.Ignore);
        if (!validLanding && elapsed + settings.networkRecoveryGrace < CancelTime(serverPiece)) return;
        serverActive = false; ShowCancel();
    }
    [ObserversRpc(ExcludeOwner = true)]
    private void ShowAction(int piece, int direction) => StartPresentation(piece, direction);
    [ObserversRpc(ExcludeOwner = true)]
    private void ShowCancel() => EndPresentation();
    [ObserversRpc(RunLocally = true)]
    private void ConfirmHit(float damage, float hitStop) => RecordHit(damage, hitStop);
    private void RecordHit(float damage, float hitStop)
    {
        if (!ComboVisible) { ComboHits = 0; ComboDamage = 0f; }
        ComboHits++; ComboDamage += damage; TotalDamage += damage; LastDamage = damage; LastHitTime = Time.time;
        PeakComboDamage = Mathf.Max(PeakComboDamage, ComboDamage);
        pauseUntil = Mathf.Max(pauseUntil, Time.time + hitStop);
        HitConfirmed?.Invoke(damage);
    }
    private void ApplyHits(int piece, int direction, int pulse)
    {
        if (NetworkObject.IsSpawned && !IsServerInitialized) return;
        var action = settings.At(piece);
        ActionKind kind = action.kind;
        float reach = action.reach + pulse * action.pulseAdvance;
        Vector3 center = transform.position + new Vector3(direction * reach, action.height, 0f);
        // NetworkTransform writes Transforms between physics steps. Query the current
        // collider poses, without globally enabling costly autoSyncTransforms.
        Physics.SyncTransforms();
        int count = Physics.OverlapBoxNonAlloc(center, action.hitHalfSize, hitBuffer, Quaternion.identity, settings.enemyLayers, QueryTriggerInteraction.Ignore);
        hitTargets.Clear();
        for (int i = 0; i < count; i++)
        {
            var enemy = hitBuffer[i].GetComponentInParent<ICombatTarget>();
            if (enemy == null || !hitTargets.Add(enemy) || enemy.IsDead) continue;
            if (kind == ActionKind.DiveStrike && !diveTargets.Add(enemy)) continue;
            float damage = (pulse == action.pulses - 1 ? action.finalDamage : action.damage) * settings.damageMultiplier;
            bool launcher = action.launcher || action.finalPulseLaunch && pulse == action.pulses - 1;
            var target = hitBuffer[i].GetComponentInParent<EnemyController>();
            float dealt = Mathf.Min(damage, enemy.Health);
            bool airborne = target != null && target.IsJuggling;
            float push = action.pull ? Mathf.Sign(transform.position.x - hitBuffer[i].transform.position.x) * action.pullSpeed : direction * action.push;
            float lift = launcher ? action.lift : airborne ? action.airLift : 0f;
            enemy.TakeHit(damage, new Vector3(push, lift, 0f), action.stun);
            target?.PauseImpact(action.hitStop);
            if (NetworkObject.IsSpawned) ConfirmHit(dealt, action.hitStop);
            else RecordHit(dealt, action.hitStop);
            if (!IsSkill(kind))
            {
                if (NetworkObject.IsSpawned) mana.Value = Mathf.Min(settings.maxMana, mana.Value + settings.hitMana);
                else soloMana = Mathf.Min(settings.maxMana, soloMana + settings.hitMana);
            }
        }
    }
}
