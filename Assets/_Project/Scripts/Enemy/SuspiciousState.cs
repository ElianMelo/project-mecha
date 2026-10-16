public sealed class SuspiciousState : IEnemyState
{
    private readonly EnemyStateMachine context;
    public SuspiciousState(EnemyStateMachine context) => this.context = context;
    public void Enter() { }
    public void Exit() { }
    public void Tick()
    {
        if (context.Actor.Role == EnemyRole.Guard && context.GetComponent<EnemyTactics>()?.HoldPosition(context.Target) == true) return;
        context.Actor.Face(context.LastKnownPosition.x - context.transform.position.x);
        context.Navigation.MoveTo(context.LastKnownPosition, context.LastKnownSupport,
            context.Settings.suspiciousSpeed, context.Settings.suspiciousArrival);
    }
}
