using UnityEngine;

// Retains the existing hit API for combat callers; new systems use IDamageable.
public interface ICombatTarget : IDamageable
{
    void TakeHit(float damage, Vector3 knockback, float stun);
}
