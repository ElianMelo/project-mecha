using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMPro.TMP_InputField))]
[DefaultExecutionOrder(40)]
public sealed class ControllerTextField : MonoBehaviour, ISubmitHandler
{
    [SerializeField] private ControllerTextEntry keyboard;
    private TMPro.TMP_InputField field;
    private void Awake() { field = GetComponent<TMPro.TMP_InputField>(); field.shouldActivateOnSelect = false; }
    private void LateUpdate()
    {
        // Gamepad editing belongs to the selectable keyboard. Native caret focus
        // consumes d-pad navigation and would otherwise trap the selected field.
        if (GameInputPresentation.UsingGamepad && field.isFocused) field.DeactivateInputField();
    }
    public void OnSubmit(BaseEventData eventData)
    {
        if (GameInputPresentation.UsingGamepad) keyboard.Open(field);
    }
}
