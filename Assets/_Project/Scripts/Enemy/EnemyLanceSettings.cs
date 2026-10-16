using FishNet.Object;
using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Enemy/Lance Thrower")]
public sealed class EnemyLanceSettings : ScriptableObject
{
    public NetworkObject projectile;
    public float preferredRange = 7f, retreatRange = 4f, detectionRange = 15f;
    public float windup = 0.65f, cooldown = 2.4f, launchHeight = 1.25f;
    public float gravity = 17f, arcHeight = 2.8f, leadTime = 0.25f, maximumLead = 1.8f;
    public float damage = 16f, lifetime = 4f, hitRadius = 0.12f;
    public Vector2 knockback = new Vector2(4f, 3f);
    public LayerMask collisionLayers = (1 << 8) | (1 << 6) | (1 << 7) | 1;
}
