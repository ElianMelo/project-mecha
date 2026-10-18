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
            return action == "Jump" ? GameText.Get("Input", "key.jump") : action == "Attack" ? GameText.Get("Input", "key.z") : action == "Dash" ? GameText.Get("Input", "key.shift") :
                action == "Tech" ? GameText.Get("Input", "key.x") : action == "SecondaryFire" ? GameText.Get("Input", "key.c") : action == "SkillA" ? GameText.Get("Input", "key.a") : action == "SkillS" ? GameText.Get("Input", "key.s") : action == "SkillD" ? GameText.Get("Input", "key.d") :
                action == "Interact" ? GameText.Get("Input", "key.interact") : action == "Menu" ? GameText.Get("Input", "key.menu") : action == "ControlsHelp" ? GameText.Get("Input", "key.f1") : action == "ChatVisibility" ? GameText.Get("Input", "key.f2") : action == "Diagnostics" ? GameText.Get("Input", "key.f3") : GameText.Get("Input", "key.enter");
        if (action == "ChatVisibility") return GameText.Get("Input", "key.f2");
        var pad = Instance.gamepad;
        InputControl control = action == "Jump" || action == "Submit" ? pad.buttonSouth : action == "Attack" ? pad.buttonWest :
            action == "Dash" ? pad.rightShoulder : action == "Tech" ? pad.buttonNorth : action == "SkillA" || action == "Cancel" ? pad.buttonEast :
            action == "SkillS" ? pad.leftShoulder : action == "SkillD" ? pad.leftTrigger :
            action == "SecondaryFire" ? pad.rightTrigger : action == "ControlsHelp" ? pad.rightStickButton : action == "Interact" ? pad.leftStickButton : action == "Menu" ? pad.startButton : pad.selectButton;
        string family = pad is UnityEngine.InputSystem.DualShock.DualShockGamepad ? "playstation." : "gamepad.";
        return GameText.Get("Input", family + control.name);
    }

    public static string ControllerHelp(ControllerInterfaceSettings settings) => GameText.Get(settings.controllerHelpLocalized)
        .Replace("{jump}", Hint("Jump")).Replace("{attack}", Hint("Attack")).Replace("{dash}", Hint("Dash"))
        .Replace("{tech}", Hint("Tech")).Replace("{skill1}", Hint("SkillA")).Replace("{skill2}", Hint("SkillS"))
        .Replace("{skill3}", Hint("SkillD")).Replace("{interact}", Hint("Interact"))
        .Replace("{menu}", Hint("Menu")).Replace("{diagnostics}", Hint("Diagnostics"));
}
