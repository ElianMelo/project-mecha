using UnityEngine;

// The same relationship gate is used by acquisition, melee and projectile contacts.
public static class CombatTargetRules
{
    public static int TargetLayers(int existing) => PvpMatchDirector.Active ? existing | (1 << LayerMask.NameToLayer("Player")) : existing;
    public static bool CanHit(PlayerHealth source, IDamageable target)
    {
        if (target == null || target.IsDead) return false;
        if (target is PlayerHealth player) return PvpMatchDirector.Instance != null && PvpMatchDirector.Instance.AreOpponents(source, player);
        return target is DestructibleProp || !PvpMatchDirector.Active;
    }
    public static bool CanAim(PlayerHealth source, ITargetable target)
    {
        if (target is PlayerHealth player) return PvpMatchDirector.Instance != null && PvpMatchDirector.Instance.AreOpponents(source, player);
        return target is DestructibleProp || !PvpMatchDirector.Active;
    }
}
