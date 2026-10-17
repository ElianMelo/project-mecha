using TMPro;
using UnityEngine;

// Native prefab graphics follow collider feet/head without inheriting actor facing.
public sealed class ActorWorldHud : MonoBehaviour
{
    [SerializeField] private SquadPresentationSettings settings;
    [SerializeField] private RectTransform bar;
    [SerializeField] private UnityEngine.UI.Image fill;
    [SerializeField] private TMP_Text pilotName;
    [SerializeField] private UnityEngine.UI.Image ring;
    private EnemyController enemy;
    private PlayerIdentity identity;
    private CapsuleCollider body;
    private void Awake()
    {
        enemy = GetComponentInParent<EnemyController>(); identity = GetComponentInParent<PlayerIdentity>();
        body = GetComponentInParent<CapsuleCollider>();
    }
    private void LateUpdate()
    {
        if (settings == null || body == null) return;
        var bounds = body.bounds;
        transform.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z - 0.65f), Quaternion.identity);
        // Cancel the imported actor scale, as well as its facing rotation.
        var scale = transform.parent.lossyScale;
        transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
        if (bar != null)
        {
            bar.gameObject.SetActive(enemy != null && !enemy.IsDead);
            bar.position = transform.position + Vector3.down * settings.barBelowFeet;
            if (enemy != null) fill.fillAmount = Mathf.Clamp01(enemy.Health / enemy.MaxHealth);
        }
        if (identity == null) return;
        var color = settings.PilotColor(identity.ColorSlot);
        pilotName.text = identity.IsSpawned ? identity.PilotName : PlayerIdentity.LocalName;
        pilotName.enabled = !identity.LocallyControlled;
        pilotName.color = color;
        pilotName.rectTransform.position = transform.position + Vector3.up * (bounds.size.y + settings.nameAboveHead);
        if (ring != null)
        {
            ring.enabled = !identity.LocallyControlled;
            ring.color = color;
            // Unlike the labels, the ring intersects the actor's depth. The default
            // world-space UI material lets the opaque body occlude its rear arc.
            var rect = ring.rectTransform;
            rect.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.min.y + settings.ringAboveFeet, bounds.center.z),
                Quaternion.Euler(settings.RingTiltDegrees, 0f, 0f));
            rect.localScale = Vector3.one;
            rect.sizeDelta = Vector2.one * settings.ringWidth;
        }
    }
}
