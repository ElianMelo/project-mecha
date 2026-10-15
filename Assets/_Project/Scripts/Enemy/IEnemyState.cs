public enum EnemyStateId { Wander, Suspicious, Alert, Prediction }

public interface IEnemyState
{
    void Enter();
    void Tick();
    void Exit();
}
