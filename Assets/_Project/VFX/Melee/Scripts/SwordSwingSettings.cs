using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/VFX/Sword Swing")]
public sealed class SwordSwingSettings : ScriptableObject
{
    [Min(.01f)] public float lightWidth = .22f, heavyWidth = .4f;
    [Min(.01f)] public float lightTail = .09f, heavyTail = .15f;
    [Range(.1f,1f)] public float bodyTrailWidth = .65f;
    [Range(0f,1f)] public float lightRumble = .12f, heavyRumble = .3f;
    [Min(0f)] public float rumbleSeconds = .07f;
    [Min(0f)] public float lightImpulse = .035f, heavyImpulse = .085f;
    [Header("Blade illumination")]
    [Min(0f)] public float idleLight = 2.8f, lightStrikeLight = 6f, heavyStrikeLight = 8f;
    [Min(0f)] public float lightDecay = .18f;
    public UiFeelSettings optionalMotion;
}
