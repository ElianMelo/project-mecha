using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One modal owns selection. Callers retain ownership of launching solo/lobby connections.
[DefaultExecutionOrder(70)]
public sealed class CharacterSelectionMenu : MonoBehaviour
{
    public static CharacterSelectionMenu Instance { get; private set; }
    public static string SelectedId { get; private set; } = "dasher";
    [SerializeField] private CharacterCatalog catalog;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button[] cards;
    [SerializeField] private Image[] chosenBorders;
    [SerializeField] private Button confirm, cancel;
    [SerializeField] private TMP_Text details, selectedName;
    private Action continuation;
    private int selected;
    private System.Threading.Tasks.TaskCompletionSource<bool> pending;
    public bool IsVisible => panel != null && panel.activeSelf;
    public bool Confirmed { get; private set; }
    public CharacterCatalog Catalog => catalog;
    private void Awake()
    {
        Instance = this; SelectedId = catalog.At(catalog.IndexOf(GameSave.GetString(GameSaveKeys.SelectedCharacter, "dasher"))).id;
        for (int i = 0; i < cards.Length; i++) { int index = i; cards[i].onClick.AddListener(() => Select(index)); }
        confirm.onClick.AddListener(Confirm); cancel.onClick.AddListener(Cancel); panel.SetActive(false);
    }
    public void Open(Action onConfirmed)
    {
        if (IsVisible) return;
        continuation = onConfirmed; panel.SetActive(true); panel.transform.SetAsLastSibling();
        Select(catalog.IndexOf(SelectedId));
        Canvas.ForceUpdateCanvases(); panel.GetComponent<UiNavigationScope>().Rebuild();
        Navigation();
        EventSystem.current?.SetSelectedGameObject(cards[selected].gameObject);
        GameAudio.Play(GameAudioCue.UiOpen);
    }
    public void Select(int index)
    {
        selected = Mathf.Clamp(index, 0, catalog.Count - 1);
        var definition = catalog.At(selected);
        selectedName.text = definition.Label; details.text = GameText.Get(definition.description);
        for (int i = 0; i < chosenBorders.Length; i++) chosenBorders[i].enabled = i == selected;
    }
    public void Confirm()
    {
        if (!IsVisible) return;
        SelectedId = catalog.At(selected).id; Confirmed = true;
        GameSave.SetString(GameSaveKeys.SelectedCharacter, SelectedId); GameSave.Save();
        var next = continuation; var result = pending; pending = null; Hide(); next?.Invoke(); result?.TrySetResult(true);
    }
    public void Cancel()
    {
        if (!IsVisible) return;
        var result = pending; pending = null; Hide(); GameOptionsMenu.Instance?.ShowHome(); result?.TrySetResult(false);
    }
    public System.Threading.Tasks.Task<bool> EnsureConfirmedAsync()
    {
        if (Confirmed) return System.Threading.Tasks.Task.FromResult(true);
        if (pending != null) return pending.Task;
        pending = new System.Threading.Tasks.TaskCompletionSource<bool>();
        if (!IsVisible) Open(null);
        return pending.Task;
    }
    public void ConfirmLaunchArgument()
    {
        string[] args = Environment.GetCommandLineArgs(); int index = System.Array.IndexOf(args, "--mecha-character");
        if (!IsVisible || index < 0 || index + 1 >= args.Length) return;
        Select(catalog.IndexOf(args[index + 1])); Confirm();
    }
    private void Hide()
    { panel.SetActive(false); continuation = null; EventSystem.current?.SetSelectedGameObject(null); GameManager.Instance?.SuppressGameplayInput(); }
    private void Update()
    {
        if (!IsVisible) return;
        var current = EventSystem.current?.currentSelectedGameObject;
        if (current == null || !current.activeInHierarchy || !current.transform.IsChildOf(panel.transform)) EventSystem.current?.SetSelectedGameObject(cards[selected].gameObject);
    }
    private void LateUpdate() { if (IsVisible) Navigation(); }
    private void Navigation()
    {
        // Screen semantics supplement the shared geometric scope with predictable rows.
        for (int i = 0; i < cards.Length; i++)
            cards[i].navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnLeft = cards[Mathf.Max(0, i - 1)], selectOnRight = cards[Mathf.Min(cards.Length - 1, i + 1)],
                selectOnUp = cards[i], selectOnDown = i == 0 ? cancel : confirm };
        confirm.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
            selectOnUp = cards[selected], selectOnDown = confirm, selectOnLeft = cancel, selectOnRight = confirm };
        cancel.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
            selectOnUp = cards[0], selectOnDown = cancel, selectOnLeft = cancel, selectOnRight = confirm };
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
