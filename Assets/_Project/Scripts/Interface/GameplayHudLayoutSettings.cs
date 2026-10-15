using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Interface/Gameplay HUD Layout")]
public class GameplayHudLayoutSettings : ScriptableObject
{
    public Vector2 skillBarAnchor = new Vector2(0f, 1f);
    public Vector2 skillBarOffset = new Vector2(24f, -100f);
    public Vector2 skillSize = new Vector2(144f, 52f);
    [Min(0f)] public float skillSpacing = 8f;
    public float skillFontSize = 13f;
    public Vector2 skillTextInset = new Vector2(8f, 5f), skillTextPadding = new Vector2(12f, 8f);
    public Vector2 tutorialSize = new Vector2(600f, 72f), tutorialOffset = new Vector2(-24f, -18f);
    public float tutorialFontSize = 12f;
    [TextArea] public string tutorialText = "MOVE Left / Right   JUMP Up / Space   DESCEND Hold Down\nATTACK Z   LAUNCHER Z + Up   DASH Double-tap / Shift\nSKILLS A / S / D";
}
