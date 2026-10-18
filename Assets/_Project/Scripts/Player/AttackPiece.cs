using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Combat/Attack Piece")]
public sealed class AttackPiece : ScriptableObject
{
    public UnityEngine.Localization.LocalizedString displayNameLocalized = new UnityEngine.Localization.LocalizedString();
    [HideInInspector] public string displayName;
    public PlayerCombatSettings.ActionData action = new PlayerCombatSettings.ActionData();
    public string Label => GameText.Get(displayNameLocalized);
}
