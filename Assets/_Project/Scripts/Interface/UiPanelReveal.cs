using UnityEngine;

[RequireComponent(typeof(CanvasGroup), typeof(Animator))]
public sealed class UiPanelReveal : MonoBehaviour
{
    [SerializeField] private UiFeelSettings settings;
    private void OnEnable()
    {
        var view = GetComponent<Animator>();
        view.enabled = settings.motionEnabled;
        if (view.enabled) { view.Play("Reveal", 0, 0f); view.Update(0f); }
        else GetComponent<CanvasGroup>().alpha = 1f;
    }
}
