using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Characters/Skills/Water Bubble")]
public sealed class WaterBubbleSkill : CharacterSkillDefinition
{
    public WaterEffect bubble, explosion;
    public float visualLifetime = 1.5f;
    private Vector3 Origin(CharacterSkills actor, int facing) => actor.transform.position + new Vector3(facing * action.action.reach, action.action.height, 0f);
    public override void Begin(CharacterSkills actor, int facing)
    {
        if (bubble != null) Instantiate(bubble, Origin(actor, facing), Quaternion.identity).Play(action.action.impact);
    }
    public override void PresentPulse(CharacterSkills actor, int facing, int pulse)
    {
        if (explosion != null) Instantiate(explosion, Origin(actor, facing), Quaternion.identity).Play(visualLifetime);
    }
}
