using System;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Manual controller input through Pipeline; one human-directed press/release per call.
// No assertions, scenarios, test fixtures or Test Runner integration.
public static class ControllerInput
{
    public static string Press(string button)
    {
        var pad = InputSystem.devices.OfType<Gamepad>().Single(p => p.name == "CodexManualController");
        var state = new GamepadState();
        if (button == "LeftTrigger") state.leftTrigger = 1f;
        else if (button == "RightTrigger") state.rightTrigger = 1f;
        else state = state.WithButton((GamepadButton)Enum.Parse(typeof(GamepadButton), button));
        InputSystem.QueueStateEvent(pad, state);
        return "Controller " + button;
    }
    public static string Release()
    {
        var pad = InputSystem.devices.OfType<Gamepad>().Single(p => p.name == "CodexManualController");
        InputSystem.QueueStateEvent(pad, new GamepadState());
        return "Controller buttons released";
    }
}
