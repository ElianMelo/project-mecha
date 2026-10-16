using UnityEngine;

public interface ICombatTarget
{
    bool IsDead { get; }
    float Health { get; }
    void TakeHit(float damage, Vector3 knockback, float stun);
}
