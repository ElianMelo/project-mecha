using UnityEngine;

public sealed class PredictionState : IEnemyState
{
    private readonly EnemyStateMachine context;
    private float expires, waitUntil;
    private bool waiting;
    public PredictionState(EnemyStateMachine context) => this.context = context;
    public void Enter() { waiting = false; expires = Time.time + context.Prediction.Settings.routeTimeout; }
    public void Exit() { waiting = false; context.Prediction.ResetTiming(); }
    public void Tick()
    {
        var target = context.Target;
        var plan = context.Prediction;
        if (target == null || !target.IsAlive || plan.Destination == null || Time.time >= expires)
        { context.EndPrediction(); return; }
        context.Actor.Face(target.transform.position.x - context.transform.position.x);
        bool arrived = context.Motor.IsGrounded && context.Motor.Support == plan.Destination &&
            Mathf.Abs(context.transform.position.x - plan.Position.x) <= plan.Settings.arrival;
        if (arrived)
        {
            if (!waiting) { waiting = true; waitUntil = Time.time + EnemyStateMachine.RandomRange(plan.Settings.waitDuration); }
            context.Motor.Move(0f);
            if (Time.time >= waitUntil) context.EndPrediction();
        }
        else context.Navigation.MoveTo(plan.Position, plan.Destination, context.Actor.ChaseSpeed, plan.Settings.arrival);
    }
}
