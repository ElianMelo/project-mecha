using System.Linq;
using FishNet.Object;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MechaProceduralSetup
{
    [MenuItem("Tools/Mecha/Apply Procedural Levels")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before applying level setup.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainScene.unity") throw new System.InvalidOperationException("Open MainScene first.");
        var profile = MechaPresentationSetup.Create<ProceduralLevelSettings>("ProceduralLevels");
        profile.scenario = MechaPresentationSetup.Profile<ScenarioSettings>("Scenario");
        profile.oneWay = MechaPresentationSetup.Profile<OneWayPlatformSettings>("OneWayPlatform");
        profile.cameraSettings = MechaPresentationSetup.Profile<RoomCameraSettings>("RoomCamera");
        if (profile.portalMaterial == null)
        {
            profile.portalMaterial = new Material(Shader.Find("Sprites/Default")) {name = "Portal Ring"};
            AssetDatabase.CreateAsset(profile.portalMaterial, "Assets/_Project/Materials/PortalRing.mat");
        }
        EditorUtility.SetDirty(profile);
        EditorUtility.SetDirty(MechaPresentationSetup.Profile<PlayerInputSettings>("PlayerInput"));
        EditorUtility.SetDirty(MechaPresentationSetup.Profile<EnemyPredictionSettings>("EnemyPrediction"));
        var first = ProceduralLevelLayout.Generate(profile, profile.firstRoomSeed, 1, 16f / 9f);
        var layout = MechaPresentationSetup.Profile<ArenaLayoutSettings>("ArenaLayout");
        layout.horizontalLimits = new Vector2(first.Bounds.xMin, first.Bounds.xMax);
        layout.cameraVerticalLimits = new Vector2(first.Bounds.yMin, first.Bounds.yMax);
        layout.ceilingHeight = first.Bounds.yMax; layout.floorHeight = profile.floorHeight;
        layout.playerStart = first.PlayerStart; layout.platforms = first.Platforms; layout.enemySpawns = first.Enemies;
        EditorUtility.SetDirty(layout);
        MechaArenaSetup.Apply();
        var framing = profile.cameraSettings;
        framing.initialPosition = new Vector3(first.PlayerStart.x, first.Bounds.yMin + framing.orthographicSize, -framing.cameraDistance);
        EditorUtility.SetDirty(framing); MechaCameraSetup.Apply();
        // Remove old decorative floor faces before rebuilding the shorter floor.
        var ground = GameObject.Find("Ground");
        foreach (Transform child in ground.transform.Cast<Transform>().ToArray())
            if (child.name.StartsWith("Floor Face")) Object.DestroyImmediate(child.gameObject);
        var crates = GameObject.Find("Destructible Supply Crates");
        if (crates != null) Object.DestroyImmediate(crates);
        profile.scenario.boxFeet = new[] {new Vector3(-2f, profile.floorHeight, 0f), new Vector3(6f, profile.floorHeight, 0f)};
        EditorUtility.SetDirty(profile.scenario);
        MechaScenarioSetup.Apply();
        var spawner = Object.FindFirstObjectByType<EnemySpawner>();
        var director = MechaPresentationSetup.Ensure<ProceduralLevelDirector>(spawner.gameObject);
        MechaPresentationSetup.Assign(director, "settings", profile);
        var spawnSettings = MechaPresentationSetup.Profile<EnemySpawnSettings>("EnemySpawning");
        spawnSettings.spawnAmount = profile.enemyCount.y; spawnSettings.initialDelay = spawnSettings.interval = 0f;
        EditorUtility.SetDirty(spawnSettings);
        string playerPath = "Assets/_Project/Prefabs/MechaPlayer.prefab";
        var playerPrefab = PrefabUtility.LoadPrefabContents(playerPath);
        try
        {
            MechaPresentationSetup.Ensure<PlayerLevelTravel>(playerPrefab);
            SerializeNetworkObjects(playerPrefab.GetComponentsInChildren<NetworkObject>(true));
            PrefabUtility.SaveAsPrefabAsset(playerPrefab, playerPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(playerPrefab); }
        ApplyHud();
        var sceneId = typeof(NetworkObject).GetMethod("CreateSceneId", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        sceneId.Invoke(null, new object[] {scene, false, 0});
        SerializeNetworkObjects(Object.FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("Procedural rooms configured: compact start, finite encounters, exit portal, pointer and infinite revives.");
    }
    private static void SerializeNetworkObjects(NetworkObject[] objects)
    {
        var method = typeof(NetworkObject).GetMethod("ReserializeEditorSetValues", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        foreach (var obj in objects) { method.Invoke(obj, new object[] {true, true}); EditorUtility.SetDirty(obj); }
    }
    private static void ApplyHud()
    {
        var hud = Object.FindFirstObjectByType<MechaGameplayHud>();
        if (hud == null) throw new System.InvalidOperationException("Gameplay HUD missing.");
        var root = MechaScenarioSetup.Child(hud.transform, "Level Objectives");
        var rt = MechaPresentationSetup.Ensure<RectTransform>(root);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var arrowGo = MechaScenarioSetup.Child(root.transform, "Objective Arrow");
        var arrow = MechaPresentationSetup.Ensure<RectTransform>(arrowGo);
        arrow.anchorMin = arrow.anchorMax = arrow.pivot = Vector2.one * 0.5f;
        ConfigureObjectiveArrow(arrowGo);
        var textGo = MechaScenarioSetup.Child(root.transform, "Room Status");
        var textRt = MechaPresentationSetup.Ensure<RectTransform>(textGo);
        textRt.anchorMin = textRt.anchorMax = textRt.pivot = new Vector2(0.5f, 0f);
        textRt.anchoredPosition = new Vector2(0f, 16f); textRt.sizeDelta = new Vector2(700f, 30f);
        var text = MechaPresentationSetup.Ensure<TextMeshProUGUI>(textGo);
        text.font = hud.GetComponentInChildren<TMP_Text>().font; text.fontSize = 17f;
        text.alignment = TextAlignmentOptions.Center; text.color = Color.white; text.raycastTarget = false;
        var objective = MechaPresentationSetup.Ensure<LevelObjectiveHud>(root);
        MechaPresentationSetup.Assign(objective, "arrow", arrow); MechaPresentationSetup.Assign(objective, "objective", text);
        var controls = hud.transform.Find("Controls")?.GetComponent<TMP_Text>();
        if (controls != null && !controls.text.Contains("PORTAL")) { controls.text += "\nPORTAL E / UP  ·  INFINITE REVIVES"; EditorUtility.SetDirty(controls); }
    }

    [MenuItem("Tools/Mecha/Apply Objective Arrow Sprite")]
    public static void ApplyObjectiveArrow()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before applying arrow setup.");
        var hud = Object.FindFirstObjectByType<LevelObjectiveHud>();
        if (hud == null) throw new System.InvalidOperationException("Open a scene with LevelObjectiveHud first.");
        var serialized = new SerializedObject(hud);
        var arrow = (RectTransform)serialized.FindProperty("arrow").objectReferenceValue;
        ConfigureObjectiveArrow(arrow.gameObject);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        EditorSceneManager.SaveScene(hud.gameObject.scene);
    }

    private static void ConfigureObjectiveArrow(GameObject arrowGo)
    {
        var profile = MechaPresentationSetup.Profile<ProceduralLevelSettings>("ProceduralLevels");
        if (profile.objectiveArrowSprite == null)
        {
            profile.objectiveArrowSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
            EditorUtility.SetDirty(profile);
        }
        // Migrate the old custom mesh without rebuilding the HUD hierarchy.
        foreach (var graphic in arrowGo.GetComponents<UnityEngine.UI.Graphic>())
            if (graphic.GetType().Name == "ObjectiveArrowGraphic") Object.DestroyImmediate(graphic);
        var image = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(arrowGo);
        image.sprite = profile.objectiveArrowSprite;
        image.type = UnityEngine.UI.Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = new Vector2(28f, 36f);
        image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        EditorUtility.SetDirty(image);
    }
}
