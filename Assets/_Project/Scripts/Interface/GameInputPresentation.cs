using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

// Tracks meaningful input for prompts; PlayerInput still owns gameplay pairing.
public sealed class GameInputPresentation : MonoBehaviour
{
    public static GameInputPresentation Instance { get; private set; }
    public static bool UsingGamepad => Instance != null && Instance.gamepad != null;
    public static Gamepad ActiveGamepad => Instance != null ? Instance.gamepad : null;
    public static bool Disconnected => UsingGamepad && !Instance.gamepad.added;
    public static int Revision => (Instance != null ? Instance.revision : 0) + GameControlBindings.Revision;
    private Gamepad gamepad;
    private int revision;
    private System.IDisposable buttonSubscription;

    private void Awake() => Instance = this;
    private void OnEnable()
    {
        InputSystem.onActionChange += ActionChanged;
        buttonSubscription = InputSystem.onAnyButtonPress.Call(control => UseDevice(control.device));
    }
    private void OnDisable() { InputSystem.onActionChange -= ActionChanged; buttonSubscription?.Dispose(); }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void ActionChanged(object source, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed || !(source is InputAction action) || action.activeControl == null) return;
        if (action.activeControl.device is Gamepad || action.activeControl.device is Keyboard || action.activeControl is ButtonControl)
            UseDevice(action.activeControl.device);
    }
    private void UseDevice(InputDevice device)
    {
        if (device is Gamepad pad && gamepad != pad) { gamepad = pad; revision++; }
        else if ((device is Keyboard || device is Mouse) && gamepad != null)
        { gamepad = null; revision++; }
    }

    public static string Hint(string action) => GameControlBindings.Hint(action == "Interact" ? "Attack" : action, UsingGamepad);
    public static string ControllerHelp(ControllerInterfaceSettings settings) => ControlTutorialText.Build();
}
