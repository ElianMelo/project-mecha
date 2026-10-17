using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.Selectable))]
public sealed class UiAudioFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private UnityEngine.UI.Selectable control;
    private UnityEngine.UI.Button button;
    private UnityEngine.UI.Toggle toggle;
    private UnityEngine.UI.Slider slider;
    private void Awake() { control = GetComponent<UnityEngine.UI.Selectable>(); button = control as UnityEngine.UI.Button; toggle = control as UnityEngine.UI.Toggle; slider = control as UnityEngine.UI.Slider; }
    private void OnEnable() { button?.onClick.AddListener(Confirm); toggle?.onValueChanged.AddListener(Toggled); slider?.onValueChanged.AddListener(Slid); }
    private void OnDisable() { button?.onClick.RemoveListener(Confirm); toggle?.onValueChanged.RemoveListener(Toggled); slider?.onValueChanged.RemoveListener(Slid); }
    private void Confirm() { if (control.IsInteractable()) GameAudio.Play(GameAudioCue.UiConfirm); }
    private void Toggled(bool value) => Confirm();
    private void Slid(float value) { if (control.IsInteractable()) GameAudio.Play(GameAudioCue.UiMove); }
    private void Focus() { if (control.IsInteractable()) GameAudio.Play(GameAudioCue.UiMove); }
    public void OnPointerEnter(PointerEventData data) => Focus();
    public void OnSelect(BaseEventData data) => Focus();
}
