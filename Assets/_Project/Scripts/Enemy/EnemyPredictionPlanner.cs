using UnityEngine;

[RequireComponent(typeof(EnemyMotor), typeof(EnemyPlatformNavigator))]
public class EnemyPredictionPlanner : MonoBehaviour
{
    [SerializeField] private EnemyPredictionSettings settings;
    private EnemyMotor motor;
    private EnemyPlatformNavigator navigation;
    private float nextDecision;
    public EnemyPredictionSettings Settings => settings;
    public Collider Destination { get; private set; }
    public Vector3 Position { get; private set; }
    private void Awake() { motor = GetComponent<EnemyMotor>(); navigation = GetComponent<EnemyPlatformNavigator>(); }
    public void ResetTiming()
    {
        if (settings != null) nextDecision = Time.time + EnemyStateMachine.RandomRange(settings.decisionInterval);
    }
    public bool TryPlan(PlayerMovement target, Collider lastSupport, float speed)
    {
        if (settings == null || target == null || !motor.IsGrounded || Time.time < nextDecision) return false;
        ResetTiming();
        if (Random.value > settings.chance || Vector2.Distance(transform.position, target.transform.position) < settings.minimumDecisionDistance) return false;
        var playerSupport = target.Support != null ? target.Support : lastSupport;
        float predictedX = target.transform.position.x + Mathf.Clamp(target.Velocity.x * settings.leadTime, -settings.maximumLead, settings.maximumLead);
        float best = float.MaxValue; Destination = null;
        foreach (var platform in OneWayPlatform.ActivePlatforms)
        {
            var option = platform.Surface;
            if (option == null || !option.enabled || option == playerSupport) continue;
            var bounds = option.bounds;
            float height = bounds.max.y - target.transform.position.y;
            if (Mathf.Abs(height) < settings.minimumSeparation || Mathf.Abs(height) > settings.verticalRange) continue;
            float x = Mathf.Clamp(predictedX, bounds.min.x + motor.Settings.landingInset, bounds.max.x - motor.Settings.landingInset);
            if (Mathf.Abs(x - predictedX) > settings.searchRadius || !navigation.CanNavigateTo(option, speed)) continue;
            float cost = Mathf.Abs(x - predictedX) + Mathf.Abs(height) * settings.heightCost + Mathf.Abs(x - transform.position.x) * settings.travelCost;
            if (cost >= best) continue;
            best = cost; Destination = option; Position = new Vector3(x, bounds.max.y, transform.position.z);
        }
        return Destination != null;
    }
    public bool NeedsDirectCombat(PlayerAwareness target)
    {
        if (settings == null || target == null) return true;
        Vector3 difference = target.transform.position - transform.position;
        return Mathf.Abs(difference.x) < settings.closeCombatDistance && Mathf.Abs(difference.y) < settings.closeCombatHeight ||
            Destination != null && motor.Support == Destination && target.Movement.Support == Destination;
    }
}
