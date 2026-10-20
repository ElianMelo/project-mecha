using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Enemy/Engagement Budget")]
public sealed class EnemyEngagementSettings : ScriptableObject
{
    [Min(1)] public int attackTokens = 2;
    [Min(.1f)] public float approachLease = 3f;
    [Min(0f)] public float handoffDelay = .22f;
    [Min(0f)] public float attackSpacing = .3f;
    [Min(0f)] public float observeDistance = 3.5f;
    [Min(0f)] public float patrolRadius = 1.2f;
    [Min(.1f)] public float patrolPeriod = 2.5f;
}
