using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Interface/Squad Presentation")]
public sealed class SquadPresentationSettings : ScriptableObject
{
    public Color[] pilotColors = { new Color(0.25f, 0.9f, 1f), new Color(1f, 0.65f, 0.2f), new Color(0.6f, 1f, 0.35f), new Color(0.95f, 0.4f, 0.85f) };
    public Color enemyHealth = new Color(1f, 0.28f, 0.22f);
    public Vector2 healthBarSize = new Vector2(0.9f, 0.09f);
    public float barBelowFeet = 0.16f, nameAboveHead = 0.25f;
    [Min(0.01f)] public float ringWidth = 1.1f;
    [Tooltip("Projected vertical diameter in the side-view camera; determines the world Canvas tilt.")]
    [Min(0.01f)] public float ringHeight = 0.28f;
    public float ringAboveFeet = 0.14f;
    public Sprite ringSprite;
    public float RingTiltDegrees => Mathf.Acos(Mathf.Clamp(ringHeight / Mathf.Max(0.01f, ringWidth), 0.05f, 1f)) * Mathf.Rad2Deg;
    public Color PilotColor(int slot) => pilotColors == null || pilotColors.Length == 0 ? Color.cyan : pilotColors[Mathf.Abs(slot) % pilotColors.Length];
}
