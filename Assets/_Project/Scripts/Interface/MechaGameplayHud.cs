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
    private PlayerMovement movement;
    private PlayerAttack player;
    private PlayerHealth health;
    private float nextSearch;
    private static readonly string[] SkillNames = { "A  MEGA SLASH", "S  SWORD FIRE", "D  CRITICAL X" };
    private void Update()
    {
        if (player == null && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 0.5f;
            foreach (var candidate in FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None))
                if (!candidate.NetworkObject.IsSpawned || candidate.IsOwner) { player = candidate; health = candidate.GetComponent<PlayerHealth>(); movement = candidate.GetComponent<PlayerMovement>(); break; }
        }
        if (player == null)
        {
            status.text = "MECHA // SYSTEM STANDBY";
            if (motion != null) motion.text = "1 LOCAL HOST    3 ONLINE LOBBIES";
            if (link != null) link.text = "NO LINK   /   F3 DIAGNOSTICS";
            return;
        }
        status.text = "MK-01  //  MECHA KNIGHT";
        if (healthValue != null) healthValue.text = $"AP   {health.Health:000} <size=12>/ {health.MaxHealth:0}</size>";
        if (energyValue != null) energyValue.text = $"EN   {player.Mana:000} <size=12>/ {player.MaxMana:0}</size>";
        if (motion != null) motion.text = $"{movement.MovementState.ToUpperInvariant()}    {Mathf.Abs(movement.Velocity.x):0.0} m/s    //    {player.Phase}";
        if (comboReadout != null) comboReadout.text = player.ComboVisible ? $"<size=38>{player.ComboHits:00}</size>  HITS\n<color=#71E5E8>{player.ComboDamage:0}</color> DAMAGE" : "";
        var network = GameManager.Instance != null ? GameManager.Instance.Network : null;
        if (link != null) link.text = network != null && network.ClientManager.Started
            ? $"{(network.ServerManager.Started ? "HOST" : "LINK")}   {network.TimeManager.RoundTripTime} ms   /   F3 DIAGNOSTICS"
            : "OFFLINE   /   F3 DIAGNOSTICS";
        healthFill.fillAmount = health.Health / health.MaxHealth;
        manaFill.fillAmount = player.Mana / player.MaxMana;
        for (int i = 0; i < skills.Length; i++)
        {
            float cooldown = player.Cooldown(i);
            skills[i].text = SkillNames[i] + "\n" + (cooldown > 0f ? $"{cooldown:0.0}s" : player.Mana < player.Cost(i) ? "LOW MP" : "READY") + $"  /  {player.Cost(i):0} MP";
            skills[i].color = cooldown > 0f || player.Mana < player.Cost(i) ? new Color(0.44f, 0.51f, 0.55f) : settings != null ? settings.textColor : Color.white;
        }
    }
}
