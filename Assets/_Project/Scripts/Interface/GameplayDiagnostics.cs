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
        history[0] = $"{Time.frameCount:000000}   {command}"; historyCount = Mathf.Min(historyCount + 1, history.Length);
    }
    private void RecordSkill(int slot) => RecordInput(GameInputPresentation.Hint(slot == 0 ? "SkillA" : slot == 1 ? "SkillS" : "SkillD") +
        (slot == 0 ? " / MEGA" : slot == 1 ? " / FIRE" : " / CRITICAL X"));
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
        { notice.text = "Enemy practice control belongs to the local host."; return; }
        bool passive = false;
        foreach (var enemy in EnemyController.ActiveEnemies) { passive = !enemy.PracticePassive; break; }
        foreach (var enemy in EnemyController.ActiveEnemies) enemy.PracticePassive = passive;
        notice.text = passive ? "Enemy AI held. Damage, flinch, launch and physics remain active." : "Enemy AI resumed.";
    }
    public void ToggleHitboxes() { hitboxes = !hitboxes; notice.text = hitboxes ? "Hit volume shown in Scene view with Gizmos enabled." : "Hit volume hidden."; }
    public void ResetCounters()
    {
        player?.ResetDamageReadout(); firstDamage = -1f; sessionDamage = 0f; historyCount = 0;
        Array.Clear(history, 0, history.Length); notice.text = "Damage and input counters reset.";
    }
    public void RefillPractice()
    {
        if (player == null || player.NetworkObject.IsSpawned) { notice.text = "Refill is available in the offline practice deck."; return; }
        player.ResetPracticeResources(); notice.text = "Practice energy and skill cooldowns reset.";
    }
    public void CopySnapshot()
    {
        Refresh(); GUIUtility.systemCopyBuffer = readout.text + "\nINPUTS\n" + inputHistory.text;
        notice.text = "Snapshot copied to clipboard.";
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
        text.AppendLine($"PERFORMANCE    {(mean > 0f ? 1000f / mean : 0f):0} FPS    {mean:0.00} ms    P95 {p95:0.00} ms");
        text.AppendLine($"MEMORY         {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f:0} MB    MANAGED {GC.GetTotalMemory(false) / 1048576f:0.0} MB");
        text.AppendLine(connected ? $"LINK           {(network.ServerManager.Started ? "LOCAL HOST" : "CLIENT")}    RTT {network.TimeManager.RoundTripTime} ms    {network.TimeManager.TickRate} Hz" : "LINK           OFFLINE    RTT --");
        if (player != null)
        {
            text.AppendLine($"MOVEMENT       {movement.MovementState}    Vx {movement.Velocity.x:0.00}    Vy {movement.Velocity.y:0.00}");
            text.AppendLine($"SUPPORT        {(movement.IsGrounded ? "GROUND" : "AIR")}    DASH RECOVERY {movement.RecoveryRemaining * 1000f:0} ms");
            text.AppendLine($"ACTION         {player.CurrentAction} / {player.Phase}    {player.ActionElapsed * 60f:0.0} / {player.ActionDuration * 60f:0.0}f @60");
            text.AppendLine($"CANCEL         {player.CancelRemaining * 1000f:0} ms    {(player.IsSkillActive ? "SKILL LOCK" : "BRANCH WINDOWS")}");
            text.AppendLine($"DAMAGE         LAST {player.LastDamage:0}    COMBO {player.ComboDamage:0} / {player.ComboHits} HITS");
            text.AppendLine($"SESSION        {player.TotalDamage:0}    BEST {player.PeakComboDamage:0}    AVG DPS {(firstDamage < 0f ? 0f : sessionDamage / Mathf.Max(1f, Time.unscaledTime - firstDamage)):0.0}");
        }
        int juggling = 0;
        foreach (var enemy in EnemyController.ActiveEnemies) if (enemy != null && !enemy.IsDead && enemy.IsJuggling) juggling++;
        text.AppendLine($"ENEMIES        {EnemyController.ActiveEnemies.Count}    AIRBORNE {juggling}    TEMPO {Time.timeScale:0.00}x");
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
