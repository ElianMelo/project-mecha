using UnityEngine;

public sealed class WanderState : IEnemyState
{
    private readonly EnemyStateMachine context;
    private readonly Collider[] candidates = new Collider[32];
    private Vector3 destination;
    private Collider surface;
    private bool travelling;
    private float nextDecision, travelUntil;
    public WanderState(EnemyStateMachine context) => this.context = context;
    public void Enter() { travelling = false; Pause(); }
    public void Exit() => travelling = false;
    public void Tick()
    {
        if (!context.Motor.IsGrounded && !travelling) { context.Motor.Move(0f); return; }
        if (!travelling)
        {
            context.Motor.Move(0f);
            if (Time.time >= nextDecision && context.Motor.IsGrounded) PickDestination();
            return;
        }
        if (context.Motor.IsGrounded && ((surface == context.Motor.Support && Mathf.Abs(context.transform.position.x - destination.x) <= context.Settings.patrolArrival) || Time.time >= travelUntil))
        { travelling = false; Pause(); context.Motor.Move(0f); return; }
        context.Actor.Face(destination.x - context.transform.position.x);
        context.Navigation.MoveTo(destination, surface, context.Settings.wanderSpeed, context.Settings.patrolArrival);
    }
    private void Pause() => nextDecision = Time.time + EnemyStateMachine.RandomRange(context.Settings.patrolPause);
    private void PickDestination()
    {
        var settings = context.Settings;
        surface = context.Motor.Support;
        bool guard = context.Actor.Role == EnemyRole.Guard;
        if (!guard && Random.value < settings.changePlatformChance)
        {
            int count = Physics.OverlapBoxNonAlloc(context.Home,
                new Vector3(settings.wanderRadius, settings.wanderVerticalRange, 1f), candidates,
                Quaternion.identity, context.Motor.Settings.groundLayers, QueryTriggerInteraction.Ignore);
            int accepted = 0;
            for (int i = 0; i < count; i++)
            {
                var option = candidates[i];
                if (option == context.Motor.Support || !context.Navigation.CanNavigateTo(option, settings.wanderSpeed)) continue;
                if (Random.Range(0, ++accepted) == 0) surface = option;
            }
        }
        if (surface == null) { Pause(); return; }
        var bounds = surface.bounds; float inset = context.Motor.Settings.landingInset;
        float radius = guard ? context.GetComponent<EnemyTactics>().GuardPatrolRadius : settings.wanderRadius;
        float minimum = Mathf.Max(context.Home.x - radius, bounds.min.x + inset);
        float maximum = Mathf.Min(context.Home.x + radius, bounds.max.x - inset);
        if (minimum > maximum) { Pause(); return; }
        destination = new Vector3(Random.Range(minimum, maximum), bounds.max.y, context.Home.z);
        travelUntil = Time.time + EnemyStateMachine.RandomRange(settings.patrolTravelTime);
        context.Navigation.ResetRoute(); travelling = true;
    }
}
