#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Explicit local development preview. No endpoint, assertions or production installation.
// --mecha-local-client --mecha-inspect-adjustments records one peer's real presentation.
public sealed class GameAdjustmentsInspection : MonoBehaviour
{
    private float started = -1f, nextLog;
    private bool composed, sent, firing, released;
    private PlayerIdentity owner;
    private Keyboard previewKeyboard;
    private Mouse previewMouse;
    private readonly System.Collections.Generic.HashSet<int> seenBullets = new System.Collections.Generic.HashSet<int>();
    private void Awake()
    {
        // Batch players have no physical devices. Supply both required scheme devices
        // before PlayerInput activates on the locally owned spawn.
        previewKeyboard = InputSystem.AddDevice<Keyboard>("AdjustmentsPreviewKeyboard");
        previewMouse = InputSystem.AddDevice<Mouse>("AdjustmentsPreviewMouse");
    }
    private void OnDestroy() => RemoveDevices();
    private void RemoveDevices()
    {
        if (previewKeyboard != null && previewKeyboard.added) InputSystem.RemoveDevice(previewKeyboard);
        if (previewMouse != null && previewMouse.added) InputSystem.RemoveDevice(previewMouse);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Environment.GetCommandLineArgs().Contains("--mecha-inspect-adjustments"))
            new GameObject("Local Adjustments Inspection", typeof(GameAdjustmentsInspection));
    }
    private void Update()
    {
        if (owner == null)
        {
            owner = FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsSpawned && p.IsOwner);
            if (owner == null) return;
            var input = owner.GetComponent<PlayerInput>();
            if (!input.isActiveAndEnabled || !input.user.valid) { owner = null; return; }
            input.SwitchCurrentControlScheme("Keyboard&Mouse", previewKeyboard, previewMouse);
            started = Time.unscaledTime;
        }
        float elapsed = Time.unscaledTime - started;
        foreach (var bullet in FindObjectsByType<SecondaryWeaponProjectile>(FindObjectsSortMode.None))
            if (bullet.IsSpawned) seenBullets.Add(bullet.ObjectId);
        if (elapsed > 38f)
        {
            RemoveDevices();
            enabled = false; return;
        }
        if (!composed && elapsed > 3f && MechaChat.Instance != null)
        { MechaChat.Instance.Open(); MechaChat.Instance.InputField.text = "Remote squad check"; composed = true; }
        else if (composed && !sent && elapsed > 3.2f) { MechaChat.Instance.Submit(); sent = true; }
        if (!firing && elapsed > 15f)
        { owner.GetComponent<PlayerMovement>().Face(1); InputSystem.QueueStateEvent(previewKeyboard, new KeyboardState(Key.C)); firing = true; }
        if (!released && elapsed > 18f)
        { InputSystem.QueueStateEvent(previewKeyboard, new KeyboardState()); released = true; }
        if (Time.unscaledTime < nextLog) return;
        nextLog = Time.unscaledTime + 1f;
        Debug.Log("MECHA_ADJUSTMENTS " + JsonUtility.ToJson(new Observation {
            elapsed = elapsed, ammo = owner.GetComponent<PlayerSecondaryWeapon>().Ammo, scale = Time.timeScale,
            chat = MechaChat.Instance?.VisibleMessages,
            chatVisible = MechaChat.Instance != null && MechaChat.Instance.ContentVisible,
            bulletsSeen = seenBullets.Count,
            pilots = FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).Select(p =>
                $"{p.PilotName} slot={p.ColorSlot} owner={p.IsOwner} ring={p.GetComponentsInChildren<SpriteRenderer>().First(r => r.name == "Teammate Ring").enabled} nameVisible={p.GetComponentsInChildren<TMPro.TMP_Text>().First(t => t.name == "Pilot Name").enabled}").ToArray(),
            enemies = EnemyController.ActiveEnemies.Where(e => !e.IsDead).Select(e => $"{e.ObjectId}:{e.Health:0}").ToArray()
        }));
    }
    [Serializable] private sealed class Observation
    { public float elapsed, scale; public int ammo, bulletsSeen; public bool chatVisible; public string chat; public string[] pilots, enemies; }
}
#endif
