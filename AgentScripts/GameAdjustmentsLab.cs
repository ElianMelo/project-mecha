using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Manual live inspection helpers; these mutate disposable Play Mode state only.
public static class GameAdjustmentsLab
{
    public static object PreviewTeammateRing(int direction = 1)
    {
        var player = UnityEngine.Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).First(p => p.IsSpawned && p.IsOwner);
        var hud = player.GetComponentInChildren<ActorWorldHud>();
        var ring = player.GetComponentsInChildren<UnityEngine.UI.Image>(true).Single(r => r.name == "Teammate Ring");
        bool hiddenForOwner = !ring.enabled;
        var movement = player.GetComponent<PlayerMovement>();
        var body = player.GetComponent<Rigidbody>();
        movement.enabled = false;
        movement.Face(direction);
        body.interpolation = RigidbodyInterpolation.None;
        body.rotation = Quaternion.Euler(0f, 90f * (direction >= 0 ? 1 : -1), 0f);
        body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        Physics.SyncTransforms();
        hud.SendMessage("LateUpdate");
        // Disposable visual staging only: expose the owner's existing ring as a
        // teammate preview without changing ownership, replication or shared assets.
        hud.enabled = false;
        ring.enabled = true;
        foreach (var enemy in EnemyController.ActiveEnemies) { enemy.PracticePassive = true; enemy.CancelAttack(); }
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) canvas.enabled = false;
        var camera = Camera.main;
        var brain = camera.GetComponent<Unity.Cinemachine.CinemachineBrain>();
        if (brain != null) brain.enabled = false;
        var bounds = player.GetComponent<CapsuleCollider>().bounds;
        camera.transform.SetPositionAndRotation(bounds.center + new Vector3(0f, -0.25f, -10f), Quaternion.identity);
        camera.orthographic = true; camera.orthographicSize = 1.3f;
        return new { hiddenForOwner, ring = ring.rectTransform.position.ToString(), tilt = ring.transform.eulerAngles.ToString(), camera = camera.name, frame = Time.frameCount };
    }
    public static object Snapshot() => new {
        frame = Time.frameCount, time = Time.time, scale = Time.timeScale,
        players = UnityEngine.Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).Select(p => new {
            p.PilotName, p.ColorSlot, p.IsOwner, p.IsSpawned,
            position = p.transform.position.ToString(), ammo = p.GetComponent<PlayerSecondaryWeapon>().Ammo,
            direction = p.GetComponent<MechaAutoAim>().Direction.ToString(),
            target = p.GetComponent<MechaAutoAim>().Target?.TargetTransform.name,
            ring = p.GetComponentsInChildren<UnityEngine.UI.Image>().FirstOrDefault(r => r.name == "Teammate Ring")?.enabled
        }).ToArray(),
        enemies = EnemyController.ActiveEnemies.Where(e => e != null).Select(e => new { e.name, e.Health, e.IsDead, position = e.transform.position.ToString() }).ToArray(),
        chat = MechaChat.Instance?.VisibleMessages, typing = MechaChat.IsTyping
    };
    public static object Stage()
    {
        foreach (var enemy in EnemyController.ActiveEnemies) { enemy.PracticePassive = true; enemy.CancelAttack(); }
        var owner = UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).First(p => p.IsSpawned && p.IsOwner);
        owner.GetComponent<PlayerHealth>().EnterLevel(new Vector3(-8f, -3.4f, 0f)); owner.Face(1);
        var targets = EnemyController.ActiveEnemies.Where(e => e != null && !e.IsDead).Take(2).ToArray();
        for (int i = 0; i < targets.Length; i++)
        {
            var enemy = targets[i]; var body = enemy.GetComponent<Rigidbody>();
            body.position = new Vector3(-4.8f + i * 2.5f, -3.4f, 0f); enemy.transform.position = body.position;
            enemy.GetComponent<EnemyMotor>().SetImpact(Vector3.zero);
            enemy.GetComponent<FishNet.Component.Transforming.NetworkTransform>().Teleport();
        }
        Physics.SyncTransforms(); return Snapshot();
    }
    public static async Task<object> Fire(int milliseconds = 1200)
    {
        var owner = UnityEngine.Object.FindObjectsByType<PlayerInputs>(FindObjectsSortMode.None).First(p => p.IsSpawned && p.IsOwner);
        var input = owner.GetComponent<PlayerInput>();
        var keyboard = InputSystem.AddDevice<Keyboard>("LocalSMGPreview");
        object during = null;
        try {
            input.SwitchCurrentControlScheme("Keyboard&Mouse", keyboard, Mouse.current);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.C));
            await Task.Delay(Math.Max(1, milliseconds / 2));
            during = new { owner.FireHeld, owner.HasControl, ammo = owner.GetComponent<PlayerSecondaryWeapon>().Ammo,
                pressed = input.actions.FindAction("Player/SecondaryFire").IsPressed(), rawPressed = keyboard.cKey.isPressed,
                devices = input.devices.Select(d => d.name).ToArray() };
            await Task.Delay(Math.Max(1, milliseconds - milliseconds / 2));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }
        finally { InputSystem.RemoveDevice(keyboard); }
        return new { during, after = Snapshot() };
    }
    public static object Lobby(bool show)
    {
        var lobby = UnityEngine.Object.FindFirstObjectByType<MechaLobbyPanel>();
        if (show) lobby.Show(); else lobby.Hide(); return show;
    }
    public static async Task<object> FirePad(int milliseconds = 1200)
    {
        var owner = UnityEngine.Object.FindObjectsByType<PlayerInputs>(FindObjectsSortMode.None).First(p => p.IsSpawned && p.IsOwner);
        var input = owner.GetComponent<PlayerInput>();
        var pad = InputSystem.AddDevice<Gamepad>("WeaponPreviewPad");
        object during = null;
        try
        {
            input.SwitchCurrentControlScheme("Gamepad", pad);
            InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = 1f });
            await Task.Delay(Math.Max(1, milliseconds / 2));
            during = new { owner.FireHeld, owner.HasControl, ammo = owner.GetComponent<PlayerSecondaryWeapon>().Ammo };
            await Task.Delay(Math.Max(1, milliseconds - milliseconds / 2));
            InputSystem.QueueStateEvent(pad, new GamepadState());
        }
        finally { InputSystem.RemoveDevice(pad); }
        return new { during, after = Snapshot() };
    }
    public static async Task<object> Press(string key)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>("InterfacePreview");
        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState((Key)Enum.Parse(typeof(Key), key)));
            await Task.Delay(250);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Task.Delay(150);
        }
        finally { InputSystem.RemoveDevice(keyboard); }
        var hud = UnityEngine.Object.FindFirstObjectByType<MechaGameplayHud>();
        var data = new UnityEditor.SerializedObject(hud);
        return new { lobby = GameManager.Instance.LobbyOpen, chat = MechaChat.Instance.ContentVisible,
            controls = ((CanvasGroup)data.FindProperty("controlsPanel").objectReferenceValue).alpha };
    }
    public static object ApplyLanceTuning()
    {
        var profile = MechaPresentationSetup.Profile<EnemyLanceSettings>("EnemyLance");
        profile.throwSpeed = 14f; profile.minimumFlightTime = 0.25f;
        profile.landingFollowThrough = 1.25f; profile.landingEdgeInset = 0.2f;
        profile.landingSearchDepth = 35f; profile.landingLayers = (1 << 6) | (1 << 7);
        UnityEditor.EditorUtility.SetDirty(profile); UnityEditor.AssetDatabase.SaveAssetIfDirty(profile);
        return new { profile.throwSpeed, profile.minimumFlightTime, profile.landingFollowThrough,
            landingLayers = profile.landingLayers.value, profile.arcHeight };
    }
    public static async Task<object> PreviewPlannedThrow(float releaseY, float platformY, bool playerInPath = false, bool dodge = false, bool airborne = false, float horizontalDistance = 6f)
    {
        foreach (var enemy in EnemyController.ActiveEnemies) { enemy.PracticePassive = true; enemy.CancelAttack(); }
        var player = UnityEngine.Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None).First(p => p.IsOwner);
        var body = player.GetComponent<Rigidbody>(); var originalPosition = body.position; var constraints = body.constraints;
        float healthBefore = player.Health;
        var deck = new GameObject("Preview Intended Player Deck", typeof(BoxCollider)); deck.layer = 6;
        deck.transform.position = new Vector3(86f, platformY - 0.125f, 0f);
        var surface = deck.GetComponent<BoxCollider>(); surface.size = new Vector3(5f, 0.25f, 2f);
        var obstacles = new GameObject[2];
        for (int i = 0; i < obstacles.Length; i++)
        {
            obstacles[i] = new GameObject("Preview Intervening Deck " + i, typeof(BoxCollider)); obstacles[i].layer = 6;
            obstacles[i].transform.position = new Vector3(83f, Mathf.Lerp(releaseY, platformY, (i + 1f) / 3f), 0f);
            obstacles[i].GetComponent<BoxCollider>().size = new Vector3(12f, 0.25f, 2f);
        }
        var profile = MechaPresentationSetup.Profile<EnemyLanceSettings>("EnemyLance");
        FishNet.Object.NetworkObject spear = null;
        var samples = new System.Collections.Generic.List<object>();
        try
        {
            body.constraints = RigidbodyConstraints.FreezeAll;
            if (playerInPath)
            {
                float feetOffset = player.GetComponent<CapsuleCollider>().bounds.min.y - player.transform.position.y;
                body.position = new Vector3(86f, platformY - feetOffset + (airborne ? 1.2f : 0f), 0f);
                player.transform.position = body.position;
            }
            Physics.SyncTransforms();
            Vector3 aim = playerInPath ? player.transform.position + Vector3.up * 0.8f : new Vector3(86f, platformY + 0.8f, 0f);
            spear = UnityEngine.Object.Instantiate(profile.projectile, new Vector3(86f - horizontalDistance, releaseY, 0f), Quaternion.identity);
            var flight = spear.GetComponent<EnemyLanceProjectile>(); flight.Launch(profile, aim, airborne ? null : surface);
            GameManager.Instance.Network.ServerManager.Spawn(spear);
            float start = Time.time;
            Func<object> snapshot = () => new { elapsed = Time.time - start,
                alive = spear != null && spear.IsSpawned, embedded = flight != null && flight.IsEmbedded,
                position = spear != null ? spear.transform.position.ToString("F3") : "despawned",
                velocity = flight != null ? flight.Velocity.ToString("F3") : "despawned",
                planned = flight != null ? flight.PlannedLanding.ToString("F3") : "despawned",
                surface = flight != null ? flight.PlannedSurface?.name : "despawned", playerHealth = player.Health };
            samples.Add(snapshot());
            if (dodge)
            {
                body.position += Vector3.right * 10f; player.transform.position = body.position; Physics.SyncTransforms();
            }
            await Task.Delay(250); samples.Add(snapshot());
            await Task.Delay(350); samples.Add(snapshot());
            await Task.Delay(650); samples.Add(snapshot());
            await Task.Delay(250); samples.Add(snapshot());
            return new { releaseY, platformY, playerInPath, dodge, airborne, healthBefore, samples };
        }
        finally
        {
            if (spear != null && spear.IsSpawned) GameManager.Instance.Network.ServerManager.Despawn(spear);
            UnityEngine.Object.Destroy(deck);
            foreach (var obstacle in obstacles) UnityEngine.Object.Destroy(obstacle);
            body.position = originalPosition; player.transform.position = originalPosition; body.constraints = constraints;
        }
    }
    public static object ShowLanceMesh()
    {
        var root = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyLance.prefab"));
        root.name = "Preview Spear Mesh"; root.transform.SetPositionAndRotation(new Vector3(80f, 20f, 0f), Quaternion.identity);
        root.GetComponent<EnemyLanceProjectile>().enabled = false;
        var camera = new GameObject("Preview Spear Camera", typeof(Camera)).GetComponent<Camera>();
        camera.transform.position = new Vector3(80f, 20f, -10f); camera.orthographic = true; camera.orthographicSize = 1.2f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.16f, 0.2f, 0.25f);
        return new { root.name, camera = camera.name };
    }
    public static void ClearMeshPreview()
    {
        foreach (var name in new[] { "Preview Spear Mesh", "Preview Spear Camera" })
        { var go = GameObject.Find(name); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
    }    public static object ApplyAimAndSpearPresentation()
    {
        MechaAdjustmentsSetup.AimFeedback();
        var settings = MechaPresentationSetup.Profile<EnemyLanceSettings>("EnemyLance");
        settings.projectileModelEuler = new Vector3(0f, 0f, -14.906113f);
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyLance.prefab");
        var mesh = prefab.GetComponentInChildren<MeshFilter>();
        var rotation = Quaternion.Euler(settings.projectileModelEuler);
        settings.tipOffset = mesh.sharedMesh.vertices.Max(v => (rotation * (v - mesh.sharedMesh.bounds.center)).x) * mesh.transform.localScale.x;
        UnityEditor.EditorUtility.SetDirty(settings); UnityEditor.AssetDatabase.SaveAssetIfDirty(settings);
        typeof(MechaNetworkSetup).GetMethod("BuildProjectile", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, new object[] { settings });
        UnityEditor.AssetDatabase.SaveAssets();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.isDirty) UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        return new { settings.tipOffset, correction = settings.projectileModelEuler.ToString(), sceneSaved = !scene.isDirty };
    }    public static async Task<object> PreviewAimCapture()
    {
        var owner = UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).First(p => p.IsOwner && p.IsSpawned);
        var enemies = EnemyController.ActiveEnemies.Where(e => !e.IsDead).ToArray();
        owner.GetComponent<PlayerHealth>().EnterLevel(new Vector3(-5f, -3.4f, 0f)); owner.Face(1);
        owner.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        for (int i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i]; enemy.PracticePassive = true; enemy.CancelAttack();
            enemy.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            var position = i == 0 ? new Vector3(-3f, 0.8f, 0f) : new Vector3(80f + i, 0f, 0f);
            enemy.GetComponent<Rigidbody>().position = position; enemy.transform.position = position;
        }
        Physics.SyncTransforms();
        var aim = owner.GetComponent<MechaAutoAim>();
        var reticle = owner.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "SMG Aim Reticle");
        var animator = reticle.GetComponent<Animator>(); var samples = new System.Collections.Generic.List<object>();
        float started = Time.time;
        for (int i = 0; i < 24; i++)
        {
            await Task.Delay(70);
            Vector3 delta = aim.TargetPoint - aim.Origin;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            samples.Add(new { elapsed = Time.time - started, aim.Angle, aim.IsLocked, target = aim.Target?.TargetTransform.name,
                error = Mathf.Abs(Mathf.DeltaAngle(aim.Angle, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg)),
                state = state.IsName("Scan") ? "Scan" : state.IsName("Capture") ? "Capture" : state.IsName("Track") ? "Track" : "other",
                size = reticle.transform.localScale.x, offset = reticle.transform.localPosition.ToString("F3"),
                z = reticle.transform.position.z, reticle.sortingLayerName, reticle.sortingOrder });
        }
        return samples;
    }    public static async Task<object> PreviewSpearAlignment()
    {
        var settings = MechaPresentationSetup.Profile<EnemyLanceSettings>("EnemyLance");
        var spear = UnityEngine.Object.Instantiate(settings.projectile, new Vector3(80f, 20f, 0f), Quaternion.identity);
        var flight = spear.GetComponent<EnemyLanceProjectile>(); flight.Launch(settings, new Vector3(90f, 20f, 0f), null);
        GameManager.Instance.Network.ServerManager.Spawn(spear);
        var mesh = spear.GetComponentInChildren<MeshFilter>();
        var min = mesh.sharedMesh.vertices.OrderBy(v => v.x).First(); var max = mesh.sharedMesh.vertices.OrderByDescending(v => v.x).First();
        var samples = new System.Collections.Generic.List<object>(); Vector3 previous = spear.GetComponent<Rigidbody>().position;
        try
        {
            for (int i = 0; i < 12; i++)
            {
                await Task.Delay(35);
                Vector3 position = spear.GetComponent<Rigidbody>().position;
                Vector3 axis = mesh.transform.TransformPoint(max) - mesh.transform.TransformPoint(min);
                Vector3 motion = position - previous;
                samples.Add(new { position = position.ToString("F3"), velocity = flight.Velocity.ToString("F3"),
                    meshToForwardDegrees = Vector3.Angle(axis, spear.transform.right),
                    meshToMotionDegrees = motion.sqrMagnitude > 0.0001f ? Vector3.Angle(axis, motion) : -1f });
                previous = position;
            }
        }
        finally { if (spear != null && spear.IsSpawned) GameManager.Instance.Network.ServerManager.Despawn(spear); }
        return samples;
    }    public static object ApplyTrackingAndGunTuning()
    {
        var settings = MechaPresentationSetup.Profile<SecondaryWeaponSettings>("SecondarySMG");
        settings.capacity = 12; settings.roundsPerSecond = 14f; settings.damage = 8f; settings.refillDelay = 1.5f;
        settings.lockedAimSmoothTime = 0.08f; settings.lockedTargetSmoothTime = 0.1f; settings.lockedDegreesPerSecond = 540f;
        UnityEditor.EditorUtility.SetDirty(settings); UnityEditor.AssetDatabase.SaveAssetIfDirty(settings);
        var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Interface/AimTracked.anim");
        foreach (var axis in new[] { "x", "y", "z" })
            UnityEditor.AnimationUtility.SetEditorCurve(clip, UnityEditor.EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale." + axis),
                AnimationCurve.Constant(0f, 0.8f, 0.55f));
        UnityEditor.EditorUtility.SetDirty(clip); UnityEditor.AssetDatabase.SaveAssetIfDirty(clip);
        MechaSettingsSetup.Organize();
        UnityEditor.AssetDatabase.SetLabels(UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/Editor/MechaSettingsSetup.cs"), new[] { "Others" });
        UnityEditor.AssetDatabase.SaveAssets();
        return new { settings.capacity, settings.roundsPerSecond, settings.damage, settings.refillDelay,
            path = UnityEditor.AssetDatabase.GetAssetPath(settings),
            profiles = UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject", new[] { MechaPresentationSetup.ConfigFolder })
                .Select(g => UnityEditor.AssetDatabase.GUIDToAssetPath(g)).OrderBy(p => p).ToArray() };
    }    public static async Task<object> PreviewMovingLock()
    {
        await PreviewAimCapture();
        var owner = UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).First(p => p.IsOwner && p.IsSpawned);
        var aim = owner.GetComponent<MechaAutoAim>();
        var enemy = aim.Target as EnemyController;
        if (enemy == null) return new { message = "No enemy in staged view", target = aim.Target?.TargetTransform.name };
        var body = enemy.GetComponent<Rigidbody>(); Vector3 origin = body.position;
        var reticle = owner.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "SMG Aim Reticle");
        var animator = reticle.GetComponent<Animator>(); var samples = new System.Collections.Generic.List<object>();
        float started = Time.time, previousAngle = aim.Angle; int unlockedSamples = 0, captureSamples = 0;
        float maxAngularStep = 0f;
        for (int i = 0; i < 48; i++)
        {
            float time = Time.time - started;
            Vector3 position = origin + new Vector3(Mathf.Sin(time * 3f) * 1.2f, Mathf.Sin(time * 4f) * 0.45f, 0f);
            body.position = position; enemy.transform.position = position; Physics.SyncTransforms();
            await Task.Delay(35);
            float step = Mathf.Abs(Mathf.DeltaAngle(previousAngle, aim.Angle)); maxAngularStep = Mathf.Max(maxAngularStep, step);
            if (!aim.IsLocked) unlockedSamples++;
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Capture")) captureSamples++;
            if (i % 8 == 0) samples.Add(new { elapsed = Time.time - started, aim.IsLocked, angle = aim.Angle,
                rawTarget = enemy.AimPoint.ToString("F3"), smoothTarget = aim.TargetPoint.ToString("F3"),
                marker = reticle.transform.position.ToString("F3"), reticle.enabled, size = reticle.transform.localScale.x });
            previousAngle = aim.Angle;
        }
        return new { unlockedSamples, captureSamples, maxAngularStep, samples };
    }
    public static async Task<object> ObserveSMGBurst()
    {
        var owner = UnityEngine.Object.FindObjectsByType<PlayerInputs>(FindObjectsSortMode.None).First(p => p.IsOwner && p.IsSpawned);
        var input = owner.GetComponent<PlayerInput>(); var weapon = owner.GetComponent<PlayerSecondaryWeapon>();
        var pad = InputSystem.AddDevice<Gamepad>("SMGTuningPreview");
        var samples = new System.Collections.Generic.List<object>(); float start = Time.time;
        Action sample = () => samples.Add(new { elapsed = Time.time - start, weapon.Ammo, weapon.Capacity, owner.FireHeld });
        try
        {
            input.SwitchCurrentControlScheme("Gamepad", pad);
            sample(); InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = 1f });
            await Task.Delay(350); sample(); await Task.Delay(250); sample(); await Task.Delay(350); sample();
            InputSystem.QueueStateEvent(pad, new GamepadState());
            await Task.Delay(900); sample(); await Task.Delay(650); sample(); await Task.Delay(2500); sample();
        }
        finally { InputSystem.RemoveDevice(pad); }
        return samples;
    }
    public static async Task<object> PreviewSMGDamage()
    {
        var enemy = EnemyController.ActiveEnemies.First(e => !e.IsDead);
        var player = UnityEngine.Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None).First(p => p.IsOwner);
        var settings = MechaPresentationSetup.Profile<SecondaryWeaponSettings>("SecondarySMG");
        float before = enemy.Health;
        var bullet = UnityEngine.Object.Instantiate(settings.projectile, enemy.AimPoint + Vector3.left * 1.4f, Quaternion.identity);
        GameManager.Instance.Network.ServerManager.Spawn(bullet);
        bullet.GetComponent<SecondaryWeaponProjectile>().Launch(settings, player, Vector3.right);
        await Task.Delay(180);
        return new { before, after = enemy.Health, settings.damage, alive = bullet != null && bullet.IsSpawned };
    }    public static object InspectCategorizedSettings()
    {
        var paths = UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject", new[] { MechaPresentationSetup.ConfigFolder })
            .Select(UnityEditor.AssetDatabase.GUIDToAssetPath).ToArray();
        var mismatches = new System.Collections.Generic.List<string>(); var missing = new System.Collections.Generic.List<string>();
        foreach (var path in paths)
        {
            var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
            var resolve = typeof(MechaPresentationSetup).GetMethod("Profile").MakeGenericMethod(asset.GetType()).Invoke(null, new object[] { asset.name });
            if (!ReferenceEquals(resolve, asset)) mismatches.Add(path);
            var serialized = new UnityEditor.SerializedObject(asset); var property = serialized.GetIterator();
            while (property.Next(true))
                if (property.propertyType == UnityEditor.SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                    missing.Add(path + ":" + property.propertyPath);
        }
        UnityEditor.AssetDatabase.SetLabels(UnityEditor.AssetDatabase.LoadMainAssetAtPath(MechaPresentationSetup.ConfigFolder), new[] { "Others" });
        UnityEditor.AssetDatabase.SaveAssets();
        var gun = MechaPresentationSetup.Profile<SecondaryWeaponSettings>("SecondarySMG");
        MechaSettingsSetup.OpenCategories();
        return new { count = paths.Length, factoryMismatches = mismatches, missingReferences = missing,
            categories = UnityEditor.AssetDatabase.GetSubFolders(MechaPresentationSetup.ConfigFolder).Select(System.IO.Path.GetFileName).OrderBy(p => p).ToArray(),
            gunPath = UnityEditor.AssetDatabase.GetAssetPath(gun), gun.capacity, gun.damage, gun.roundsPerSecond, gun.refillDelay,
            prefabReferencesGun = UnityEditor.AssetDatabase.GetDependencies("Assets/_Project/Prefabs/MechaPlayer.prefab").Contains(UnityEditor.AssetDatabase.GetAssetPath(gun)),
            sceneClean = !UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty };
    }    public static object ConfigureNearestTargeting()
    {
        var settings = MechaPresentationSetup.Profile<SecondaryWeaponSettings>("SecondarySMG");
        settings.targetSwitchDelay = 0.65f; settings.targetSwitchDistanceMargin = 0.15f;
        UnityEditor.EditorUtility.SetDirty(settings); UnityEditor.AssetDatabase.SaveAssetIfDirty(settings);
        return new { settings.targetSwitchDelay, settings.targetSwitchDistanceMargin, settings.capacity, settings.roundsPerSecond };
    }
    public static async Task<object> PreviewNearestSelection()
    {
        var owner = UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).First(p => p.IsOwner && p.IsSpawned);
        var enemies = EnemyController.ActiveEnemies.Where(e => !e.IsDead).ToArray();
        owner.GetComponent<PlayerHealth>().EnterLevel(new Vector3(-7f, -3.4f, 0f)); owner.Face(1);
        owner.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        Action<EnemyController, Vector3> place = (enemy, position) => {
            enemy.GetComponent<Rigidbody>().position = position; enemy.transform.position = position; Physics.SyncTransforms(); };
        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i].PracticePassive = true; enemies[i].CancelAttack();
            enemies[i].GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            place(enemies[i], new Vector3(80f + i, -3.4f, 0f));
        }
        var aim = owner.GetComponent<MechaAutoAim>();
        var deadline = typeof(MechaAutoAim).GetField("switchAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var samples = new System.Collections.Generic.List<object>(); float started = Time.time;
        Func<string> current = () => ReferenceEquals(aim.Target, enemies[0]) ? "A" : ReferenceEquals(aim.Target, enemies[1]) ? "B"
            : ReferenceEquals(aim.Target, enemies[2]) ? "C" : aim.Target?.TargetTransform.name ?? "none";
        Action<string> sample = label => samples.Add(new { label, elapsed = Time.time - started,
            target = current(), switchAllowedIn = Mathf.Max(0f, (float)deadline.GetValue(aim) - Time.time) });
        await Task.Delay(450);
        place(enemies[0], new Vector3(-1f, -3.4f, 0f));
        await Task.Delay(150); sample("A enters");
        place(enemies[1], new Vector3(-4.5f, -3.4f, 0f)); sample("B is nearer");
        for (int i = 0; i < 12; i++) { await Task.Delay(70); if (i % 2 == 0) sample("Waiting for B"); }
        place(enemies[2], new Vector3(-5.6f, -3.4f, 0f)); sample("C is nearest");
        for (int i = 0; i < 12; i++) { await Task.Delay(70); if (i % 2 == 0) sample("Waiting for C"); }
        place(enemies[1], new Vector3(-5.65f, -3.4f, 0f)); sample("B and C nearly tied");
        await Task.Delay(900); sample("Near tie stays stable");
        place(enemies[1], new Vector3(-6f, -3.4f, 0f)); sample("B clearly closer");
        await Task.Delay(220); sample("Nearest reconsidered");
        return samples;
    }}
