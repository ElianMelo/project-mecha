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
    }
    public ActionData[] actions;
    public float maxMana = 100f, manaRegen = 7f, hitMana = 4f;
    public float attackBuffer = 0.4f, skillBuffer = 0.4f, networkRecoveryGrace = 0.04f, comboWindow = 0.9f;
    public LayerMask enemyLayers = 1 << 9;
    public ActionData Get(PlayerAttack.ActionKind kind)
    {
        foreach (var action in actions) if (action.kind == kind) return action;
        throw new InvalidOperationException("Missing combat action: " + kind);
    }
}
