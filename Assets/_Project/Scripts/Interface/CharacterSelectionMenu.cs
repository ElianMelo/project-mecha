using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Selection changes only through this explicit swap modal or a development launch override.
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
    public bool IsVisible => panel != null && panel.activeSelf;
    public CharacterCatalog Catalog => catalog;
    private void Awake()
    {
        Instance = this; SelectedId = catalog.At(catalog.IndexOf(GameSave.GetString(GameSaveKeys.SelectedCharacter, "dasher"))).id;
        for (int i = 0; i < cards.Length; i++) { int index = i; cards[i].onClick.AddListener(() => Select(index)); }
        confirm.onClick.AddListener(Confirm); cancel.onClick.AddListener(Cancel); panel.SetActive(false);
        ConfirmLaunchArgument();
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
        SelectedId = catalog.At(selected).id;
        GameSave.SetString(GameSaveKeys.SelectedCharacter, SelectedId); GameSave.Save();
        var next = continuation; Hide(); next?.Invoke();
    }
    public void Cancel()
    {
        if (!IsVisible) return;
        Hide(); GameOptionsMenu.Instance?.ShowHome();
    }
    public void ConfirmLaunchArgument()
    {
        string[] args = Environment.GetCommandLineArgs(); int index = System.Array.IndexOf(args, "--mecha-character");
        if (index < 0 || index + 1 >= args.Length) return;
        // Resolve before any connection starts; a launch override is local to this run.
        SelectedId = catalog.At(catalog.IndexOf(args[index + 1])).id;
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
