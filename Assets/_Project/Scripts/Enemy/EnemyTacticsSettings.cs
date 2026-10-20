using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Enemy/Encounter Tactics")]
public sealed class EnemyTacticsSettings : ScriptableObject
{
    public float guardEngageRadius = 3.8f, guardLeash = 4.5f, homeArrival = 0.45f;
    public float guardPatrolRadius = 1.3f;
}
