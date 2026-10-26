using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/VFX/Rising water variant")]
public sealed class RisingWaterVariantSettings : CharacterSkillPresentation
{
    public RisingWaterVariantEffect prefab;
    public AttackPiece previewAction;
    [Min(0f)] public float tailSeconds = 0.35f;
    [Min(0f)] public float interruptedTail = 0.22f;
    [Min(0)] public int dropletsPerPulse = 6;

    public override CharacterSkillPresentationInstance Begin(CharacterSkills actor,
        PlayerCombatSettings.ActionData action, int facing)
    {
        var feet = actor.transform.position;
        var collider = actor.GetComponent<CapsuleCollider>();
        if (collider != null) feet.y = collider.bounds.min.y;
        var away = Camera.main != null ? feet - Camera.main.transform.position : Vector3.forward;
        away.y = 0f;
        return SpawnAt(feet, away.sqrMagnitude > 0.001f ? Quaternion.LookRotation(away) : Quaternion.identity, action.duration);
    }

    public RisingWaterVariantEffect SpawnAt(Vector3 feet, Quaternion rotation, float duration)
    {
        if (prefab == null) return null;
        var effect = Instantiate(prefab, feet, rotation);
        effect.Initialize(this, duration);
        return effect;
    }
}
