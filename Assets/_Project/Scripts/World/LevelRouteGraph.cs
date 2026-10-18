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
        int depths = Math.Clamp(settings.stepCount, 2, 30);
        int maximumWidth = Math.Clamp(settings.maximumRowWidth, 2, 4);
        int minimumWidth = Math.Clamp(settings.minimumRowWidth, 1, maximumWidth);
        int maximumStraight = Math.Clamp(settings.maximumStraightStreak, 1, 2);
        int maximumWidthStreak = Math.Clamp(settings.maximumRowWidthStreak, 1, 2);
        int singleRows = 0, previousWidth = 0, widthStreak = 0;
        for (int depth = 0; depth < depths; depth++)
        {
            int width = 1;
            if (depth > 0)
            {
                // Pick uniformly from legal widths instead of rerolling, which
                // could stall or choose the same capped width again.
                var widths = Enumerable.Range(minimumWidth, maximumWidth - minimumWidth + 1)
                    .Where(value => (value != 1 || singleRows < maximumStraight) &&
                        (value != previousWidth || widthStreak < maximumWidthStreak)).ToArray();
                if (widths.Length == 0)
                    throw new InvalidOperationException("Routing width range must allow another node count to respect the row repetition limit.");
                width = widths[random.Next(widths.Length)];
            }
            widthStreak = width == previousWidth ? widthStreak + 1 : 1;
            previousWidth = width;
            singleRows = width == 1 ? singleRows + 1 : 0;
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
        var straightRun = new int[nodes.Count];
        for (int depth = 0; depth < rows.Count - 1; depth++)
        {
            int[] from = rows[depth], to = rows[depth + 1];
            var edges = from.Select(_ => new SortedSet<int>()).ToArray();
            for (int i = 0; i < from.Length; i++) edges[i].Add(to[i * to.Length / from.Length]);
            for (int j = 0; j < to.Length; j++) edges[j * from.Length / to.Length].Add(to[j]);
            // Reserve room for forced single choices after the next row. Branch
            // before a narrow funnel, and cap straight paths even in wide rows.
            int forcedAhead = 0;
            for (int d = depth + 2; d < rows.Count && rows[d].Length == 1; d++) forcedAhead++;
            for (int i = 0; i < from.Length; i++)
            {
                if (to.Length > 1 && edges[i].Count == 1 && straightRun[from[i]] + 1 + forcedAhead > maximumStraight)
                {
                    float lane = from.Length == 1 ? 0.5f : i / (float)(from.Length - 1);
                    int extra = Enumerable.Range(0, to.Length).Where(j => !edges[i].Contains(to[j]))
                        .OrderBy(j => Math.Abs(j / (float)(to.Length - 1) - lane)).ThenBy(j => j).First();
                    edges[i].Add(to[extra]);
                }
                var node = nodes[from[i]]; node.next = edges[i].ToArray(); nodes[from[i]] = node;
                int run = edges[i].Count == 1 ? straightRun[from[i]] + 1 : 0;
                foreach (int next in node.next) straightRun[next] = Math.Max(straightRun[next], run);
            }
        }
        return nodes.ToArray();
    }
}
