using System;
using System.Collections.Generic;
using UnityEngine;

public enum LevelShape { Rising, Long, Big }
public enum LevelStart { TopLeft, TopRight, BottomLeft, BottomRight, MiddleCenter, TopCenter, BottomCenter, LeftCenter, RightCenter }

// Pure layout data, shared by editor preview and each peer. Never modifies a profile.
public sealed class ProceduralLevelLayout
{
    public LevelShape Shape;
    public LevelStart Start;
    public Rect Bounds;
    public Vector3 PlayerStart, Portal;
    public ArenaLayoutSettings.PlatformData[] Platforms;
    public Vector3[] Enemies;
    public string Description;

    public static ProceduralLevelLayout Generate(ProceduralLevelSettings settings, int seed, int number, float aspect, LevelShape? shape = null)
    {
        var random = new System.Random(seed);
        var room = new ProceduralLevelLayout();
        room.Shape = number == 1 ? LevelShape.Big : shape ?? (LevelShape)((number - 2) % 3);
        room.Start = number == 1 ? LevelStart.BottomLeft : (LevelStart)random.Next(9);
        float width = number == 1 ? settings.firstRoomWidth : room.Shape == LevelShape.Rising
            ? 2f * settings.cameraSettings.orthographicSize * aspect + 0.1f
            : Range(random, room.Shape == LevelShape.Long ? settings.longWidth : settings.bigWidth);
        int tiers = number == 1 ? settings.firstRoomTiers : room.Shape == LevelShape.Long ? random.Next(1, 3)
            : Next(random, room.Shape == LevelShape.Rising ? settings.risingTiers : settings.bigTiers);
        float floor = settings.floorHeight;
        room.Bounds = new Rect(-width * 0.5f, floor - 0.7f, width,
            tiers * settings.tierHeight + 2f * settings.cameraSettings.orthographicSize);
        int columns = Mathf.Max(3, Mathf.FloorToInt(width / settings.horizontalCellWidth));
        if (columns % 2 == 0) columns++;
        float cell = (width - 2f * settings.edgeInset) / columns;
        var platforms = new List<ArenaLayoutSettings.PlatformData>();
        var supports = new List<Vector3>();
        for (int col = 0; col < columns; col++)
            supports.Add(new Vector3(-width * 0.5f + settings.edgeInset + (col + 0.5f) * cell, floor + 0.08f, 0f));
        int previousPattern = -1;
        for (int row = 1; row <= tiers; row++)
        {
            if (number == 1 && settings.firstRoomPlatforms != null && settings.firstRoomPlatforms.Length > 0)
            {
                foreach (var deck in settings.firstRoomPlatforms)
                    AddPlatform(platforms, supports, deck.name, deck.position.x - deck.width * 0.5f,
                        deck.position.y + settings.platformThickness * 0.5f, deck.width, settings);
                break;
            }
            if (room.Shape == LevelShape.Rising)
            {
                int pattern = (previousPattern + 1 + random.Next(3)) % 4;
                previousPattern = pattern;
                float y = floor + row * settings.tierHeight;
                float left = -width * 0.5f;
                if (pattern == 0)
                    AddPlatform(platforms, supports, $"Wide Bridge {row:00}", left, y, width, settings);
                else if (pattern == 1)
                {
                    float gap = settings.risingGap;
                    float split = Range(random, new Vector2(0.4f, 0.6f));
                    float a = (width - gap) * split, b = width - gap - a;
                    AddPlatform(platforms, supports, $"Offset Left {row:00}", left, y, a, settings);
                    AddPlatform(platforms, supports, $"Offset Right {row:00}", left + a + gap, y + settings.risingStepOffset, b, settings);
                }
                else if (pattern == 2)
                {
                    bool flip = random.Next(2) == 0;
                    float ledge = width * 0.24f, gap = settings.risingGap;
                    float wide = width - ledge - gap;
                    AddPlatform(platforms, supports, $"Broad Deck {row:00}", flip ? left + ledge + gap : left, y, wide, settings);
                    AddPlatform(platforms, supports, $"Side Ledge {row:00}", flip ? left : left + wide + gap, y + settings.risingStepOffset, ledge, settings);
                }
                else
                {
                    float gap = settings.risingGap, a = width * 0.2f, b = width * 0.46f, c = width - a - b - gap * 2f;
                    AddPlatform(platforms, supports, $"Short Left {row:00}", left, y, a, settings);
                    AddPlatform(platforms, supports, $"Long Center {row:00}", left + a + gap, y + settings.risingStepOffset, b, settings);
                    AddPlatform(platforms, supports, $"Short Right {row:00}", left + a + b + gap * 2f, y, c, settings);
                }
                continue;
            }
            // Unequal runs merge neighboring cells; alternating seams and heights remove the grid silhouette.
            float cursor = -width * 0.5f + settings.edgeInset;
            int segment = 0;
            while (cursor < width * 0.5f - settings.edgeInset - 1.4f)
            {
                float available = width * 0.5f - settings.edgeInset - cursor;
                float length = Mathf.Min(available, cell * Range(random, new Vector2(0.65f, 1.7f)));
                if (available - length < 2f) length = available;
                float y = floor + row * settings.tierHeight + Range(random, new Vector2(-0.22f, 0.22f));
                AddPlatform(platforms, supports, $"Staggered Deck {row:00}-{segment++:00}", cursor, y, length, settings);
                cursor += length + Range(random, new Vector2(0.7f, 1.2f));
            }
        }
        Vector2 normalized = StartCoordinate(room.Start);
        Vector3 desired = new Vector3(Mathf.Lerp(-width * 0.5f + cell * 0.5f, width * 0.5f - cell * 0.5f, normalized.x),
            floor + tiers * settings.tierHeight * normalized.y, 0f);
        room.PlayerStart = supports[0];
        foreach (var support in supports)
            if ((support - desired).sqrMagnitude < (room.PlayerStart - desired).sqrMagnitude) room.PlayerStart = support;
        room.Portal = room.PlayerStart;
        foreach (var support in supports)
            if ((support - room.PlayerStart).sqrMagnitude > (room.Portal - room.PlayerStart).sqrMagnitude) room.Portal = support;
        for (int i = supports.Count - 1; i > 0; i--) { int j = random.Next(i + 1); var t = supports[i]; supports[i] = supports[j]; supports[j] = t; }
        int count = number == 1 ? settings.enemyCount.x : Next(random, settings.enemyCount);
        var enemies = new List<Vector3>();
        foreach (var point in supports)
            if (enemies.Count < count && Vector3.Distance(point, room.PlayerStart) >= settings.minimumSpawnDistance &&
                !enemies.Exists(other => Vector3.Distance(other, point) < settings.minimumEnemySpacing)) enemies.Add(point);
        room.Platforms = platforms.ToArray(); room.Enemies = enemies.ToArray();
        string direction = room.Shape == LevelShape.Rising ? (normalized.y == 0f ? "UP" : normalized.y == 1f ? "DOWN" : "BOTH")
            : room.Shape == LevelShape.Long ? (normalized.x == 0f ? "RIGHT" : normalized.x == 1f ? "LEFT" : "BOTH") : "";
        room.Description = $"{room.Shape.ToString().ToUpperInvariant()} {direction}".Trim();
        return room;
    }
    private static float Range(System.Random r, Vector2 range) => Mathf.Lerp(range.x, range.y, (float)r.NextDouble());
    private static void AddPlatform(List<ArenaLayoutSettings.PlatformData> platforms, List<Vector3> supports,
        string name, float left, float top, float width, ProceduralLevelSettings settings)
    {
        platforms.Add(new ArenaLayoutSettings.PlatformData {name = name,
            position = new Vector2(left + width * 0.5f, top - settings.platformThickness * 0.5f), width = width});
        int places = Mathf.Max(1, Mathf.FloorToInt(width / settings.horizontalCellWidth));
        for (int i = 0; i < places; i++) supports.Add(new Vector3(left + (i + 0.5f) * width / places, top + 0.08f, 0f));
    }
    private static int Next(System.Random r, Vector2Int range) => r.Next(range.x, Mathf.Max(range.x + 1, range.y + 1));
    private static Vector2 StartCoordinate(LevelStart start)
    {
        switch (start)
        {
            case LevelStart.TopLeft: return new Vector2(0f, 1f);
            case LevelStart.TopRight: return Vector2.one;
            case LevelStart.BottomLeft: return Vector2.zero;
            case LevelStart.BottomRight: return Vector2.right;
            case LevelStart.TopCenter: return new Vector2(0.5f, 1f);
            case LevelStart.BottomCenter: return new Vector2(0.5f, 0f);
            case LevelStart.LeftCenter: return new Vector2(0f, 0.5f);
            case LevelStart.RightCenter: return new Vector2(1f, 0.5f);
            default: return Vector2.one * 0.5f;
        }
    }
}
