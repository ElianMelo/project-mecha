using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Interface/Feel")]
public sealed class UiFeelSettings : ScriptableObject
{
    public bool motionEnabled = true;
    public RuntimeAnimatorController buttonController, readoutController, comboController, panelController, keyboardController;
    public Sprite frameSprite;
    public Color focusColor = new Color(0.36f, 0.9f, 0.92f);
    public Color damageTrailColor = new Color(1f, 0.58f, 0.25f, 0.9f);
    public Color energyTrailColor = new Color(0.3f, 0.65f, 0.85f, 0.65f);
    public Color criticalColor = new Color(1f, 0.48f, 0.28f);
    [Min(0.01f)] public float resourceResponse = 0.1f;
    [Min(0f)] public float damageTrailDelay = 0.32f;
    [Min(0.01f)] public float damageTrailSpeed = 0.8f;
    [Range(0f, 1f)] public float criticalHealth = 0.25f;
}
