using UnityEngine;
using UnityEngine.Localization;

[System.Flags]
public enum CombatCapabilities { None = 0, LightMelee = 1, HeavyMelee = 2, Ranged = 4, All = 7 }

// Immutable authored character composition. Shared systems consume these contracts.
[CreateAssetMenu(menuName = "Mecha/Characters/Character")]
public sealed class CharacterDefinition : ScriptableObject
{
    public string id;
    public LocalizedString displayName, description;
    public Sprite portrait;
    public GameObject visualPrefab;
    [Min(.01f)] public float visualScale = .65f;
    public PlayerMovementSettings movement;
    public PlayerCombatSettings combat;
    public CharacterAnimationSettings animation;
    public CharacterSkillDefinition[] skills = new CharacterSkillDefinition[3];
    public CombatCapabilities capabilities = CombatCapabilities.All;
    public bool Allows(CombatCapabilities capability) => (capabilities & capability) == capability;
    public string Label => GameText.Get(displayName);
}
