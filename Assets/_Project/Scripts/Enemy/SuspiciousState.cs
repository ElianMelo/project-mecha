public sealed class SuspiciousState : IEnemyState
{
    private readonly EnemyStateMachine context;
    public SuspiciousState(EnemyStateMachine context) => this.context = context;
    public void Enter() { }
    public void Exit() { }
    public void Tick()
    {
        context.Actor.Face(context.LastKnownPosition.x - context.transform.position.x);
        context.Navigation.MoveTo(context.LastKnownPosition, context.LastKnownSupport,
            context.Settings.suspiciousSpeed, context.Settings.suspiciousArrival);
    }
}
