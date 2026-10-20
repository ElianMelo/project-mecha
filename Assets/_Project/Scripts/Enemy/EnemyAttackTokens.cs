using System.Collections.Generic;
using UnityEngine;

// Authority-only encounter budget. FIFO requests share melee and lance slots.
// Unity's existing hunter-distance limit cannot reserve a slot for a live projectile.
public static class EnemyAttackTokens
{
    private static readonly Dictionary<EnemyController, float> holders = new Dictionary<EnemyController, float>();
    private static readonly List<EnemyController> waiting = new List<EnemyController>();
    private static readonly List<EnemyController> expired = new List<EnemyController>();
    private static readonly Dictionary<EnemyController, EnemyLanceProjectile> flights = new Dictionary<EnemyController, EnemyLanceProjectile>();
    private static float nextAttack;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { holders.Clear(); waiting.Clear(); expired.Clear(); flights.Clear(); nextAttack = 0f; }

    public static bool Request(EnemyController actor, EnemyEngagementSettings settings)
    {
        expired.Clear();
        foreach (var pair in holders)
        {
            if (InFlight(pair.Key)) continue;
            if (pair.Key == null || !pair.Key.WantsEngagement || Time.time >= pair.Value && !pair.Key.EngagementBusy)
                expired.Add(pair.Key);
        }
        foreach (var old in expired) { holders.Remove(old); flights.Remove(old); }
        waiting.RemoveAll(e => e == null || !e.WantsEngagement);
        if (holders.ContainsKey(actor)) return true;
        if (!actor.WantsEngagement) return false;
        if (!waiting.Contains(actor)) waiting.Add(actor);
        if (holders.Count >= Mathf.Max(1, settings.attackTokens) || waiting[0] != actor) return false;
        waiting.RemoveAt(0);
        holders.Add(actor, Time.time + Mathf.Max(.1f, settings.approachLease));
        return true;
    }

    public static bool TryBeginAttack(EnemyController actor, EnemyEngagementSettings settings)
    {
        if (!holders.ContainsKey(actor) || Time.time < nextAttack) return false;
        nextAttack = Time.time + settings.attackSpacing;
        return true;
    }

    private static bool InFlight(EnemyController actor) => flights.TryGetValue(actor, out var shot) && shot != null && !shot.IsEmbedded;
    public static void TrackProjectile(EnemyController actor, EnemyLanceProjectile shot) => flights[actor] = shot;
    public static void Release(EnemyController actor)
    {
        if (!InFlight(actor)) { holders.Remove(actor); flights.Remove(actor); }
        waiting.Remove(actor);
    }
}
