using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Presentation/Skill Focus")]
public sealed class SkillPresentationSettings : ScriptableObject
{
    public UnityEngine.Localization.LocalizedString skillPrefixLocalized = new UnityEngine.Localization.LocalizedString();
    public Sprite backdropSprite;
    public Material backdropMaterial;
    [Range(0f, 1f)] public float darkness = 0.92f;
    public float fadeIn = 0.06f, fadeOut = 0.22f;
    public float backdropDepth = 4f, flashDuration = 0.055f, flashOpacity = 0.12f;
    public Vector2 letterboxHeight = new Vector2(22f, 22f);
    [HideInInspector] public string skillPrefix = "CORE OUTPUT // ";
}
