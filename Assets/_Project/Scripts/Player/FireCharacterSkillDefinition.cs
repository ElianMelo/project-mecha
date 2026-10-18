using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Characters/Skills/Fire")]
public sealed class FireCharacterSkillDefinition : CharacterSkillDefinition
{
    public override void Begin(CharacterSkills actor, int facing) => actor.GetComponent<FireSkillPresentation>()?.Begin(action.action, facing);
    public override void PresentPulse(CharacterSkills actor, int facing, int pulse) => actor.GetComponent<FireSkillPresentation>()?.Strike(action.action, facing, pulse);
}
