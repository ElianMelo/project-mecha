using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Combat/Reaction Timing")]
public sealed class CombatReactionSettings : ScriptableObject
{
    [Min(.01f)] public float flinchSeconds = .22f;
    [Min(.01f)] public float heavyReactionThreshold = .4f;
    [Min(0f)] public float launchThreshold = 3.5f;
    [Min(.01f)] public float launchSeconds = .28f;
    [Min(.01f)] public float minimumAirSeconds = .12f;
    [Min(.1f)] public float maximumAirSeconds = 3f;
    [Min(0f)] public float knockdownSeconds = .18f;
    [Min(.01f)] public float standUpSeconds = .48f;
    [Min(0f)] public float recoveryProtection = .25f;
}
