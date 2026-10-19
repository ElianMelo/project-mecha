using System;
using System.Linq;
using FishNet.Object;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;

// Applies this feature pass through Unity APIs. Each stage can be reapplied independently.
public static class MechaAdjustmentsSetup
{
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
    private static Sprite Frame => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    private const string Prefabs = "Assets/_Project/Prefabs";
    [MenuItem("Tools/Mecha/Apply Game Adjustments")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/MainScene.unity") throw new InvalidOperationException("Open MainScene first.");
        Unsection(); MechaControllerSetup.Apply();
        Actors(); Chat(); Interface(); Organize();
        foreach (var networkObject in UnityEngine.Object.FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Serialize(networkObject.gameObject);
        AssetDatabase.SaveAssets(); EditorSceneManager.SaveOpenScenes();
    }
    public static void Actors()
    {
        AssetDatabase.ImportAsset("Assets/_Project/Art/Interface/HollowRing.png");
        var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/_Project/Art/Interface/HollowRing.png");
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 256f; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        var style = MechaPresentationSetup.Create<SquadPresentationSettings>("SquadPresentation");
        style.ringSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Interface/HollowRing.png");
        EditorUtility.SetDirty(style);
        var weapon = MechaPresentationSetup.Create<SecondaryWeaponSettings>("SecondarySMG");
        Projectile(weapon);
        // Removed time-dilation fields are also removed from serialized authoring data.
        EditorUtility.SetDirty(MechaPresentationSetup.Profile<PlayerCombatSettings>("PlayerCombat"));
        EditorUtility.SetDirty(MechaPresentationSetup.Profile<GameplayHudLayoutSettings>("GameplayHudLayout"));
        foreach (string guid in AssetDatabase.FindAssets("t:AttackPiece")) EditorUtility.SetDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
        foreach (string path in new[] { Prefabs + "/MechaPlayer.prefab", Prefabs + "/EvaEnemy.prefab", Prefabs + "/LanceEnemy.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool player = root.GetComponent<PlayerInputs>() != null;
                if (player) { MechaPresentationSetup.Ensure<PlayerIdentity>(root); Weapon(root, weapon, style); }
                WorldHud(root, style, player);
                Serialize(root); PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // Existing preview instances may not be connected to the network prefab.
            if (!PrefabUtility.IsPartOfPrefabInstance(player.gameObject))
            {
                MechaPresentationSetup.Ensure<PlayerIdentity>(player.gameObject); Weapon(player.gameObject, weapon, style); WorldHud(player.gameObject, style, true); Serialize(player.gameObject);
            }
        }
    }
    [MenuItem("Tools/Mecha/Refresh Player World Indicators")]
    public static void PlayerWorldIndicators()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
        const string path = Prefabs + "/MechaPlayer.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var style = MechaPresentationSetup.Profile<SquadPresentationSettings>("SquadPresentation");
            var indicators = root.transform.Find("World Indicators");
            TeammateRing(indicators, style, indicators.GetComponent<ActorWorldHud>());
            PrefabUtility.SaveAsPrefabAsset(root, path);
            EditorUtility.SetDirty(style);
            AssetDatabase.SaveAssetIfDirty(style);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void WorldHud(GameObject actor, SquadPresentationSettings style, bool player)
    {
        var root = Child(actor.transform, "World Indicators");
        var presenter = MechaPresentationSetup.Ensure<ActorWorldHud>(root);
        Assign(presenter, "settings", style);
        var canvasRect = Rect(root.transform, "World Canvas", Vector2.zero, Vector2.zero, new Vector2(100f, 100f));
        var canvas = MechaPresentationSetup.Ensure<Canvas>(canvasRect.gameObject); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 20;
        canvasRect.localScale = Vector3.one * 0.01f;
        if (!player)
        {
            var bar = Rect(canvasRect, "Enemy Health", new Vector2(0.5f, 0.5f), Vector2.zero, style.healthBarSize * 100f);
            var border = Image(bar, Color.black); border.type = UnityEngine.UI.Image.Type.Sliced; border.fillCenter = false;
            var fillRect = Rect(bar, "Health Fill", new Vector2(0.5f, 0.5f), Vector2.zero, bar.sizeDelta - new Vector2(4f, 4f));
            var fill = Image(fillRect, style.enemyHealth); fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 1f;
            Assign(presenter, "bar", bar); Assign(presenter, "fill", fill);
        }
        else
        {
            var name = Label(canvasRect, "Pilot Name", "PILOT", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250f, 30f), 16f);
            name.alignment = TextAlignmentOptions.Center; name.outlineWidth = 0.2f;
            Assign(presenter, "pilotName", name);
            TeammateRing(root.transform, style, presenter);
        }
    }
    private static void TeammateRing(Transform parent, SquadPresentationSettings style, ActorWorldHud presenter)
    {
        var ringObject = Child(parent, "Teammate Ring");
        var oldSprite = ringObject.GetComponent<SpriteRenderer>();
        if (oldSprite != null) UnityEngine.Object.DestroyImmediate(oldSprite);
        var ringCanvas = MechaPresentationSetup.Ensure<Canvas>(ringObject);
        ringCanvas.renderMode = RenderMode.WorldSpace; ringCanvas.sortingOrder = 18;
        var ringRect = (RectTransform)ringObject.transform;
        ringRect.anchorMin = ringRect.anchorMax = ringRect.pivot = Vector2.one * 0.5f;
        ringRect.localScale = Vector3.one;
        ringRect.localPosition = Vector3.up * style.ringAboveFeet;
        ringRect.localRotation = Quaternion.Euler(style.RingTiltDegrees, 0f, 0f);
        ringRect.sizeDelta = Vector2.one * style.ringWidth;
        var ring = Image(ringRect, style.PilotColor(0));
        ring.sprite = style.ringSprite; ring.type = UnityEngine.UI.Image.Type.Simple;
        ring.preserveAspect = true; ring.enabled = false;
        Assign(presenter, "ring", ring);
    }
    private static void Weapon(GameObject root, SecondaryWeaponSettings settings, SquadPresentationSettings style)
    {
        var aim = MechaPresentationSetup.Ensure<MechaAutoAim>(root); Assign(aim, "settings", settings);
        var view = MechaPresentationSetup.Ensure<SecondaryWeaponPresentation>(root); Assign(view, "settings", settings);
        var arm = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "Bip001 L Hand");
        var gun = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Secondary Weapon Aim") ?? Child(arm, "Secondary Weapon Aim").transform;
        gun.SetParent(arm, false); gun.localPosition = arm.Find("Bip001 L Finger0").localPosition*.5f;
        Assign(aim, "leftArm", arm); Assign(view, "leftArm", arm);
        if (gun.childCount == 0)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci Fi Gun Heavy/Prefabs/GunHeavy/SciFiGunHeavy_Yellow.prefab"));
            model.transform.SetParent(gun, false); model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            model.transform.localPosition = new Vector3(0.15f, 0f, 0f); model.transform.localScale = Vector3.one * 0.72f;
        }
        foreach (var mesh in gun.GetComponentsInChildren<Renderer>())
            mesh.sharedMaterials = mesh.sharedMaterials.Select(WeaponMaterial).ToArray();
        var oldBolts = root.transform.Find("Yellow SMG Bolts");
        if (oldBolts != null) UnityEngine.Object.DestroyImmediate(oldBolts.gameObject);
        var reticle = root.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name == "SMG Aim Reticle")
            ?? MechaPresentationSetup.Ensure<SpriteRenderer>(Child(root.transform, "SMG Aim Reticle"));
        reticle.sprite = style.ringSprite; reticle.color = new Color(1f, 0.85f, 0.2f, 0.8f); reticle.sortingOrder = 22;
        ConfigureAimFeedback(root, settings, reticle);
        Assign(view, "gun", gun); Assign(view, "reticle", reticle);
        Assign(MechaPresentationSetup.Ensure<PlayerSecondaryWeapon>(root), "settings", settings);
    }
    private static void Projectile(SecondaryWeaponSettings settings)
    {
        const string path = Prefabs + "/SecondaryBullet.prefab";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        var root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject("Secondary Bullet");
        try
        {
            root.layer = 2; // Ignore Raycast: the shooter's aim query never targets its own bullet.
            MechaPresentationSetup.Ensure<NetworkObject>(root);
            var body = MechaPresentationSetup.Ensure<Rigidbody>(root);
            body.useGravity = false; body.isKinematic = false; body.linearDamping = 0f;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var collider = MechaPresentationSetup.Ensure<SphereCollider>(root);
            collider.radius = settings.projectileRadius; collider.excludeLayers = ~settings.hitLayers.value;
            Assign(MechaPresentationSetup.Ensure<SecondaryWeaponProjectile>(root), "settings", settings);
            var sync = MechaPresentationSetup.Ensure<FishNet.Component.Transforming.NetworkTransform>(root);
            MechaNetworkSynchronizationSetup.ConfigureTransform(sync, MechaPresentationSetup.Profile<NetworkSimulationSettings>("NetworkSimulation"), false);
            var visual = root.transform.Find("Bullet Model");
            if (visual == null)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name = "Bullet Model";
                UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
                sphere.transform.SetParent(root.transform, false); visual = sphere.transform;
            }
            visual.gameObject.layer = 2; visual.localScale = new Vector3(0.23f, 0.09f, 0.09f);
            visual.localPosition = Vector3.forward * settings.projectileVisualDepth;
            ProjectileForeground(visual.GetComponent<Renderer>());
            const string materialPath = "Assets/_Project/Materials/SecondaryBulletGlow.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", new Color(1f, 0.8f, 0.05f));
                material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", settings.shotColor);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            visual.GetComponent<MeshRenderer>().sharedMaterial = material;
            var light = MechaPresentationSetup.Ensure<Light>(root); light.type = LightType.Point;
            light.color = new Color(1f, 0.8f, 0.05f); light.range = 0.7f; light.intensity = 0.5f; light.shadows = LightShadows.None;
            Serialize(root); PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { if (exists) PrefabUtility.UnloadPrefabContents(root); else UnityEngine.Object.DestroyImmediate(root); }
        settings.projectile = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<NetworkObject>(); EditorUtility.SetDirty(settings);
    }
    [MenuItem("Tools/Mecha/Apply Aim Feedback")]
    public static void AimFeedback()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
        var settings = MechaPresentationSetup.Profile<SecondaryWeaponSettings>("SecondarySMG");
        settings.aimSmoothTime = 0.45f; settings.aimDegreesPerSecond = 100f;
        settings.lockAngle = 12f; settings.captureDegreesPerSecond = 300f;
        settings.reticleVisualDepth = -2f; EnsureAimController(settings);
        EditorUtility.SetDirty(settings); AssetDatabase.SaveAssetIfDirty(settings);
        const string path = Prefabs + "/MechaPlayer.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ConfigureAimFeedback(root, settings, root.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "SMG Aim Reticle"));
            Serialize(root); PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        foreach (var view in UnityEngine.Object.FindObjectsByType<SecondaryWeaponPresentation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // Connected scene instances inherit the authored hierarchy. Existing
            // standalone previews need the same native components/references.
            if (PrefabUtility.IsPartOfPrefabInstance(view.gameObject)) continue;
            ConfigureAimFeedback(view.gameObject, settings,
                view.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "SMG Aim Reticle"));
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }
        AssetDatabase.SaveAssets();
    }
    private static void ConfigureAimFeedback(GameObject root, SecondaryWeaponSettings settings, SpriteRenderer reticle)
    {
        EnsureAimController(settings);
        var pivot = Child(root.transform, "Aim Reticle Pivot").transform;
        reticle.transform.SetParent(pivot, false); reticle.transform.localPosition = Vector3.zero;
        reticle.transform.localRotation = Quaternion.identity; reticle.transform.localScale = Vector3.one * 0.75f;
        ProjectileForeground(reticle); reticle.sortingOrder = 30; reticle.enabled = false;
        var animator = MechaPresentationSetup.Ensure<Animator>(reticle.gameObject);
        animator.runtimeAnimatorController = settings.reticleController;
        animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var view = root.GetComponent<SecondaryWeaponPresentation>();
        Assign(view, "reticlePivot", pivot); Assign(view, "reticleAnimator", animator);
    }
    private static void EnsureAimController(SecondaryWeaponSettings settings)
    {
        if (settings.reticleController != null) return;
        const string folder = "Assets/_Project/Animations/Interface/";
        const string path = folder + "MechaAim.controller";
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
        if (controller == null)
        {
            controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Locked", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[0].stateMachine;
            var scan = machine.AddState("Scan"); scan.writeDefaultValues = false;
            scan.motion = AimClip("AimScan", true, AimCurve(0f, 0.75f, 0.9f, 0.82f, 1.8f, 0.75f),
                AimCurve(0f, 0f, 1.8f, 0f), AimCurve(0f, 0f, 1.8f, 0f), AimCurve(0f, 0.5f, 0.9f, 0.7f, 1.8f, 0.5f));
            var capture = machine.AddState("Capture"); capture.writeDefaultValues = false;
            capture.motion = AimClip("AimCapture", false,
                AimCurve(0f, 0.85f, 0.065f, 0.42f, 0.15f, 0.68f, 0.24f, 0.50f, 0.33f, 0.59f, 0.44f, 0.55f),
                AimCurve(0f, 0.08f, 0.08f, -0.045f, 0.18f, 0.025f, 0.30f, -0.01f, 0.44f, 0f),
                AimCurve(0f, 0.12f, 0.08f, -0.075f, 0.18f, 0.04f, 0.30f, -0.015f, 0.44f, 0f),
                AimCurve(0f, 1f, 0.16f, 0.8f, 0.30f, 1f, 0.44f, 0.95f));
            var tracked = machine.AddState("Track"); tracked.writeDefaultValues = false;
            tracked.motion = AimClip("AimTracked", true, AimCurve(0f, 0.55f, 0.8f, 0.55f),
                AimCurve(0f, 0f, 0.8f, 0f), AimCurve(0f, 0f, 0.8f, 0f), AimCurve(0f, 0.95f, 0.8f, 0.95f));
            machine.defaultState = scan;
            var acquire = scan.AddTransition(capture); acquire.duration = 0f; acquire.hasExitTime = false;
            acquire.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Locked");
            var settle = capture.AddTransition(tracked); settle.duration = 0f; settle.hasExitTime = true; settle.exitTime = 1f;
            foreach (var state in new[] { capture, tracked })
            {
                var release = state.AddTransition(scan); release.duration = 0.08f; release.hasExitTime = false;
                release.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0f, "Locked");
            }
            AssetDatabase.SetLabels(controller, new[] { "Authored Data" });
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller);
        }
        settings.reticleController = controller; EditorUtility.SetDirty(settings);
    }
    private static AnimationCurve AimCurve(params float[] values)
    {
        var keys = new Keyframe[values.Length / 2];
        for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(values[i * 2], values[i * 2 + 1]);
        return new AnimationCurve(keys);
    }
    private static AnimationClip AimClip(string name, bool loop, AnimationCurve scale, AnimationCurve x, AnimationCurve y, AnimationCurve alpha)
    {
        string path = "Assets/_Project/Animations/Interface/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null) return clip;
        clip = new AnimationClip { name = name, frameRate = 60f };
        foreach (string axis in new[] { "x", "y", "z" })
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale." + axis), scale);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x"), x);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.y"), y);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_Color.a"), alpha);
        var data = AnimationUtility.GetAnimationClipSettings(clip); data.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, data); AssetDatabase.CreateAsset(clip, path);
        AssetDatabase.SetLabels(clip, new[] { "Placeholder" }); return clip;
    }
    public static void ProjectileForeground(Renderer renderer)
    {
        if (!SortingLayer.layers.Any(layer => layer.name == "Projectiles"))
        {
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("m_SortingLayers"); int index = layers.arraySize; layers.InsertArrayElementAtIndex(index);
            var layer = layers.GetArrayElementAtIndex(index); layer.FindPropertyRelative("name").stringValue = "Projectiles";
            uint id; do { id = unchecked((uint)Guid.NewGuid().GetHashCode()); } while (id == 0 || SortingLayer.layers.Any(existing => existing.id == unchecked((int)id)));
            layer.FindPropertyRelative("uniqueID").longValue = id; layer.FindPropertyRelative("locked").boolValue = false;
            tags.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(tags.targetObject);
        }
        renderer.sortingLayerName = "Projectiles"; renderer.sortingOrder = 20;
    }
    private static Material WeaponMaterial(Material source)
    {
        if (source.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal)) return source;
        string path = "Assets/_Project/Materials/SecondaryGunYellow.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(source);
            // Official URP upgrader preserves source textures, emission and specular workflow.
            new UnityEditor.Rendering.Universal.StandardUpgrader(source.shader.name).Upgrade(material, UnityEditor.Rendering.MaterialUpgrader.UpgradeFlags.None);
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }
    public static void Chat()
    {
        var profile = MechaPresentationSetup.Create<ChatSettings>("Chat");
        var root = GameObject.Find("Squad Chat") ?? new GameObject("Squad Chat", typeof(RectTransform));
        Canvas(root, 300);
        var panel = Rect(root.transform, "Comms Section", profile.anchor, profile.offset, profile.size);
        Image(panel, new Color(0.015f, 0.028f, 0.045f, 0.72f));
        var hint = Label(panel, "Chat Hint", "SQUAD / ENTER Chat / F2 Hide", new Vector2(0f, 1f), new Vector2(12f, -6f), new Vector2(profile.size.x - 24f, 18f), 10f);
        hint.color = new Color(0.36f, 0.9f, 0.92f);
        var messages = Label(panel, "Messages", "", new Vector2(0f, 1f), new Vector2(12f, -26f), new Vector2(profile.size.x - 24f, profile.size.y - 66f), 12f);
        messages.alignment = TextAlignmentOptions.BottomLeft; messages.overflowMode = TextOverflowModes.Truncate;
        var rect = Rect(panel, "Message Input", Vector2.zero, new Vector2(10f, 8f), new Vector2(profile.size.x - 20f, 26f));
        var image = Image(rect, new Color(0.025f, 0.06f, 0.085f)); image.raycastTarget = true;
        var input = MechaPresentationSetup.Ensure<TMP_InputField>(rect.gameObject);
        var viewport = Rect(rect, "Viewport", Vector2.zero, new Vector2(8f, 0f), new Vector2(profile.size.x - 36f, 26f));
        MechaPresentationSetup.Ensure<UnityEngine.UI.RectMask2D>(viewport.gameObject);
        var text = Label(viewport, "Text", "", Vector2.zero, Vector2.zero, viewport.sizeDelta, 14f); text.alignment = TextAlignmentOptions.MidlineLeft;
        var placeholder = Label(viewport, "Placeholder", "Message your squad…", Vector2.zero, Vector2.zero, viewport.sizeDelta, 14f);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft; placeholder.color = new Color(0.5f, 0.62f, 0.7f);
        input.textViewport = viewport; input.textComponent = text; input.placeholder = placeholder; input.targetGraphic = image;
        input.lineType = TMP_InputField.LineType.SingleLine; input.shouldActivateOnSelect = true;
        input.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        var chat = MechaPresentationSetup.Ensure<MechaChat>(root);
        Assign(chat, "settings", profile); Assign(chat, "input", input); Assign(chat, "messages", messages); Assign(chat, "hint", hint);
        Assign(chat, "content", panel.gameObject);
        var network = UnityEngine.Object.FindFirstObjectByType<FishNet.Managing.NetworkManager>();
        Assign(chat, "network", network);
        Prefab(root, "SquadChat"); Assign(chat, "network", network);
    }
    public static void ControlsHelp()
    {
        var hud = UnityEngine.Object.FindFirstObjectByType<MechaGameplayHud>();
        if (PrefabUtility.IsPartOfPrefabInstance(hud.gameObject))
            PrefabUtility.UnpackPrefabInstance(hud.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        var data = new SerializedObject(hud);
        var controls = data.FindProperty("controls").objectReferenceValue as TMP_Text;
        // Reuse the existing readout and remove duplicate generated groups from old scene overrides.
        controls.transform.SetParent(hud.transform, false);
        var chat = MechaPresentationSetup.Profile<ChatSettings>("Chat");
        var parent = hud.transform.Find("Mission Guidance") ?? hud.transform;
        var existing = parent.Find("Controls Help");
        foreach (var duplicate in hud.GetComponentsInChildren<RectTransform>(true).Where(t => t.name == "Controls Help" && t != existing).ToArray())
            UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
        var group = Rect(parent, "Controls Help", chat.anchor, chat.offset + Vector2.down * (chat.size.y + 12f), new Vector2(chat.size.x, 218f));
        // Keep help above menu backdrops, in the same screen column as persistent comms.
        var canvas = MechaPresentationSetup.Ensure<UnityEngine.Canvas>(group.gameObject);
        canvas.overrideSorting = true; canvas.sortingOrder = 300;
        MechaPresentationSetup.Ensure<UnityEngine.UI.GraphicRaycaster>(group.gameObject);
        var buttonRect = Rect(group, "Controls Toggle", Vector2.one, Vector2.zero, new Vector2(chat.size.x, 30f));
        var image = Image(buttonRect, new Color(0.025f, 0.06f, 0.085f, 0.96f)); image.raycastTarget = true;
        var button = MechaPresentationSetup.Ensure<UnityEngine.UI.Button>(buttonRect.gameObject);
        button.targetGraphic = image;
        var colors = button.colors; colors.disabledColor = new Color(0.38f, 0.38f, 0.38f, 0.65f); button.colors = colors;
        var label = Label(buttonRect, "Label", "SHOW CONTROLS", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(chat.size.x - 24f, 24f), 12f);
        label.alignment = TextAlignmentOptions.Center;
        var panel = Rect(group, "Controls Panel", Vector2.one, new Vector2(0f, -38f), new Vector2(chat.size.x, 180f));
        Image(panel, new Color(0.015f, 0.028f, 0.045f, 0.92f));
        controls.rectTransform.SetParent(panel, false);
        controls.rectTransform.anchorMin = controls.rectTransform.anchorMax = controls.rectTransform.pivot = new Vector2(0f, 1f);
        controls.rectTransform.anchoredPosition = new Vector2(12f, -10f);
        controls.rectTransform.sizeDelta = new Vector2(chat.size.x - 24f, 160f);
        controls.rectTransform.localScale = Vector3.one;
        controls.alignment = TextAlignmentOptions.TopLeft; controls.overflowMode = TextOverflowModes.Overflow;
        // Retain text geometry while its CanvasGroup is hidden so reopening renders immediately.
        controls.canvasRenderer.cullTransparentMesh = false;
        controls.gameObject.SetActive(true); panel.gameObject.SetActive(true);
        var visibility = MechaPresentationSetup.Ensure<CanvasGroup>(panel.gameObject);
        visibility.alpha = 0f; visibility.interactable = false; visibility.blocksRaycasts = false;
        Assign(hud, "controlsPanel", visibility); Assign(hud, "controlsToggle", button); Assign(hud, "controlsToggleLabel", label);
        Assign(hud, "controlsRoot", group); Assign(hud, "chatLayout", chat);
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(button.onClick, i);
        UnityEventTools.AddPersistentListener(button.onClick, hud.ToggleControls);
        MechaUiFeelSetup.Button(button, MechaPresentationSetup.Profile<UiFeelSettings>("UiFeel"));
        // The former top-right link readout now occupies the cleared lower-left corner.
        var link = data.FindProperty("link").objectReferenceValue as TMP_Text;
        link.rectTransform.anchorMin = link.rectTransform.anchorMax = link.rectTransform.pivot = Vector2.zero;
        link.rectTransform.anchoredPosition = new Vector2(28f, 22f); link.alignment = TextAlignmentOptions.BottomLeft;
        EditorUtility.SetDirty(hud);
        Prefab(hud.gameObject, "GameplayInterface");
    }
    public static void Interface()
    {
        var lobby = UnityEngine.Object.FindFirstObjectByType<MechaLobbyPanel>();
        var panel = lobby.transform.Find("Lobby Panel");
        MechaPresentationSetup.Ensure<UiNavigationScope>(panel.gameObject);
        // Keep keyboard navigation inside its own modal scope.
        MechaPresentationSetup.Ensure<UiNavigationScope>(panel.Find("Controller Keyboard").gameObject);
        var hud = UnityEngine.Object.FindFirstObjectByType<MechaGameplayHud>();
        var telemetry = hud.GetComponentsInChildren<Transform>(true).First(t => t.name == "Pilot Telemetry");
        var ammo = Label(telemetry, "Secondary Ammunition", "C  SMG  25 / 25 · AUTO REFILL", new Vector2(0f, 1f), new Vector2(16f, -240f), new Vector2(430f, 25f), 16f);
        Assign(hud, "ammunition", ammo);
        var diagnostics = hud.transform.Find("F3 Diagnostics");
        if (diagnostics.Find("Tempo") != null) UnityEngine.Object.DestroyImmediate(diagnostics.Find("Tempo").gameObject);
        diagnostics.Find("Phase Key").GetComponent<TMP_Text>().text = "BLUE Startup    RED Active    GREEN Recovery";
        diagnostics.Find("Notice").GetComponent<TMP_Text>().text = "Offline practice: refill. Hit volumes appear in Scene view.";
        MechaPresentationSetup.Ensure<UiNavigationScope>(diagnostics.gameObject);
        var layout = MechaPresentationSetup.Profile<GameplayHudLayoutSettings>("GameplayHudLayout");
        layout.tutorialText = layout.tutorialText.Replace("SMG Press C", "SMG Hold C");
        if (!layout.tutorialText.Contains("F1")) layout.tutorialText += "\nF1 Controls / F2 Chat visibility";
        EditorUtility.SetDirty(layout);
        var controller = MechaPresentationSetup.Profile<ControllerInterfaceSettings>("ControllerInterface");
        if (!controller.controllerHelp.Contains("SMG")) controller.controllerHelp += "\nRT SMG   /   ENTER Chat";
        EditorUtility.SetDirty(controller);
        // Comms are hidden in the lobby, so retain the full authored menu size.
        var browser = panel.Find("Browser") as RectTransform;
        browser.localScale = Vector3.one;
        var waiting = panel.Find("Waiting Room") as RectTransform; waiting.localScale = Vector3.one;
        var leave = panel.Find("Leave") as RectTransform; leave.anchoredPosition = new Vector2(40f, -650f);
        var status = panel.Find("Status") as RectTransform; status.anchoredPosition = new Vector2(310f, -646f); status.sizeDelta = new Vector2(930f, 54f);
        var keyboard = panel.Find("Controller Keyboard"); keyboard.SetAsLastSibling();
        var scope = panel.GetComponent<UiNavigationScope>();
        // Sections use native RectTransforms; preserve every existing presenter reference.
        Section(hud.transform, "Pilot Resources", new[] { "Pilot Telemetry", "Skills Top Bar", "Expedition Mark" });
        Section(hud.transform, "Mission Guidance", new[] { "Controls", "Level Objectives", "Link" });
        Section(hud.transform, "Combat Feedback", new[] { "Combo Readout", "Skill Letterbox", "Skill Impact Flash" });
        ControlsHelp();
        var service = UnityEngine.Object.FindFirstObjectByType<MechaLobbyService>();
        var backdrop = UnityEngine.Object.FindFirstObjectByType<SkillFocusPresentation>();
        var backdropReference = new SerializedObject(backdrop).FindProperty("backdrop").objectReferenceValue;
        Prefab(hud.gameObject, "GameplayInterface"); Assign(backdrop, "backdrop", backdropReference);
        Prefab(lobby.gameObject, "LobbyInterface"); Assign(lobby, "service", service);
        // Modal keys stay above the lobby; persistent comms occupy their own clear area.
        var modalCanvas = MechaPresentationSetup.Ensure<UnityEngine.Canvas>(keyboard.gameObject);
        modalCanvas.overrideSorting = true; modalCanvas.sortingOrder = 250;
        MechaPresentationSetup.Ensure<UnityEngine.UI.GraphicRaycaster>(keyboard.gameObject);
    }
    public static void Organize()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var systems = Group("00 Systems"); var world = Group("10 World"); var cameras = Group("20 Cameras");
        var actors = Group("30 Actors and Spawns"); var ui = Group("40 Interface");
        foreach (var go in roots)
        {
            // FishNet calls DontDestroyOnLoad: Unity requires this object to remain a scene root.
            if (go.GetComponent<FishNet.Managing.NetworkManager>() != null) { go.transform.SetParent(null, true); continue; }
            if (go.name == "00 Systems" || go.name == "10 World" || go.name == "20 Cameras" || go.name == "30 Actors and Spawns" || go.name == "40 Interface") continue;
            Transform parent = world;
            if (go.GetComponent<Canvas>() != null) parent = ui;
            else if (go.GetComponent<Camera>() != null || go.GetComponent<RoomCameraRig>() != null || go.name == "Camera Room Bounds") parent = cameras;
            else if (go.GetComponent<GameManager>() != null || go.GetComponent<FishNet.Managing.NetworkManager>() != null || go.GetComponent<UnityEngine.EventSystems.EventSystem>() != null || go.GetComponent<EnemySpawner>() != null) parent = systems;
            else if (go.GetComponent<PlayerInputs>() != null || go.name == "PlayerStart" || go.name == "SpawnPoint") parent = actors;
            go.transform.SetParent(parent, true);
        }
        var networkRoot = UnityEngine.Object.FindFirstObjectByType<FishNet.Managing.NetworkManager>();
        if (networkRoot != null) networkRoot.transform.SetParent(null, true);
        // Reusable environment assemblies retain all native colliders and imported art.
        var scenario = GameObject.Find("Cyberpunk Scenario"); if (scenario != null) Prefab(scenario, "ArenaScenery");
        var platforms = Child(world, "Authored Platforms").transform;
        foreach (var platform in UnityEngine.Object.FindObjectsByType<OneWayPlatform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            platform.transform.SetParent(platforms, true);
        Prefab(platforms.gameObject, "ArenaPlatforms");
    }
    private static void Serialize(GameObject root)
    {
        var method = typeof(NetworkObject).GetMethod("ReserializeEditorSetValues", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        foreach (var obj in root.GetComponentsInChildren<NetworkObject>(true)) { method.Invoke(obj, new object[] { true, true }); EditorUtility.SetDirty(obj); }
    }
    private static void Prefab(GameObject root, string name)
    {
        MechaPresentationSetup.Folder(Prefabs + "/Assemblies");
        if (PrefabUtility.IsPartOfPrefabInstance(root))
        {
            // Preserve scene overrides; snapshot a reusable assembly without applying unrelated changes.
            var clone = UnityEngine.Object.Instantiate(root); clone.name = root.name;
            try {
                if (PrefabUtility.IsPartOfPrefabInstance(clone)) PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                PrefabUtility.SaveAsPrefabAsset(clone, Prefabs + "/Assemblies/" + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
        }
        else PrefabUtility.SaveAsPrefabAssetAndConnect(root, Prefabs + "/Assemblies/" + name + ".prefab", InteractionMode.AutomatedAction);
    }
    private static void Section(Transform parent, string name, string[] children)
    {
        var section = Rect(parent, name, Vector2.zero, Vector2.zero, Vector2.zero);
        section.anchorMax = Vector2.one; section.offsetMin = section.offsetMax = Vector2.zero;
        foreach (string child in children) { var item = parent.Find(child); if (item != null) item.SetParent(section, true); }
    }
    private static void Unsection()
    {
        var hud = UnityEngine.Object.FindFirstObjectByType<MechaGameplayHud>();
        if (hud == null) return;
        if (PrefabUtility.IsPartOfPrefabInstance(hud.gameObject)) PrefabUtility.UnpackPrefabInstance(hud.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        foreach (string name in new[] { "Pilot Resources", "Mission Guidance", "Combat Feedback" })
        {
            var section = hud.transform.Find(name); if (section == null) continue;
            foreach (var child in section.Cast<Transform>().ToArray()) child.SetParent(hud.transform, true);
            UnityEngine.Object.DestroyImmediate(section.gameObject);
        }
        var help = hud.transform.Find("Controls Help");
        if (help != null)
        {
            var assigned = new SerializedObject(hud).FindProperty("controls").objectReferenceValue as TMP_Text;
            if (assigned != null) { assigned.transform.SetParent(hud.transform, true); assigned.gameObject.SetActive(true); }
            UnityEngine.Object.DestroyImmediate(help.gameObject);
        }
    }
    private static Transform Group(string name) => (GameObject.Find(name) ?? new GameObject(name)).transform;
    private static GameObject Child(Transform parent, string name)
    {
        var child = parent.Find(name); if (child != null) return child.gameObject;
        var go = new GameObject(name); go.transform.SetParent(parent, false); return go;
    }
    private static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        var child = parent.Find(name);
        var rect = child != null ? child as RectTransform : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = offset; rect.sizeDelta = size; return rect;
    }
    private static UnityEngine.UI.Image Image(RectTransform rect, Color color)
    {
        var image = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(rect.gameObject);
        image.sprite = Frame; image.type = UnityEngine.UI.Image.Type.Sliced; image.color = color; image.raycastTarget = false; return image;
    }
    private static TMP_Text Label(Transform parent, string name, string text, Vector2 anchor, Vector2 offset, Vector2 size, float fontSize)
    {
        var label = MechaPresentationSetup.Ensure<TextMeshProUGUI>(Rect(parent, name, anchor, offset, size).gameObject);
        label.font = Font; label.fontSize = fontSize; label.text = text; label.richText = false;
        label.color = new Color(0.88f, 0.94f, 0.97f); label.raycastTarget = false; return label;
    }
    private static void Canvas(GameObject root, int order)
    {
        var canvas = MechaPresentationSetup.Ensure<UnityEngine.Canvas>(root); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
        var scaler = MechaPresentationSetup.Ensure<UnityEngine.UI.CanvasScaler>(root);
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280f, 720f); scaler.matchWidthOrHeight = 0.5f;
        MechaPresentationSetup.Ensure<UnityEngine.UI.GraphicRaycaster>(root);
    }
    private static void Assign(Component component, string field, UnityEngine.Object value) => MechaPresentationSetup.Assign(component, field, value);
}
