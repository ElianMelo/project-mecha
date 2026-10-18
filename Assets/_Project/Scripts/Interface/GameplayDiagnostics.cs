using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Telemetry, offline resource/tempo controls, and authority-checked enemy practice.
public sealed class GameplayDiagnostics : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text readout, inputHistory, notice;
    [SerializeField] private UnityEngine.UI.Image[] frameMeter;
    [SerializeField] private GameplayHudLayoutSettings settings;
    private PlayerAttack player;
    private PlayerMovement movement;
    private PlayerInputs inputs;
    private readonly float[] frameTimes = new float[240];
    private readonly float[] sortedTimes = new float[240];
    private readonly string[] history = new string[12];
    private int samples, cursor, historyCount, meterCursor;
    private float nextRefresh, nextSearch, meterTime, firstDamage = -1f, sessionDamage;
    private bool hitboxes;
    private bool skipFrameSample;
    private readonly StringBuilder text = new StringBuilder(1600);
    public bool Visible => panel != null && panel.activeSelf;
    private void Start()
    {
        panel.SetActive(false);
    }
    private void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.pauseStateChanged += EditorPauseChanged;
#endif
    }
#if UNITY_EDITOR
    private void EditorPauseChanged(UnityEditor.PauseState state)
    { samples = cursor = 0; meterTime = 0f; skipFrameSample = true; }
#endif
    private void Update()
    {
        if (skipFrameSample) skipFrameSample = false;
        else
        {
            frameTimes[cursor++ % frameTimes.Length] = Time.unscaledDeltaTime * 1000f;
            samples = Mathf.Min(samples + 1, frameTimes.Length);
        }
        if (player == null && Time.unscaledTime >= nextSearch)
        { nextSearch = Time.unscaledTime + 0.5f; BindPlayer(); }
        if (!Visible) return;
        meterTime += Time.unscaledDeltaTime;
        if (frameMeter != null && frameMeter.Length > 0 && meterTime >= 1f / 60f)
        {
            meterTime %= 1f / 60f;
            string phase = player != null ? player.Phase : "NEUTRAL";
            frameMeter[meterCursor++ % frameMeter.Length].color = phase == "STARTUP" ? settings.startupColor :
                phase == "ACTIVE" ? settings.activeColor : phase == "HIT STOP" ? Color.white :
                phase == "RECOVERY" ? settings.recoveryColor : settings.neutralColor;
        }
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + settings.diagnosticsRefresh;
        Refresh();
    }
    private void BindPlayer()
    {
        foreach (var candidate in FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None))
        {
            if (candidate.NetworkObject.IsSpawned && !candidate.IsOwner) continue;
            player = candidate; movement = player.GetComponent<PlayerMovement>(); inputs = player.GetComponent<PlayerInputs>();
            inputs.OnCommand += RecordInput; inputs.OnSkillInput += RecordSkill; player.HitConfirmed += RecordDamage;
            break;
        }
    }
    private void RecordInput(string command)
    {
        for (int i = history.Length - 1; i > 0; i--) history[i] = history[i - 1];
        history[0] = GameText.Get("Diagnostics", "input.history", (Time.frameCount), (command)); historyCount = Mathf.Min(historyCount + 1, history.Length);
    }
    private void RecordSkill(int slot) => RecordInput(GameInputPresentation.Hint(slot == 0 ? "SkillA" : slot == 1 ? "SkillS" : "SkillD") +
        (slot == 0 ? GameText.Get("Diagnostics", "skill.mega") : slot == 1 ? GameText.Get("Diagnostics", "skill.fire") : GameText.Get("Diagnostics", "skill.critical")));
    private void RecordDamage(float damage) { if (firstDamage < 0f) firstDamage = Time.unscaledTime; sessionDamage += damage; }
    public void Toggle()
    {
        panel.SetActive(!panel.activeSelf);
        GameManager.Instance?.SuppressGameplayInput();
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events != null) events.SetSelectedGameObject(Visible ? panel.GetComponentInChildren<UnityEngine.UI.Button>()?.gameObject : null);
    }
    public void ToggleEnemyPractice()
    {
        var network = GameManager.Instance != null ? GameManager.Instance.Network : null;
        if (network != null && network.ClientManager.Started && !network.ServerManager.Started)
        { notice.text = GameText.Get("Diagnostics", "practice.hostOnly"); return; }
        bool passive = false;
        foreach (var enemy in EnemyController.ActiveEnemies) { passive = !enemy.PracticePassive; break; }
        foreach (var enemy in EnemyController.ActiveEnemies) enemy.PracticePassive = passive;
        notice.text = passive ? GameText.Get("Diagnostics", "ai.held") : GameText.Get("Diagnostics", "ai.resumed");
    }
    public void ToggleHitboxes() { hitboxes = !hitboxes; notice.text = hitboxes ? GameText.Get("Diagnostics", "hitbox.shown") : GameText.Get("Diagnostics", "hitbox.hidden"); }
    public void ResetCounters()
    {
        player?.ResetDamageReadout(); firstDamage = -1f; sessionDamage = 0f; historyCount = 0;
        Array.Clear(history, 0, history.Length); notice.text = GameText.Get("Diagnostics", "counters.reset");
    }
    public void RefillPractice()
    {
        if (player == null || player.NetworkObject.IsSpawned) { notice.text = GameText.Get("Diagnostics", "refill.offlineOnly"); return; }
        player.ResetPracticeResources(); notice.text = GameText.Get("Diagnostics", "resources.reset");
    }
    public void CopySnapshot()
    {
        Refresh(); GUIUtility.systemCopyBuffer = readout.text + GameText.Get("Diagnostics", "input.heading") + inputHistory.text;
        notice.text = GameText.Get("Diagnostics", "snapshot.copied");
    }
    private void Refresh()
    {
        float sum = 0f;
        for (int i = 0; i < samples; i++) { sum += frameTimes[i]; sortedTimes[i] = frameTimes[i]; }
        Array.Sort(sortedTimes, 0, samples);
        float mean = samples > 0 ? sum / samples : 0f;
        float p95 = samples > 0 ? sortedTimes[Mathf.Clamp(Mathf.CeilToInt(samples * 0.95f) - 1, 0, samples - 1)] : 0f;
        var network = GameManager.Instance != null ? GameManager.Instance.Network : null;
        bool connected = network != null && network.ClientManager.Started;
        text.Clear();
        text.AppendLine(GameText.Get("Diagnostics", "stats.performance", ((mean > 0f ? 1000f / mean : 0f)), (mean), (p95)));
        text.AppendLine(GameText.Get("Diagnostics", "stats.memory", (UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f), (GC.GetTotalMemory(false) / 1048576f)));
        text.AppendLine(connected ? GameText.Get("Diagnostics", "stats.link", ((network.ServerManager.Started ? GameText.Get("Diagnostics", "link.host") : GameText.Get("Diagnostics", "link.client"))), (network.TimeManager.RoundTripTime), (network.TimeManager.TickRate)) : GameText.Get("Diagnostics", "link.offline"));
        if (player != null)
        {
            text.AppendLine(GameText.Get("Diagnostics", "stats.movement", (GameText.Term("Combat", movement.MovementState)), (movement.Velocity.x), (movement.Velocity.y)));
            text.AppendLine(GameText.Get("Diagnostics", "stats.support", ((movement.IsGrounded ? GameText.Get("Diagnostics", "support.ground") : GameText.Get("Diagnostics", "support.air"))), (movement.RecoveryRemaining * 1000f)));
            text.AppendLine(GameText.Get("Diagnostics", "stats.action", (GameText.Term("Combat", player.CurrentAction)), (GameText.Term("Combat", player.Phase)), (player.ActionElapsed * 60f), (player.ActionDuration * 60f)));
            text.AppendLine(GameText.Get("Diagnostics", "stats.cancel", (player.CancelRemaining * 1000f), ((player.IsSkillActive ? GameText.Get("Diagnostics", "cancel.skillLock") : GameText.Get("Diagnostics", "cancel.branchWindows")))));
            text.AppendLine(GameText.Get("Diagnostics", "stats.damage", (player.LastDamage), (player.ComboDamage), (player.ComboHits)));
            text.AppendLine(GameText.Get("Diagnostics", "stats.session", (player.TotalDamage), (player.PeakComboDamage), ((firstDamage < 0f ? 0f : sessionDamage / Mathf.Max(1f, Time.unscaledTime - firstDamage)))));
        }
        int juggling = 0;
        foreach (var enemy in EnemyController.ActiveEnemies) if (enemy != null && !enemy.IsDead && enemy.IsJuggling) juggling++;
        text.AppendLine(GameText.Get("Diagnostics", "stats.enemies", (EnemyController.ActiveEnemies.Count), (juggling), (Time.timeScale)));
        readout.text = text.ToString();
        text.Clear();
        for (int i = 0; i < historyCount; i++) text.AppendLine(history[i]);
        inputHistory.text = text.ToString();
    }
    private void OnDrawGizmos()
    {
        if (!hitboxes || player == null || player.ActiveData == null) return;
        var data = player.ActiveData;
        Gizmos.color = player.Phase == "ACTIVE" ? Color.red : Color.cyan;
        Gizmos.DrawWireCube(player.transform.position + new Vector3(movement.Facing * data.reach, data.height, 0f), data.hitHalfSize * 2f);
    }
    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.pauseStateChanged -= EditorPauseChanged;
#endif
    }
    private void OnDestroy()
    {
        if (inputs != null) { inputs.OnCommand -= RecordInput; inputs.OnSkillInput -= RecordSkill; }
        if (player != null) player.HitConfirmed -= RecordDamage;
    }
}
