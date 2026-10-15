using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Player/Input")]
public class PlayerInputSettings : ScriptableObject
{
    public float doubleTapWindow = 0.23f;
    [Range(0f, 1f)] public float movementDeadzone = 0.25f;
}
