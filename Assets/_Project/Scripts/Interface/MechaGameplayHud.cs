using TMPro;
using UnityEngine;

public class MechaGameplayHud : MonoBehaviour
{
    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text[] skills;
    [SerializeField] private UnityEngine.UI.Image healthFill;
    [SerializeField] private UnityEngine.UI.Image manaFill;
    [SerializeField] private TMP_Text healthValue, energyValue, motion, comboReadout, link;
    [SerializeField] private GameplayHudLayoutSettings settings;
    [SerializeField] private TMP_Text controls;
    [SerializeField] private CanvasGroup controlsPanel;
    [SerializeField] private UnityEngine.UI.Button controlsToggle;
    [SerializeField] private TMP_Text controlsToggleLabel;
    [SerializeField] private RectTransform controlsRoot;
    [SerializeField] private ChatSettings chatLayout;
    [SerializeField] private TMP_Text ammunition;
    private PlayerMovement movement;
    private PlayerAttack player;
    private PlayerHealth health;
    private PlayerSecondaryWeapon secondary;
    private float nextSearch;
    private GameplayHudFeedback feedback;
    private int promptRevision = -1;
    private bool controlsVisible;
    private bool refreshControlsLayout;
    private readonly string[] skillNames = new string[3];
    private static readonly string[] SkillTitles = { "MEGA SLASH", "SWORD FIRE", "CRITICAL X" };
    private static readonly string[] SkillActions = { "SkillA", "SkillS", "SkillD" };
    private void Awake()
    {
        feedback = GetComponent<GameplayHudFeedback>();
        if (controlsPanel != null) controlsPanel.alpha = 0f;
        RefreshControlsToggle();
    }
    public void ToggleControls()
    {
        if (MechaChat.IsTyping || controlsPanel == null || GameManager.Instance != null && GameManager.Instance.LobbyOpen) return;
        controlsVisible = !controlsVisible;
        controlsPanel.alpha = controlsVisible ? 1f : 0f;
        refreshControlsLayout = controlsVisible;
        RefreshControlsToggle();
    }
    private void LateUpdate()
    {
        if (!refreshControlsLayout) return;
        refreshControlsLayout = false;
        controls?.SetAllDirty();
        Canvas.ForceUpdateCanvases();
    }
    private void RefreshControlsToggle()
    {
        if (controlsToggle != null) controlsToggle.interactable = !MechaChat.IsTyping;
        if (controlsToggleLabel != null)
        {
            controlsToggleLabel.text = GameInputPresentation.Hint("ControlsHelp") + (controlsVisible ? "  HIDE CONTROLS" : "  SHOW CONTROLS");
            controlsToggleLabel.alpha = MechaChat.IsTyping ? 0.4f : 1f;
        }
    }
    private void Update()
    {
        RefreshControlsToggle();
        if (controlsRoot != null)
        {
            controlsRoot.gameObject.SetActive(GameManager.Instance == null || !GameManager.Instance.LobbyOpen);
            if (chatLayout != null) controlsRoot.anchoredPosition = chatLayout.offset + Vector2.down *
                (MechaChat.Instance != null && MechaChat.Instance.ContentVisible ? chatLayout.size.y + 12f : 0f);
        }
        if (promptRevision != GameInputPresentation.Revision)
        {
            promptRevision = GameInputPresentation.Revision;
            for (int i = 0; i < skillNames.Length; i++) skillNames[i] = GameInputPresentation.Hint(SkillActions[i]) + "  " + SkillTitles[i];
            if (controls != null) controls.text = GameInputPresentation.UsingGamepad && GameManager.Instance != null
                ? GameInputPresentation.ControllerHelp(GameManager.Instance.ControllerSettings) : settings.tutorialText;
        }
        if (player == null && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 0.5f;
            foreach (var candidate in FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None))
                if (!candidate.NetworkObject.IsSpawned || candidate.IsOwner) { player = candidate; health = candidate.GetComponent<PlayerHealth>(); movement = candidate.GetComponent<PlayerMovement>(); secondary = candidate.GetComponent<PlayerSecondaryWeapon>(); break; }
        }
        if (player == null)
        {
            feedback?.Combo(false, 0);
            status.text = "MECHA // SYSTEM STANDBY";
            if (motion != null) motion.text = GameInputPresentation.UsingGamepad ? GameInputPresentation.Hint("Menu") + "  /  PLAY & ONLINE LOBBIES" : "1 LOCAL HOST    3 ONLINE LOBBIES";
            if (link != null) link.text = "NO LINK   /   " + GameInputPresentation.Hint("Diagnostics") + " DIAGNOSTICS";
            return;
        }
        status.text = "MK-01  //  MECHA KNIGHT";
        if (ammunition != null) ammunition.text = secondary != null ? $"{GameInputPresentation.Hint("SecondaryFire")}  SMG   {secondary.Ammo:00} / {secondary.Capacity}   ·   AUTO REFILL" : "";
        if (healthValue != null) healthValue.text = $"AP   {health.Health:000} <size=12>/ {health.MaxHealth:0}</size>";
        if (energyValue != null) energyValue.text = $"EN   {player.Mana:000} <size=12>/ {player.MaxMana:0}</size>";
        if (motion != null) motion.text = $"{movement.MovementState.ToUpperInvariant()}    {Mathf.Abs(movement.Velocity.x):0.0} m/s    //    {player.Phase}";
        if (motion != null && GameInputPresentation.Disconnected) motion.text = "CONTROLLER DISCONNECTED / RECONNECT OR USE KEYBOARD";
        if (comboReadout != null && (player.ComboVisible || feedback == null)) comboReadout.text = player.ComboVisible ? $"<size=38>{player.ComboHits:00}</size>  HITS\n<color=#71E5E8>{player.ComboDamage:0}</color> DAMAGE" : "";
        feedback?.Combo(player.ComboVisible, player.ComboHits);
        var network = GameManager.Instance != null ? GameManager.Instance.Network : null;
        if (link != null) link.text = network != null && network.ClientManager.Started
            ? $"{(network.ServerManager.Started ? "HOST" : "LINK")}   {network.TimeManager.RoundTripTime} ms   /   {GameInputPresentation.Hint("Diagnostics")} DIAGNOSTICS"
            : "OFFLINE   /   " + GameInputPresentation.Hint("Diagnostics") + " DIAGNOSTICS";
        if (feedback != null) feedback.Resources(health.Health, health.MaxHealth, player.Mana, player.MaxMana);
        else { healthFill.fillAmount = health.Health / health.MaxHealth; manaFill.fillAmount = player.Mana / player.MaxMana; }
        for (int i = 0; i < skills.Length; i++)
        {
            float cooldown = player.Cooldown(i);
            feedback?.Skill(i, cooldown, player.Mana >= player.Cost(i));
            skills[i].text = skillNames[i] + "\n" + (cooldown > 0f ? $"{cooldown:0.0}s" : player.Mana < player.Cost(i) ? "LOW MP" : "READY") + $"  /  {player.Cost(i):0} MP";
            skills[i].color = cooldown > 0f || player.Mana < player.Cost(i) ? new Color(0.44f, 0.51f, 0.55f) : settings != null ? settings.textColor : Color.white;
        }
    }
}
