using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class VfxTestingAuthoring
{
    public const string Root = "Assets/_Project/VFX/Testing";
    public const string ScenePath = "Assets/Scenes/VfxTesting.unity";
    public const string CatalogPath = Root + "/Profiles/VfxCreatorCatalog.asset";
    private static readonly Color Green = new Color(0.43f, 0.94f, 0.61f);
    private static TMP_FontAsset font;
    private static UiFeelSettings style;

    [MenuItem("Tools/Mecha/VFX/Open testing scene")]
    public static void OpenScene()
    {
        RequireEditMode();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        CreateScene();
        EditorSceneManager.OpenScene(ScenePath);
    }

    public static string CreateScene()
    {
        RequireEditMode();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return ScenePath;
        Folder(Root + "/Profiles/Entries");
        Folder(Root + "/Prefabs");
        Folder(Root + "/Materials");
        var catalog = EnsureCatalog();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Localization/Fonts/EnglishDefault.asset");
        style = AssetDatabase.LoadAssetAtPath<UiFeelSettings>(Root + "/Profiles/BrowserFeel.asset");
        if (style == null)
        {
            var original = AssetDatabase.LoadAssetAtPath<UiFeelSettings>("Assets/_Project/Settings/Interface/UiFeel.asset");
            style = UnityEngine.Object.Instantiate(original);
            style.name = "VFX browser feel";
            style.focusColor = Green;
            AssetDatabase.CreateAsset(style, Root + "/Profiles/BrowserFeel.asset");
            LabelAsset(style, "Authored Data");
        }
        CaptureText();
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var cameraObject = new GameObject("Preview camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.transform.position = new Vector3(0f, 2.1f, -12f);
            camera.orthographic = true; camera.orthographicSize = 4.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.05f);
            camera.rect = new Rect(0.29f, 0f, 0.71f, 1f);
            camera.allowHDR = true;
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var light = new GameObject("Stage light", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1f;
            light.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.22f, 0.26f);

            var floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Stage.mat");
            if (floorMaterial == null)
            {
                floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                floorMaterial.SetColor("_BaseColor", new Color(0.13f, 0.15f, 0.18f));
                floorMaterial.SetFloat("_Smoothness", 0.22f);
                AssetDatabase.CreateAsset(floorMaterial, Root + "/Materials/Stage.mat");
                LabelAsset(floorMaterial, "Others");
            }
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Preview floor";
            floor.transform.position = new Vector3(0f, -0.2f, 1f);
            floor.transform.localScale = new Vector3(16f, 0.4f, 8f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root + "/Profiles/PreviewVolume.asset");
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, Root + "/Profiles/PreviewVolume.asset");
                var bloom = profile.Add<Bloom>();
                bloom.intensity.Override(0.25f); bloom.threshold.Override(1f);
                AssetDatabase.AddObjectToAsset(bloom, profile);
                EditorUtility.SetDirty(profile); LabelAsset(profile, "Authored Data");
            }
            var volume = new GameObject("Preview volume", typeof(Volume)).GetComponent<Volume>();
            volume.isGlobal = true; volume.sharedProfile = profile;
            var origin = new GameObject("Effect origin").transform;
            origin.position = new Vector3(0f, 1f, 0f);
            var player = new GameObject("Preview player", typeof(VfxPreviewPlayer)).GetComponent<VfxPreviewPlayer>();
            Assign(player, "origin", origin);

            var canvas = new GameObject("Diagnostics canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f); scaler.matchWidthOrHeight = 0f;
            var scope = canvas.AddComponent<UiNavigationScope>();
            var browser = canvas.AddComponent<VfxTestingBrowser>();
            var panel = Rect(canvas.transform, "Effect browser", Vector2.zero, new Vector2(448f, 900f));
            panel.anchorMin = Vector2.zero; panel.anchorMax = new Vector2(0f, 1f); panel.pivot = new Vector2(0f, 0.5f);
            panel.sizeDelta = new Vector2(448f, 0f); panel.anchoredPosition = Vector2.zero;
            Background(panel, new Color(0.035f, 0.05f, 0.065f));
            var accent = Rect(panel, "Accent", new Vector2(446f, 0f), new Vector2(2f, 900f));
            accent.anchorMin = Vector2.zero; accent.anchorMax = new Vector2(0f, 1f); accent.pivot = new Vector2(0f, 0.5f);
            accent.sizeDelta = new Vector2(2f, 0f); accent.anchoredPosition = new Vector2(446f, 0f);
            Background(accent, new Color(0.22f, 0.38f, 0.3f));
            Label(panel, "Title", "vfx.title", "VFX testing", 28, 30, 390, 48, 34);
            Label(panel, "Subtitle", "vfx.subtitle", "Effects created with VFX Creator", 30, 86, 388, 54, 18);
            var refresh = Button(panel, "Refresh list", "vfx.refresh", "Refresh list", 30, 150, 386, 46);
            var count = Label(panel, "Count", null, "", 30, 215, 386, 32, 17);

            var scrollRoot = Rect(panel, "Effect list", new Vector2(30f, -266f), new Vector2(386f, 430f));
            scrollRoot.anchorMin = new Vector2(0f, 0f); scrollRoot.anchorMax = new Vector2(1f, 1f);
            scrollRoot.offsetMin = new Vector2(30f, 203f); scrollRoot.offsetMax = new Vector2(-30f, -266f);
            var scroll = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            var viewport = Rect(scrollRoot, "Viewport", Vector2.zero, Vector2.zero); Stretch(viewport);
            Background(viewport, Color.white);
            var mask = viewport.gameObject.AddComponent<UnityEngine.UI.Mask>(); mask.showMaskGraphic = false;
            var content = Rect(viewport, "Content", Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1f);
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 12f; layout.padding = new RectOffset(7, 7, 7, 7);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35f;
            scrollRoot.gameObject.AddComponent<UiSelectionScroll>();
            var empty = Label(panel, "Empty list", "vfx.empty.list", "No effects registered yet", 52, 300, 344, 120, 23);
            empty.alignment = TextAlignmentOptions.TopLeft;
            var replay = Button(panel, "Replay", "vfx.replay", "Replay", 30, 734, 186, 52);
            var stop = Button(panel, "Stop", "vfx.stop", "Stop", 230, 734, 186, 52);
            Bottom((RectTransform)replay.transform, 30f, 100f);
            Bottom((RectTransform)stop.transform, 230f, 100f);
            replay.interactable = stop.interactable = false;
            Label(panel, "Browser footer", "vfx.browser.hint", "Select an effect, then replay it.", 30, 810, 386, 62, 17);
            var footer = (RectTransform)panel.Find("Browser footer"); Bottom(footer, 30f, 25f);

            var info = Rect(canvas.transform, "Preview information", new Vector2(490f, -32f), new Vector2(1060f, 190f));
            info.anchorMin = new Vector2(0.29f, 1f); info.anchorMax = Vector2.one;
            info.offsetMin = new Vector2(28f, -190f); info.offsetMax = new Vector2(-28f, -28f);
            var selectedName = Label(info, "Selected effect", null, "", 0, 0, 960, 58, 34);
            var detail = Label(info, "Effect description", null, "", 0, 65, 940, 95, 20);
            var status = Label(canvas.transform, "Playback status", null, "", 500, 750, 960, 42, 20);
            var statusRect = (RectTransform)status.transform;
            statusRect.anchorMin = statusRect.anchorMax = new Vector2(0.29f, 0f); statusRect.pivot = Vector2.zero;
            statusRect.anchoredPosition = new Vector2(28f, 106f);
            var hint = Label(canvas.transform, "Input hints", "vfx.controls", "Arrows / D-pad: browse | Enter / confirm: play\nSpace: replay | Esc / back: stop", 500, 810, 1020, 70, 19);
            var hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = new Vector2(0.29f, 0f); hintRect.anchorMax = new Vector2(1f, 0f); hintRect.pivot = Vector2.zero;
            hintRect.anchoredPosition = new Vector2(28f, 22f); hintRect.sizeDelta = new Vector2(-56f, 70f);

            var rowPrefab = CreateRow();
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = events.GetComponent<InputSystemUIInputModule>(); module.AssignDefaultActions();
            var input = UnityEngine.Object.Instantiate(module.actionsAsset);
            input.name = "VFX testing input";
            var submit = input.FindAction("UI/Submit", true);
            while (submit.bindings.Count > 0) submit.ChangeBinding(0).Erase();
            submit.AddBinding("<Keyboard>/enter"); submit.AddBinding("<Gamepad>/buttonSouth");
            var preview = input.AddActionMap("Preview");
            preview.AddAction("Replay", InputActionType.Button, "<Keyboard>/space");
            var cancel = preview.AddAction("Stop", InputActionType.Button, "<Keyboard>/escape");
            cancel.AddBinding("<Gamepad>/buttonEast");
            AssetDatabase.CreateAsset(input, Root + "/Profiles/PreviewInput.asset");
            module.actionsAsset = input;
            module.point = ActionReference(input, "UI/Point");
            module.leftClick = ActionReference(input, "UI/Click");
            module.rightClick = ActionReference(input, "UI/RightClick");
            module.middleClick = ActionReference(input, "UI/MiddleClick");
            module.scrollWheel = ActionReference(input, "UI/ScrollWheel");
            module.move = ActionReference(input, "UI/Navigate");
            module.submit = ActionReference(input, "UI/Submit");
            module.cancel = ActionReference(input, "UI/Cancel");
            module.trackedDevicePosition = ActionReference(input, "UI/TrackedDevicePosition");
            module.trackedDeviceOrientation = ActionReference(input, "UI/TrackedDeviceOrientation");
            LabelAsset(input, "Authored Data");
            Assign(browser, "catalog", catalog); Assign(browser, "player", player); Assign(browser, "rowPrefab", rowPrefab);
            Assign(browser, "content", content); Assign(browser, "scroll", scroll); Assign(browser, "navigationScope", scope);
            Assign(browser, "replay", replay); Assign(browser, "stop", stop); Assign(browser, "refresh", refresh);
            Assign(browser, "selectedName", selectedName); Assign(browser, "description", detail);
            Assign(browser, "count", count); Assign(browser, "status", status); Assign(browser, "emptyMessage", empty.gameObject);
            Assign(browser, "actions", input);
            MechaLocalizationAuthoring.LocalizeRoot(canvas);
            EditorSceneManager.SaveScene(scene, ScenePath);
            LabelAsset(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), "Scenes");
            AssetDatabase.SaveAssets();
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
        return ScenePath;
    }

    // Call only for a project effect newly created/adapted with VFX Creator.
    public static string Register(string prefabPath, string effectKey, string englishName, string englishDescription, float playSeconds, float tailSeconds)
    {
        RequireEditMode();
        if (!prefabPath.StartsWith("Assets/_Project/VFX/", StringComparison.Ordinal) || !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Register a project-owned VFX prefab.");
        if (!Regex.IsMatch(effectKey, @"^[a-z][a-z0-9]*\.[a-z][a-z0-9]*$"))
            throw new ArgumentException("Use two concise key words, such as energy.impact.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null || string.IsNullOrWhiteSpace(englishName) || string.IsNullOrWhiteSpace(englishDescription) ||
            !float.IsFinite(playSeconds) || !float.IsFinite(tailSeconds) || playSeconds < 0.05f || tailSeconds < 0f)
            throw new ArgumentException("Supply a prefab, localized wording and finite preview/tail durations.");
        var catalog = EnsureCatalog();
        string path = Root + "/Profiles/Entries/" + effectKey + ".asset";
        var definition = AssetDatabase.LoadAssetAtPath<VfxPreviewDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<VfxPreviewDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }
        Undo.RecordObject(definition, "Register VFX preview");
        definition.prefab = prefab;
        definition.displayName = MechaLocalizationAuthoring.Capture("Diagnostics", effectKey, englishName);
        definition.description = MechaLocalizationAuthoring.Capture("Diagnostics", effectKey + ".detail", englishDescription);
        definition.playSeconds = playSeconds; definition.tailSeconds = tailSeconds;
        EditorUtility.SetDirty(definition);
        if (catalog.effects == null || !catalog.effects.Contains(definition))
        {
            Undo.RecordObject(catalog, "Register VFX preview");
            catalog.effects = (catalog.effects ?? Array.Empty<VfxPreviewDefinition>()).Concat(new[] { definition }).ToArray();
            EditorUtility.SetDirty(catalog);
        }
        LabelAsset(definition, "Authored Data", "VFXCreator");
        LabelAsset(prefab, "Prefabs", "VFXCreator");
        AssetDatabase.SaveAssets();
        return path;
    }

    private static VfxPreviewCatalog EnsureCatalog()
    {
        Folder(Root + "/Profiles/Entries");
        var catalog = AssetDatabase.LoadAssetAtPath<VfxPreviewCatalog>(CatalogPath);
        if (catalog != null) return catalog;
        catalog = ScriptableObject.CreateInstance<VfxPreviewCatalog>();
        AssetDatabase.CreateAsset(catalog, CatalogPath); LabelAsset(catalog, "Authored Data");
        return catalog;
    }

    private static VfxBrowserRow CreateRow()
    {
        string path = Root + "/Prefabs/VfxBrowserRow.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<VfxBrowserRow>(path);
        if (existing != null) return existing;
        var button = Button(null, "Effect row", null, "", 0, 0, 372, 64);
        button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 64f;
        var row = button.gameObject.AddComponent<VfxBrowserRow>();
        var marker = Rect(button.transform, "Selected marker", new Vector2(0f, -8f), new Vector2(4f, 48f));
        var image = Background(marker, Green); image.enabled = false;
        Assign(row, "button", button); Assign(row, "label", button.GetComponentInChildren<TMP_Text>()); Assign(row, "selectedMarker", image);
        MechaLocalizationAuthoring.LocalizeRoot(button.gameObject);
        var prefab = PrefabUtility.SaveAsPrefabAsset(button.gameObject, path);
        UnityEngine.Object.DestroyImmediate(button.gameObject);
        LabelAsset(prefab, "Prefabs");
        return prefab.GetComponent<VfxBrowserRow>();
    }

    private static void CaptureText()
    {
        Text("vfx.count", "{0} registered effects");
        Text("vfx.none", "Your next effect starts here");
        Text("vfx.empty.detail", "Create an effect with VFX Creator to add it to this browser.\nChoose it from the list, then press Space to replay.");
        Text("vfx.empty.status", "Waiting for the first effect");
        Text("vfx.playing", "Playing"); Text("vfx.ready", "Ready to replay");
    }

    private static LocalizedString Text(string key, string english) => MechaLocalizationAuthoring.Capture("Diagnostics", key, english);
    private static TMP_Text Label(Transform parent, string name, string key, string english, float x, float y, float w, float h, float size)
    {
        var rect = Rect(parent, name, new Vector2(x, -y), new Vector2(w, h));
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = size; text.text = english;
        text.color = new Color(0.87f, 0.91f, 0.9f); text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.TopLeft; text.textWrappingMode = TextWrappingModes.Normal;
        if (key != null)
        {
            var binding = rect.gameObject.AddComponent<LocalizeStringEvent>(); binding.StringReference = Text(key, english);
            UnityEventTools.AddPersistentListener(binding.OnUpdateString, (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), text, "set_text"));
            binding.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
        }
        return text;
    }

    private static UnityEngine.UI.Button Button(Transform parent, string name, string key, string english, float x, float y, float w, float h)
    {
        var rect = Rect(parent, name, new Vector2(x, -y), new Vector2(w, h));
        var image = Background(rect, new Color(0.08f, 0.13f, 0.12f)); image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(0.74f, 1f, 0.83f); colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f); button.colors = colors;
        var label = Label(rect, "Label", key, english, 18, 0, w - 36, h, 20);
        Stretch((RectTransform)label.transform); label.margin = new Vector4(18f, 0f, 18f, 0f); label.alignment = TextAlignmentOptions.MidlineLeft;
        MechaUiFeelSetup.Button(button, style);
        return button;
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    private static UnityEngine.UI.Image Background(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; return image;
    }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static void Bottom(RectTransform rect, float x, float y) { rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero; rect.anchoredPosition = new Vector2(x, y); }
    private static InputActionReference ActionReference(InputActionAsset input, string path)
    {
        var reference = InputActionReference.Create(input.FindAction(path, true));
        reference.name = path.Replace('/', ' ');
        AssetDatabase.AddObjectToAsset(reference, input);
        return reference;
    }
    private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/'); Folder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
    private static void LabelAsset(UnityEngine.Object asset, string category, params string[] additional)
    {
        var labels = AssetDatabase.GetLabels(asset).Concat(new[] { category }).Concat(additional).ToList();
        if (!labels.Contains("Keep") && !labels.Contains("Replace")) labels.Add("Keep");
        AssetDatabase.SetLabels(asset, labels.Distinct().ToArray());
    }
    private static void RequireEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring VFX previews.");
    }
}
