using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Combat/Attack Piece")]
public sealed class AttackPiece : ScriptableObject
{
    public string displayName;
    public PlayerCombatSettings.ActionData action = new PlayerCombatSettings.ActionData();
    public string Label => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
}
