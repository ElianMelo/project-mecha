using TMPro;
using UnityEngine;

public sealed class LevelObjectiveHud : MonoBehaviour
{
    [SerializeField] private RectTransform arrow;
    [SerializeField] private TMP_Text objective;
    [SerializeField] private GameObject portalHold;
    [SerializeField] private UnityEngine.UI.Image portalFill;
    [SerializeField] private TMP_Text portalButton;
    private PlayerMovement player;
    private Camera view;
    private float nextSearch;
    private UnityEngine.UI.Image graphic;
    private void Awake() { view = Camera.main; graphic = arrow.GetComponent<UnityEngine.UI.Image>(); }
    private void LateUpdate()
    {
        if (PvpMatchDirector.Active) { arrow.gameObject.SetActive(false); if (portalHold != null) portalHold.SetActive(false); objective.text = ""; return; }
        var levels = ProceduralLevelDirector.Instance;
        if (player == null && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 0.25f;
            foreach (var candidate in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
                if (candidate.HasControl) { player = candidate; break; }
        }
        if (levels == null || levels.Layout == null || player == null || view == null)
        { arrow.gameObject.SetActive(false); if (portalHold != null) portalHold.SetActive(false); objective.text = ""; return; }
        Vector3 target = levels.Layout.Portal + Vector3.up * 0.9f;
        bool visibleEnemy = false, foundEnemy = false;
        float closest = float.MaxValue;
        foreach (var enemy in EnemyController.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            Vector3 center = enemy.transform.position + Vector3.up * 0.85f;
            Vector3 viewport = view.WorldToViewportPoint(center);
            if (viewport.z > 0 && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f) visibleEnemy = true;
            float distance = (enemy.transform.position - player.transform.position).sqrMagnitude;
            if (distance < closest) { closest = distance; target = center; foundEnemy = true; }
        }
        bool portal = levels.IsClear;
        if (portal) target = levels.Layout.Portal + Vector3.up * 0.9f;
        objective.text = GameText.Get("HUD", "room.summary", (levels.LevelNumber),
            (GameText.Term("Routing", levels.Layout.Shape).ToUpperInvariant() + (string.IsNullOrEmpty(levels.Layout.DescriptionDirection) ? "" : " " + GameText.Term("Routing", levels.Layout.DescriptionDirection)))) +
            (portal ? (Vector3.Distance(player.transform.position, levels.Layout.Portal) <= levels.Settings.portalInteractionRange
                ? GameText.Get("HUD", "portal.hold") + GameInputPresentation.Hint("Attack") + GameText.Get("HUD", "portal.enter") : GameText.Get("HUD", "room.cleared")) : GameText.Get("HUD", "room.enemies", (levels.Remaining)));
        var travel = player.GetComponent<PlayerLevelTravel>();
        if (portalHold != null)
        {
            portalHold.SetActive(portal && travel != null && travel.CanUsePortal);
            portalFill.fillAmount = travel != null ? travel.HoldProgress : 0f;
            portalButton.text = GameInputPresentation.Hint("Attack");
        }
        arrow.gameObject.SetActive(portal || foundEnemy && !visibleEnemy);
        if (!arrow.gameObject.activeSelf) return;
        graphic.color = portal ? levels.Settings.portalColor : levels.Settings.enemyPointerColor;
        Vector3 point = view.WorldToViewportPoint(target);
        Vector2 direction = new Vector2(point.x - 0.5f, point.y - 0.5f);
        if (point.z < 0f) direction = -direction;
        var parent = (RectTransform)arrow.parent;
        Vector2 half = parent.rect.size * 0.5f - Vector2.one * levels.Settings.pointerInset;
        direction = Vector2.Scale(direction, parent.rect.size);
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.up;
        float scale = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(direction.x)), half.y / Mathf.Max(0.001f, Mathf.Abs(direction.y)));
        arrow.anchoredPosition = direction * scale;
        // The native dropdown sprite points down; rotate its image toward the objective.
        arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f);
    }
}
