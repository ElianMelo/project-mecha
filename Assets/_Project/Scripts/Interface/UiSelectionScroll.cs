using UnityEngine;
using UnityEngine.EventSystems;

// TMP's native dropdown does not scroll offscreen controller selections into view.
// Retain its Toggle navigation and move only the native ScrollRect content.
[RequireComponent(typeof(UnityEngine.UI.ScrollRect))]
[DefaultExecutionOrder(100)]
public sealed class UiSelectionScroll : MonoBehaviour
{
    private UnityEngine.UI.ScrollRect scroll;
    private GameObject previous;
    private void Awake() => scroll = GetComponent<UnityEngine.UI.ScrollRect>();
    private void OnEnable() => previous = null;
    private void LateUpdate()
    {
        var selected = EventSystem.current?.currentSelectedGameObject;
        if (selected == null || selected == previous || scroll.content == null || !selected.transform.IsChildOf(scroll.content)) return;
        previous = selected;
        if (!(selected.transform is RectTransform item)) return;
        Canvas.ForceUpdateCanvases();
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, item);
        var viewport = scroll.viewport.rect;
        float delta = bounds.max.y > viewport.yMax ? viewport.yMax - bounds.max.y : bounds.min.y < viewport.yMin ? viewport.yMin - bounds.min.y : 0f;
        scroll.StopMovement();
        scroll.content.anchoredPosition += new Vector2(0f, delta);
    }
}
