using System.Collections.Generic;
using UnityEngine;

public sealed class FireSkillPresentation : MonoBehaviour
{
    [SerializeField] private FireSkillSettings[] skills;
    private CombatEmissionOrigin origin;
    private FireSourceWarmup preparing;
    private readonly List<FireSkillBurst> feeding = new List<FireSkillBurst>();
    private void Awake() => origin = GetComponent<CombatEmissionOrigin>();
    private FireSkillSettings Find(PlayerAttack.ActionKind kind)
    {
        if (skills != null) foreach (var skill in skills) if (skill != null && skill.kind == kind) return skill;
        return null;
    }
    public bool Handles(PlayerAttack.ActionKind kind) => Find(kind) != null;
    public void Begin(PlayerCombatSettings.ActionData action, int facing)
    {
        var settings = Find(action.kind);
        if (settings == null || settings.warmup == null) return;
        if (preparing != null) Destroy(preparing.gameObject);
        preparing = Instantiate(settings.warmup);
        preparing.Play(settings, origin, action, facing);
    }
    public bool Strike(PlayerCombatSettings.ActionData action, int facing, int pulse)
    {
        var settings = Find(action.kind);
        if (settings == null || settings.burst == null || origin == null) return false;
        if (settings.firstPulseOnly && pulse > 0) return true;
        if (pulse == 0 && preparing != null) Destroy(preparing.gameObject);
        feeding.RemoveAll(effect => effect == null);
        var effect = Instantiate(settings.burst);
        var position = origin.Resolve(action, facing, pulse) + new Vector3(facing * settings.burstOffset.x, settings.burstOffset.y, 0f);
        float angle = settings.facingAngle * Mathf.Deg2Rad;
        var direction = settings.alignToFacing ? new Vector3(facing * Mathf.Cos(angle), Mathf.Sin(angle), 0f) : CombatEmissionOrigin.Direction(action, facing, pulse);
        effect.Play(settings, position, direction,
            settings.firstPulseOnly || pulse == action.pulses - 1, action.hitFromHand ? origin.SkillHand : null);
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
        if (preparing != null) Destroy(preparing.gameObject);
        foreach (var effect in feeding) if (effect != null) effect.StopEmission();
        feeding.Clear();
    }
    private void OnDisable() => StopFeeding();
}
