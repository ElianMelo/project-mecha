using System.IO;
using System.Linq;
using FishNet.Component.Transforming;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Explicit, repeatable project setup. Scene and prefab writes go through Unity's APIs.
public static class MechaGameplaySetup
{
    [MenuItem("Tools/Mecha/Apply Mecha Gameplay")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode before applying gameplay assets.");
        MechaPresentationSetup.EnsureProfiles();
        ConfigureLayers(); ConfigureInputs(); ConfigurePlayer(); ConfigureEnemy(); ConfigureScene();
        MechaPresentationSetup.Apply();
        AssetDatabase.SaveAssets();
        Debug.Log("Mecha gameplay configured: player, enemy, inputs, animator, one-way arena and HUD.");
    }
    private static void ConfigureLayers()
    {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue = "Player";
        tags.FindProperty("layers").GetArrayElementAtIndex(9).stringValue = "Enemy";
        tags.ApplyModifiedProperties();
        Physics.IgnoreLayerCollision(8, 8, true); Physics.IgnoreLayerCollision(8, 9, false); Physics.IgnoreLayerCollision(9, 9, true);
        // Persist the collision matrix as well as its live state.
        var physics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset")[0]);
        EditorUtility.SetDirty(physics.targetObject);
    }
    private static void ConfigureInputs()
    {
        const string path = "Assets/_Project/Inputs/InputSystem_Actions.inputactions";
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        var old = asset.FindActionMap("Player", false);
        if (old != null) asset.RemoveActionMap(old);
        var map = asset.AddActionMap("Player");
        var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
        move.AddCompositeBinding("2DVector").With("Left", "<Keyboard>/leftArrow", groups: "Keyboard&Mouse").With("Right", "<Keyboard>/rightArrow", groups: "Keyboard&Mouse");
        move.AddBinding("<Gamepad>/leftStick", groups: "Gamepad");
        Bind(map, "Jump", "<Keyboard>/upArrow", "<Keyboard>/space", "<Gamepad>/buttonSouth");
        Bind(map, "Down", "<Keyboard>/downArrow", null, "<Gamepad>/dpad/down");
        Bind(map, "Attack", "<Keyboard>/z", "<Mouse>/leftButton", "<Gamepad>/buttonWest");
        Bind(map, "Dash", "<Keyboard>/leftShift", "<Keyboard>/rightShift", "<Gamepad>/rightShoulder");
        Bind(map, "SkillA", "<Keyboard>/a", null, "<Gamepad>/buttonNorth");
        Bind(map, "SkillS", "<Keyboard>/s", null, "<Gamepad>/buttonEast");
        Bind(map, "SkillD", "<Keyboard>/d", null, "<Gamepad>/leftShoulder");
        Bind(map, "Tech", "<Keyboard>/x", null, "<Gamepad>/rightStickPress");
        File.WriteAllText(path, asset.ToJson()); AssetDatabase.ImportAsset(path);
    }
    private static void Bind(InputActionMap map, string name, string key, string alternate, string pad)
    {
        var action = map.AddAction(name, InputActionType.Button);
        action.AddBinding(key, groups: "Keyboard&Mouse");
        if (alternate != null) action.AddBinding(alternate, groups: "Keyboard&Mouse");
        action.AddBinding(pad, groups: "Gamepad");
    }
    private static T Ensure<T>(GameObject go) where T : Component
    {
        var component=go.GetComponent<T>();
        return component!=null?component:go.AddComponent<T>();
    }
    private static void ConfigurePlayer()
    {
        const string path = "Assets/_Project/Prefabs/MechaPlayer.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Ensure<PlayerHealth>(root); Ensure<PlayerCombatVfx>(root);
            root.layer = 8; root.SetActive(true);
            var rigidbody = root.GetComponent<Rigidbody>(); rigidbody.useGravity = false; rigidbody.linearDamping = 0f;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            var movement = new SerializedObject(root.GetComponent<PlayerMovement>());
            movement.FindProperty("settings").objectReferenceValue = MechaPresentationSetup.Profile<PlayerMovementSettings>("PlayerMovement");
            movement.ApplyModifiedPropertiesWithoutUndo();
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                if (collider.gameObject != root) collider.enabled = false;
            foreach (var childBody in root.GetComponentsInChildren<Rigidbody>(true))
                if (childBody != rigidbody) { childBody.isKinematic = true; childBody.useGravity = false; }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void ConfigureEnemy()
    {
        const string path = "Assets/_Project/Prefabs/EvaEnemy.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Ensure<EnemyPresentation>(root); Ensure<NetworkTransform>(root);
            var netTransform = new SerializedObject(root.GetComponent<NetworkTransform>());
            netTransform.FindProperty("_clientAuthoritative").boolValue = false;
            netTransform.ApplyModifiedPropertiesWithoutUndo();
            root.layer = 9; root.SetActive(true);
            var rigidbody = root.GetComponent<Rigidbody>(); rigidbody.useGravity = false;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            root.transform.position = Vector3.zero;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void ConfigureScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainScene.unity") throw new System.InvalidOperationException("Open MainScene before configuring the arena.");
        MechaArenaSetup.Apply();
        var networkHud = GameObject.Find("NetworkHudCanvas");
        if (networkHud != null) networkHud.SetActive(false);
        if (Object.FindFirstObjectByType<MechaGameplayHud>() == null) CreateHud();
        var hud = Object.FindFirstObjectByType<MechaGameplayHud>();
        hud.gameObject.name = "Mecha Gameplay HUD";
        var controls = hud.transform.Find("Controls") as RectTransform;
        if (controls != null)
        {
            controls.anchorMin = controls.anchorMax = controls.pivot = new Vector2(0.5f, 1f);
            controls.anchoredPosition = new Vector2(0f, -110f); controls.sizeDelta = new Vector2(1180f, 40f);
            controls.GetComponent<TMP_Text>().text = "ARROWS move / jump    Hold DOWN descend    Z combo + UP launcher    Double-tap / SHIFT dash    A / S / D skills";
        }
        MechaHudSetup.Apply(hud);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)); var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = position; rt.sizeDelta = size; return rt;
    }
    private static TMP_Text Label(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string value, float fontSize)
    {
        var rt = Rect(name, parent, anchor, position, size); var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = value; label.fontSize = fontSize; label.color = Color.white; label.raycastTarget = false; return label;
    }
    private static UnityEngine.UI.Image Bar(string name, Transform parent, Vector2 position, Color tint)
    {
        var rt = Rect(name, parent, new Vector2(0f, 1f), position, new Vector2(280f, 12f));
        rt.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.16f, 0.22f);
        var fill = Rect("Fill", rt, Vector2.zero, Vector2.zero, new Vector2(280f, 12f)).gameObject.AddComponent<UnityEngine.UI.Image>();
        fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.color = tint; fill.raycastTarget = false; return fill;
    }
    private static void CreateHud()
    {
        var go = new GameObject("Mecha Gameplay HUD", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; go.GetComponent<Canvas>().sortingOrder = 20;
        var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f); scaler.matchWidthOrHeight = 0.5f;
        var hud = go.AddComponent<MechaGameplayHud>(); var data = new SerializedObject(hud);
        data.FindProperty("status").objectReferenceValue = Label("Status", go.transform, new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(950f, 40f), "TRAINING ARENA", 20f);
        data.FindProperty("healthFill").objectReferenceValue = Bar("Health", go.transform, new Vector2(24f, -56f), new Color(0.9f, 0.23f, 0.28f));
        data.FindProperty("manaFill").objectReferenceValue = Bar("Mana", go.transform, new Vector2(24f, -76f), new Color(0.2f, 0.65f, 1f));
        var array = data.FindProperty("skills"); array.arraySize = 3;
        string[] names = { "A  MEGA SLASH", "S  SWORD FIRE", "D  CRITICAL X" };
        for (int i = 0; i < 3; i++)
        {
            var rt = Rect(names[i], go.transform, new Vector2(0.5f, 0f), new Vector2(-230f + i * 230f, 22f), new Vector2(210f, 72f));
            rt.pivot = new Vector2(0.5f, 0f); rt.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.055f, 0.1f, 0.17f, 0.94f);
            var label = Label("Skill", rt, Vector2.zero, new Vector2(14f, 10f), new Vector2(195f, 58f), names[i] + "\nREADY", 18f);
            array.GetArrayElementAtIndex(i).objectReferenceValue = label;
        }
        var controls = Label("Controls", go.transform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1180f, 40f), "ARROWS move / jump / descend    Z attack    Double-tap or SHIFT dash    A / S / D skills", 17f);
        controls.rectTransform.pivot = new Vector2(0.5f, 1f); controls.alignment = TextAlignmentOptions.Center;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
