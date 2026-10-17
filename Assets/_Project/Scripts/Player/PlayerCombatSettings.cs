using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Player/Combat")]
public class PlayerCombatSettings : ScriptableObject
{
    [Serializable]
    public class ActionData
    {
        public PlayerAttack.ActionKind kind;
        public float duration = 0.45f, impact = 0.16f, cancel = 0.3f;
        public int pulses = 1;
        public float pulseInterval = 0.13f;
        public float damage = 9f, finalDamage = 9f;
        public bool launcher, finalPulseLaunch;
        public Vector3 hitHalfSize = new Vector3(0.65f, 0.7f, 0.7f);
        public float reach = 0.7f, pulseAdvance, height = 0.85f;
        public float push = 1.6f, lift = 1f, stun = 0.25f;
        public float manaCost, cooldown;
        [Header("Cadence and authored motion")]
        public float stepSpeed = 2.8f, stepDuration = 0.09f;
        public float airLift = 4.5f;
        public bool airOnly, requiresGround, sideSwitch, pull;
        public float diveSpeed, pullSpeed = 6f;
        [Header("Skill contact envelope (native capsule, once per target per pulse)")]
        [Min(0f)] public float hitActiveTime;
        [Min(0f)] public float hitLength;
        [Min(0f)] public float hitTravelTime;
        [Min(0.01f)] public float hitRadius = 0.3f;
        public float hitFirstAngle, hitAngleStep, hitPulseHeight;
        public Vector2 hitOrigin = new Vector2(0.15f, 0.85f);
        public bool hitFromHand;
        public LayerMask hitObstructions = (1 << 6) | (1 << 7);
    }
    [HideInInspector] public ActionData[] actions; // Legacy data retained for migration.
    public AttackPiece[] pieces;
    public ComboLoadout combos;
    [Min(0f)] public float damageMultiplier = 2f;
    public float maxMana = 100f, manaRegen = 7f, hitMana = 4f;
    public float attackBuffer = 0.4f, skillBuffer = 0.4f, networkRecoveryGrace = 0.04f, comboWindow = 0.9f;
    public LayerMask enemyLayers = 1 << 9;
    public float comboDisplayWindow = 1.25f;
    public ActionData Get(PlayerAttack.ActionKind kind)
    {
        if (pieces != null) foreach (var piece in pieces) if (piece != null && piece.action.kind == kind) return piece.action;
        if (actions != null) foreach (var action in actions) if (action.kind == kind) return action;
        throw new InvalidOperationException("Missing combat action: " + kind);
    }
    public int IndexOf(AttackPiece piece) => pieces == null ? -1 : Array.IndexOf(pieces, piece);
    public int IndexOf(PlayerAttack.ActionKind kind)
    {
        if (pieces != null) for (int i = 0; i < pieces.Length; i++)
            if (pieces[i] != null && pieces[i].action.kind == kind) return i;
        return -1;
    }
    public bool IsValidPiece(int index) => pieces != null && index >= 0 && index < pieces.Length && pieces[index] != null;
    public ActionData At(int index) => pieces[index].action;
}
