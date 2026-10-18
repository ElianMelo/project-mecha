using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Characters/Skills/Water Fountain")]
public sealed class WaterFountainSkill : CharacterSkillDefinition
{
    public WaterEffect preparation, fountain;
    public float visualLifetime = 1.6f;
    public override void Begin(CharacterSkills actor, int facing)
    { if (preparation != null) Instantiate(preparation, actor.transform.position, Quaternion.identity).Play(action.action.impact); }
    public override void PresentPulse(CharacterSkills actor, int facing, int pulse)
    { if (pulse == 0 && fountain != null) Instantiate(fountain, actor.transform.position, Quaternion.identity).Play(visualLifetime); }
}
