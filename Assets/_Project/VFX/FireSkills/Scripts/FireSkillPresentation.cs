using System.Collections.Generic;
using UnityEngine;

public sealed class FireSkillPresentation : MonoBehaviour
{
    [SerializeField] private FireSkillSettings[] skills;
    private CombatEmissionOrigin origin;
    private readonly List<FireSkillBurst> feeding = new List<FireSkillBurst>();
    private void Awake() => origin = GetComponent<CombatEmissionOrigin>();
    private FireSkillSettings Find(PlayerAttack.ActionKind kind)
    {
        if (skills != null) foreach (var skill in skills) if (skill != null && skill.kind == kind) return skill;
        return null;
    }
    public bool Handles(PlayerAttack.ActionKind kind) => Find(kind) != null;
    public bool Strike(PlayerCombatSettings.ActionData action, int facing, int pulse)
    {
        var settings = Find(action.kind);
        if (settings == null || settings.burst == null || origin == null) return false;
        feeding.RemoveAll(effect => effect == null);
        var effect = Instantiate(settings.burst);
        effect.Play(settings, origin.Resolve(action, facing, pulse), CombatEmissionOrigin.Direction(action, facing, pulse),
            pulse == action.pulses - 1, action.hitFromHand ? origin.SkillHand : null);
        feeding.Add(effect);
        return true;
    }
    public void Contact(PlayerAttack.ActionKind kind, Vector3 point, Vector3 incoming, Vector3 normal, bool final)
    {
        var settings = Find(kind);
        if (settings == null || settings.hit == null) return;
        Instantiate(settings.hit).Play(point, incoming, normal, final, settings.visualDepth);
    }
    public void StopFeeding()
    {
        foreach (var effect in feeding) if (effect != null) effect.StopEmission();
        feeding.Clear();
    }
    private void OnDisable() => StopFeeding();
}
