using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public struct LevelRouteNode
{
    public int id, depth, lane, width, seed;
    public string encounterId;
    public int[] next;
}

// Layered DAG: every node has a route in and out, with ordered edges and equal encounter counts.
public static class LevelRouteGraph
{
    public static LevelRouteNode[] Generate(LevelRouteSettings settings, RouteEncounterDefinition[] catalogue, int seed, bool opening)
    {
        if (settings.openingEncounter == null) throw new InvalidOperationException("Routing needs an opening encounter.");
        if (catalogue.Length == 0) throw new InvalidOperationException("No route encounters are registered.");
        var random = new Random(seed);
        var nodes = new List<LevelRouteNode>();
        var rows = new List<int[]>();
        var widths = settings.rowWidths;
        int depths = Math.Max(2, widths == null ? 0 : widths.Length);
        for (int depth = 0; depth < depths; depth++)
        {
            int width = depth == 0 || depth == depths - 1 ? 1 : Math.Clamp(widths[depth], 1, 3);
            var row = new int[width];
            var available = catalogue.Where(x => x.includeInRoutes && depth + 1 >= x.minimumDepth && depth + 1 <= x.maximumDepth).ToList();
            if (available.Count == 0) available.Add(settings.openingEncounter);
            for (int lane = 0; lane < width; lane++)
            {
                RouteEncounterDefinition room;
                if (depth == 0 && opening) room = settings.openingEncounter;
                else
                {
                    double roll = random.NextDouble() * available.Sum(x => Math.Max(0.01f, x.weight));
                    room = available[available.Count - 1];
                    foreach (var candidate in available) { roll -= Math.Max(0.01f, candidate.weight); if (roll <= 0) { room = candidate; break; } }
                    // Prefer distinct previews across a branch when the catalogue permits it.
                    if (available.Count > 1) available.Remove(room);
                }
                row[lane] = nodes.Count;
                nodes.Add(new LevelRouteNode { id = nodes.Count, depth = depth, lane = lane, width = width,
                    seed = depth == 0 && opening ? room.roomSettings.firstRoomSeed : random.Next(),
                    encounterId = room.encounterId, next = Array.Empty<int>() });
            }
            rows.Add(row);
        }
        for (int depth = 0; depth < rows.Count - 1; depth++)
        {
            int[] from = rows[depth], to = rows[depth + 1];
            var edges = from.Select(_ => new SortedSet<int>()).ToArray();
            for (int i = 0; i < from.Length; i++) edges[i].Add(to[i * to.Length / from.Length]);
            for (int j = 0; j < to.Length; j++) edges[j * from.Length / to.Length].Add(to[j]);
            for (int i = 0; i < from.Length; i++) { var node = nodes[from[i]]; node.next = edges[i].ToArray(); nodes[from[i]] = node; }
        }
        return nodes.ToArray();
    }
}
