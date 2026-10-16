using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Interface/Gameplay HUD Layout")]
public class GameplayHudLayoutSettings : ScriptableObject
{
    [Header("Cockpit palette")]
    public Color panelColor = new Color(0.025f, 0.042f, 0.055f, 0.92f);
    public Color textColor = new Color(0.84f, 0.9f, 0.91f);
    public Color accentColor = new Color(0.36f, 0.9f, 0.92f);
    public Color warningColor = new Color(1f, 0.48f, 0.22f);
    public Color startupColor = new Color(0.25f, 0.6f, 0.95f), activeColor = new Color(1f, 0.3f, 0.15f);
    public Color recoveryColor = new Color(0.35f, 0.78f, 0.38f), neutralColor = new Color(0.15f, 0.2f, 0.23f);
    public Sprite panelSprite;
    [Range(0.25f, 1f)] public float pilotHudScale = 0.5f;
    public Vector2 cockpitAnchor = new Vector2(0f, 1f);
    public Vector2 cockpitOffset = new Vector2(28f, -48f), cockpitSize = new Vector2(348f, 148f);
    public Vector2 diagnosticsSize = new Vector2(660f, 414f), diagnosticsOffset = new Vector2(24f, -84f);
    public float diagnosticsRefresh = 0.15f, practiceTempo = 0.5f;
    public Vector2 skillBarAnchor = new Vector2(0f, 1f);
    public Vector2 skillBarOffset = new Vector2(28f, -126f);
    public Vector2 skillSize = new Vector2(144f, 52f);
    [Min(0f)] public float skillSpacing = 8f;
    public float skillFontSize = 13f;
    public Vector2 skillTextInset = new Vector2(8f, 5f), skillTextPadding = new Vector2(12f, 8f);
    public Vector2 tutorialSize = new Vector2(600f, 72f), tutorialOffset = new Vector2(-24f, -18f);
    public float tutorialFontSize = 12f;
    [TextArea] public string tutorialText = "MOVE Left / Right   JUMP Up / Space   DESCEND Hold Down\nATTACK Z   LAUNCHER Z + Up   DASH Double-tap / Shift\nSKILLS A / S / D";
}
