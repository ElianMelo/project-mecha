using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Enemy/States and Patrol")]
public class EnemyStateSettings : ScriptableObject
{
    public Vector2 wanderHold = new Vector2(2f, 3.5f), suspiciousHold = new Vector2(1.5f, 2.8f), alertHold = new Vector2(3f, 4.5f);
    public Vector2 transitionDelay = new Vector2(0.35f, 0.8f);
    public float rangeHysteresis = 0.45f;
    public float wanderSpeed = 1.25f, suspiciousSpeed = 2.1f;
    public float wanderRadius = 7f, wanderVerticalRange = 4f, patrolArrival = 0.35f;
    public Vector2 patrolPause = new Vector2(0.8f, 1.8f), patrolTravelTime = new Vector2(3f, 5f);
    [Range(0f, 1f)] public float changePlatformChance = 0.5f;
    public float suspiciousArrival = 0.9f;
    public Vector2 HoldFor(EnemyStateId state) => state == EnemyStateId.Alert || state == EnemyStateId.Prediction ? alertHold : state == EnemyStateId.Suspicious ? suspiciousHold : wanderHold;
}
