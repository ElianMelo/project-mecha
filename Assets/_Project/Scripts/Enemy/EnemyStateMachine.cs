using UnityEngine;

[RequireComponent(typeof(EnemyMotor), typeof(EnemyPlatformNavigator))]
public class EnemyStateMachine : MonoBehaviour
{
    [SerializeField] private EnemyStateSettings settings;
    private IEnemyState[] states;
    private EnemyStateId currentState, pendingState;
    private float holdUntil, pendingUntil, holdDuration;
    private bool hasPending;
    public EnemyStateId CurrentState => currentState;
    public float HoldRemaining => Mathf.Max(0f, holdUntil - Time.time);
    public EnemyStateSettings Settings => settings;
    public EnemyController Actor { get; private set; }
    public EnemyMotor Motor { get; private set; }
    public EnemyPlatformNavigator Navigation { get; private set; }
    public EnemyPredictionPlanner Prediction { get; private set; }
    public PlayerAwareness Target { get; private set; }
    public Vector3 Home { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }
    public Collider LastKnownSupport { get; private set; }
    private void Awake()
    {
        Actor = GetComponent<EnemyController>(); Motor = GetComponent<EnemyMotor>(); Navigation = GetComponent<EnemyPlatformNavigator>();
        Prediction = GetComponent<EnemyPredictionPlanner>();
        Home = LastKnownPosition = transform.position;
        states = new IEnemyState[] {new WanderState(this), new SuspiciousState(this), new AlertState(this), new PredictionState(this)};
        ResetBehavior();
    }
    public void ResetBehavior()
    {
        Home = LastKnownPosition = transform.position; Target = null; LastKnownSupport = null;
        Actor.CancelAttack();
        Prediction?.ResetTiming();
        hasPending = false;
        currentState = EnemyStateId.Wander;
        holdDuration = RandomRange(settings.wanderHold); holdUntil = Time.time + holdDuration;
        states[(int)currentState].Enter();
    }
    public void UpdateAwareness()
    {
        var candidate = NearestRelevantPlayer(out var desired);
        if (candidate != null)
        {
            Target = candidate;
            LastKnownPosition = candidate.transform.position;
            if (candidate.Movement.Support != null) LastKnownSupport = candidate.Movement.Support;
        }
        else if (Target != null && !Target.IsAlive) Target = null;
        // An interceptor may retain a different elevation; immediate melee danger still wins.
        if (currentState == EnemyStateId.Prediction && candidate != null && desired != EnemyStateId.Wander && !Prediction.NeedsDirectCombat(candidate))
        { hasPending = false; return; }
        // Entering the hard range otherwise wins over dwell and boundary delays.
        if (candidate != null && candidate.DistanceFrom(transform.position) <= candidate.Settings.hardRadius)
        { ChangeState(EnemyStateId.Alert); holdUntil = Time.time + holdDuration; return; }
        if (desired == currentState)
        {
            hasPending = false;
            if (currentState != EnemyStateId.Wander) holdUntil = Time.time + holdDuration;
            return;
        }
        if (!hasPending || pendingState != desired)
        {
            pendingState = desired; pendingUntil = Time.time + RandomRange(settings.transitionDelay); hasPending = true;
        }
        if (Time.time >= holdUntil && Time.time >= pendingUntil) ChangeState(desired);
    }
    private PlayerAwareness NearestRelevantPlayer(out EnemyStateId desired)
    {
        PlayerAwareness best = null;
        desired = EnemyStateId.Wander; float nearest = float.MaxValue;
        foreach (var player in PlayerAwareness.ActivePlayers)
        {
            if (player == null || !player.IsAlive || player.Settings == null) continue;
            float distance = player.DistanceFrom(transform.position);
            if (distance > player.Settings.largeRadius) continue;
            var state = player.StateAtDistance(distance, currentState, settings.rangeHysteresis);
            if (best != null && (state < desired || state == desired && distance >= nearest)) continue;
            best = player; desired = state; nearest = distance;
        }
        return best;
    }
    private void ChangeState(EnemyStateId state)
    {
        hasPending = false;
        if (state == currentState) return;
        states[(int)currentState].Exit();
        Actor.CancelAttack(); Navigation.ResetRoute();
        currentState = state;
        holdDuration = RandomRange(settings.HoldFor(state)); holdUntil = Time.time + holdDuration;
        states[(int)currentState].Enter();
        if (state == EnemyStateId.Wander) Target = null;
    }
    public void TickMovement() => states[(int)currentState].Tick();
    public bool TryBeginPrediction()
    {
        if (Prediction == null || Target == null || !Prediction.TryPlan(Target.Movement, LastKnownSupport, Actor.ChaseSpeed)) return false;
        ChangeState(EnemyStateId.Prediction); return true;
    }
    public void EndPrediction() => ChangeState(EnemyStateId.Alert);
    public void AlertFromDamage()
    {
        if (Target == null || !Target.IsAlive)
        {
            float nearest = float.MaxValue;
            foreach (var player in PlayerAwareness.ActivePlayers)
            {
                if (player == null || !player.IsAlive) continue;
                float distance = player.DistanceFrom(transform.position);
                if (distance < nearest) { Target = player; nearest = distance; }
            }
        }
        if (Target != null)
        {
            LastKnownPosition = Target.transform.position; LastKnownSupport = Target.Movement.Support;
            ChangeState(EnemyStateId.Alert);
            holdDuration = RandomRange(settings.alertHold); holdUntil = Time.time + holdDuration;
        }
    }
    public static float RandomRange(Vector2 range) => Random.Range(range.x, Mathf.Max(range.x, range.y));
}
