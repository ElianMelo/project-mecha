using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class MechaComboConfiguratorSetup
{
    private static TMP_FontAsset font;
    private static readonly Color Card = new Color(0.045f, 0.065f, 0.1f, 0.98f);
    private static readonly Color Accent = new Color(0.06f, 0.44f, 0.52f);

    public static void Apply(ComboConfiguratorSettings settings)
    {
        const string path = "Assets/Scenes/ComboConfigurator.unity";
        // Keep the user's scene loaded and active after generating the dedicated authoring scene.
        var original = SceneManager.GetActiveScene();
        var existing = SceneManager.GetSceneByPath(path);
        bool wasLoaded = existing.IsValid() && existing.isLoaded;
        var scene = wasLoaded ? existing : AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null
            ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Combo Configurator UI") ?? new GameObject("Combo Configurator UI");
            var canvas = MechaPresentationSetup.Ensure<Canvas>(root); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = MechaPresentationSetup.Ensure<UnityEngine.UI.CanvasScaler>(root);
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f); scaler.matchWidthOrHeight = 0.5f;
            MechaPresentationSetup.Ensure<UnityEngine.UI.GraphicRaycaster>(root);
            var ui = MechaPresentationSetup.Ensure<ComboConfigurator>(root);
            var card = Rect(root.transform, "Editor Panel", new Vector2(16f, 16f), new Vector2(580f, 688f));
            MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(card.gameObject).color = Card;
            Label(card, "Heading", "COMBO CONFIGURATOR", 20, 12, 540, 34, 26);
            Label(card, "Intro", "Assemble reusable attacks. Preview, then save to the game.", 20, 54, 540, 26, 15);
            var ground = Button(card, "Ground", "GROUND", 20, 92, 262, 38);
            var air = Button(card, "Air", "AIR / UP", 296, 92, 262, 38);
            var previous = Button(card, "Previous Preset", "<", 20, 146, 36, 36);
            var title = Label(card, "Preset", "Ground Original", 68, 147, 350, 34, 20);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            var next = Button(card, "Next Preset", ">", 430, 146, 36, 36);
            var duplicate = Button(card, "Duplicate", "COPY", 478, 146, 80, 36);
            var pattern = Label(card, "Pattern", "Z  Z  Z", 20, 192, 536, 32, 22); pattern.color = new Color(0.25f, 0.9f, 0.92f);
            var rows = new GameObject[8]; var labels = new TMP_Text[8];
            var prevPiece = new UnityEngine.UI.Button[8]; var nextPiece = new UnityEngine.UI.Button[8];
            var up = new UnityEngine.UI.Button[8]; var down = new UnityEngine.UI.Button[8]; var remove = new UnityEngine.UI.Button[8];
            for (int i = 0; i < rows.Length; i++)
            {
                var row = Rect(card, "Step " + i, new Vector2(20f, 232f + i * 32f), new Vector2(538f, 28f)); rows[i] = row.gameObject;
                labels[i] = Label(row, "Attack", "", 8, 0, 322, 28, 16);
                prevPiece[i] = Button(row, "Previous Piece", "<", 322, 0, 32, 28);
                nextPiece[i] = Button(row, "Next Piece", ">", 359, 0, 32, 28);
                up[i] = Button(row, "Move Up", "UP", 396, 0, 40, 28);
                down[i] = Button(row, "Move Down", "DN", 441, 0, 40, 28);
                remove[i] = Button(row, "Remove", "X", 486, 0, 42, 28);
            }
            var add = Button(card, "Add Step", "+ ADD ATTACK", 20, 494, 538, 32);
            var detail = Label(card, "Details", "", 20, 539, 538, 84, 14);
            var save = Button(card, "Save", "SAVE & USE IN GAME", 20, 633, 538, 36, Accent);
            Label(root.transform, "Preview Title", "PRACTICE DECK", 638, 30, 600, 36, 25);
            Label(root.transform, "Preview Help", "Z attack   /   Up jump   /   A S D skills\nUse the arrows to replace pieces, UP / DN to reorder.", 638, 78, 600, 62, 17);
            var previewStatus = Label(root.transform, "Preview Status", "", 638, 514, 600, 60, 20);
            var preview = Button(root.transform, "Preview Combo", "PLAY COMBO", 638, 598, 288, 42, Accent);
            var reset = Button(root.transform, "Reset Preview", "RESET TARGET", 944, 598, 288, 42);
            var status = Label(root.transform, "Status", "", 638, 662, 600, 42, 15);
            var cameraObject = Find(scene, "Main Camera") ?? new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = MechaPresentationSetup.Ensure<Camera>(cameraObject);
            camera.orthographic = true; camera.orthographicSize = 4.8f; camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.transform.position = new Vector3(0f, 1.65f, -20f);
            camera.transform.rotation = Quaternion.identity; MechaPresentationSetup.Ensure<AudioListener>(cameraObject);
            var lightObject = Find(scene, "Key Light") ?? new GameObject("Key Light");
            var light = MechaPresentationSetup.Ensure<Light>(lightObject); light.type = LightType.Directional; light.intensity = 1.4f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            var floor = Find(scene, "Practice Floor");
            if (floor == null) { floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Practice Floor"; }
            floor.layer = 7; floor.transform.position = new Vector3(2f, -0.2f, 0f); floor.transform.localScale = new Vector3(18f, 0.4f, 3f);
            floor.GetComponent<Renderer>().sharedMaterial = MechaScenarioSetup.Material("PracticeFloor", new Color(0.09f, 0.17f, 0.23f));
            var player = Find(scene, "Practice Pilot");
            if (player == null)
            { player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/MechaPlayer.prefab"), scene); player.name = "Practice Pilot"; }
            player.transform.position = settings.previewSpawn; player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            player.GetComponent<FishNet.Object.NetworkObject>().SetIsNetworked(false);
            var dummy = Find(scene, "Training Target") ?? new GameObject("Training Target");
            dummy.layer = 9; dummy.transform.position = new Vector3(3.15f, 0f, 0f); dummy.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            var collider = MechaPresentationSetup.Ensure<CapsuleCollider>(dummy); collider.height = 3.4f; collider.center = new Vector3(0f, 1.7f, 0f); collider.radius = 0.3f;
            MechaPresentationSetup.Ensure<Rigidbody>(dummy).isKinematic = true;
            var target = MechaPresentationSetup.Ensure<ComboTrainingTarget>(dummy);
            if (dummy.transform.childCount == 0)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/EvaVisual.prefab"), dummy.transform);
                visual.transform.localPosition = Vector3.zero; visual.transform.localRotation = Quaternion.identity;
                var animator = visual.GetComponentInChildren<Animator>();
                animator.runtimeAnimatorController = MechaPresentationSetup.Profile<CharacterAnimationSettings>("EnemyAnimation").controller;
                animator.applyRootMotion = false;
            }
            var events = Find(scene, "EventSystem") ?? new GameObject("EventSystem");
            MechaPresentationSetup.Ensure<UnityEngine.EventSystems.EventSystem>(events);
            MechaPresentationSetup.Ensure<InputSystemUIInputModule>(events).AssignDefaultActions();
            Assign(ui, "settings", settings); Assign(ui, "player", player.GetComponent<PlayerAttack>()); Assign(ui, "target", target);
            Assign(ui, "ground", ground); Assign(ui, "air", air); Assign(ui, "previous", previous); Assign(ui, "next", next);
            Assign(ui, "duplicate", duplicate); Assign(ui, "add", add); Assign(ui, "save", save); Assign(ui, "preview", preview); Assign(ui, "reset", reset);
            Assign(ui, "title", title); Assign(ui, "pattern", pattern); Assign(ui, "detail", detail); Assign(ui, "status", status); Assign(ui, "previewStatus", previewStatus);
            Array(ui, "rows", rows); Array(ui, "rowLabels", labels); Array(ui, "piecePrevious", prevPiece); Array(ui, "pieceNext", nextPiece);
            Array(ui, "moveUp", up); Array(ui, "moveDown", down); Array(ui, "remove", remove);
            var sceneIds = typeof(FishNet.Object.NetworkObject).GetMethod("CreateSceneId", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            sceneIds.Invoke(null, new object[] {scene, false, 0});
            var serialize = typeof(FishNet.Object.NetworkObject).GetMethod("ReserializeEditorSetValues", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            foreach (var obj in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<FishNet.Object.NetworkObject>(true)))
                serialize.Invoke(obj, new object[] {true, true});
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, path);
            var builds = EditorBuildSettings.scenes.ToList();
            if (!builds.Any(s => s.path == path)) { builds.Add(new EditorBuildSettingsScene(path, true)); EditorBuildSettings.scenes = builds.ToArray(); }
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }
    private static GameObject Find(Scene scene, string name) => scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
    private static void Assign(Component c, string field, UnityEngine.Object value) => MechaPresentationSetup.Assign(c, field, value);
    private static void Array(Component c, string field, UnityEngine.Object[] values)
    {
        var so = new SerializedObject(c); var p = so.FindProperty(field); p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static RectTransform Rect(Transform parent, string name, Vector2 offset, Vector2 size)
    {
        var rect = MechaPresentationSetup.Ensure<RectTransform>(MechaScenarioSetup.Child(parent, name));
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = new Vector2(offset.x, -offset.y); rect.sizeDelta = size; return rect;
    }
    private static TMP_Text Label(Transform p, string name, string text, float x, float y, float w, float h, float size)
    {
        var rect = Rect(p, name, new Vector2(x, y), new Vector2(w, h));
        var label = MechaPresentationSetup.Ensure<TextMeshProUGUI>(rect.gameObject); label.font = font; label.text = text;
        label.fontSize = size; label.color = new Color(0.88f, 0.94f, 0.97f); label.raycastTarget = false; label.alignment = TextAlignmentOptions.TopLeft; return label;
    }
    private static UnityEngine.UI.Button Button(Transform p, string name, string text, float x, float y, float w, float h, Color? color = null)
    {
        var rect = Rect(p, name, new Vector2(x, y), new Vector2(w, h));
        var image = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(rect.gameObject); image.color = color ?? new Color(0.11f, 0.18f, 0.25f);
        var button = MechaPresentationSetup.Ensure<UnityEngine.UI.Button>(rect.gameObject); button.targetGraphic = image;
        Label(rect, "Label", text, 2, 0, w - 4, h, h < 30 ? 13 : 16).alignment = TextAlignmentOptions.Center; return button;
    }
}
