using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/VFX/Fire Skill")]
public sealed class FireSkillSettings : ScriptableObject
{
    public PlayerAttack.ActionKind kind;
    public AttackPiece attack;
    public FireSkillBurst burst;
    public FireSkillHitVfx hit;
    public FireSourceWarmup warmup;
    [Min(0.1f)] public float warmupScale = 0.8f;
    public Vector2 warmupOffset;
    [Min(0.01f)] public float emissionTime = 0.12f;
    [Min(0.1f)] public float cleanupTime = 1.35f;
    public float visualDepth = -0.24f;
    public bool firstPulseOnly;
    public bool alignToFacing;
    public float facingAngle;
    [Range(0f, 0.35f)] public float motionVariation = 0.18f;
    public Vector2 burstOffset;
    [Range(1f, 1.5f)] public float finalScale = 1.15f;
}
