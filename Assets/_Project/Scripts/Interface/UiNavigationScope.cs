using System.Collections.Generic;
using UnityEngine;

// Native explicit navigation, rebuilt for visible/interactable controls only.
// Prefer controls sharing the requested row/column before crossing to another section.
public sealed class UiNavigationScope : MonoBehaviour
{
    private float nextRefresh;
    private int lastSignature;
    private readonly List<UnityEngine.UI.Selectable> controls = new List<UnityEngine.UI.Selectable>();
    private readonly List<Rect> rectangles = new List<Rect>();
    private readonly Vector3[] corners = new Vector3[4];
    private void OnEnable() { nextRefresh = 0f; lastSignature = int.MinValue; }
    private void LateUpdate()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.12f; Rebuild();
    }
    public void Rebuild()
    {
        GetComponentsInChildren(false, controls);
        controls.RemoveAll(control => !control.IsInteractable() || control.GetComponentInParent<UiNavigationScope>() != this);
        rectangles.Clear(); int signature = controls.Count;
        foreach (var control in controls)
        {
            ((RectTransform)control.transform).GetWorldCorners(corners);
            var bounds = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            rectangles.Add(bounds);
            unchecked { signature = signature * 31 + control.GetInstanceID(); signature = signature * 31 + bounds.GetHashCode(); }
        }
        if (signature == lastSignature) return;
        lastSignature = signature;
        for (int i = 0; i < controls.Count; i++)
            controls[i].navigation = new UnityEngine.UI.Navigation {
                mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnUp = Find(i, Vector2.up), selectOnDown = Find(i, Vector2.down),
                selectOnLeft = Find(i, Vector2.left), selectOnRight = Find(i, Vector2.right)
            };
    }
    private UnityEngine.UI.Selectable Find(int fromIndex, Vector2 direction)
    {
        Rect origin = rectangles[fromIndex];
        UnityEngine.UI.Selectable best = controls[fromIndex];
        float score = float.MaxValue;
        bool vertical = direction.y != 0f;
        for (int i = 0; i < controls.Count; i++)
        {
            if (i == fromIndex) continue;
            Rect rect = rectangles[i];
            Vector2 delta = rect.center - origin.center;
            float forward = Vector2.Dot(delta, direction);
            float perpendicular = vertical ? Mathf.Abs(delta.x) : Mathf.Abs(delta.y);
            if (forward < 1f || forward < perpendicular * 0.65f) continue;
            float sideways = vertical ? Mathf.Max(0f, Mathf.Max(origin.xMin - rect.xMax, rect.xMin - origin.xMax)) :
                Mathf.Max(0f, Mathf.Max(origin.yMin - rect.yMax, rect.yMin - origin.yMax));
            float value = forward + sideways * 5f + (sideways > 1f ? 1000f : 0f);
            if (value < score) { best = controls[i]; score = value; }
        }
        return best;
    }
}
