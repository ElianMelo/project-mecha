using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(PlayerInputs), typeof(PlayerMovement))]
public class PlayerAttack : NetworkBehaviour
{
    public enum ActionKind { Slash1, Slash2, Slash3, AirSlash, DashSlash, MegaSlash, SwordFire, CriticalX, RisingSlash, DiveStrike, CriticalStrike, MagneticPull, Heavy1, Heavy2, Heavy3 }
    [SerializeField] private PlayerCombatSettings settings;
    private readonly SyncVar<float> mana = new SyncVar<float>(100f);
    private float soloMana = 100f;
    private readonly float[] localCooldowns = new float[3], serverCooldowns = new float[3];
    private Collider[] hitBuffer = new Collider[64];
    private readonly List<SkillContactWindow> skillWindows = new List<SkillContactWindow>();
    private CombatEmissionOrigin emissionOrigin;
    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
    private readonly HashSet<IDamageable> diveTargets = new HashSet<IDamageable>();
    private PlayerInputs inputs;
    private PlayerMovement movement;
    private PlayerHealth health;
    private PlayerCombatVfx vfx;
    private CharacterAnimator animationView;
    private CharacterRuntime character;
    private CharacterSkills characterSkills;
    private CharacterModifiers modifiers;
    private float localSpeed = 1f, serverSpeed = 1f;
    public void Configure(PlayerCombatSettings profile) { Interrupt(); settings = profile; soloMana = profile.maxMana; }
    public string SkillTitle(int slot) => character?.Skill(slot) is CharacterSkillDefinition skill ? GameText.Get(skill.displayName) : settings.Get((ActionKind)(slot + 5)).kind == ActionKind.MegaSlash ? GameText.Get("Combat", "skill.megaSlash") : slot == 1 ? GameText.Get("Combat", "skill.swordFire") : GameText.Get("Combat", "skill.criticalX");
    public string ActiveTitle => IsSkill(localKind) ? SkillTitle((int)localKind - 5) : CurrentAction;
    public float SkillStatus(int slot) => character?.Skill(slot)?.StatusRemaining(characterSkills) ?? 0f;
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
        health?.ResetEscapeResources();
        System.Array.Clear(localCooldowns, 0, localCooldowns.Length);
    }
    public void BeginAirCombo() => sequence.Reset();
    public void ResetMatchResources()
    {
        if (!IsServerInitialized) return;
        mana.Value = settings.maxMana;
        health?.ResetEscapeResources();
        System.Array.Clear(serverCooldowns, 0, serverCooldowns.Length);
        ResetMatchPresentation();
    }
    [ObserversRpc(RunLocally = true)]
    private void ResetMatchPresentation() { Interrupt(); System.Array.Clear(localCooldowns, 0, localCooldowns.Length); ResetDamageReadout(); }
    private float localStart, serverStart, comboExpires, attackBuffered = -10f;
    private int localPulse, serverPulse, localDirection, serverDirection, combo;
    private bool localActive, serverActive;
    private float risingBufferedUntil = -10f, skillBufferedUntil = -10f;
    private int bufferedSkill = -1;
    private bool bufferedDive, bufferedHeavy;
    private float branchUntil = -10f;
    private int bufferedBranch = -1, branchDirection;
    public bool ServerBusy => serverActive;
    public PlayerCombatSettings.ActionData ActiveData => localActive ? settings.At(localPiece) : null;
    public float ActionElapsed => localActive ? Mathf.Max(0f, (Time.time - localStart) * localSpeed) : 0f;
    public float ActionDuration => localActive ? Duration(localPiece) : 0f;
    public float CancelRemaining => localActive ? Mathf.Max(0f, CancelTime(localPiece) - ActionElapsed) : 0f;
    public string Phase => !localActive ? "NEUTRAL" :
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
    public static bool IsHeavy(ActionKind kind) => kind >= ActionKind.Heavy1 && kind <= ActionKind.Heavy3;
    private float Duration(int piece) => settings.At(piece).duration;
    private int Pulses(int piece) => settings.At(piece).pulses;
    private float ImpactTime(int piece, int pulse) => settings.At(piece).impact + pulse * settings.At(piece).pulseInterval;
    private float CancelTime(int piece) => IsSkill(settings.At(piece).kind) ? Duration(piece) : settings.At(piece).cancel;
    private void Awake()
    {
        inputs = GetComponent<PlayerInputs>(); movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>(); vfx = GetComponent<PlayerCombatVfx>(); animationView = GetComponent<CharacterAnimator>();
        character = GetComponent<CharacterRuntime>(); characterSkills = GetComponent<CharacterSkills>(); modifiers = GetComponent<CharacterModifiers>();
        emissionOrigin = GetComponent<CombatEmissionOrigin>();
        soloMana = settings.maxMana;
    }
    private void OnEnable() { inputs.OnAttackInput += HandleAttack; inputs.OnSkillInput += HandleSkill; inputs.OnTechInput += HandleTech; ActiveActors.Add(this); }
    private void OnDisable() { inputs.OnAttackInput -= HandleAttack; inputs.OnSkillInput -= HandleSkill; inputs.OnTechInput -= HandleTech; ActiveActors.Remove(this); localActive = serverActive = false; skillWindows.Clear(); ClearBuffers(); sequence.Reset(); }
    public override void OnStartServer() { base.OnStartServer(); mana.Value = settings.maxMana; }
    private void Update()
    {
        if (PvpMatchDirector.BlocksCombat) { if (localActive || serverActive) Interrupt(); return; }
        bool solo = !NetworkObject.IsSpawned;
        if (solo) soloMana = Mathf.Min(settings.maxMana, soloMana + settings.manaRegen * Time.deltaTime);
        else if (IsServerInitialized) mana.Value = Mathf.Min(settings.maxMana, mana.Value + settings.manaRegen * Time.deltaTime);
        if (localActive)
        {
            float elapsed = (Time.time - localStart) * localSpeed;
            while (localPulse < Pulses(localPiece) && elapsed >= ImpactTime(localPiece, localPulse))
            {
                vfx?.Strike(settings.At(localPiece), localDirection, localPulse);
                if (!settings.At(localPiece).sideSwitch) movement.ApplyAttackStep(settings.At(localPiece), localDirection);
                if (solo) ApplyHits(localPiece, localDirection, localPulse);
                localPulse++;
            }
            if (elapsed >= Duration(localPiece)) EndPresentation();
        }
        if (IsServerInitialized && serverActive)
        {
            float elapsed = (Time.time - serverStart) * serverSpeed;
            while (serverPulse < Pulses(serverPiece) && elapsed >= ImpactTime(serverPiece, serverPulse))
                ApplyHits(serverPiece, serverDirection, serverPulse++);
            if (elapsed >= Duration(serverPiece)) serverActive = false;
        }
        if (solo || IsServerInitialized) UpdateSkillContacts();
        bool canChain = HasControl && (!localActive || ActionElapsed >= CancelTime(localPiece));
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
            bool dive = bufferedDive, heavy = bufferedHeavy; bufferedDive = bufferedHeavy = false;
            attackBuffered = -10f; HandleAttack(dive, heavy);
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
        HandleAttack(false, true);
    }
    public void HandleAttack()
        => HandleAttack(false);
    private void HandleAttack(bool bufferedAirDive, bool heavy = false)
    {
        if (character != null && !character.Allows(heavy ? CombatCapabilities.HeavyMelee : CombatCapabilities.LightMelee)) return;
        if (!HasControl || (health != null && health.IsStunned)) return;
        if (localActive && IsSkill(localKind)) return;
        bool wantsDive = !heavy && (bufferedAirDive || !movement.IsGrounded && inputs.DownHeld);
        if (localActive && ActionElapsed < CancelTime(localPiece)) { attackBuffered = Time.time; bufferedDive = wantsDive; bufferedHeavy = heavy; return; }
        if (Loadout != null)
        {
            AttackPiece piece;
            if (heavy) piece = sequence.ConsumeVariant(movement.IsGrounded ? Loadout.heavyCombo : Loadout.heavyAirCombo, Time.time);
            else if (!movement.IsGrounded && wantsDive && Loadout.diveAttack != null) { sequence.Reset(); piece = Loadout.diveAttack; }
            else if (movement.IsDashing) { sequence.Reset(); piece = Loadout.dashAttack; }
            else piece = sequence.ConsumeVariant(movement.IsGrounded ? Loadout.groundCombo : Loadout.airCombo, Time.time);
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
        if (localActive && ActionElapsed < CancelTime(localPiece))
        {
            bufferedSkill = skill; skillBufferedUntil = Time.time + settings.skillBuffer; return;
        }
        bufferedSkill = -1; attackBuffered = risingBufferedUntil = -10f;
        sequence.Reset(); TryBeginPiece(settings.IndexOf((ActionKind)(skill + 5)));
    }
    private bool TryBeginPiece(int piece, int direction = 0)
    {
        if (PvpMatchDirector.BlocksCombat) return false;
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
        if (!NetworkObject.IsSpawned) skillWindows.Clear();
        localPiece = piece;
        ActionKind kind = settings.At(piece).kind;
        localSpeed = IsSkill(kind) ? 1f : modifiers != null ? modifiers.AttackSpeed : 1f;
        localKind = kind; localStart = Time.time; localPulse = 0; localDirection = direction; localActive = true;
        movement.Face(direction);
        movement.BeginAttackMotion(settings.At(piece), direction);
        vfx?.BeginAction(settings.At(piece), direction);
        animationView?.Action(kind, Duration(piece) / localSpeed);
    }
    private void EndPresentation()
    {
        localActive = false;
        vfx?.EndAction();
        animationView?.ReleaseAction();
    }
    public bool TryMovementCancel()
    {
        if (!localActive) return true;
        if (IsSkill(localKind) || IsAirAttack(localKind) || ActionElapsed < CancelTime(localPiece)) return false;
        EndPresentation(); ClearBuffers(); combo = 0; sequence.Reset();
        if (NetworkObject.IsSpawned && IsOwner) RequestCancel(false);
        return true;
    }
    public void Interrupt()
    {
        EndPresentation(); ClearBuffers(); combo = 0; sequence.Reset();
        serverActive = false;
        skillWindows.Clear();
    }
    private void ClearBuffers() { bufferedDive = bufferedHeavy = false; bufferedBranch = -1; branchUntil = -10f; attackBuffered = risingBufferedUntil = skillBufferedUntil = -10f; bufferedSkill = -1; }
    [ServerRpc]
    private void RequestAction(int piece, int direction)
    {
        if (PvpMatchDirector.BlocksCombat) { RejectAction(Owner); return; }
        if (character != null && !character.Ready) { RejectAction(Owner); return; }
        if (!settings.IsValidPiece(piece) || (direction != -1 && direction != 1) || (health != null && health.IsStunned)) { RejectAction(Owner); return; }
        var action = settings.At(piece);
        ActionKind kind = action.kind;
        var capability = kind == ActionKind.MagneticPull || IsHeavy(kind) ? CombatCapabilities.HeavyMelee : CombatCapabilities.LightMelee;
        if (!IsSkill(kind) && character != null && !character.Allows(capability)) { RejectAction(Owner); return; }
        if (action.airOnly && movement.IsGrounded || action.requiresGround && !movement.IsGrounded) { RejectAction(Owner); return; }
        // RPCs begin on a network tick, after the owner's presentation starts.
        // A small grace covers that tick boundary while preserving startup locks.
        if (serverActive && (IsSkill(serverKind) || (Time.time - serverStart + settings.networkRecoveryGrace) * serverSpeed < CancelTime(serverPiece))) { RejectAction(Owner); return; }
        if (IsSkill(kind))
        {
            int skill = (int)kind - 5;
            if (Time.time < serverCooldowns[skill] || mana.Value < action.manaCost) { RejectAction(Owner); return; }
            mana.Value -= action.manaCost; serverCooldowns[skill] = Time.time + action.cooldown;
        }
        movement.StopDash();
        diveTargets.Clear();
        skillWindows.Clear();
        serverPiece = piece; serverKind = kind; serverDirection = direction; serverStart = Time.time; serverPulse = 0; serverActive = true;
        serverSpeed = IsSkill(kind) ? 1f : modifiers != null ? modifiers.AttackSpeed : 1f;
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
        float elapsed = (Time.time - serverStart) * serverSpeed;
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
    private void ConfirmHit(float damage) => RecordHit(damage);
    private void RecordHit(float damage)
    {
        if (!ComboVisible) { ComboHits = 0; ComboDamage = 0f; }
        ComboHits++; ComboDamage += damage; TotalDamage += damage; LastDamage = damage; LastHitTime = Time.time;
        PeakComboDamage = Mathf.Max(PeakComboDamage, ComboDamage);
        HitConfirmed?.Invoke(damage);
    }
    private void ApplyHits(int piece, int direction, int pulse)
    {
        if (NetworkObject.IsSpawned && !IsServerInitialized) return;
        var action = settings.At(piece);
        if (characterSkills != null && characterSkills.ApplyAuthority(action.kind, direction, pulse)) return;
        if (action.hitLength > 0f && emissionOrigin != null)
        {
            skillWindows.Add(new SkillContactWindow { piece = piece, pulse = pulse, facing = direction,
                started = Time.time, expires = Time.time + action.hitActiveTime, previousOrigin = emissionOrigin.Resolve(action, direction, pulse) });
            return;
        }
        ActionKind kind = action.kind;
        float reach = action.reach + pulse * action.pulseAdvance;
        Vector3 center = transform.position + new Vector3(direction * reach, action.height, 0f);
        // NetworkTransform writes Transforms between physics steps. Query the current
        // collider poses, without globally enabling costly autoSyncTransforms.
        Physics.SyncTransforms();
        int count = Physics.OverlapBoxNonAlloc(center, action.hitHalfSize, hitBuffer, Quaternion.identity, CombatTargetRules.TargetLayers(settings.enemyLayers), QueryTriggerInteraction.Ignore);
        hitTargets.Clear();
        for (int i = 0; i < count; i++)
        {
            var enemy = hitBuffer[i].GetComponentInParent<IDamageable>();
            if (!CombatTargetRules.CanHit(health, enemy) || !hitTargets.Add(enemy)) continue;
            if (kind == ActionKind.DiveStrike && !diveTargets.Add(enemy)) continue;
            DealHit(action, direction, pulse, enemy, hitBuffer[i]);
        }
    }

    private void UpdateSkillContacts()
    {
        if (skillWindows.Count == 0) return;
        Physics.SyncTransforms();
        for (int w = skillWindows.Count - 1; w >= 0; w--)
        {
            var window = skillWindows[w];
            if (Time.time > window.expires) { skillWindows.RemoveAt(w); continue; }
            var action = settings.At(window.piece);
            Vector3 origin = emissionOrigin.Resolve(action, window.facing, window.pulse);
            Vector3 axis = CombatEmissionOrigin.Direction(action, window.facing, window.pulse);
            int steps = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(origin, window.previousOrigin) / action.hitRadius), 1, 6);
            for (int s = 1; s <= steps; s++)
            {
                Vector3 start = Vector3.Lerp(window.previousOrigin, origin, (float)s / steps);
                float length = action.hitTravelTime > 0f ? action.hitLength * Mathf.Clamp01((Time.time - window.started) / action.hitTravelTime) : action.hitLength;
                if (Physics.Raycast(start, axis, out var wall, length, action.hitObstructions, QueryTriggerInteraction.Ignore))
                    length = Mathf.Max(0f, wall.distance - 0.03f);
                Vector3 end = start + axis * length;
                int count;
                while (true)
                {
                    count = Physics.OverlapCapsuleNonAlloc(start, end, action.hitRadius, hitBuffer, CombatTargetRules.TargetLayers(settings.enemyLayers), QueryTriggerInteraction.Ignore);
                    if (count < hitBuffer.Length) break;
                    if (hitBuffer.Length >= 256) { hitBuffer = Physics.OverlapCapsule(start, end, action.hitRadius, CombatTargetRules.TargetLayers(settings.enemyLayers), QueryTriggerInteraction.Ignore); count = hitBuffer.Length; break; }
                    System.Array.Resize(ref hitBuffer, hitBuffer.Length * 2);
                }
                for (int i = 0; i < count; i++)
                {
                    var collider = hitBuffer[i];
                    var target = collider.GetComponentInParent<IDamageable>();
                    if (!CombatTargetRules.CanHit(health, target) || window.targets.Contains(target)) continue;
                    Vector3 point = collider.ClosestPoint(start);
                    Vector3 towardTarget = collider.bounds.center - start;
                    Vector3 normal;
                    if (towardTarget.sqrMagnitude > 0.001f && collider.Raycast(new Ray(start, towardTarget.normalized), out var surface, towardTarget.magnitude + collider.bounds.extents.magnitude))
                    { point = surface.point; normal = surface.normal; }
                    else { normal = -axis; }
                    // Caps must not reach through a wall or behind the firing hand.
                    if (Vector3.Dot(point - start, axis) < -0.05f ||
                        Physics.Linecast(start, point, action.hitObstructions, QueryTriggerInteraction.Ignore)) continue;
                    window.targets.Add(target);
                    Vector3 incoming = point - start;
                    incoming = incoming.sqrMagnitude > 0.001f ? incoming.normalized : axis;
                    DealHit(action, window.facing, window.pulse, target, collider);
                    if (IsSkill(action.kind))
                    {
                        if (NetworkObject.IsSpawned) ShowSkillContact((int)action.kind, point, incoming, normal, window.pulse == action.pulses - 1);
                        else vfx?.SkillContact(action.kind, point, incoming, normal, window.pulse == action.pulses - 1);
                    }
                }
            }
            window.previousOrigin = origin;
            if (Time.time >= window.expires) skillWindows.RemoveAt(w);
        }
    }

    [ObserversRpc(RunLocally = true)]
    private void ShowSkillContact(int kind, Vector3 point, Vector3 incoming, Vector3 normal, bool final)
        => vfx?.SkillContact((ActionKind)kind, point, incoming, normal, final);

    private void DealHit(PlayerCombatSettings.ActionData action, int direction, int pulse, IDamageable enemy, Collider contact)
    {
            var kind = action.kind;
            float damage = (pulse == action.pulses - 1 ? action.finalDamage : action.damage) * settings.damageMultiplier;
            bool launcher = action.launcher || action.finalPulseLaunch && pulse == action.pulses - 1;
            var target = contact.GetComponentInParent<EnemyController>();
            float before = enemy.Health;
            bool airborne = target != null && target.IsJuggling;
            float push = action.pull ? Mathf.Sign(transform.position.x - contact.transform.position.x) * action.pullSpeed : direction * action.push;
            float lift = launcher ? action.lift : airborne ? action.airLift : 0f;
            enemy.TakeDamage(damage, new Vector3(push, lift, 0f), action.stun, health);
            float dealt = float.IsPositiveInfinity(before) ? damage : Mathf.Max(0f, before - enemy.Health);
            if (dealt <= 0f) return;
            if (NetworkObject.IsSpawned) ConfirmHit(dealt);
            else RecordHit(dealt);
            if (!IsSkill(kind))
            {
                if (NetworkObject.IsSpawned) mana.Value = Mathf.Min(settings.maxMana, mana.Value + settings.hitMana);
                else soloMana = Mathf.Min(settings.maxMana, soloMana + settings.hitMana);
            }
    }
}
