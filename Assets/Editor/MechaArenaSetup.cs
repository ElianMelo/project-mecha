using System.Linq;
using FishNet.Component.Spawning;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MechaArenaSetup
{
    public static void Apply()
    {
        var layout = MechaPresentationSetup.Create<ArenaLayoutSettings>("ArenaLayout");
        EnsureLayout(layout);
        var camera = MechaPresentationSetup.Profile<RoomCameraSettings>("RoomCamera");
        camera.physicalWalls = layout.horizontalLimits;
        camera.framingFloorHeight = layout.floorHeight;
        camera.viewBounds = new Rect(layout.horizontalLimits.x, layout.cameraVerticalLimits.x,
            layout.horizontalLimits.y - layout.horizontalLimits.x, layout.cameraVerticalLimits.y - layout.cameraVerticalLimits.x);
        camera.initialPosition.x = layout.playerStart.x;
        EditorUtility.SetDirty(camera);
        var ground = GameObject.Find("Ground");
        if (ground == null) throw new System.InvalidOperationException("Arena floor missing.");
        ground.transform.position = new Vector3((layout.horizontalLimits.x + layout.horizontalLimits.y) * 0.5f, layout.floorHeight - layout.floorThickness * 0.5f, 0f);
        ground.transform.localScale = new Vector3(layout.horizontalLimits.y - layout.horizontalLimits.x, layout.floorThickness, layout.floorDepth);
        var platforms = Object.FindObjectsByType<OneWayPlatform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var old in platforms)
            if (!layout.platforms.Any(p => p.name == old.name)) Object.DestroyImmediate(old.gameObject);
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/MechaPlatform.mat");
        var platformSettings = MechaPresentationSetup.Profile<OneWayPlatformSettings>("OneWayPlatform");
        foreach (var data in layout.platforms)
        {
            var go = GameObject.Find(data.name) ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = data.name; go.tag = "Platform"; go.layer = 6;
            go.transform.SetPositionAndRotation(new Vector3(data.position.x, data.position.y, 0f), Quaternion.identity);
            go.transform.localScale = new Vector3(data.width, layout.platformThickness, layout.platformDepth);
            var surface = MechaPresentationSetup.Ensure<OneWayPlatform>(go);
            MechaPresentationSetup.Assign(surface, "settings", platformSettings);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
        var top = GameObject.Find("Top");
        if (top != null)
        {
            top.transform.position = new Vector3(0f, layout.ceilingHeight, 0f);
            top.transform.localScale = new Vector3(layout.horizontalLimits.y - layout.horizontalLimits.x + 2f, 1f, layout.floorDepth);
            var renderer = top.GetComponent<Renderer>(); if (renderer != null) renderer.enabled = false;
        }
        var playerStart = GameObject.Find("PlayerStart") ?? new GameObject("PlayerStart");
        playerStart.transform.position = layout.playerStart;
        var playerSpawner = Object.FindFirstObjectByType<PlayerSpawner>();
        if (playerSpawner != null)
        {
            var serialized = new SerializedObject(playerSpawner);
            var spawns = serialized.FindProperty("Spawns") ?? serialized.FindProperty("_spawns");
            if (spawns != null) { spawns.arraySize = 1; spawns.GetArrayElementAtIndex(0).objectReferenceValue = playerStart.transform; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        }
        var spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner == null) return;
        // Reuse the existing dedicated spawn markers before creating additional ones.
        spawner.spawnPoints.RemoveAll(point => point == null);
        for (int i = 0; i < layout.enemySpawns.Length; i++)
        {
            Transform point;
            if (i < spawner.spawnPoints.Count) point = spawner.spawnPoints[i];
            else { point = new GameObject("Enemy Spawn").transform; point.SetParent(spawner.transform, false); spawner.spawnPoints.Add(point); }
            point.name = $"Enemy Spawn {i + 1:00}"; point.position = layout.enemySpawns[i];
        }
        for (int i = spawner.spawnPoints.Count - 1; i >= layout.enemySpawns.Length; i--)
        { Object.DestroyImmediate(spawner.spawnPoints[i].gameObject); spawner.spawnPoints.RemoveAt(i); }
        EditorUtility.SetDirty(spawner);
    }

    private static void EnsureLayout(ArenaLayoutSettings layout)
    {
        if (layout.platforms != null && layout.platforms.Length > 0) return;
        var platforms = new System.Collections.Generic.List<ArenaLayoutSettings.PlatformData>();
        for (int i = 0; i < 8; i++)
        {
            platforms.Add(new ArenaLayoutSettings.PlatformData {name=$"Lower Deck {i + 1:00}", position=new Vector2(-29f + i * 8f, -1.8f), width=6.2f});
            platforms.Add(new ArenaLayoutSettings.PlatformData {name=$"Upper Deck {i + 1:00}", position=new Vector2(-25f + i * 8f, 0.05f), width=6.2f});
        }
        for (int i = 0; i < 4; i++)
            platforms.Add(new ArenaLayoutSettings.PlatformData {name=$"Summit Deck {i + 1:00}", position=new Vector2(-20f + i * 16f, 1.9f), width=6f});
        layout.platforms = platforms.ToArray();
        layout.enemySpawns = new[] {
            new Vector3(-32f,-3.35f,0f), new Vector3(-20f,-3.35f,0f), new Vector3(-8f,-3.35f,0f),
            new Vector3(8f,-3.35f,0f), new Vector3(20f,-3.35f,0f), new Vector3(32f,-3.35f,0f),
            new Vector3(-29f,-1.54f,0f), new Vector3(-13f,-1.54f,0f), new Vector3(3f,-1.54f,0f), new Vector3(19f,-1.54f,0f),
            new Vector3(-25f,0.31f,0f), new Vector3(-9f,0.31f,0f), new Vector3(7f,0.31f,0f), new Vector3(23f,0.31f,0f),
            new Vector3(-20f,2.16f,0f), new Vector3(12f,2.16f,0f)
        };
        EditorUtility.SetDirty(layout);
    }
}
