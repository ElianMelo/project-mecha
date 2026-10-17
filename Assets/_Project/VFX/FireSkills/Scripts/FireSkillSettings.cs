using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/VFX/Fire Skill")]
public sealed class FireSkillSettings : ScriptableObject
{
    public PlayerAttack.ActionKind kind;
    public AttackPiece attack;
    public FireSkillBurst burst;
    public FireSkillHitVfx hit;
    [Min(0.01f)] public float emissionTime = 0.12f;
    [Min(0.1f)] public float cleanupTime = 1.35f;
    public float visualDepth = -0.24f;
    [Range(1f, 1.5f)] public float finalScale = 1.15f;
}
