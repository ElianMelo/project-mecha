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
    private static string[] SkillTitles => new[] { GameText.Get("Combat", "skill.megaSlash"), GameText.Get("Combat", "skill.swordFire"), GameText.Get("Combat", "skill.criticalX") };
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
        bool routing = LevelRouteDirector.Instance != null && LevelRouteDirector.Instance.IsRouting;
        if (controlsToggle != null) controlsToggle.gameObject.SetActive(!routing);
        if (controlsToggle != null) controlsToggle.interactable = !MechaChat.IsTyping;
        if (controlsToggleLabel != null)
        {
            controlsToggleLabel.text = GameInputPresentation.Hint("ControlsHelp") + (controlsVisible ? GameText.Get("HUD", "controls.hide") : GameText.Get("HUD", "controls.show"));
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
                ((PvpMatchDirector.Active || MechaChat.Instance != null && MechaChat.Instance.ContentVisible ? chatLayout.size.y + 12f : 0f) + PvpScoreboard.ReservedHeight);
        }
        if (promptRevision != GameInputPresentation.Revision + GameText.Revision)
        {
            promptRevision = GameInputPresentation.Revision + GameText.Revision;
            for (int i = 0; i < skillNames.Length; i++) skillNames[i] = GameInputPresentation.Hint(SkillActions[i]) + "  " + SkillTitles[i];
            if (controls != null) controls.text = ControlTutorialText.Build(player != null ? player.GetComponent<CharacterRuntime>()?.Definition : null);
        }
        if (player == null && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 0.5f;
            foreach (var candidate in FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None))
                if (!candidate.NetworkObject.IsSpawned || candidate.IsOwner) { player = candidate; health = candidate.GetComponent<PlayerHealth>(); movement = candidate.GetComponent<PlayerMovement>(); secondary = candidate.GetComponent<PlayerSecondaryWeapon>(); promptRevision = -1; break; }
        }
        if (player == null)
        {
            feedback?.Combo(false, 0);
            status.text = GameText.Get("HUD", "status.standby");
            if (motion != null) motion.text = GameText.Get("Input", "tutorial.standby", GameInputPresentation.Hint("Menu"), GameControlBindings.Title("System/Menu"), GameInputPresentation.Hint("LocalHost"), GameControlBindings.Title("System/LocalHost"), GameInputPresentation.Hint("LocalJoin"), GameControlBindings.Title("System/LocalJoin"));
            if (link != null) link.text = GameText.Get("HUD", "link.none") + GameInputPresentation.Hint("Diagnostics") + GameText.Get("HUD", "link.diagnostics");
            return;
        }
        var character = player.GetComponent<CharacterRuntime>();
        status.text = character != null && character.Definition != null ? character.Definition.Label : GameText.Get("HUD", "pilot.title");
        for (int i = 0; i < skillNames.Length; i++) skillNames[i] = GameInputPresentation.Hint(SkillActions[i]) + "  " + player.SkillTitle(i);
        if (ammunition != null) ammunition.text = secondary != null ? GameText.Get("HUD", "ammo.summary", (GameInputPresentation.Hint("SecondaryFire")), (secondary.Ammo), (secondary.Capacity)) : "";
        if (healthValue != null) healthValue.text = GameText.Get("HUD", "resources.armor", (health.Health), (health.MaxHealth));
        if (energyValue != null) energyValue.text = GameText.Get("HUD", "resources.energy", (player.Mana), (player.MaxMana));
        if (motion != null) motion.text = GameText.Get("HUD", "motion.summary", (GameText.Term("Combat", movement.MovementState).ToUpperInvariant()), (Mathf.Abs(movement.Velocity.x)), (GameText.Term("Combat", player.Phase)));
        if (motion != null) motion.text += "\n" + GameText.Get("HUD", "escape.charges", GameInputPresentation.Hint("Dash"), health.EscapeCharges, health.EscapeCapacity);
        if (motion != null && GameInputPresentation.Disconnected) motion.text = GameText.Get("HUD", "controller.disconnected");
        if (comboReadout != null && (player.ComboVisible || feedback == null)) comboReadout.text = player.ComboVisible ? GameText.Get("HUD", "combo.summary", (player.ComboHits), (player.ComboDamage)) : "";
        feedback?.Combo(player.ComboVisible, player.ComboHits);
        var network = GameManager.Instance != null ? GameManager.Instance.Network : null;
        if (link != null) link.text = network != null && network.ClientManager.Started
            ? GameText.Get("HUD", "link.summary", ((network.ServerManager.Started ? GameText.Get("HUD", "link.host") : GameText.Get("HUD", "link.client"))), (network.TimeManager.RoundTripTime), (GameInputPresentation.Hint("Diagnostics")))
            : GameText.Get("HUD", "link.offline") + GameInputPresentation.Hint("Diagnostics") + GameText.Get("HUD", "link.diagnostics");
        if (feedback != null) feedback.Resources(health.Health, health.MaxHealth, player.Mana, player.MaxMana);
        else { healthFill.fillAmount = health.Health / health.MaxHealth; manaFill.fillAmount = player.Mana / player.MaxMana; }
        for (int i = 0; i < skills.Length; i++)
        {
            float cooldown = player.Cooldown(i);
            feedback?.Skill(i, cooldown, player.Mana >= player.Cost(i));
            skills[i].text = skillNames[i] + "\n" + (cooldown > 0f ? GameText.Get("HUD", "skill.cooldown", (cooldown)) : player.Mana < player.Cost(i) ? GameText.Get("HUD", "skill.lowEnergy") : GameText.Get("HUD", "skill.ready")) + GameText.Get("HUD", "skill.cost", (player.Cost(i)));
            if (player.SkillStatus(i) > 0f) skills[i].text = skillNames[i] + "\n" + GameText.Get("Combat", "mode.remaining", player.SkillStatus(i));
            skills[i].color = cooldown > 0f || player.Mana < player.Cost(i) ? new Color(0.44f, 0.51f, 0.55f) : settings != null ? settings.textColor : Color.white;
        }
    }
}
