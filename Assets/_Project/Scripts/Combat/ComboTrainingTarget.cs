using UnityEngine;

public sealed class ComboTrainingTarget : MonoBehaviour, ICombatTarget
{
    public bool IsDead => false;
    public float Health => float.PositiveInfinity;
    public float TotalDamage { get; private set; }
    public int Hits { get; private set; }
    public void TakeHit(float damage, Vector3 knockback, float stun)
    {
        TotalDamage += damage; Hits++;
    }
    public void ResetTarget() { TotalDamage = 0f; Hits = 0; }
}
