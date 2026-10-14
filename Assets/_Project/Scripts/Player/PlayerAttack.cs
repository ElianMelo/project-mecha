using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(PlayerInputs), typeof(PlayerMovement))]
public class PlayerAttack : NetworkBehaviour
{
    public enum ActionKind { Slash1, Slash2, Slash3, AirSlash, DashSlash, MegaSlash, SwordFire, CriticalX, RisingSlash }
    [SerializeField] private LayerMask enemyLayer = 1 << 9;
    [SerializeField] private float manaRegen = 7f;
    private readonly SyncVar<float> mana = new SyncVar<float>(100f);
    private float soloMana = 100f;
    private readonly float[] localCooldowns = new float[3], serverCooldowns = new float[3];
    private readonly Collider[] hitBuffer = new Collider[48];
    private readonly HashSet<EnemyController> hitTargets = new HashSet<EnemyController>();
    private PlayerInputs inputs;
    private PlayerMovement movement;
    private PlayerHealth health;
    private PlayerCombatVfx vfx;
    private Animator animator;
    private ActionKind localKind, serverKind;
    private float localStart, serverStart, comboExpires, attackBuffered = -10f;
    private int localPulse, serverPulse, localDirection, serverDirection, combo;
    private bool localActive, serverActive;
    [SerializeField] private float attackInputBuffer = 0.22f;
    [SerializeField] private float skillInputBuffer = 0.2f;
    [SerializeField] private float networkRecoveryGrace = 0.035f;
    private float risingBufferedUntil = -10f, skillBufferedUntil = -10f;
    private int bufferedSkill = -1;
    private bool landedDuringAirSlash;
    public bool IsBusy => localActive;
    public bool IsSkillActive => (localActive && IsSkill(localKind)) || (IsServerInitialized && serverActive && IsSkill(serverKind));
    public bool CanAirSteer => localActive && localKind == ActionKind.AirSlash;
    public float Mana => NetworkObject.IsSpawned ? mana.Value : soloMana;
    public string CurrentAction => localActive ? localKind.ToString() : "Ready";
    public float Cooldown(int skill) => Mathf.Max(0f, localCooldowns[skill] - Time.time);
    public static float Cost(int skill) => skill == 0 ? 25f : skill == 1 ? 40f : 60f;
    private bool HasControl => !NetworkObject.IsSpawned || IsOwner;
    public static bool IsSkill(ActionKind kind) => kind >= ActionKind.MegaSlash && kind <= ActionKind.CriticalX;
    private static float Duration(ActionKind kind) => kind == ActionKind.MegaSlash ? 0.8f : kind == ActionKind.SwordFire ? 0.85f : kind == ActionKind.CriticalX ? 1.05f : kind == ActionKind.Slash3 ? 0.46f : kind == ActionKind.RisingSlash ? 0.42f : 0.34f;
    private static int Pulses(ActionKind kind) => kind == ActionKind.MegaSlash ? 4 : kind == ActionKind.SwordFire ? 3 : kind == ActionKind.CriticalX ? 2 : 1;
    private static float ImpactTime(ActionKind kind, int pulse) => kind == ActionKind.MegaSlash ? 0.12f + pulse * 0.13f : kind == ActionKind.SwordFire ? 0.24f + pulse * 0.12f : kind == ActionKind.CriticalX ? 0.25f + pulse * 0.2f : 0.1f;
    private static float CancelTime(ActionKind kind) => IsSkill(kind) ? ImpactTime(kind, Pulses(kind) - 1) + 0.08f : 0.19f;
    private void Awake()
    {
        inputs = GetComponent<PlayerInputs>(); movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>(); vfx = GetComponent<PlayerCombatVfx>(); animator = GetComponentInChildren<Animator>();
    }
    private void OnEnable() { inputs.OnAttackInput += HandleAttack; inputs.OnSkillInput += HandleSkill; }
    private void OnDisable() { inputs.OnAttackInput -= HandleAttack; inputs.OnSkillInput -= HandleSkill; localActive = serverActive = false; ClearBuffers(); }
    private void Update()
    {
        bool solo = !NetworkObject.IsSpawned;
        if (solo) soloMana = Mathf.Min(100f, soloMana + manaRegen * Time.deltaTime);
        else if (IsServerInitialized) mana.Value = Mathf.Min(100f, mana.Value + manaRegen * Time.deltaTime);
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
        else if (canChain && Time.time - attackBuffered <= attackInputBuffer)
        {
            attackBuffered = -10f; HandleAttack();
        }
    }
    public bool TryRisingBranch()
    {
        if (!HasControl || !movement.IsGrounded || !localActive || localKind > ActionKind.Slash3 || (health != null && health.IsStunned)) return false;
        risingBufferedUntil = Time.time + attackInputBuffer;
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
        comboExpires = Time.time + 0.8f;
        StartPresentation(kind, movement.Facing);
        if (NetworkObject.IsSpawned) RequestAction(kind, movement.Facing);
    }
    public void HandleSkill(int skill)
    {
        if (!HasControl || skill < 0 || skill > 2 || (health != null && health.IsStunned) || Cooldown(skill) > 0f || Mana < Cost(skill)) return;
        if (localActive && Time.time - localStart < CancelTime(localKind))
        {
            bufferedSkill = skill; skillBufferedUntil = Time.time + skillInputBuffer; return;
        }
        bufferedSkill = -1; attackBuffered = risingBufferedUntil = -10f;
        localCooldowns[skill] = Time.time + SkillCooldown(skill);
        if (!NetworkObject.IsSpawned) soloMana -= Cost(skill);
        StartPresentation((ActionKind)(skill + 5), movement.Facing);
        if (NetworkObject.IsSpawned) RequestAction((ActionKind)(skill + 5), movement.Facing);
    }
    private static float SkillCooldown(int skill) => skill == 0 ? 3f : skill == 1 ? 5f : 7f;
    private void StartPresentation(ActionKind kind, int direction)
    {
        localKind = kind; localStart = Time.time; localPulse = 0; localDirection = direction; localActive = true;
        landedDuringAirSlash = false;
        movement.StopDash();
        if (animator != null)
        {
            animator.SetFloat("AttackSpeed", 2f / Duration(kind));
            animator.Play("Attack", 0, 0f);
        }
        vfx?.Begin(kind, direction);
    }
    private void EndPresentation()
    {
        localActive = false;
        landedDuringAirSlash = false;
        if (animator != null) animator.CrossFade(movement.IsGrounded && Mathf.Abs(movement.Velocity.x) > 0.3f ? "Run" : "Idle", 0.04f);
    }
    public bool TryMovementCancel()
    {
        if (!localActive) return true;
        if (Time.time - localStart < CancelTime(localKind)) return false;
        EndPresentation(); ClearBuffers(); combo = 0;
        if (NetworkObject.IsSpawned && IsOwner) RequestCancel(false);
        return true;
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
        if ((int)kind < 0 || (int)kind > 8 || (direction != -1 && direction != 1) || (health != null && health.IsStunned)) { RejectAction(Owner); return; }
        // RPCs begin on a network tick, after the owner's presentation starts.
        // A small grace covers that tick boundary while preserving startup locks.
        if (serverActive && Time.time - serverStart + networkRecoveryGrace < CancelTime(serverKind)) { RejectAction(Owner); return; }
        if (IsSkill(kind))
        {
            int skill = (int)kind - 5;
            if (Time.time < serverCooldowns[skill] || mana.Value < Cost(skill)) { RejectAction(Owner); return; }
            mana.Value -= Cost(skill); serverCooldowns[skill] = Time.time + SkillCooldown(skill);
        }
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
        if (!validLanding && elapsed + networkRecoveryGrace < CancelTime(serverKind)) return;
        serverActive = false; ShowCancel();
    }
    [ObserversRpc(ExcludeOwner = true)]
    private void ShowAction(ActionKind kind, int direction) => StartPresentation(kind, direction);
    [ObserversRpc(ExcludeOwner = true)]
    private void ShowCancel() => EndPresentation();
    private void ApplyHits(ActionKind kind, int direction, int pulse)
    {
        if (NetworkObject.IsSpawned && !IsServerInitialized) return;
        Vector3 half = kind == ActionKind.SwordFire ? new Vector3(0.7f, 0.7f, 0.75f) : kind == ActionKind.CriticalX ? new Vector3(1.55f, 1.5f, 0.75f) : kind == ActionKind.RisingSlash ? new Vector3(0.6f, 0.9f, 0.65f) : kind == ActionKind.MegaSlash ? new Vector3(0.7f, 0.7f, 0.65f) : new Vector3(0.55f, 0.65f, 0.65f);
        float reach = kind == ActionKind.SwordFire ? 1.7f + pulse * 0.7f : kind == ActionKind.CriticalX ? 0.8f : kind == ActionKind.MegaSlash ? 0.65f : 0.5f;
        Vector3 center = transform.position + new Vector3(direction * reach, kind == ActionKind.CriticalX ? 1.2f : kind == ActionKind.RisingSlash ? 1f : 0.7f, 0f);
        int count = Physics.OverlapBoxNonAlloc(center, half, hitBuffer, Quaternion.identity, enemyLayer, QueryTriggerInteraction.Ignore);
        hitTargets.Clear();
        for (int i = 0; i < count; i++)
        {
            var enemy = hitBuffer[i].GetComponentInParent<EnemyController>();
            if (enemy == null || !hitTargets.Add(enemy) || enemy.IsDead) continue;
            float damage = kind == ActionKind.MegaSlash ? (pulse == 3 ? 18f : 8f) : kind == ActionKind.SwordFire ? 14f : kind == ActionKind.CriticalX ? 28f : kind == ActionKind.Slash3 ? 15f : kind == ActionKind.RisingSlash ? 13f : 9f;
            bool launcher = kind == ActionKind.CriticalX || kind == ActionKind.Slash3 || kind == ActionKind.RisingSlash || (kind == ActionKind.MegaSlash && pulse == 3);
            enemy.TakeHit(damage, new Vector3(direction * (launcher ? 4f : 1.6f), launcher ? 7f : 1f, 0f), launcher ? 0.55f : 0.22f);
            if (!IsSkill(kind))
            {
                if (NetworkObject.IsSpawned) mana.Value = Mathf.Min(100f, mana.Value + 4f);
                else soloMana = Mathf.Min(100f, soloMana + 4f);
            }
        }
    }
}
