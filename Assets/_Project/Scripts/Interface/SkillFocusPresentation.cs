using TMPro;
using UnityEngine;

// Presentation observes combat; it never changes damage, inputs or shared profile data.
public sealed class SkillFocusPresentation : MonoBehaviour
{
    [SerializeField] private SkillPresentationSettings settings;
    [SerializeField] private SpriteRenderer backdrop;
    [SerializeField] private CanvasGroup letterbox;
    [SerializeField] private TMP_Text title;
    [SerializeField] private UnityEngine.UI.Image flash;
    private PlayerAttack player;
    private float nextSearch, opacity, flashUntil;
    private void Update()
    {
        if (player == null && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 0.5f;
            foreach (var candidate in FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None))
                if (!candidate.NetworkObject.IsSpawned || candidate.IsOwner) { player = candidate; player.HitConfirmed += OnImpact; break; }
        }
        bool active = player != null && player.IsSkillActive;
        opacity = Mathf.MoveTowards(opacity, active ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, active ? settings.fadeIn : settings.fadeOut));
        var camera = Camera.main;
        if (camera != null && backdrop != null && settings.backdropSprite != null)
        {
            backdrop.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, settings.backdropDepth);
            Vector2 size = settings.backdropSprite.bounds.size;
            backdrop.transform.localScale = new Vector3(camera.orthographicSize * 2f * camera.aspect / size.x * 1.1f, camera.orthographicSize * 2f / size.y * 1.1f, 1f);
            backdrop.color = new Color(0f, 0f, 0f, opacity * settings.darkness);
        }
        if (letterbox != null) letterbox.alpha = opacity;
        if (title != null) title.text = active ? settings.skillPrefix + ObjectNames(player.CurrentAction) : "";
        if (flash != null) flash.color = new Color(0.75f, 0.92f, 1f, Time.unscaledTime < flashUntil ? settings.flashOpacity : 0f);
    }
    private static string ObjectNames(string value) => value == "MegaSlash" ? "MEGA SLASH" : value == "SwordFire" ? "SWORD FIRE" : "CRITICAL X";
    private void OnImpact(float damage) { if (player != null && player.IsSkillActive) flashUntil = Time.unscaledTime + settings.flashDuration; }
    private void OnDisable()
    {
        if (backdrop != null) backdrop.color = Color.clear;
        if (flash != null) flash.color = Color.clear;
        if (letterbox != null) letterbox.alpha = 0f;
    }
    private void OnDestroy() { if (player != null) player.HitConfirmed -= OnImpact; }
}
