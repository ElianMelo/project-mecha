using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

// Repeatable native authoring. Mixer structure is created in Unity's Audio Mixer window.
public static class MechaAudioSetup
{
    private const string Root = "Assets/_Project/Audio";
    private static readonly List<string> Origins = new List<string>();
    private static readonly HashSet<string> Imported = new HashSet<string>();
    private static Sprite sprite;
    private static TMP_FontAsset font;

    [MenuItem("Tools/Mecha/Audio/Implement Audio")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring audio.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainScene.unity") throw new InvalidOperationException("Open MainScene first.");
        MechaPresentationSetup.Folder(Root);
        var profile = MechaPresentationSetup.Create<GameAudioSettings>("GameAudio");
        if (profile.cues.Length == 0) AuthorCues(profile);
        Connect(profile);
        var root = GameObject.Find("Game Audio") ?? new GameObject("Game Audio");
        MechaPresentationSetup.Assign(MechaPresentationSetup.Ensure<GameAudio>(root), "settings", profile);
        AddActorFootsteps();
        BuildMenu();
        foreach (var control in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            MechaPresentationSetup.Ensure<UiAudioFeedback>(control.gameObject);
        // Native voice virtualization retains headroom for four players, enemies and complete skills.
        var audioManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset").FirstOrDefault();
        if (audioManager != null)
        {
            var data = new SerializedObject(audioManager);
            var real = data.FindProperty("m_RealVoiceCount"); var virtualVoices = data.FindProperty("m_VirtualVoiceCount");
            if (real != null) real.intValue = Math.Max(real.intValue, 128);
            if (virtualVoices != null) virtualVoices.intValue = Math.Max(virtualVoices.intValue, 512);
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("Mecha audio authored: " + profile.cues.Length + " cues, five volume layers, native pooled sources and Esc menu. Mixer connected: " + (profile.mixer != null));
    }

    private static void AuthorCues(GameAudioSettings profile)
    {
        Origins.Clear(); Imported.Clear();
        var cues = new List<GameAudioSettings.Cue>();
        void Cue(GameAudioCue id, string category, string[] files, float volume = 0.45f, int priority = 120, int limit = 6,
            bool protect = false, GameAudioBus bus = GameAudioBus.SFX, bool loop = false, float pitchLow = 0.94f, float pitchHigh = 1.06f)
        {
            cues.Add(new GameAudioSettings.Cue { id = id, clips = files.Select(file => Clip(file, category, bus)).ToArray(),
                volume = volume, priority = priority, maxInstances = limit, protect = protect, bus = bus, loop = loop,
                pitch = new Vector2(pitchLow, pitchHigh), spatialBlend = bus == GameAudioBus.UI || id == GameAudioCue.PortalTravel || id == GameAudioCue.PortalOpen ? 0f : 0.8f,
                minimumInterval = bus == GameAudioBus.UI ? 0.065f : id == GameAudioCue.Footstep || id == GameAudioCue.EnemyFootstep ? 0.16f : 0.025f });
        }
        Cue(GameAudioCue.UiMove, "UI", new[] { "UI_Mechanical_Move_02.wav", "UI_Mechanical_Move_03.wav" }, 0.22f, 60, 2, bus: GameAudioBus.UI);
        Cue(GameAudioCue.UiConfirm, "UI", new[] { "UI_Mechanical_Confirm_01_FX.wav" }, 0.45f, 45, 3, bus: GameAudioBus.UI);
        Cue(GameAudioCue.UiCancel, "UI", new[] { "UI_Mechanical_Cancel_01_FX.wav" }, 0.35f, 45, 2, bus: GameAudioBus.UI);
        Cue(GameAudioCue.UiOpen, "UI", new[] { "UI_Mechanical_Turning-On_04_Raw.wav" }, 0.32f, 40, 2, bus: GameAudioBus.UI, pitchLow: 1f, pitchHigh: 1f);
        Cue(GameAudioCue.UiClose, "UI", new[] { "UI_Mechanical_Turning-Off_02_Raw.wav" }, 0.32f, 40, 2, bus: GameAudioBus.UI, pitchLow: 1f, pitchHigh: 1f);
        Cue(GameAudioCue.UiError, "UI", new[] { "UI_Mechanical_Error_01_Raw.wav" }, 0.35f, 40, 2, bus: GameAudioBus.UI);
        Cue(GameAudioCue.Notification, "UI", new[] { "UI_Mechanical_Notification_01_FX.wav" }, 0.35f, 35, 2, bus: GameAudioBus.UI);
        var swipe = new[] { "knife_unsheathe_01.wav", "knife_unsheathe_02.wav", "knife_unsheathe_03.wav" };
        Cue(GameAudioCue.Slash, "Combat/Melee", swipe, 0.42f, 110);
        Cue(GameAudioCue.Combo, "Combat/Melee", swipe, 0.46f, 105, pitchLow: 1.03f, pitchHigh: 1.12f);
        Cue(GameAudioCue.Finisher, "Combat/Melee", new[] { "unsheathe_sword_with_ringout.wav" }, 0.5f, 75, 4, pitchLow: 0.85f, pitchHigh: 0.95f);
        Cue(GameAudioCue.AirSlash, "Combat/Melee", swipe, 0.4f, 110);
        Cue(GameAudioCue.DashSlash, "Combat/Melee", swipe, 0.47f, 100, pitchLow: 0.85f, pitchHigh: 1f);
        Cue(GameAudioCue.RisingSlash, "Combat/Melee", new[] { "knife_unsheathe_04_with_ring.wav" }, 0.5f, 85);
        Cue(GameAudioCue.DiveStrike, "Combat/Melee", new[] { "sword_hit_impact_heavy_03.wav" }, 0.48f, 85);
        Cue(GameAudioCue.CriticalStrike, "Combat/Melee", new[] { "sword_hit_impact_ringing_01.wav" }, 0.52f, 70);
        Cue(GameAudioCue.MagneticPull, "Combat/Skills", new[] { "AA_SciFi_ForceField_Deploy_v1a.wav", "AA_SciFi_ForceField_Deploy_v1b.wav" }, 0.42f, 20, protect: true);
        Cue(GameAudioCue.MegaSlash, "Combat/Skills", new[] { "AA_SciFi_ForceField_Break_v1a.wav" }, 0.6f, 10, protect: true, pitchLow: 0.92f, pitchHigh: 1.02f);
        Cue(GameAudioCue.SwordFire, "Combat/Skills", new[] { "gas_large_flame_ignite_01.wav", "gas_large_flame_ignite_02.wav" }, 0.58f, 10, protect: true);
        Cue(GameAudioCue.CriticalX, "Combat/Skills", new[] { "AA_SciFi_ForceField_Impact_Heavy_v1a.wav", "AA_SciFi_ForceField_Impact_Heavy_v1b.wav" }, 0.62f, 10, protect: true);
        Cue(GameAudioCue.SkillPulse, "Combat/Skills", new[] { "AA_SciFi_ForceField_Impact_Light_v1a.wav", "AA_SciFi_ForceField_Impact_Light_v1b.wav", "AA_SciFi_ForceField_Impact_Light_v1c.wav" }, 0.25f, 125, 8, pitchLow: 0.9f, pitchHigh: 1.12f);
        Cue(GameAudioCue.Hit, "Combat/Impacts", new[] { "sword_hit_impact_01.wav", "sword_hit_impact_02.wav", "sword_hit_impact_03.wav", "sword_hit_impact_04.wav" }, 0.42f, 115, 10);
        Cue(GameAudioCue.PlayerHit, "Combat/Impacts", new[] { "AA_SciFi_ForceField_Impact_Light_v1d.wav", "AA_SciFi_ForceField_Impact_Light_v1e.wav" }, 0.52f, 65, 4);
        Cue(GameAudioCue.EnemyAttack, "Combat/Melee", swipe, 0.3f, 140, 5, pitchLow: 0.8f, pitchHigh: 0.94f);
        Cue(GameAudioCue.EnemyDeath, "Actors", new[] { "sci-fi_power_down_01.wav", "sci-fi_power_down_02.wav", "sci-fi_power_down_03.wav" }, 0.4f, 55, 8);
        Cue(GameAudioCue.PlayerDeath, "Actors", new[] { "sci-fi_power_down_object_01.wav" }, 0.65f, 15, protect: true, pitchLow: 1f, pitchHigh: 1f);
        Cue(GameAudioCue.Gun, "Combat/Guns", new[] { "sci-fi_weapon_rifle_laser_shot_01.wav", "sci-fi_weapon_rifle_laser_shot_02.wav" }, 0.32f, 105, 8, pitchLow: 0.95f, pitchHigh: 1.05f);
        Cue(GameAudioCue.BulletImpact, "Combat/Impacts", new[] { "bullet_impact_metal_heavy_01.wav", "bullet_impact_metal_heavy_02.wav", "bullet_impact_metal_heavy_03.wav" }, 0.23f, 150, 8);
        Cue(GameAudioCue.Throw, "Combat/Throws", new[] { "knife_unsheathe_03.wav", "knife_unsheathe_02.wav" }, 0.42f, 90, 5, pitchLow: 0.72f, pitchHigh: 0.84f);
        Cue(GameAudioCue.LanceImpact, "Combat/Throws", new[] { "sword_hit_impact_heavy_01.wav", "sword_hit_impact_heavy_02.wav" }, 0.45f, 90, 6);
        var feet = new[] { "footstep_metal_high_run_01.wav", "footstep_metal_high_run_02.wav", "footstep_metal_high_run_03.wav", "footstep_metal_high_run_04.wav" };
        Cue(GameAudioCue.Footstep, "Foley/Footsteps", feet, 0.24f, 195, 5, pitchLow: 0.9f, pitchHigh: 1.1f);
        Cue(GameAudioCue.EnemyFootstep, "Foley/Footsteps", feet, 0.12f, 220, 4, pitchLow: 0.8f, pitchHigh: 0.98f);
        Cue(GameAudioCue.Jump, "Foley/Movement", new[] { "footstep_metal_land_01.wav" }, 0.18f, 175, 4, pitchLow: 1.12f, pitchHigh: 1.22f);
        Cue(GameAudioCue.Land, "Foley/Movement", new[] { "footstep_metal_land_01.wav", "footstep_metal_land_02.wav" }, 0.3f, 160, 5, pitchLow: 0.88f, pitchHigh: 1.02f);
        Cue(GameAudioCue.Dash, "Foley/Movement", new[] { "sci-fi_small_spaceship_jet_blast_01.wav", "sci-fi_small_spaceship_jet_blast_02.wav" }, 0.3f, 95, 6);
        Cue(GameAudioCue.CrateHit, "World/Destruction", new[] { "bullet_impact_wood_heavy_01.wav", "bullet_impact_wood_heavy_02.wav", "bullet_impact_wood_heavy_03.wav" }, 0.4f, 145, 5);
        Cue(GameAudioCue.CrateBreak, "World/Destruction", new[] { "bullet_impact_wood_heavy_04.wav", "bullet_impact_wood_heavy_05.wav" }, 0.7f, 65, 6, pitchLow: 0.65f, pitchHigh: 0.8f);
        Cue(GameAudioCue.Heal, "Actors", new[] { "collect_item_chime_01.wav" }, 0.35f, 70, 4);
        Cue(GameAudioCue.Revive, "Actors", new[] { "sci-fi_power_up_object_01.wav" }, 0.5f, 20, protect: true, pitchLow: 1f, pitchHigh: 1f);
        Cue(GameAudioCue.PortalOpen, "World/Portals", new[] { "AA_SciFi_ForceField_Deploy_v1c.wav" }, 0.6f, 5, protect: true, pitchLow: 1f, pitchHigh: 1f);
        Cue(GameAudioCue.PortalTravel, "World/Portals", new[] { "sci-fi_small_warp_machine_01.wav" }, 0.65f, 5, protect: true, pitchLow: 1f, pitchHigh: 1f);
        Cue(GameAudioCue.PortalHum, "World/Portals", new[] { "AA_SciFi_ForceField_Idle_Loop_v1.wav" }, 0.22f, 5, protect: true, bus: GameAudioBus.Ambient, loop: true, pitchLow: 1f, pitchHigh: 1f);
        profile.cues = cues.ToArray();
        profile.musicPlaylist = new[] { "Sci-Fi 1 Loop.wav", "Sci-Fi 3 Loop.wav", "Sci-Fi 5 Loop.wav" }.Select(f => Clip(f, "Music", GameAudioBus.Music)).ToArray();
        profile.ambientPlaylist = new[] { "Atmos 1 Loop.wav", "Atmos 3 Loop.wav" }.Select(f => Clip(f, "Ambient", GameAudioBus.Ambient)).ToArray();
        string manifest = "Docs/AudioClipOrigins.csv";
        File.WriteAllLines(manifest, new[] { "OriginalPath,CurrentPath,Origin,Readiness" }.Concat(Origins));
    }
    private static AudioClip Clip(string file, string category, GameAudioBus bus)
    {
        string folder = Root + "/" + category; MechaPresentationSetup.Folder(folder);
        // The same clip can serve multiple cues, retaining one GUID and one imported copy.
        var paths = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(file) + " t:AudioClip")
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetFileName(p) == file).ToArray();
        if (paths.Length != 1) throw new InvalidOperationException("Expected one clip: " + file + "; found " + paths.Length);
        string source = paths[0], destination = folder + "/" + file;
        if (source.StartsWith(Root + "/", StringComparison.Ordinal)) destination = source;
        else
        {
            string guid = AssetDatabase.AssetPathToGUID(source);
            string error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            if (AssetDatabase.AssetPathToGUID(destination) != guid) throw new InvalidOperationException("GUID changed: " + file);
            Origins.Add("\"" + source + "\",\"" + destination + "\",\"Supplied / imported; upstream AI origin unverified\",\"Placeholder\"");
        }
        if (Imported.Add(destination))
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(destination);
            var sample = importer.defaultSampleSettings;
            bool bed = bus == GameAudioBus.Music || bus == GameAudioBus.Ambient && category == "Ambient";
            bool longEffect = !bed && AssetDatabase.LoadAssetAtPath<AudioClip>(destination).length > 5f;
            sample.loadType = bed ? AudioClipLoadType.Streaming : longEffect ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            sample.compressionFormat = bed || longEffect ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            sample.quality = bed || longEffect ? 0.8f : 1f; sample.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            sample.preloadAudioData = !bed;
            importer.defaultSampleSettings = sample;
            importer.forceToMono = bus != GameAudioBus.Music && bus != GameAudioBus.UI && !bed;
            importer.loadInBackground = bed; importer.SaveAndReimport();
            AssetDatabase.SetLabels(AssetDatabase.LoadMainAssetAtPath(destination), new[] { "Placeholder" });
        }
        return AssetDatabase.LoadAssetAtPath<AudioClip>(destination);
    }
    [MenuItem("Tools/Mecha/Audio/Connect Mixer")]
    public static void ConnectMixer()
    {
        var profile = MechaPresentationSetup.Profile<GameAudioSettings>("GameAudio");
        if (profile == null) throw new InvalidOperationException("Run Implement Audio first.");
        Connect(profile); EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
    }
    private static void Connect(GameAudioSettings profile)
    {
        profile.mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Root + "/MechaAudio.mixer");
        if (profile.mixer == null) return;
        AudioMixerGroup Group(string name) => profile.mixer.FindMatchingGroups("").SingleOrDefault(group => group.name == name);
        profile.ui = Group("UI"); profile.sfx = Group("SFX"); profile.ambient = Group("Ambient"); profile.music = Group("Music");
        // Rename only the volume parameters already exposed by the user. Do not
        // create groups/parameters or change the mixer's structure through internals.
        var mixerData = new SerializedObject(profile.mixer);
        var exposed = mixerData.FindProperty("m_ExposedParameters");
        if (exposed != null)
        {
            foreach (var group in profile.mixer.FindMatchingGroups(""))
            {
                var groupVolume = new SerializedObject(group).FindProperty("m_Volume");
                if (groupVolume == null) continue;
                for (int i = 0; i < exposed.arraySize; i++)
                {
                    var parameter = exposed.GetArrayElementAtIndex(i);
                    if (SerializedProperty.DataEquals(groupVolume, parameter.FindPropertyRelative("guid")))
                        parameter.FindPropertyRelative("name").stringValue = group.name + "Volume";
                }
            }
            mixerData.ApplyModifiedPropertiesWithoutUndo();
        }
        Debug.Log("Audio mixer routing: UI=" + (profile.ui != null) + ", SFX=" + (profile.sfx != null) + ", Ambient=" + (profile.ambient != null) + ", Music=" + (profile.music != null));
    }
    private static void AddActorFootsteps()
    {
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset.GetComponent<PlayerMovement>() == null && asset.GetComponent<EnemyController>() == null) continue;
            var contents = PrefabUtility.LoadPrefabContents(path);
            try { MechaPresentationSetup.Ensure<ActorFootstepAudio>(contents); PrefabUtility.SaveAsPrefabAsset(contents, path); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
        foreach (var actor in UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None)) MechaPresentationSetup.Ensure<ActorFootstepAudio>(actor.gameObject);
        foreach (var actor in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) MechaPresentationSetup.Ensure<ActorFootstepAudio>(actor.gameObject);
    }
    private static void BuildMenu()
    {
        sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        var root = GameObject.Find("Audio Configuration") ?? new GameObject("Audio Configuration", typeof(RectTransform));
        var canvas = MechaPresentationSetup.Ensure<Canvas>(root); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 300;
        var scaler = MechaPresentationSetup.Ensure<UnityEngine.UI.CanvasScaler>(root);
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280f, 720f); scaler.matchWidthOrHeight = 0.5f;
        MechaPresentationSetup.Ensure<UnityEngine.UI.GraphicRaycaster>(root);
        var menu = MechaPresentationSetup.Ensure<AudioConfigurationMenu>(root);
        var overlay = Rect(root.transform, "Audio Panel", Vector2.zero, Vector2.zero);
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        Image(overlay, new Color(0.006f, 0.012f, 0.025f, 0.94f), true);
        var card = Rect(overlay, "Settings Card", Vector2.zero, new Vector2(760f, 612f));
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f); card.anchoredPosition = Vector2.zero;
        Image(card, new Color(0.025f, 0.05f, 0.085f));
        Label(card, "Title", "AUDIO CONFIGURATION", new Vector2(44f, 28f), new Vector2(660f, 44f), 30f);
        Label(card, "Subtitle", "Balance your cockpit. Changes are saved automatically.", new Vector2(44f, 78f), new Vector2(660f, 28f), 17f);
        var sliders = new List<UnityEngine.UI.Slider>(); var readouts = new List<TMP_Text>();
        string[] names = { "MASTER", "UI", "SFX", "AMBIENT", "MUSIC" };
        for (int i = 0; i < names.Length; i++)
        {
            float y = 133f + i * 66f;
            Label(card, names[i] + " Label", names[i], new Vector2(44f, y), new Vector2(160f, 32f), 18f);
            var control = Rect(card, names[i] + " Slider", new Vector2(216f, y), new Vector2(410f, 34f));
            Image(control, Color.clear, true);
            var slider = MechaPresentationSetup.Ensure<UnityEngine.UI.Slider>(control.gameObject); slider.minValue = 0f; slider.maxValue = 1f;
            var background = Rect(control, "Track", new Vector2(8f, 13f), new Vector2(394f, 8f)); Image(background, new Color(0.14f, 0.22f, 0.3f));
            var fillArea = Rect(control, "Fill Area", Vector2.zero, Vector2.zero); Stretch(fillArea, new Vector2(8f, 13f), new Vector2(-8f, -13f));
            var fill = Rect(fillArea, "Fill", Vector2.zero, Vector2.zero); Stretch(fill, Vector2.zero, Vector2.zero); Image(fill, new Color(0.2f, 0.86f, 0.9f));
            var handleArea = Rect(control, "Handle Area", Vector2.zero, Vector2.zero); Stretch(handleArea, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            var handle = Rect(handleArea, "Handle", Vector2.zero, new Vector2(20f, 0f)); handle.pivot = new Vector2(0.5f, 0.5f);
            var handleImage = Image(handle, new Color(0.75f, 0.98f, 1f), true);
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handleImage; slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            sliders.Add(slider);
            var readout = Label(card, names[i] + " Value", "100%", new Vector2(640f, y), new Vector2(80f, 32f), 18f);
            readout.alignment = TextAlignmentOptions.MidlineRight; readouts.Add(readout);
        }
        Label(card, "Help", "0% mutes a layer. SFX includes combat, movement and destruction.\nUp / Down: select   •   Left / Right: adjust   •   Right button / Esc: back", new Vector2(44f, 466f), new Vector2(670f, 54f), 14f);
        var defaults = Button(card, "Defaults", "DEFAULTS", new Vector2(44f, 542f), new Vector2(300f, 44f));
        var oldLobby = card.Find("Lobby"); if (oldLobby != null) UnityEngine.Object.DestroyImmediate(oldLobby.gameObject);
        var close = Button(card, "Close", "BACK TO OPTIONS", new Vector2(366f, 542f), new Vector2(350f, 44f));
        var controls = sliders.Cast<UnityEngine.UI.Selectable>().Concat(new UnityEngine.UI.Selectable[] { defaults, close }).ToArray();
        for (int i = 0; i < sliders.Count; i++) sliders[i].navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
            selectOnUp = i == 0 ? close : sliders[i - 1], selectOnDown = i == sliders.Count - 1 ? defaults : sliders[i + 1] };
        var buttons = new[] { defaults, close };
        for (int i = 0; i < buttons.Length; i++) buttons[i].navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
            selectOnUp = i == 0 ? sliders[4] : buttons[i - 1], selectOnDown = i == buttons.Length - 1 ? sliders[0] : buttons[i + 1],
            selectOnLeft = buttons[(i + 1) % 2], selectOnRight = buttons[(i + 1) % 2] };
        var serialized = new SerializedObject(menu);
        serialized.FindProperty("panel").objectReferenceValue = overlay.gameObject;
        AssignArray(serialized.FindProperty("sliders"), sliders.Cast<UnityEngine.Object>().ToArray());
        AssignArray(serialized.FindProperty("readouts"), readouts.Cast<UnityEngine.Object>().ToArray());
        serialized.FindProperty("defaults").objectReferenceValue = defaults; serialized.FindProperty("close").objectReferenceValue = close;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        foreach (var control in controls)
        {
            MechaPresentationSetup.Ensure<UiAudioFeedback>(control.gameObject);
            MechaUiFeelSetup.Button(control, MechaPresentationSetup.Profile<UiFeelSettings>("UiFeel"));
        }
        overlay.gameObject.SetActive(false);
        MechaPresentationSetup.Folder("Assets/_Project/Prefabs/Interface");
        string path = "Assets/_Project/Prefabs/Interface/AudioConfiguration.prefab";
        if (!PrefabUtility.IsPartOfPrefabInstance(root)) PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
        else PrefabUtility.ApplyPrefabInstance(root, InteractionMode.AutomatedAction);
    }
    private static void AssignArray(SerializedProperty property, UnityEngine.Object[] values)
    { property.arraySize = values.Length; for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
    private static RectTransform Rect(Transform parent, string title, Vector2 position, Vector2 size)
    {
        var child = parent.Find(title);
        var rect = child != null ? child.GetComponent<RectTransform>() : new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>();
        if (rect.parent != parent) rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(position.x, -position.y); rect.sizeDelta = size; return rect;
    }
    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max; }
    private static UnityEngine.UI.Image Image(RectTransform rect, Color color, bool raycast = false)
    {
        var image = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(rect.gameObject); image.sprite = sprite; image.type = UnityEngine.UI.Image.Type.Sliced;
        image.color = color; image.raycastTarget = raycast; return image;
    }
    private static TMP_Text Label(Transform parent, string title, string text, Vector2 position, Vector2 size, float fontSize)
    {
        var rect = Rect(parent, title, position, size); var label = MechaPresentationSetup.Ensure<TextMeshProUGUI>(rect.gameObject);
        label.font = font; label.text = text; label.fontSize = fontSize; label.color = new Color(0.88f, 0.95f, 0.99f);
        label.alignment = TextAlignmentOptions.MidlineLeft; label.raycastTarget = false; return label;
    }
    private static UnityEngine.UI.Button Button(Transform parent, string title, string text, Vector2 position, Vector2 size)
    {
        var rect = Rect(parent, title, position, size); var image = Image(rect, new Color(0.12f, 0.25f, 0.34f), true);
        var button = MechaPresentationSetup.Ensure<UnityEngine.UI.Button>(rect.gameObject); button.targetGraphic = image;
        var label = Label(rect, "Label", text, Vector2.zero, size, 16f); label.alignment = TextAlignmentOptions.Center; return button;
    }
}
