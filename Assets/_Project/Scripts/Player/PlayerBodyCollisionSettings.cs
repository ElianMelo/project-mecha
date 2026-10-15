using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Player/Body Collision")]
public sealed class PlayerBodyCollisionSettings : ScriptableObject
{
    [Min(0f), Tooltip("Maximum extra phase time if a dash finishes inside an enemy. Clear pairs restore immediately.")]
    public float reentryGrace = 0.15f;
    [Min(0.1f), Tooltip("Limits the native solver's separation speed if a dash ends inside a crowd.")]
    public float maximumSeparationSpeed = 3f;
}
