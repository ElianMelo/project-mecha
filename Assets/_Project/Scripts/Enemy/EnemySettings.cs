using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Enemy/Behavior")]
public class EnemySettings : ScriptableObject
{
    public float maxHealth = 90f, moveSpeed = 3.6f, attackDamage = 12f;
    public float windup = 0.24f, attackCooldown = 0.65f, deathDuration = 0.65f;
    public float attackRange = 1.05f, impactRange = 1.35f, heightTolerance = 0.9f, stopDistance = 0.65f;
    public float fallLimit = -12f, abandonWindupDistance = 1.8f;
    public Vector3 attackKnockback = new Vector3(5f, 4f, 0f);
}
