using UnityEngine;

[RequireComponent(typeof(CharacterRuntime), typeof(CharacterModifiers))]
public sealed class CharacterSkills : FishNet.Object.NetworkBehaviour
{
    public struct ModifierState { public double ends; public float move, attack; }
    private readonly FishNet.Object.Synchronizing.SyncVar<ModifierState> modifier = new FishNet.Object.Synchronizing.SyncVar<ModifierState>();
    private CharacterRuntime character;
    private CharacterSkillDefinition presentedSkill;
    public CharacterSkillPresentationInstance ActivePresentation { get; private set; }
    public CharacterModifiers Modifiers { get; private set; }
    public CharacterSkillDefinition Find(PlayerAttack.ActionKind kind) => PlayerAttack.IsSkill(kind) ? character.Skill((int)kind - 5) : null;
    private void Awake() { character = GetComponent<CharacterRuntime>(); Modifiers = GetComponent<CharacterModifiers>(); modifier.OnChange += ModifierChanged; }
    public override void OnStartClient() { base.OnStartClient(); if (!IsServerInitialized) ApplyModifier(modifier.Value); }
    private void ModifierChanged(ModifierState previous, ModifierState next, bool asServer)
    { if (asServer || !IsServerInitialized) ApplyModifier(next); }
    private void ApplyModifier(ModifierState state)
    {
        float remaining = (float)(state.ends - TimeManager.TicksToTime(FishNet.Managing.Timing.TickType.Tick));
        if (remaining <= 0f) { Modifiers.Clear(); return; }
        Modifiers.Activate(remaining, state.move, state.attack);
        character.Skill(2)?.PresentModifier(this, remaining);
    }
    public bool Begin(PlayerCombatSettings.ActionData action, int facing)
    {
        var skill = Find(action.kind);
        if (skill == null) return false;
        EndPresentation();
        presentedSkill = skill;
        if (skill.presentationOverride != null)
            ActivePresentation = skill.presentationOverride.Begin(this, action, facing);
        else skill.Begin(this, facing);
        return true;
    }
    public bool PresentPulse(PlayerCombatSettings.ActionData action, int facing, int pulse)
    {
        var skill = Find(action.kind);
        if (skill == null) return false;
        if (skill.presentationOverride != null)
        {
            if (presentedSkill == skill && ActivePresentation != null) ActivePresentation.Pulse(pulse);
        }
        else skill.PresentPulse(this, facing, pulse);
        return true;
    }
    public void EndPresentation()
    {
        if (ActivePresentation != null) ActivePresentation.End();
        ActivePresentation = null;
        presentedSkill = null;
    }
    public bool ApplyAuthority(PlayerAttack.ActionKind kind, int facing, int pulse)
        => Find(kind)?.ApplyAuthority(this, facing, pulse) ?? false;
    public void ActivateModifier(float duration, float move, float attack)
    {
        if (!IsSpawned) { Modifiers.Activate(duration, move, attack); character.Skill(2)?.PresentModifier(this, duration); }
        else if (IsServerInitialized) modifier.Value = new ModifierState { ends = TimeManager.TicksToTime(FishNet.Managing.Timing.TickType.Tick) + duration, move = move, attack = attack };
    }
    public void ClearModifier() { Modifiers.Clear(); if (IsServerInitialized) modifier.Value = default; }
    private void OnDisable() => EndPresentation();
    private void OnDestroy() { EndPresentation(); modifier.OnChange -= ModifierChanged; }
}
