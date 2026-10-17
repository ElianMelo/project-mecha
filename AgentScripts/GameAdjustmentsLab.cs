using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Manual live inspection helpers; these mutate disposable Play Mode state only.
public static class GameAdjustmentsLab
{
    public static object Snapshot() => new {
        frame = Time.frameCount, time = Time.time, scale = Time.timeScale,
        players = UnityEngine.Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).Select(p => new {
            p.PilotName, p.ColorSlot, p.IsOwner, p.IsSpawned,
            position = p.transform.position.ToString(), ammo = p.GetComponent<PlayerSecondaryWeapon>().Ammo,
            direction = p.GetComponent<MechaAutoAim>().Direction.ToString(),
            target = p.GetComponent<MechaAutoAim>().Target?.TargetTransform.name,
            ring = p.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r => r.name == "Teammate Ring")?.enabled
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
        var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyLanceSettings>("Assets/_Project/Settings/EnemyLance.asset");
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
        var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyLanceSettings>("Assets/_Project/Settings/EnemyLance.asset");
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
}
