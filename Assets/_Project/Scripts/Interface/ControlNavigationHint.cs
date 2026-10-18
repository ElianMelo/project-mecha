using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public sealed class ControlNavigationHint : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private LocalizedString introduction = new LocalizedString();
    private int revision = -1;
    private void OnEnable() => revision = -1;
    private void Update()
    {
        int current = GameInputPresentation.Revision + GameText.Revision;
        if (current == revision) return;
        revision = current;
        string intro = GameText.Get(introduction);
        text.text = (string.IsNullOrEmpty(intro) ? "" : intro + "\n") + GameText.Get("Input", "navigation.hint",
            GameInputPresentation.Hint("Navigate"), GameControlBindings.Title("UI/Navigate"),
            GameInputPresentation.Hint("Submit"), GameControlBindings.Title("UI/Submit"),
            GameInputPresentation.Hint("Cancel"), GameControlBindings.Title("UI/Cancel"));
    }
}
