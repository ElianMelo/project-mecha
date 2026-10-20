using UnityEngine;

public enum CombatReactionState { None, Flinch, HitReaction, KnockUp, AirHit, Knockdown, StandUp, Death }

// Damage/authority remain in health; Rigidbody movement remains in the motors.
// This small state machine coordinates control locks and native Animator states.
public sealed class CombatReaction : MonoBehaviour
{
    [SerializeField] private CombatReactionSettings settings;
    private CharacterAnimator view;
    private PlayerMovement player;
    private EnemyController enemy;
    private PlayerAttack attack;
    private float until, hitUntil, airStarted;
    private bool interrupting, airborne;
    public CombatReactionState State { get; private set; }
    public bool BlocksControl => interrupting && State != CombatReactionState.None;
    public bool ProtectsRecovery => State == CombatReactionState.Knockdown || State == CombatReactionState.StandUp;
    private bool Grounded => player != null ? player.IsGrounded : enemy != null && enemy.IsGrounded;

    private void Awake()
    {
        view = GetComponent<CharacterAnimator>(); player = GetComponent<PlayerMovement>();
        enemy = GetComponent<EnemyController>(); attack = GetComponent<PlayerAttack>();
    }

    public void Hit(Vector3 impulse, float stun, bool interrupts = true)
    {
        if (settings == null || State == CombatReactionState.Death || ProtectsRecovery) return;
        if (!interrupts)
        {
            // Armor hit: feedback always plays, but attacks and locomotion keep control.
            if (BlocksControl || attack != null && attack.IsBusy || player != null && player.IsDashing) return;
            interrupting = false; Set(CombatReactionState.Flinch, settings.flinchSeconds); return;
        }
        interrupting = true;
        hitUntil = Time.time + Mathf.Max(0f, stun);
        if (impulse.y >= settings.launchThreshold || airborne)
        {
            airborne = true; airStarted = Time.time;
            Set(CombatReactionState.KnockUp, settings.launchSeconds);
        }
        else Set(stun > settings.heavyReactionThreshold ? CombatReactionState.HitReaction : CombatReactionState.Flinch,
            Mathf.Max(settings.flinchSeconds, stun));
    }

    private void Update()
    {
        if (settings == null || State == CombatReactionState.None || State == CombatReactionState.Death) return;
        // A new action may replace an armor flinch without waiting for its animation.
        if (!interrupting && attack != null && attack.IsBusy) { State = CombatReactionState.None; return; }
        if (airborne)
        {
            if (Time.time - airStarted >= settings.minimumAirSeconds && Grounded)
            { airborne = false; Set(CombatReactionState.Knockdown, settings.knockdownSeconds); }
            else if (Time.time - airStarted >= settings.maximumAirSeconds)
            { airborne = false; Clear(); }
            else if (State == CombatReactionState.KnockUp && Time.time >= until)
                Set(CombatReactionState.AirHit, settings.maximumAirSeconds);
            return;
        }
        if (Time.time < until || interrupting && Time.time < hitUntil) return;
        if (State == CombatReactionState.Knockdown) Set(CombatReactionState.StandUp, settings.standUpSeconds);
        else
        {
            bool recovered = State == CombatReactionState.StandUp;
            Clear();
            if (recovered) GetComponent<PlayerHealth>()?.ProtectAfterRecovery(settings.recoveryProtection);
        }
    }

    private void Set(CombatReactionState state, float duration)
    {
        State = state; until = Time.time + duration;
        view?.ReactionState(state.ToString(), Mathf.Max(.01f, duration));
    }
    public void Die(float duration) { airborne = false; interrupting = true; State = CombatReactionState.Death; view?.Death(duration); }
    public void Clear() { State = CombatReactionState.None; interrupting = airborne = false; hitUntil = until = 0f; view?.ReleaseAction(); }
}
