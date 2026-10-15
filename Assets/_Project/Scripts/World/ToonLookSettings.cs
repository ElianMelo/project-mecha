using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Presentation/Toon Look")]
public class ToonLookSettings : ScriptableObject
{
    public Color firstShade = new Color(0.28f,0.34f,0.46f), secondShade = new Color(0.055f,0.07f,0.12f), outline = new Color(0.01f,0.014f,0.025f);
    public float baseStep = 0.58f, shadeStep = 0.27f, feather = 0.001f, outlineWidth = 0.008f;
    public Color highlight = new Color(0.16f,0.2f,0.24f), ambient = new Color(0.12f,0.15f,0.22f), keyColor = new Color(1f,0.94f,0.84f);
    public float keyIntensity = 1.25f, highlightPower = 0.82f, shadowBias = 0.025f, shadowNormalBias = 0.15f;
    public float shadowDistance = 35f;
    public Vector3 keyRotation = new Vector3(38f, -35f, 0f);
    public Color background = new Color(0.035f,0.05f,0.075f);
    public float playerModelScale = 0.65f;
    public Vector3 playerModelOffset = Vector3.zero;
}
