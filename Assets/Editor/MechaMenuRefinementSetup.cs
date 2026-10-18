using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public static class MechaMenuRefinementSetup
{
    private static Sprite frame;
    private static TMP_FontAsset font;
    [MenuItem("Tools/Mecha/Apply Controller Menu and Portal Refinements")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring the interface.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/MainScene.unity") throw new InvalidOperationException("Open MainScene first.");
        frame = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        BuildOptions(); AudioNavigation(); LobbyNavigation(); Portal(); Audio(); Input();
        Finish();
    }
    [MenuItem("Tools/Mecha/Apply Configuration and Combat Controls")]
    public static void ApplyConfiguration()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring the interface.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/MainScene.unity") throw new InvalidOperationException("Open MainScene first.");
        frame = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        BuildOptions(); AudioNavigation(); LobbyNavigation(); Input(); Finish();
    }
    [MenuItem("Tools/Mecha/Refine Configuration Submenus")]
    public static void RefineConfiguration()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring the interface.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/MainScene.unity") throw new InvalidOperationException("Open MainScene first.");
        frame = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        BuildOptions(); AudioNavigation(); LobbyNavigation();
        foreach (var collection in UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollections())
        {
            foreach (var table in collection.StringTables)
                foreach (var entry in table.Values)
                {
                    string value = entry.LocalizedValue;
                    if (value == "BACK TO OPTIONS") entry.Value = "BACK";
                    else if (value == "LOBBY / SQUAD") entry.Value = "LOBBY";
                    else if (value.StartsWith("Refresh rate applies to exclusive fullscreen.")) entry.Value = "V-sync uses the display rate. Texture quality controls texture resolution.\nD-pad / stick: select   •   Bottom button: open / confirm   •   Right button / Esc: back";
                    else continue;
                    EditorUtility.SetDirty(table);
                }
        }
        var lobby = UnityEngine.Object.FindFirstObjectByType<MechaLobbyPanel>();
        var close = (UnityEngine.UI.Button)new SerializedObject(lobby).FindProperty("close").objectReferenceValue;
        close.GetComponentInChildren<TMP_Text>(true).GetComponent<UnityEngine.Localization.Components.LocalizeStringEvent>().StringReference = MechaLocalizationAuthoring.Capture("Lobby", "navigation.back", "BACK");
        CaptureVideoText();
        foreach (var binding in UnityEngine.Object.FindObjectsByType<UnityEngine.Localization.Components.LocalizeStringEvent>(FindObjectsInactive.Include, FindObjectsSortMode.None)) binding.RefreshString();
        EditorUtility.SetDirty(MechaPresentationSetup.Profile<VideoConfigurationSettings>("VideoConfiguration"));
        MechaLocalizationAuthoring.LocalizeScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Finish();
    }
    private static void CaptureVideoText()
    {
        MechaLocalizationAuthoring.Capture("Settings", "antialiasing.off", "OFF");
        MechaLocalizationAuthoring.Capture("Settings", "antialiasing.fxaa", "FXAA");
        MechaLocalizationAuthoring.Capture("Settings", "antialiasing.smaa", "SMAA");
        MechaLocalizationAuthoring.Capture("Settings", "antialiasing.msaa2", "MSAA 2×");
        MechaLocalizationAuthoring.Capture("Settings", "antialiasing.msaa4", "MSAA 4×");
        MechaLocalizationAuthoring.Capture("Settings", "antialiasing.msaa8", "MSAA 8×");
        MechaLocalizationAuthoring.Capture("Settings", "texture.full", "FULL");
        MechaLocalizationAuthoring.Capture("Settings", "texture.half", "HALF");
        MechaLocalizationAuthoring.Capture("Settings", "texture.quarter", "QUARTER");
        MechaLocalizationAuthoring.Capture("Settings", "texture.eighth", "EIGHTH");
        MechaLocalizationAuthoring.Capture("Settings", "language.english", "English");
    }
    private static void Finish()
    {
        var style = MechaPresentationSetup.Profile<UiFeelSettings>("UiFeel");
        foreach (var control in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            MechaUiFeelSetup.Button(control, style);
            MechaPresentationSetup.Ensure<UiAudioFeedback>(control.gameObject);
        }
        SaveAssembly(GameObject.Find("Audio Configuration"), "Assets/_Project/Prefabs/Interface/AudioConfiguration.prefab");
        SaveAssembly(GameObject.Find("Game Options"), "Assets/_Project/Prefabs/Interface/GameOptions.prefab");
        var lobbyRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(UnityEngine.Object.FindFirstObjectByType<MechaLobbyPanel>());
        if (lobbyRoot != null) PrefabUtility.ApplyPrefabInstance(lobbyRoot, InteractionMode.AutomatedAction);
        AssetDatabase.SaveAssets();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("Controller-first configuration submenus and lobby navigation saved.");
    }
    private static void BuildOptions()
    {
        var root = GameObject.Find("Game Options") ?? new GameObject("Game Options", typeof(RectTransform));
        var canvas = MechaPresentationSetup.Ensure<Canvas>(root); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 310;
        var scaler = MechaPresentationSetup.Ensure<UnityEngine.UI.CanvasScaler>(root);
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280f, 720f); scaler.matchWidthOrHeight = 0.5f;
        MechaPresentationSetup.Ensure<UnityEngine.UI.GraphicRaycaster>(root);
        var menu = MechaPresentationSetup.Ensure<GameOptionsMenu>(root);
        var home = Overlay(root.transform, "Options Home"); var homeCard = Card(home, "Options", new Vector2(580f, 644f));
        Label(homeCard, "Title", "CONFIGURATION", new Vector2(40f, 28f), new Vector2(500f, 46f), 30f);
        Label(homeCard, "Subtitle", "CHOOSE A SCREEN", new Vector2(40f, 82f), new Vector2(500f, 28f), 16f);
        var resume = Button(homeCard, "Resume", "RESUME GAME", new Vector2(40f, 132f), new Vector2(500f, 64f));
        var lobby = Button(homeCard, "Lobby", "LOBBY", new Vector2(40f, 214f), new Vector2(500f, 64f));
        var audio = Button(homeCard, "Audio", "AUDIO", new Vector2(40f, 296f), new Vector2(500f, 64f));
        var video = Button(homeCard, "Video", "VIDEO", new Vector2(40f, 378f), new Vector2(500f, 64f));
        var general = Button(homeCard, "General", "GENERAL", new Vector2(40f, 460f), new Vector2(500f, 64f));
        Chain(new UnityEngine.UI.Selectable[] { resume, lobby, audio, video, general });
        Label(homeCard, "Help", "D-pad / stick: select   •   Bottom button: confirm\nRight button / Esc: back", new Vector2(40f, 566f), new Vector2(500f, 48f), 14f);
        var videoPanel = Overlay(root.transform, "Video Settings");
        var videoSettings = MechaVideoSetup.Build(videoPanel);
        var back = videoPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "Back");
        Assign(menu, "home", home.gameObject); Assign(menu, "video", videoPanel.gameObject);
        Assign(menu, "audio", UnityEngine.Object.FindFirstObjectByType<AudioConfigurationMenu>()); Assign(menu, "videoSettings", videoSettings);
        Assign(menu, "resume", resume); Assign(menu, "lobby", lobby); Assign(menu, "audioButton", audio); Assign(menu, "videoButton", video); Assign(menu, "videoBack", back);
        Assign(menu, "lobbyPanel", UnityEngine.Object.FindFirstObjectByType<MechaLobbyPanel>());
        var generalPanel = Overlay(root.transform, "General Settings");
        var generalCard = Card(generalPanel, "General", new Vector2(900f, 330f));
        Label(generalCard, "Title", "GENERAL", new Vector2(40f, 22f), new Vector2(820f, 44f), 30f);
        var language = MechaVideoSetup.Dropdown(generalCard, "Language", "LANGUAGE", 112f);
        var generalBack = Button(generalCard, "Back", "BACK", new Vector2(40f, 240f), new Vector2(820f, 48f));
        var generalMenu = MechaPresentationSetup.Ensure<GeneralConfigurationMenu>(generalPanel.gameObject);
        Assign(generalMenu, "language", language); Assign(generalMenu, "back", generalBack);
        Assign(menu, "general", generalPanel.gameObject); Assign(menu, "generalSettings", generalMenu);
        Assign(menu, "generalButton", general); Assign(menu, "generalBack", generalBack);
        generalPanel.gameObject.SetActive(false);
        home.gameObject.SetActive(false); videoPanel.gameObject.SetActive(false);
    }
    private static void AudioNavigation()
    {
        var audio = UnityEngine.Object.FindFirstObjectByType<AudioConfigurationMenu>();
        var so = new SerializedObject(audio);
        var sliders = Enumerable.Range(0, so.FindProperty("sliders").arraySize).Select(i => (UnityEngine.UI.Selectable)so.FindProperty("sliders").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        foreach (var slider in sliders.Cast<UnityEngine.UI.Slider>()) slider.handleRect.sizeDelta = new Vector2(20f, 0f);
        var defaults = (UnityEngine.UI.Button)so.FindProperty("defaults").objectReferenceValue;
        var oldLobby = audio.transform.Find("Audio Panel/Settings Card/Lobby"); if (oldLobby != null) UnityEngine.Object.DestroyImmediate(oldLobby.gameObject);
        var close = (UnityEngine.UI.Button)so.FindProperty("close").objectReferenceValue;
        Chain(sliders.Concat(new UnityEngine.UI.Selectable[] { defaults, close }).ToArray());
        var buttons = new[] { defaults, close };
        for (int i = 0; i < buttons.Length; i++)
        {
            var n = buttons[i].navigation;
            n.selectOnLeft = buttons[(i + 1) % 2]; n.selectOnRight = buttons[(i + 1) % 2]; buttons[i].navigation = n;
        }
        Rect(defaults.transform.parent, "Defaults", new Vector2(44f, 542f), new Vector2(300f, 44f));
        Rect(close.transform.parent, "Close", new Vector2(366f, 542f), new Vector2(350f, 44f));
        close.GetComponentInChildren<TMP_Text>(true).text = "BACK";
        var help = audio.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Help");
        help.text = "0% mutes a layer. SFX includes combat, movement and destruction.\nUp / Down: select   •   Left / Right: adjust   •   Right button / Esc: back";
    }
    private static void LobbyNavigation()
    {
        var lobby = UnityEngine.Object.FindFirstObjectByType<MechaLobbyPanel>();
        var lobbyData = new SerializedObject(lobby);
        var host = (UnityEngine.UI.Button)lobbyData.FindProperty("localHost").objectReferenceValue;
        host.GetComponentInChildren<TMP_Text>(true).text = "SOLO HOST";
        var close = (UnityEngine.UI.Button)lobbyData.FindProperty("close").objectReferenceValue;
        close.GetComponentInChildren<TMP_Text>(true).text = "BACK";
        var panel = (GameObject)lobbyData.FindProperty("panel").objectReferenceValue;
        var scope = MechaPresentationSetup.Ensure<UiNavigationScope>(panel);
        Assign(scope, "excludedControls", panel.transform.Find("Controller Keyboard"));
        bool active = panel.activeSelf; panel.SetActive(true); Canvas.ForceUpdateCanvases(); scope.Rebuild(); panel.SetActive(active);
    }
    private static void Portal()
    {
        var profile = MechaPresentationSetup.Profile<ProceduralLevelSettings>("ProceduralLevels");
        profile.portalHoldDuration = 0.9f;
        profile.portalProgressSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Interface/HollowRing.png");
        EditorUtility.SetDirty(profile);
        var lance = MechaPresentationSetup.Profile<EnemyLanceSettings>("EnemyLance");
        lance.playerImpactLifetime = 0.8f; EditorUtility.SetDirty(lance);
        var hud = UnityEngine.Object.FindFirstObjectByType<LevelObjectiveHud>();
        var holder = Rect(hud.transform, "Portal Hold", Vector2.zero, profile.portalProgressSize);
        holder.anchorMin = holder.anchorMax = holder.pivot = new Vector2(0.5f, 0f); holder.anchoredPosition = profile.portalProgressOffset;
        var track = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(holder.gameObject);
        track.sprite = profile.portalProgressSprite; track.type = UnityEngine.UI.Image.Type.Simple; track.color = new Color(0.15f, 0.3f, 0.34f, 0.8f); track.raycastTarget = false;
        var fill = Rect(holder, "Progress", Vector2.zero, Vector2.zero); Stretch(fill);
        var image = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(fill.gameObject);
        image.sprite = profile.portalProgressSprite; image.type = UnityEngine.UI.Image.Type.Filled;
        image.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360; image.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
        image.fillClockwise = true; image.fillAmount = 0f; image.color = profile.portalColor; image.raycastTarget = false;
        var hint = Label(holder, "Button", "Z", Vector2.zero, Vector2.zero, 24f); Stretch(hint.rectTransform); hint.alignment = TextAlignmentOptions.Center;
        Assign(hud, "portalHold", holder.gameObject); Assign(hud, "portalFill", image); Assign(hud, "portalButton", hint);
        holder.gameObject.SetActive(false);
    }
    private static void Audio()
    {
        var profile = MechaPresentationSetup.Profile<GameAudioSettings>("GameAudio");
        profile.menuMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Music/Sci-Fi 1 Loop.wav");
        profile.musicPlaylist = new[] { "Sci-Fi 2 Loop.wav", "Sci-Fi 4 Loop.wav", "Sci-Fi 7 Loop.wav" }.Select(f => ImportClip(f, "Music", true)).ToArray();
        profile.musicGain = 0.6f;
        foreach (var cue in profile.cues)
        {
            if (cue.id == GameAudioCue.MegaSlash || cue.id == GameAudioCue.CriticalX) { cue.volume = 0.8f; cue.spatialBlend = 0.45f; }
            if (cue.id == GameAudioCue.SwordFire) { cue.volume = 0.75f; cue.pitch = new Vector2(0.85f, 0.97f); cue.spatialBlend = 0.45f; }
            if (cue.id == GameAudioCue.SkillPulse) cue.volume = 0.38f;
        }
        var cues = profile.cues.ToList();
        Impact(GameAudioCue.MegaSlashImpact, "sci-fi_weapon_blaster_laser_boom_heavy_01.wav", 0.85f);
        Impact(GameAudioCue.SwordFireImpact, "explosion_small_short_01.wav", 0.8f);
        Impact(GameAudioCue.CriticalXImpact, "sci-fi_shield_power_deflect_boom_01.wav", 0.85f);
        void Impact(GameAudioCue id, string file, float gain)
        {
            var cue = cues.FirstOrDefault(c => c.id == id);
            if (cue == null) { cue = new GameAudioSettings.Cue { id = id }; cues.Add(cue); }
            cue.clips = new[] { ImportClip(file, "Combat/Skills") }; cue.volume = gain; cue.protect = true;
            cue.priority = 8; cue.pitch = new Vector2(0.88f, 1f); cue.spatialBlend = 0.45f;
        }
        profile.cues = cues.ToArray(); EditorUtility.SetDirty(profile);
    }
    private static AudioClip ImportClip(string file, string category, bool music = false)
    {
        var paths = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(file) + " t:AudioClip").Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetFileName(p) == file).ToArray();
        if (paths.Length != 1) throw new InvalidOperationException("Ambiguous/missing clip: " + file);
        string path = paths[0], folder = "Assets/_Project/Audio/" + category;
        if (!path.StartsWith("Assets/_Project/Audio/", StringComparison.Ordinal))
        {
            MechaPresentationSetup.Folder(folder); string destination = folder + "/" + file;
            string guid = AssetDatabase.AssetPathToGUID(path);
            string error = AssetDatabase.MoveAsset(path, destination); if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            if (AssetDatabase.AssetPathToGUID(destination) != guid) throw new InvalidOperationException("GUID changed: " + path);
            File.AppendAllText("Docs/AudioClipOrigins.csv", $"\"{path}\",\"{destination}\",\"Supplied / imported; upstream AI origin unverified\",\"Placeholder\"{Environment.NewLine}");
            path = destination;
        }
        var importer = (AudioImporter)AssetImporter.GetAtPath(path); var sample = importer.defaultSampleSettings;
        sample.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        sample.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
        sample.preloadAudioData = !music; sample.quality = music ? 0.8f : 1f;
        importer.defaultSampleSettings = sample; importer.forceToMono = !music; importer.loadInBackground = music; importer.SaveAndReimport();
        AssetDatabase.SetLabels(AssetDatabase.LoadMainAssetAtPath(path), new[] { "Placeholder" });
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    private static void Input()
    {
        const string path = "Assets/_Project/Inputs/InputSystem_Actions.inputactions";
        var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path); var clone = UnityEngine.Object.Instantiate(source);
        try { MechaControllerSetup.ConfigureCombatBindings(clone); var action = clone.FindAction("Player/Interact", true); for (int i = action.bindings.Count - 1; i >= 0; i--) action.ChangeBinding(i).Erase(); File.WriteAllText(path, clone.ToJson()); }
        finally { UnityEngine.Object.DestroyImmediate(clone); }
        AssetDatabase.ImportAsset(path);
        var profile = MechaPresentationSetup.Profile<ControllerInterfaceSettings>("ControllerInterface");
        profile.controllerHelp = profile.controllerHelp.Replace("{interact} Portal", "HOLD {attack} Portal").Replace("{attack} Combo", "{attack} Light attack").Replace("{tech} Recall", "{tech} Heavy attack").Replace("{skill1} / {skill2} / {skill3} Skills", "{skill1} 25 MP / {skill2} 40 MP / {skill3} 60 MP"); EditorUtility.SetDirty(profile);
        foreach (var label in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (label.text.Contains("PORTAL E / UP")) { label.text = label.text.Replace("PORTAL E / UP", "PORTAL HOLD Z / X"); EditorUtility.SetDirty(label); }
    }
    private static void SaveAssembly(GameObject root, string path)
    {
        MechaPresentationSetup.Folder(Path.GetDirectoryName(path).Replace('\\', '/'));
        if (PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.ApplyPrefabInstance(root, InteractionMode.AutomatedAction);
        else PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
    }
    private static void Chain(UnityEngine.UI.Selectable[] controls)
    {
        for (int i = 0; i < controls.Length; i++) controls[i].navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
            selectOnUp = controls[(i + controls.Length - 1) % controls.Length], selectOnDown = controls[(i + 1) % controls.Length] };
    }
    private static void Assign(Component component, string field, UnityEngine.Object value) => MechaPresentationSetup.Assign(component, field, value);
    private static RectTransform Rect(Transform parent, string name, Vector2 offset, Vector2 size)
    {
        var child = parent.Find(name); var rect = child != null ? child.GetComponent<RectTransform>() : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        if (rect.parent != parent) rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = new Vector2(offset.x, -offset.y); rect.sizeDelta = size; return rect;
    }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static UnityEngine.UI.Image Image(RectTransform rect, Color color, bool raycast = false)
    { var image = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(rect.gameObject); image.sprite = frame; image.type = UnityEngine.UI.Image.Type.Sliced; image.color = color; image.raycastTarget = raycast; return image; }
    private static RectTransform Overlay(Transform parent, string name)
    { var rect = Rect(parent, name, Vector2.zero, Vector2.zero); Stretch(rect); Image(rect, new Color(0.005f, 0.012f, 0.023f, 0.94f), true); return rect; }
    private static RectTransform Card(Transform parent, string name, Vector2 size)
    {
        var rect = Rect(parent, name, Vector2.zero, size); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f; rect.anchoredPosition = Vector2.zero;
        Image(rect, new Color(0.025f, 0.05f, 0.085f)); return rect;
    }
    private static TMP_Text Label(Transform parent, string name, string text, Vector2 position, Vector2 size, float fontSize)
    {
        var label = MechaPresentationSetup.Ensure<TextMeshProUGUI>(Rect(parent, name, position, size).gameObject);
        label.font = font; label.text = text; label.fontSize = fontSize; label.color = new Color(0.88f, 0.95f, 0.99f); label.raycastTarget = false; return label;
    }
    private static UnityEngine.UI.Button Button(Transform parent, string name, string text, Vector2 position, Vector2 size)
    {
        var rect = Rect(parent, name, position, size); var image = Image(rect, new Color(0.1f, 0.23f, 0.3f), true);
        var button = MechaPresentationSetup.Ensure<UnityEngine.UI.Button>(rect.gameObject); button.targetGraphic = image;
        var label = Label(rect, "Label", text, Vector2.zero, size, 20f); label.alignment = TextAlignmentOptions.Center; return button;
    }
}
