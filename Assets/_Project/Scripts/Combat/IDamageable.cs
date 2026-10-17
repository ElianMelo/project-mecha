using UnityEngine;

// Damage reception is independent from whether an object can be acquired by aim.
public interface IDamageable
{
    bool IsDead { get; }
    float Health { get; }
    void TakeDamage(float damage, Vector3 knockback, float stun, PlayerHealth attacker);
}
