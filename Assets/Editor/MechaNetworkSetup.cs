using System.Linq;
using FishNet.Component.Transforming;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Object;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MechaNetworkSetup
{
    [MenuItem("Tools/Mecha/Apply Online Lobby and Enemy Roles")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        ApplyEncounterTuning();
        MechaProceduralSetup.Apply();
        var network = Object.FindFirstObjectByType<NetworkManager>();
        var lobbySettings = MechaPresentationSetup.Create<LobbySettings>("Lobby");
        lobbySettings.rosterRefreshSeconds = 3f; EditorUtility.SetDirty(lobbySettings);
        var service = MechaPresentationSetup.Ensure<MechaLobbyService>(GameObject.Find("System"));
        MechaPresentationSetup.Ensure<NetworkSessionDiagnostics>(service.gameObject);
        MechaPresentationSetup.Assign(service, "settings", lobbySettings); MechaPresentationSetup.Assign(service, "network", network);
        MechaPresentationSetup.Assign(Object.FindFirstObjectByType<GameManager>(), "lobbySettings", lobbySettings);
        MechaLobbySetup.Apply(service);
        var hudSettings = MechaPresentationSetup.Profile<GameplayHudLayoutSettings>("GameplayHudLayout");
        if (!hudSettings.tutorialText.Contains("LOBBIES"))
            hudSettings.tutorialText += "\nONLINE LOBBIES 3   LOCAL HOST 1   PORTAL E / UP   INFINITE REVIVES";
        EditorUtility.SetDirty(hudSettings); MechaHudSetup.Apply(Object.FindFirstObjectByType<MechaGameplayHud>());
        var tactics = MechaPresentationSetup.Create<EnemyTacticsSettings>("EnemyTactics");
        EditorUtility.SetDirty(tactics);
        var lance = MechaPresentationSetup.Create<EnemyLanceSettings>("EnemyLance");
        BuildProjectile(lance);
        ConfigureMelee(tactics);
        BuildLancer(lance, tactics);
        var crowd = MechaPresentationSetup.Profile<EnemyCrowdSettings>("EnemyCrowd");
        crowd.sparseOverlap = 0f; crowd.denseOverlap = 0.05f; crowd.neighborRadius = 3f; EditorUtility.SetDirty(crowd);
        var background = MechaPresentationSetup.Profile<ParallaxSceneSettings>("ParallaxScene");
        for (int i = 0; i < background.layers.Length; i++)
            background.layers[i].tint = i == 0 ? new Color(0.3f, 0.33f, 0.42f) : i == 1 ? new Color(0.23f, 0.26f, 0.32f) : new Color(0.18f, 0.21f, 0.27f);
        EditorUtility.SetDirty(background);
        MechaBackgroundSetup.Apply(GameObject.Find("Cyberpunk Scenario").transform);
        var player = PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/MechaPlayer.prefab");
        try { ConfigureTransform(player, true); Serialize(player); PrefabUtility.SaveAsPrefabAsset(player, "Assets/_Project/Prefabs/MechaPlayer.prefab"); }
        finally { PrefabUtility.UnloadPrefabContents(player); }
        var prefabs = AssetDatabase.LoadAssetAtPath<DefaultPrefabObjects>("Assets/DefaultPrefabObjects.asset");
        prefabs.AddObject(lance.projectile, true, false);
        prefabs.AddObject(MechaPresentationSetup.Profile<EnemySpawnSettings>("EnemySpawning").lancePrefab, true, false);
        typeof(DefaultPrefabObjects).GetMethod("SetAssetPathHashes", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(prefabs, new object[] {0});
        typeof(DefaultPrefabObjects).GetMethod("Sort", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(prefabs, null);
        EditorUtility.SetDirty(prefabs);
        AssetDatabase.SaveAssets();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        MechaNetworkSynchronizationSetup.Apply();
    }
    private static void ConfigureMelee(EnemyTacticsSettings profile)
    {
        const string path = "Assets/_Project/Prefabs/EvaEnemy.prefab";
        var go = PrefabUtility.LoadPrefabContents(path);
        try
        {
            MechaPresentationSetup.Assign(MechaPresentationSetup.Ensure<EnemyTactics>(go), "settings", profile);
            ConfigureTransform(go, false); Serialize(go); PrefabUtility.SaveAsPrefabAsset(go, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(go); }
    }
    private static void BuildLancer(EnemyLanceSettings profile, EnemyTacticsSettings tactics)
    {
        const string path = "Assets/_Project/Prefabs/LanceEnemy.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) AssetDatabase.CopyAsset("Assets/_Project/Prefabs/EvaEnemy.prefab", path);
        var go = PrefabUtility.LoadPrefabContents(path);
        try
        {
            go.name = "Lance Enemy";
            var attack = MechaPresentationSetup.Ensure<EnemyLanceAttack>(go);
            MechaPresentationSetup.Ensure<EnemyLancePresentation>(go);
            MechaPresentationSetup.Assign(attack, "settings", profile);
            var weapon = go.GetComponentsInChildren<Transform>(true).First(t => t.name == "CassiussSword");
            MechaPresentationSetup.Assign(attack, "heldLance", weapon);
            MechaPresentationSetup.Assign(MechaPresentationSetup.Ensure<EnemyTactics>(go), "settings", tactics);
            var marker = go.transform.Find("Lance Thrower Crest");
            if (marker != null) Object.DestroyImmediate(marker.gameObject);
            ConfigureTransform(go, false); Serialize(go); PrefabUtility.SaveAsPrefabAsset(go, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(go); }
        var spawn = MechaPresentationSetup.Profile<EnemySpawnSettings>("EnemySpawning");
        spawn.lancePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<NetworkObject>(); EditorUtility.SetDirty(spawn);
    }
    private static void BuildProjectile(EnemyLanceSettings settings)
    {
        const string path = "Assets/_Project/Prefabs/EnemyLance.prefab";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        var go = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject("Enemy Lance");
        try
        {
            MechaPresentationSetup.Ensure<NetworkObject>(go);
            MechaPresentationSetup.Assign(MechaPresentationSetup.Ensure<EnemyLanceProjectile>(go), "settings", settings);
            MechaPresentationSetup.Ensure<NetworkTransform>(go); ConfigureTransform(go, false);
            var body = go.GetComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false; body.interpolation = RigidbodyInterpolation.Interpolate;
            var line = go.GetComponent<LineRenderer>(); if (line != null) Object.DestroyImmediate(line);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EvaEnemy.prefab")
                .GetComponentsInChildren<MeshFilter>(true).First(m => m.transform.parent.name == "CassiussSword");
            var visual = MechaScenarioSetup.Child(go.transform, "Thrown Lance Model");
            var mesh = MechaPresentationSetup.Ensure<MeshFilter>(visual); mesh.sharedMesh = source.sharedMesh;
            MechaPresentationSetup.Ensure<MeshRenderer>(visual).sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
            // Match the held weapon's world size and retain its imported 3D geometry/materials.
            float scale = source.transform.lossyScale.x;
            visual.transform.localScale = Vector3.one * scale;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localPosition = -source.sharedMesh.bounds.center * scale;
            visual.transform.localPosition += Vector3.forward * settings.projectileVisualDepth;
            MechaAdjustmentsSetup.ProjectileForeground(visual.GetComponent<Renderer>());
            Serialize(go); PrefabUtility.SaveAsPrefabAsset(go, path);
        }
        finally { if (exists) PrefabUtility.UnloadPrefabContents(go); else Object.DestroyImmediate(go); }
        settings.projectile = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<NetworkObject>(); EditorUtility.SetDirty(settings);
    }
    private static void ApplyEncounterTuning()
    {
        var traversal = MechaPresentationSetup.Profile<EnemyTraversalSettings>("EnemyTraversal");
        traversal.jumpSpeed = 15f; traversal.risingGravity = 40f; traversal.fallingGravity = 64f;
        traversal.terminalVelocity = 28f; traversal.minimumHopHeight = 0.85f; traversal.landingClearance = 0.4f;
        EditorUtility.SetDirty(traversal);
        var room = MechaPresentationSetup.Create<ProceduralLevelSettings>("ProceduralLevels");
        // A compact, uneven opening route: broad merged decks, short side perches, staggered upper bridges.
        room.firstRoomPlatforms = new[] {
            Deck("Arrival Shelf", -7.6f, -2f, 5.6f, room), Deck("Lower Crossing", -1.2f, -1.6f, 5f, room),
            Deck("Right Landing", 6.5f, -2.25f, 6.8f, room), Deck("Left Overhang", -6f, -0.25f, 6.4f, room),
            Deck("Central Causeway", 2f, 0.15f, 7.2f, room), Deck("Right Perch", 8.6f, -0.6f, 3.6f, room),
            Deck("Upper Left Perch", -7.1f, 1.35f, 4.4f, room), Deck("Upper Bridge", 2.7f, 1.75f, 12.6f, room)
        };
        EditorUtility.SetDirty(room);
    }
    private static ArenaLayoutSettings.PlatformData Deck(string name, float x, float top, float width, ProceduralLevelSettings room)
        => new ArenaLayoutSettings.PlatformData {name = name, position = new Vector2(x, top - room.platformThickness * 0.5f), width = width};
    private static void ConfigureTransform(GameObject go, bool owner)
    {
        var transform = go.GetComponent<NetworkTransform>(); if (transform == null) return;
        MechaNetworkSynchronizationSetup.ConfigureTransform(transform,
            MechaPresentationSetup.Create<NetworkSimulationSettings>("NetworkSimulation"), owner,
            go.GetComponent<EnemyLanceProjectile>() != null);
    }
    private static void Serialize(GameObject go)
    {
        var method = typeof(NetworkObject).GetMethod("ReserializeEditorSetValues", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        foreach (var obj in go.GetComponentsInChildren<NetworkObject>(true)) { method.Invoke(obj, new object[] {true, true}); EditorUtility.SetDirty(obj); }
    }
}
