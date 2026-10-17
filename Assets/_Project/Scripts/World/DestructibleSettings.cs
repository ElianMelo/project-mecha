using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/World/Destructible")]
public class DestructibleSettings : ScriptableObject
{
    public float health = 28f;
    [Min(0f)] public float healOnBreak = 25f;
    public CombatVfxSettings hitFeedback;
    public ParticleEffectSettings debris, smoke;
}
