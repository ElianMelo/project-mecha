using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Player/Health")]
public class PlayerHealthSettings : ScriptableObject
{
    public float maxHealth = 100f, invincibility = 0.9f, hitStun = 0.3f, respawnInvincibility = 2f, fallLimit = -12f;
    public float defeatDuration = 1.1f;
    [Min(0f)] public float enemyHitProtection = .35f;
    [Header("Stagger escape (Shift / RB)")]
    [Min(1)] public int escapeCapacity = 3;
    [Min(.1f)] public float escapeRefillSeconds = 5f;
    [Min(0f)] public float escapeInvincibility = .25f;
    [Min(0f)] public float staggerDrag = 24f;
    [Min(0f)] public float airStaggerDrag = 3f;
}
