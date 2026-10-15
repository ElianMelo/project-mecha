using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/World/Destructible")]
public class DestructibleSettings : ScriptableObject
{
    public float health = 28f;
    public CombatVfxSettings hitFeedback;
    public ParticleEffectSettings debris, smoke;
}
