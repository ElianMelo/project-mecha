using UnityEngine;

// Optional cosmetics replace a skill's visual callbacks without replacing its authority.
public abstract class CharacterSkillPresentation : ScriptableObject
{
    public abstract CharacterSkillPresentationInstance Begin(CharacterSkills actor,
        PlayerCombatSettings.ActionData action, int facing);
}
