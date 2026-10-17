using FishNet.Object;
using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Enemy/Lance Thrower")]
public sealed class EnemyLanceSettings : ScriptableObject
{
    public NetworkObject projectile;
    public ParticleEffectSettings impactParticles;
    public float preferredRange = 7f, retreatRange = 4f, detectionRange = 15f;
    public float windup = 0.65f, cooldown = 2.4f, launchHeight = 1.25f;
    public float gravity = 17f, arcHeight = 0.35f, leadTime = 0.25f, maximumLead = 1.8f;
    [Range(1f, 2f)] public float embeddedLifetime = 1.5f;
    [Min(0f)] public float tipOffset = 1f, embedDepth = 0.18f;
    public float damage = 16f, lifetime = 4f, hitRadius = 0.12f;
    [Header("Planned throw and landing")]
    [Min(1f)] public float throwSpeed = 14f;
    [Min(0.1f)] public float minimumFlightTime = 0.25f;
    [Min(0f)] public float landingFollowThrough = 1.25f, landingEdgeInset = 0.2f;
    [Min(1f)] public float landingSearchDepth = 35f;
    public LayerMask landingLayers = (1 << 6) | (1 << 7);
    [Header("Readability")]
    public Vector3 projectileModelEuler = new Vector3(0f, 0f, -14.9061f);
    public float projectileVisualDepth = -1.2f;
    public Vector2 knockback = new Vector2(4f, 3f);
    public LayerMask collisionLayers = (1 << 8) | (1 << 6) | (1 << 7) | 1;
}
