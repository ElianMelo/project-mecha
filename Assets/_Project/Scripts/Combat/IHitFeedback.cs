using UnityEngine;

public interface IHitFeedback
{
    void Hit(float damage, Vector3 incomingDirection, DamageContact contact = default);
}
