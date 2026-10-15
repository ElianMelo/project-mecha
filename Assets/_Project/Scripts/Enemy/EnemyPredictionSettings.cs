using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Enemy/Prediction")]
public class EnemyPredictionSettings : ScriptableObject
{
    [Range(0f, 1f)] public float chance = 0.45f;
    public Vector2 decisionInterval = new Vector2(1.5f, 3.5f), waitDuration = new Vector2(1.8f, 3.2f);
    public float routeTimeout = 7f, leadTime = 0.6f, maximumLead = 3f;
    public float searchRadius = 8f, verticalRange = 4f, minimumSeparation = 0.7f;
    public float arrival = 0.45f, closeCombatDistance = 1.4f, closeCombatHeight = 0.8f;
    public float heightCost = 0.65f, travelCost = 0.2f, minimumDecisionDistance = 1.8f;
}
