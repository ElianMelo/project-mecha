using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Combat/Combo Loadout")]
public sealed class ComboLoadout : ScriptableObject
{
    public ComboDefinition groundCombo;
    public ComboDefinition airCombo;
    public AttackPiece dashAttack;
    public AttackPiece risingBranch;
    public AttackPiece diveAttack;
    public AttackPiece criticalBranch;
    public AttackPiece techBranch;
}
