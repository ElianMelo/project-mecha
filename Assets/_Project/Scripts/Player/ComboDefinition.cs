using System;
using System.Linq;
using UnityEngine;

public enum ComboDomain { Ground, Air }

[CreateAssetMenu(menuName = "Mecha/Combat/Combo Sequence")]
public sealed class ComboDefinition : ScriptableObject
{
    public UnityEngine.Localization.LocalizedString displayNameLocalized = new UnityEngine.Localization.LocalizedString();
    [HideInInspector] public string displayName;
    public ComboDomain domain;
    [Min(0.1f)] public float inputWindow = 0.9f;
    public bool loop = true;
    public AttackPiece[] attacks = Array.Empty<AttackPiece>();
    public string Label => GameText.Get(displayNameLocalized);
    public string Inputs => (domain == ComboDomain.Air ? GameText.Get("Combat", "combo.air.prefix") : "") + string.Join("  ", (attacks ?? Array.Empty<AttackPiece>()).Select(p => GameInputPresentation.Hint(p != null && PlayerAttack.IsHeavy(p.action.kind) ? "Tech" : "Attack")));
}
