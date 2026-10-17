using UnityEngine;
using UnityEngine.EventSystems;

// Native Selectable owns interaction; Animator owns timing and visual motion.
[RequireComponent(typeof(UnityEngine.UI.Selectable), typeof(Animator))]
public sealed class UiInteractionFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] private UiFeelSettings settings;
    [SerializeField] private UnityEngine.UI.Image selectionBorder;
    private Animator view;
    private UnityEngine.UI.Selectable control;
    private bool hovered, selected, disabled;
    private void Awake()
    {
        view = GetComponent<Animator>(); control = GetComponent<UnityEngine.UI.Selectable>();
        view.enabled = settings != null && settings.motionEnabled;
    }
    private void OnEnable()
    {
        hovered = false; selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        RefreshBorder();
        if (view == null || !view.enabled) return;
        view.Rebind(); view.SetBool("Focused", selected); view.SetBool("Pressed", false);
        disabled = !control.IsInteractable(); view.SetBool("Disabled", disabled);
    }
    private void Update()
    {
        selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        RefreshBorder();
        if (view == null || !view.enabled || disabled == !control.IsInteractable()) return;
        disabled = !control.IsInteractable(); view.SetBool("Disabled", disabled);
    }
    private void RefreshBorder() { if (selectionBorder != null) selectionBorder.enabled = control != null && control.IsInteractable() && selected; }
    private void OnDisable() { if (selectionBorder != null) selectionBorder.enabled = false; }
    private void Focus() { RefreshBorder(); if (view != null && view.enabled) view.SetBool("Focused", hovered || selected); }
    public void OnPointerEnter(PointerEventData data) { hovered = true; Focus(); }
    public void OnPointerExit(PointerEventData data) { hovered = false; Focus(); }
    public void OnSelect(BaseEventData data) { selected = true; Focus(); }
    public void OnDeselect(BaseEventData data) { selected = false; Focus(); }
    public void OnPointerDown(PointerEventData data)
    { if (data.button == PointerEventData.InputButton.Left && control.IsInteractable() && view.enabled) view.SetBool("Pressed", true); }
    public void OnPointerUp(PointerEventData data) { if (view.enabled) view.SetBool("Pressed", false); }
    public void OnPointerClick(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) Confirm(); }
    public void OnSubmit(BaseEventData data) => Confirm();
    private void Confirm() { if (control.IsInteractable() && view.enabled) view.SetTrigger("Confirm"); }
}
