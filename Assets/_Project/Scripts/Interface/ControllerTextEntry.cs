using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Desktop TMP has no controller text keyboard; native selectable buttons fill that gap.
[DefaultExecutionOrder(50)]
public sealed class ControllerTextEntry : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text preview, title;
    [SerializeField] private UnityEngine.UI.Button[] keys;
    [SerializeField] private UnityEngine.UI.Button erase, space, changeCase, confirm, cancel;
    [SerializeField] private ControllerInterfaceSettings settings;
    private TMP_InputField field;
    private string draft;
    private bool lowerCase;
    public bool IsVisible => panel != null && panel.activeInHierarchy;
    private void LateUpdate()
    {
        if (!IsVisible || EventSystem.current == null) return;
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.transform.IsChildOf(panel.transform))
            EventSystem.current.SetSelectedGameObject(keys[0].gameObject);
    }

    private void Awake()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            int index = i;
            keys[i].onClick.AddListener(() => Append(settings.keyboardCharacters[index].ToString()));
        }
        erase.onClick.AddListener(() => { if (draft.Length > 0) draft = draft.Substring(0, draft.Length - 1); Refresh(); });
        space.onClick.AddListener(() => Append(" "));
        changeCase.onClick.AddListener(() => { lowerCase = !lowerCase; Refresh(); });
        confirm.onClick.AddListener(Confirm); cancel.onClick.AddListener(Cancel);
        panel.SetActive(false);
    }
    public void Open(TMP_InputField target)
    {
        if (IsVisible) return;
        field = target; draft = field.text; lowerCase = false;
        field.DeactivateInputField();
        title.text = "EDIT " + field.gameObject.name.ToUpperInvariant();
        panel.SetActive(true); Refresh();
        EventSystem.current?.SetSelectedGameObject(keys[0].gameObject);
    }
    private void Append(string value)
    {
        if (field.characterLimit > 0 && draft.Length >= field.characterLimit) return;
        draft += lowerCase ? value.ToLowerInvariant() : value;
        Refresh();
    }
    private void Refresh()
    {
        preview.text = draft + "|";
        for (int i = 0; i < keys.Length; i++) keys[i].GetComponentInChildren<TMP_Text>().text =
            lowerCase ? settings.keyboardCharacters[i].ToString().ToLowerInvariant() : settings.keyboardCharacters[i].ToString();
        changeCase.GetComponentInChildren<TMP_Text>().text = lowerCase ? "ABC" : "abc";
    }
    private void Confirm() { if (field != null) field.text = draft; Close(); }
    public void Cancel() => Close();
    private void Close()
    {
        panel.SetActive(false);
        if (field != null && field.gameObject.activeInHierarchy) EventSystem.current?.SetSelectedGameObject(field.gameObject);
        GameManager.Instance?.SuppressGameplayInput();
        field = null;
    }
}
