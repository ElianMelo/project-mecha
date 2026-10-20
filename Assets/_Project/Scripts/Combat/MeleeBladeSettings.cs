using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Combat/Melee Blade")]
public sealed class MeleeBladeSettings : ScriptableObject
{
    [Min(.01f)] public float contactRadius = .075f;
    [Range(4,128)] public int maximumSweepSteps = 64;
}
