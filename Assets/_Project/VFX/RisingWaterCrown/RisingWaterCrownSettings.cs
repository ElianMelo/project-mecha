using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/VFX/Rising water crown")]
public sealed class RisingWaterCrownSettings : CharacterSkillPresentation
{
    public RisingWaterCrownEffect prefab;
    [Header("Authored crown geometry")]
    public Vector2 radius = new Vector2(1.65f, .64f);
    public float crestHeight = 2.9f;
    [ColorUsage(false, true)] public Color bodyColor = new Color(.006f, .07f, .24f);
    [ColorUsage(false, true)] public Color flowColor = new Color(.025f, 1.05f, 1.65f);
    [ColorUsage(false, true)] public Color foamColor = new Color(1.35f, 2.25f, 2.6f);
    public Color lightColor = new Color(.08f, .65f, 1f);
    public float lightIntensity = 5f, lightRange = 4.5f;
    [Header("Lifetime and cosmetic release")]
    public float lifetime = 1.6f, interruptedTail = .22f;
    public int dropletsPerPulse = 5;
    public float surfaceProbeDistance = .2f;
    public LayerMask surfaceLayers = (1 << 6) | (1 << 7);

    public override CharacterSkillPresentationInstance Begin(CharacterSkills actor,
        PlayerCombatSettings.ActionData action, int facing)
    {
        if (prefab == null) return null;
        var feet = actor.transform.position;
        var capsule = actor.GetComponent<CapsuleCollider>();
        if (capsule != null) feet.y = capsule.bounds.min.y;
        var away = Camera.main != null ? feet - Camera.main.transform.position : Vector3.forward;
        away.y = 0f;
        var rotation = away.sqrMagnitude > .001f ? Quaternion.LookRotation(away) : Quaternion.identity;
        var effect = Instantiate(prefab, feet, rotation);
        effect.Initialize(this, action.duration);
        return effect;
    }
}
