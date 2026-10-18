using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Characters/Skills/Water Mode")]
public sealed class WaterModeSkill : CharacterSkillDefinition
{
    public WaterEffect aura;
    public float duration = 8f, moveMultiplier = 1.3f, attackMultiplier = 1.4f;
    public override float StatusRemaining(CharacterSkills actor) => actor.Modifiers.Remaining;
    public override void PresentModifier(CharacterSkills actor, float remaining)
    { if (aura != null) Instantiate(aura, actor.transform.position, Quaternion.identity).Play(remaining, actor.transform, actor.Modifiers); }
    public override bool ApplyAuthority(CharacterSkills actor, int facing, int pulse)
    { actor.ActivateModifier(duration, moveMultiplier, attackMultiplier); return true; }
}
