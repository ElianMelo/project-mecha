using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Player/Awareness Ranges")]
public class PlayerAwarenessSettings : ScriptableObject
{
    [Min(0.1f)] public float hardRadius = 3.5f;
    [Min(0.1f)] public float softRadius = 8f;
    [Min(0.1f)] public float largeRadius = 16f;
    public Color hardColor = new Color(1f, 0.2f, 0.15f, 0.7f);
    public Color softColor = new Color(1f, 0.75f, 0.15f, 0.6f);
    public Color largeColor = new Color(0.25f, 0.65f, 1f, 0.4f);
    private void OnValidate()
    {
        hardRadius = Mathf.Max(0.1f, hardRadius);
        softRadius = Mathf.Max(hardRadius, softRadius);
        largeRadius = Mathf.Max(softRadius, largeRadius);
    }
}
