public sealed class AlertState : IEnemyState
{
    private readonly EnemyStateMachine context;
    public AlertState(EnemyStateMachine context) => this.context = context;
    public void Enter() { }
    public void Exit() { }
    public void Tick()
    {
        var target = context.Target;
        if (target == null || !target.IsAlive) { context.Motor.Move(0f); return; }
        context.Actor.Face(target.transform.position.x - context.transform.position.x);
        if (context.Actor.TryStartAttack(target.Health)) return;
        if (context.GetComponent<EnemyTactics>()?.HoldPosition(target) == true) return;
        if (context.TryBeginPrediction()) return;
        context.Navigation.Pursue(target.Movement, context.Actor.ChaseSpeed, context.Actor.StopDistance);
    }
}
