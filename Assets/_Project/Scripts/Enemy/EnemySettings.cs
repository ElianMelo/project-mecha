using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Enemy/Behavior")]
public class EnemySettings : ScriptableObject
{
    public EnemyEngagementSettings engagement;
    public float maxHealth = 90f, moveSpeed = 3.6f, attackDamage = 12f;
    public float windup = 0.24f, attackCooldown = 0.65f, deathDuration = 0.65f;
    public float attackRange = 1.05f, impactRange = 1.35f, heightTolerance = 0.9f, stopDistance = 0.65f;
    public float fallLimit = -12f, abandonWindupDistance = 1.8f;
    public float hitDrag = 6f;
    [Min(0.01f)] public float facingSpeedThreshold = 0.15f;
    [Range(0.1f, 1f)] public float juggleGravityScale = 0.55f;
    public Vector3 attackKnockback = new Vector3(5f, 4f, 0f);
    [Header("Telegraphed launcher (ordinary hits never interrupt pilots)")]
    [Min(1)] public int specialEveryAttacks = 3;
    [Min(.1f)] public float specialWindup = .8f;
    [Min(0f)] public float specialCooldown = 5f, specialStun = .55f;
    [Min(0f)] public float attackRecovery = .42f, specialRecovery = .8f;
    [Min(1f)] public float specialDamageMultiplier = 1.5f;
    public Vector2 specialKnockback = new Vector2(7f, 8f);
}
