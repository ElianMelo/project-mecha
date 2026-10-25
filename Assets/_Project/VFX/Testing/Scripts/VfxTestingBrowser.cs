using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;

[DefaultExecutionOrder(150)]
public sealed class VfxTestingBrowser : MonoBehaviour
{
    [SerializeField] private VfxPreviewCatalog catalog;
    [SerializeField] private VfxPreviewPlayer player;
    [SerializeField] private VfxBrowserRow rowPrefab;
    [SerializeField] private RectTransform content;
    [SerializeField] private UnityEngine.UI.ScrollRect scroll;
    [SerializeField] private UiNavigationScope navigationScope;
    [SerializeField] private UnityEngine.UI.Button replay, stop, refresh;
    [SerializeField] private TMP_Text selectedName, description, count, status;
    [SerializeField] private GameObject emptyMessage;
    [SerializeField] private InputActionAsset actions;
    private readonly List<VfxBrowserRow> rows = new List<VfxBrowserRow>();
    private VfxBrowserRow selected;
    private InputAction replayAction, stopAction;
    private bool ready;
    public VfxPreviewDefinition Selected => selected != null ? selected.Definition : null;
    public int VisibleCount => rows.Count;

    private void OnEnable()
    {
        replayAction = actions.FindAction("Preview/Replay", true);
        stopAction = actions.FindAction("Preview/Stop", true);
        replayAction.performed += ReplayInput;
        stopAction.performed += StopInput;
        replayAction.Enable(); stopAction.Enable();
        player.Changed += RefreshControls;
    }

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        replay.onClick.AddListener(Replay);
        stop.onClick.AddListener(Stop);
        refresh.onClick.AddListener(RefreshList);
        ready = true;
        RefreshList();
    }

    private void OnDisable()
    {
        if (replayAction != null) { replayAction.performed -= ReplayInput; replayAction.Disable(); }
        if (stopAction != null) { stopAction.performed -= StopInput; stopAction.Disable(); }
        if (player != null) { player.Changed -= RefreshControls; player.StopPreview(); }
    }

    private void ReplayInput(InputAction.CallbackContext context) => Replay();
    private void StopInput(InputAction.CallbackContext context) => Stop();

    private void LateUpdate()
    {
        if (!ready) return;
        // Spatial navigation can jump to the footer before reaching clipped rows.
        // Keep native explicit links in catalog order after the shared scope refresh.
        var first = rows.Count > 0 ? rows[0].Button : refresh;
        var current = selected != null ? selected.Button : refresh;
        Link(refresh, replay.interactable ? replay : refresh, first, refresh, replay.interactable ? replay : refresh);
        for (int i = 0; i < rows.Count; i++)
            Link(rows[i].Button, i > 0 ? rows[i - 1].Button : refresh,
                i + 1 < rows.Count ? rows[i + 1].Button : replay, rows[i].Button, replay);
        Link(replay, current, refresh, current, stop.interactable ? stop : replay);
        Link(stop, current, refresh, replay, stop);
    }

    private static void Link(UnityEngine.UI.Button button, UnityEngine.UI.Selectable up,
        UnityEngine.UI.Selectable down, UnityEngine.UI.Selectable left, UnityEngine.UI.Selectable right)
    {
        button.navigation = new UnityEngine.UI.Navigation {
            mode = UnityEngine.UI.Navigation.Mode.Explicit,
            selectOnUp = up, selectOnDown = down, selectOnLeft = left, selectOnRight = right
        };
    }

    public void RefreshList()
    {
        if (!ready) return;
        var previous = Selected;
        player.StopPreview();
        EventSystem.current?.SetSelectedGameObject(null);
        foreach (var row in rows) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
        rows.Clear(); selected = null;
        var seen = new HashSet<VfxPreviewDefinition>();
        if (catalog != null && catalog.effects != null)
            foreach (var definition in catalog.effects)
            {
                if (definition == null || definition.prefab == null || !seen.Add(definition)) continue;
                var row = Instantiate(rowPrefab, content);
                row.Bind(this, definition);
                rows.Add(row);
                if (definition == previous) selected = row;
            }

        emptyMessage.SetActive(rows.Count == 0);
        count.text = GameText.Get("Diagnostics", "vfx.count", rows.Count);
        scroll.verticalNormalizedPosition = 1f;
        if (selected == null && rows.Count > 0) selected = rows[0];
        if (selected != null) Select(selected);
        else
        {
            selectedName.text = GameText.Get("Diagnostics", "vfx.none");
            description.text = GameText.Get("Diagnostics", "vfx.empty.detail");
        }
        RefreshControls();
        Canvas.ForceUpdateCanvases();
        navigationScope.Rebuild();
        EventSystem.current?.SetSelectedGameObject(selected != null ? selected.gameObject : refresh.gameObject);
    }

    public void Select(VfxBrowserRow row)
    {
        if (!ready || row == null || !rows.Contains(row)) return;
        if (selected != row) player.StopPreview();
        selected = row;
        foreach (var candidate in rows) candidate.ShowSelected(candidate == selected);
        selectedName.text = GameText.Get(row.Definition.displayName);
        description.text = GameText.Get(row.Definition.description);
        RefreshControls();
    }

    public void Replay()
    {
        if (!ready || Selected == null) return;
        // Space belongs only to Preview/Replay, never to the UI Submit action.
        player.Play(Selected);
    }

    public void Stop()
    {
        if (!ready) return;
        bool focusedStop = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == stop.gameObject;
        player.StopPreview();
        if (focusedStop && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(selected != null ? selected.gameObject : refresh.gameObject);
    }

    private void RefreshControls()
    {
        if (!ready) return;
        // Selectable clears selection as soon as it becomes non-interactable.
        // Capture focus first so replay/expiry can restore it reliably.
        bool focusedStop = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == stop.gameObject;
        replay.interactable = Selected != null;
        stop.interactable = player.IsPlaying;
        status.text = GameText.Get("Diagnostics", rows.Count == 0 ? "vfx.empty.status" : player.IsPlaying ? "vfx.playing" : "vfx.ready");
        if (!stop.interactable && focusedStop && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(replay.interactable ? replay.gameObject : refresh.gameObject);
        navigationScope.Rebuild();
    }
}
