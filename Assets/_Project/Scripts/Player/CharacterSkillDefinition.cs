using UnityEngine;
using UnityEngine.Localization;

// Slot behavior is extensible independently of combo sequencing and resource validation.
public abstract class CharacterSkillDefinition : ScriptableObject
{
    public LocalizedString displayName;
    public AttackPiece action;
    public CharacterSkillPresentation presentationOverride;
    public virtual void Begin(CharacterSkills actor, int facing) { }
    public virtual void PresentPulse(CharacterSkills actor, int facing, int pulse) { }
    public virtual void PresentModifier(CharacterSkills actor, float remaining) { }
    public virtual float StatusRemaining(CharacterSkills actor) => 0f;
    // Return true when this skill replaces the shared native damage envelope.
    public virtual bool ApplyAuthority(CharacterSkills actor, int facing, int pulse) => false;
}
