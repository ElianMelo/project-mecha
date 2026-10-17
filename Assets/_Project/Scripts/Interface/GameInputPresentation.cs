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
    public static int Revision => Instance != null ? Instance.revision : 0;
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

    public static string Hint(string action)
    {
        if (action == "Interact") action = "Attack";
        if (!UsingGamepad)
            return action == "Jump" ? "UP / SPACE" : action == "Attack" ? "Z" : action == "Dash" ? "SHIFT" :
                action == "Tech" ? "X" : action == "SecondaryFire" ? "C" : action == "SkillA" ? "A" : action == "SkillS" ? "S" : action == "SkillD" ? "D" :
                action == "Interact" ? "E / UP" : action == "Menu" ? "ESC / 3" : action == "ControlsHelp" ? "F1" : action == "ChatVisibility" ? "F2" : action == "Diagnostics" ? "F3" : "ENTER";
        if (action == "ChatVisibility") return "F2";
        var pad = Instance.gamepad;
        InputControl control = action == "Jump" || action == "Submit" ? pad.buttonSouth : action == "Attack" ? pad.buttonWest :
            action == "Dash" ? pad.rightShoulder : action == "Tech" ? pad.buttonNorth : action == "SkillA" || action == "Cancel" ? pad.buttonEast :
            action == "SkillS" ? pad.leftShoulder : action == "SkillD" ? pad.leftTrigger :
            action == "SecondaryFire" ? pad.rightTrigger : action == "ControlsHelp" ? pad.rightStickButton : action == "Interact" ? pad.leftStickButton : action == "Menu" ? pad.startButton : pad.selectButton;
        return (string.IsNullOrEmpty(control.shortDisplayName) ? control.displayName : control.shortDisplayName).ToUpperInvariant();
    }

    public static string ControllerHelp(ControllerInterfaceSettings settings) => settings.controllerHelp
        .Replace("{jump}", Hint("Jump")).Replace("{attack}", Hint("Attack")).Replace("{dash}", Hint("Dash"))
        .Replace("{tech}", Hint("Tech")).Replace("{skill1}", Hint("SkillA")).Replace("{skill2}", Hint("SkillS"))
        .Replace("{skill3}", Hint("SkillD")).Replace("{interact}", Hint("Interact"))
        .Replace("{menu}", Hint("Menu")).Replace("{diagnostics}", Hint("Diagnostics"));
}
