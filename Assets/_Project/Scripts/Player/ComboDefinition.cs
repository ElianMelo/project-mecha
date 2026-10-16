using System;
using System.Linq;
using UnityEngine;

public enum ComboDomain { Ground, Air }

[CreateAssetMenu(menuName = "Mecha/Combat/Combo Sequence")]
public sealed class ComboDefinition : ScriptableObject
{
    public string displayName;
    public ComboDomain domain;
    [Min(0.1f)] public float inputWindow = 0.9f;
    public bool loop = true;
    public AttackPiece[] attacks = Array.Empty<AttackPiece>();
    public string Label => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Inputs => (domain == ComboDomain.Air ? "Up  " : "") + string.Join("  ", (attacks ?? Array.Empty<AttackPiece>()).Select(_ => "Z"));
}
