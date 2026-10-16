using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Presentation/Sword Fire VFX")]
public sealed class SwordFireVfxSettings : ScriptableObject
{
    public GameObject prefab;
    [Range(1, 128)] public int flameCount = 48, emberCount = 20;
    public Gradient flameColors, emberColors;
    public Vector3 scale = Vector3.one;
    public float firstPulseAngle, pulseAngleStep;
    public float lifetime = 1.2f, maximumViewDistance = 36f;
}
