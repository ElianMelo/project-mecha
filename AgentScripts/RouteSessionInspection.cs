using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Operator-invoked inspection/staging only: no assertions, timed scenarios, or Test Runner integration.
public static class RouteSessionInspection
{
    public static object Snapshot()
    {
        var route = LevelRouteDirector.Instance;
        return new { frame = Time.frameCount, phase = route.Phase.ToString(), route.Sector, route.Round, route.CurrentNode, route.Winner,
            route.SecondsRemaining, room = ProceduralLevelDirector.Instance.LevelNumber, remaining = ProceduralLevelDirector.Instance.Remaining,
            votes = route.Pilots.Select(x => new { x.playerId, x.nodeId, x.name }).ToArray(),
            available = route.Nodes.Where(x => route.IsAvailable(x.id)).Select(x => new { x.id, x.encounterId, x.next }).ToArray() };
    }
    public static object ClearCurrentEncounter()
    {
        foreach (var enemy in EnemyController.ActiveEnemies.ToArray()) if (enemy.IsSpawned && !enemy.IsDead) enemy.TakeHit(100000f, Vector3.zero, 0f);
        return "Applied lethal damage through the ordinary authoritative enemy damage method.";
    }
    public static object PlaceAtExit()
    {
        var player = UnityEngine.Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None).First(p => p.IsSpawned && p.IsOwner);
        player.EnterLevel(ProceduralLevelDirector.Instance.Layout.Portal);
        return "Local pilot placed at the exit for a manual portal interaction.";
    }
    public static object StageGuestVote(int node)
    {
        var route = LevelRouteDirector.Instance;
        if (!route.IsServerInitialized || !route.IsAvailable(node)) throw new InvalidOperationException("Host needs an available route.");
        var guest = route.Pilots.First(p => p.playerId != route.ClientManager.Connection.ClientId);
        var field = typeof(LevelRouteDirector).GetField("ballot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var ballot = (RouteBallot)field.GetValue(route);
        bool accepted = ballot.Vote(guest.playerId, node, Time.realtimeSinceStartupAsDouble);
        typeof(LevelRouteDirector).GetMethod("Publish", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(route, null);
        return new { accepted, note = "Guest ballot staged on the host for visual inspection; this does not exercise guest input or its ServerRpc." };
    }
}
