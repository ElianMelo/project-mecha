using TMPro;
using UnityEngine;

public class MechaGameplayHud : MonoBehaviour
{
    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text[] skills;
    [SerializeField] private UnityEngine.UI.Image healthFill;
    [SerializeField] private UnityEngine.UI.Image manaFill;
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
                if (!candidate.NetworkObject.IsSpawned || candidate.IsOwner) { player = candidate; health = candidate.GetComponent<PlayerHealth>(); break; }
        }
        if (player == null)
        {
            status.text = "TRAINING ARENA  /  Press 1 to host locally  /  2 to join";
            return;
        }
        status.text = $"MECHA KNIGHT  /  HP {health.Health:0}  /  MP {player.Mana:0}  /  {player.CurrentAction}";
        healthFill.fillAmount = health.Health / health.MaxHealth;
        manaFill.fillAmount = player.Mana / player.MaxMana;
        for (int i = 0; i < skills.Length; i++)
        {
            float cooldown = player.Cooldown(i);
            skills[i].text = SkillNames[i] + "\n" + (cooldown > 0f ? $"{cooldown:0.0}s" : player.Mana < player.Cost(i) ? "LOW MP" : "READY") + $"  /  {player.Cost(i):0} MP";
            skills[i].color = cooldown > 0f || player.Mana < player.Cost(i) ? new Color(0.5f, 0.6f, 0.68f) : Color.white;
        }
    }
}
