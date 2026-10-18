using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class ControlsConfigurationMenu : MonoBehaviour
{
    [SerializeField] private ControlBindingSettings settings;
    [SerializeField] private UnityEngine.UI.Button controllerTab, keyboardTab, defaults, back, cancel;
    [SerializeField] private UnityEngine.UI.Image controllerIndicator, keyboardIndicator;
    [SerializeField] private RectTransform content;
    [SerializeField] private ControlBindingRow rowTemplate;
    [SerializeField] private GameObject capturePanel;
    [SerializeField] private CanvasGroup body;
    [SerializeField] private TMP_Text status, captureText;
    [SerializeField] private UnityEngine.UI.ScrollRect scroll;
    private readonly List<ControlBindingRow> rows = new List<ControlBindingRow>();
    private InputActionRebindingExtensions.RebindingOperation operation;
    private InputAction captureAction;
    private int captureIndex;
    private string previousOverride;
    private bool controller, initialized, capturing;
    private float captureUntil, releaseUntil, cancelHeldAt = -1f;
    private GameObject returnFocus;
    private int promptRevision = -1;
    public bool IsVisible => gameObject.activeSelf;
    public bool IsCapturing => capturing;

    private void Initialize()
    {
        if (initialized) return;
        GameControlBindings.Initialize(settings); initialized = true;
        controllerTab.onClick.AddListener(() => ShowTab(true)); keyboardTab.onClick.AddListener(() => ShowTab(false));
        defaults.onClick.AddListener(RestoreDefaults); back.onClick.AddListener(() => GameOptionsMenu.Instance.Back());
        cancel.onClick.AddListener(CancelCapture);
    }
    public void Show()
    {
        Initialize(); gameObject.SetActive(true); ShowTab(GameInputPresentation.UsingGamepad);
    }
    public void Hide() { CancelCapture(); gameObject.SetActive(false); }
    public bool ConsumeBack()
    {
        if (!IsCapturing) return Time.unscaledTime < releaseUntil;
        CancelCapture(); return true;
    }
    public void ShowTab(bool useController)
    {
        if (IsCapturing) return;
        controller = useController;
        controllerIndicator.enabled = controller; keyboardIndicator.enabled = !controller;
        foreach (var row in rows) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
        rows.Clear();
        foreach (var descriptor in settings.descriptions)
        {
            var action = GameControlBindings.Actions.FindAction(descriptor.action, false);
            if (action == null) continue;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (!GameControlBindings.Matches(binding, controller)) continue;
                int index = i;
                var row = Instantiate(rowTemplate, content); rows.Add(row); row.gameObject.SetActive(true);
                row.name = descriptor.action.Replace('/', ' ') + " " + i;
                string title = GameText.Get(descriptor.title);
                if (binding.isPartOfComposite) title = GameText.Get("Input", "binding.direction", title, GameText.Get("Input", "direction." + binding.name.ToLowerInvariant()));
                row.title.text = title; row.description.text = GameText.Get(descriptor.description);
                GameText.BindFont(row.title); GameText.BindFont(row.description); GameText.BindFont(row.binding);
                row.remap.onClick.AddListener(() => BeginCapture(action, index, row.remap.gameObject));
                row.clear.onClick.AddListener(() => { if (IsCapturing || !GameControlBindings.CanClear(action, index, controller)) return; action.ApplyBindingOverride(index, ""); bool saved = GameControlBindings.Save(); RefreshRows(); status.text = SaveStatus(saved); row.remap.Select(); });
                row.reset.onClick.AddListener(() => ResetBinding(action, index, row.remap));
            }
        }
        RefreshRows(); status.text = GameText.Get("Input", "binding.saved");
        Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1f;
        RebuildNavigation(); (controller ? controllerTab : keyboardTab).Select();
    }
    private void RefreshRows()
    {
        int rowIndex = 0;
        foreach (var descriptor in settings.descriptions)
        {
            var action = GameControlBindings.Actions.FindAction(descriptor.action, false);
            if (action == null) continue;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!GameControlBindings.Matches(action.bindings[i], controller)) continue;
                var row = rows[rowIndex++]; row.binding.text = GameControlBindings.Display(action, i, controller);
                row.clear.interactable = !string.IsNullOrEmpty(action.bindings[i].effectivePath) && GameControlBindings.CanClear(action, i, controller);
                row.reset.interactable = action.bindings[i].hasOverrides;
            }
        }
        RebuildNavigation();
    }
    private void RebuildNavigation()
    {
        UnityEngine.UI.Selectable first = rows.Count > 0 ? rows[0].remap : defaults;
        UnityEngine.UI.Selectable last = rows.Count > 0 ? rows[rows.Count - 1].remap : controllerTab;
        SetLinks(controllerTab, back, first, keyboardTab, keyboardTab);
        SetLinks(keyboardTab, back, first, controllerTab, controllerTab);
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i]; var columns = new List<UnityEngine.UI.Selectable> { row.remap };
            if (row.clear.IsInteractable()) columns.Add(row.clear);
            if (row.reset.IsInteractable()) columns.Add(row.reset);
            foreach (var control in columns)
            {
                var up = i == 0 ? (controller ? controllerTab : keyboardTab) : RowColumn(rows[i - 1], control == row.clear ? 1 : control == row.reset ? 2 : 0);
                var down = i + 1 == rows.Count ? defaults : RowColumn(rows[i + 1], control == row.clear ? 1 : control == row.reset ? 2 : 0);
                int column = columns.IndexOf(control);
                SetLinks(control, up, down, columns[Mathf.Max(0,column-1)], columns[Mathf.Min(columns.Count-1,column+1)]);
            }
        }
        SetLinks(defaults, last, controllerTab, back, back); SetLinks(back, last, keyboardTab, defaults, defaults);
    }
    private static UnityEngine.UI.Selectable RowColumn(ControlBindingRow row, int column) => column == 2 && row.reset.IsInteractable() ? row.reset : column == 1 && row.clear.IsInteractable() ? row.clear : row.remap;
    private static void SetLinks(UnityEngine.UI.Selectable control, UnityEngine.UI.Selectable up, UnityEngine.UI.Selectable down, UnityEngine.UI.Selectable left, UnityEngine.UI.Selectable right) =>
        control.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnUp = up, selectOnDown = down, selectOnLeft = left, selectOnRight = right };
    public void BeginCapture(InputAction action, int index, GameObject focus)
    {
        if (IsCapturing || Time.unscaledTime < releaseUntil) return;
        captureAction = action; captureIndex = index; previousOverride = action.bindings[index].overridePath; returnFocus = focus;
        capturing = true; GameControlBindings.Capturing = true; body.interactable = false; body.blocksRaycasts = false;
        SetNavigationInput(false);
        capturePanel.SetActive(true); cancel.Select(); captureUntil = Time.unscaledTime + settings.captureTimeout;
        cancelHeldAt = -1f;
        StartCoroutine(CaptureAfterRelease());
    }
    private IEnumerator CaptureAfterRelease()
    {
        // Wait for the opening click/confirm and stick navigation to return to rest.
        yield return new WaitForSecondsRealtime(0.25f);
        while (IsCapturing && OpeningControlHeld()) yield return null;
        if (!IsCapturing) yield break;
        operation = captureAction.PerformInteractiveRebinding(captureIndex)
            .WithControlsHavingToMatchPath(controller ? "<Gamepad>" : "<Keyboard>")
            .WithControlsExcluding("<Pointer>/position").WithControlsExcluding("<Pointer>/delta")
            .WithCancelingThrough("<Keyboard>/escape")
            .WithMatchingEventsBeingSuppressed(!controller)
            .OnMatchWaitForAnother(controller ? 0.85f : 0.1f)
            .OnPotentialMatch(candidate =>
            {
                // A click on the modal Cancel button is a UI command, even though
                // other mouse-button presses are valid replacement bindings.
                if (candidate.selectedControl == Mouse.current?.leftButton &&
                    RectTransformUtility.RectangleContainsScreenPoint((RectTransform)cancel.transform, Mouse.current.position.ReadValue())) candidate.Cancel();
            })
            .OnCancel(_ => FinishCapture(false))
            .OnComplete(_ => FinishCapture(true));
        // Keyboard and mouse buttons share this device tab and can replace one another.
        if (!controller) operation.WithControlsHavingToMatchPath("<Mouse>");
        operation.WithExpectedControlType(captureAction.expectedControlType == "Vector2" && !captureAction.bindings[captureIndex].isPartOfComposite ? "Vector2" : "Button");
        operation.Start();
    }
    private static bool OpeningControlHeld() => Mouse.current?.leftButton.isPressed == true || Keyboard.current?.enterKey.isPressed == true ||
        Gamepad.current != null && (Gamepad.current.buttonSouth.isPressed || Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.15f || Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.15f);
    private void Update()
    {
        int revision = GameInputPresentation.Revision + GameText.Revision;
        if (initialized && revision != promptRevision) { promptRevision = revision; RefreshRows(); }
        if (!IsCapturing) return;
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, captureUntil - Time.unscaledTime));
        captureText.text = controller ? GameText.Get("Input", "binding.listen.controller", seconds, GameControlBindings.Hint("UI/Cancel",true)) : GameText.Get("Input", "binding.listen", seconds);
        if (controller)
        {
            var pad = GameInputPresentation.ActiveGamepad ?? Gamepad.current;
            var action = GameControlBindings.Actions.FindAction("UI/Cancel");
            bool held = false;
            foreach (var binding in action.bindings)
                if (pad != null && GameControlBindings.Matches(binding,true) && InputControlPath.TryFindControl(pad,binding.effectivePath) is UnityEngine.InputSystem.Controls.ButtonControl button && button.isPressed) held = true;
            if (!held) cancelHeldAt = -1f;
            else if (cancelHeldAt < 0f) cancelHeldAt = Time.unscaledTime;
            else if (Time.unscaledTime - cancelHeldAt >= 0.6f) { CancelCapture(); return; }
        }
        if (Time.unscaledTime >= captureUntil || GameInputPresentation.Disconnected) CancelCapture();
    }
    public void CancelCapture()
    {
        if (!IsCapturing) return;
        if (operation != null) operation.Cancel(); else FinishCapture(false);
    }
    private void FinishCapture(bool completed)
    {
        if (!capturing) return;
        capturing = false;
        string conflict = completed ? GameControlBindings.Conflict(captureAction, captureIndex, captureAction.bindings[captureIndex].effectivePath) : null;
        if (!completed || conflict != null) captureAction.ApplyBindingOverride(captureIndex, new InputBinding { overridePath = previousOverride });
        operation?.Dispose(); operation = null;
        StopAllCoroutines(); capturePanel.SetActive(false); body.interactable = true; body.blocksRaycasts = true;
        // Keep shortcuts suppressed until the capture event has left all input callbacks.
        releaseUntil = Time.unscaledTime + 0.25f;
        StartCoroutine(ReleaseCapture());
        bool saved = !completed || conflict != null || GameControlBindings.Save();
        RefreshRows();
        status.text = conflict != null ? GameText.Get("Input", "binding.conflict", conflict) : completed ? SaveStatus(saved) : GameText.Get("Input", "binding.cancelled");
        if (returnFocus != null && returnFocus.activeInHierarchy) EventSystem.current?.SetSelectedGameObject(returnFocus);
    }
    private IEnumerator ReleaseCapture() { yield return new WaitForSecondsRealtime(0.25f); GameControlBindings.Capturing = false; SetNavigationInput(true); }
    private static void SetNavigationInput(bool enabled)
    {
        var module = EventSystem.current?.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (module == null) return;
        foreach (var reference in new[] { module.move, module.submit, module.cancel })
            if (reference != null) { if (enabled) reference.action.Enable(); else reference.action.Disable(); }
    }
    private void ResetBinding(InputAction action, int index, UnityEngine.UI.Button focus)
    {
        if (IsCapturing) return;
        string conflict = GameControlBindings.Conflict(action, index, action.bindings[index].path);
        if (conflict != null) { status.text = GameText.Get("Input", "binding.conflict", conflict); return; }
        action.RemoveBindingOverride(index); bool saved = GameControlBindings.Save(); RefreshRows(); status.text = SaveStatus(saved); focus.Select();
    }
    private void RestoreDefaults()
    {
        if (IsCapturing) return;
        foreach (var action in GameControlBindings.Actions)
            for (int i = 0; i < action.bindings.Count; i++) if (GameControlBindings.Matches(action.bindings[i], controller)) action.RemoveBindingOverride(i);
        bool saved = GameControlBindings.Save(); RefreshRows(); status.text = SaveStatus(saved, "binding.restored");
    }
    private static string SaveStatus(bool saved, string success = "binding.saved") => saved ? GameText.Get("Input", success) : GameText.Get("Settings", "save.failed");
    private void OnDisable()
    {
        if (operation != null) { operation.OnCancel(_ => { }); operation.Cancel(); operation.Dispose(); operation = null; }
        StopAllCoroutines(); capturing = false; GameControlBindings.Capturing = false; SetNavigationInput(true);
        if (capturePanel != null) capturePanel.SetActive(false);
        if (body != null) { body.interactable = true; body.blocksRaycasts = true; }
    }
}
