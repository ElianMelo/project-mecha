using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// Native binding overrides own remapping. Authored assets are never changed in play.
public static class GameControlBindings
{
    public const string SaveKey = GameSaveKeys.ControlsOverrides;
    private static readonly List<InputActionAsset> consumers = new List<InputActionAsset>();
    public static InputActionAsset Actions { get; private set; }
    public static ControlBindingSettings Settings { get; private set; }
    public static int Revision { get; private set; }
    public static bool Capturing { get; set; }
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        consumers.Clear(); Actions = null; Settings = null; Revision = 0; Capturing = false; Changed = null;
    }
    public static void Initialize(ControlBindingSettings settings)
    {
        if (Actions != null) return;
        Settings = settings;
        Actions = UnityEngine.Object.Instantiate(settings.actions);
        try { var json = GameSave.GetJson(SaveKey); if (json != null) Actions.LoadBindingOverridesFromJson(json); }
        catch (Exception error) { Actions.RemoveAllBindingOverrides(); Debug.LogWarning("Saved controls could not be loaded: " + error.Message); }
        SynchronizeBack();
        foreach (var consumer in consumers) if (consumer != null) CopyOverrides(consumer);
    }
    public static void Register(InputActionAsset actions)
    {
        if (actions == null || consumers.Contains(actions)) return;
        consumers.Add(actions);
        if (Actions != null) CopyOverrides(actions);
    }
    public static void Unregister(InputActionAsset actions) => consumers.Remove(actions);
    public static string Title(string action)
    {
        var description = Settings?.descriptions.FirstOrDefault(d => d.action == action);
        return description == null ? string.Empty : GameText.Get(description.title);
    }
    private static void CopyOverrides(InputActionAsset target)
    {
        // Overrides can affect enabled maps; preserve each consumer's enabled state.
        var enabled = target.actionMaps.Where(m => m.enabled).ToArray();
        target.Disable();
        target.LoadBindingOverridesFromJson(Actions.SaveBindingOverridesAsJson());
        foreach (var map in enabled) map.Enable();
    }
    public static bool Save()
    {
        SynchronizeBack();
        GameSave.SetJson(SaveKey, Actions.SaveBindingOverridesAsJson()); bool saved = GameSave.Save();
        consumers.RemoveAll(a => a == null);
        foreach (var consumer in consumers) CopyOverrides(consumer);
        Revision++; Changed?.Invoke();
        GameManager.Instance?.SuppressGameplayInput();
        return saved;
    }
    private static void SynchronizeBack()
    {
        var cancel = Actions.FindAction("UI/Cancel"); var back = Actions.FindAction("System/Back");
        for (int i = 0; i < back.bindings.Count; i++)
        {
            bool controller = Matches(back.bindings[i], true);
            string path = cancel.bindings.First(b => Matches(b, controller)).effectivePath;
            // Menu already returns through the screen stack. Avoid two callbacks
            // for a control shared by Menu and Cancel (Escape in the default layout).
            var menu = Actions.FindAction("System/Menu");
            if (menu.bindings.Any(b => string.Equals(b.effectivePath,path,StringComparison.OrdinalIgnoreCase))) path = "";
            back.ApplyBindingOverride(i, path);
        }
    }
    public static bool Matches(InputBinding binding, bool controller) => !binding.isComposite &&
        (binding.groups ?? "").Split(';').Contains(controller ? "Gamepad" : "Keyboard&Mouse");

    public static string Display(InputAction action, int index, bool controller)
    {
        return DisplayPath(action.bindings[index].effectivePath,controller);
    }
    private static string DisplayPath(string path, bool controller)
    {
        if (string.IsNullOrEmpty(path)) return GameText.Get("Input", "binding.unbound");
        if (controller && GameInputPresentation.ActiveGamepad != null)
        {
            var control = UnityEngine.InputSystem.InputControlPath.TryFindControl(GameInputPresentation.ActiveGamepad, path);
            if (control != null)
            {
                string family = GameInputPresentation.ActiveGamepad is UnityEngine.InputSystem.DualShock.DualShockGamepad ? "playstation." : "gamepad.";
                if (control is UnityEngine.InputSystem.Controls.StickControl || control is UnityEngine.InputSystem.Controls.DpadControl)
                    return GameText.Get("Input", "control." + control.name);
                if (control.parent is Gamepad) return GameText.Get("Input", family + control.name);
            }
        }
        return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
    }
    public static string Hint(string name, bool controller)
    {
        string path = name.Contains("/") ? name : (name == "Submit" || name == "Cancel" || name == "Navigate" ? "UI/" :
            name == "Menu" || name == "Diagnostics" || name == "ControlsHelp" || name == "ChatVisibility" || name == "Chat" || name == "LocalHost" || name == "LocalJoin" ? "System/" : "Player/") + name;
        var action = Actions?.FindAction(path, false);
        if (action == null) return GameText.Get("Input", "binding.unbound");
        var values = new List<string>();
        var paths = action.bindings.Where(b => Matches(b,controller) && !string.IsNullOrEmpty(b.effectivePath)).Select(b => b.effectivePath).ToArray();
        for (int i = 0; i < action.bindings.Count; i++)
            if (Matches(action.bindings[i], controller) && !string.IsNullOrEmpty(action.bindings[i].effectivePath))
            {
                string bindingPath = action.bindings[i].effectivePath;
                // A complete set of stick directions reads as one control in navigation help.
                int slash = bindingPath.LastIndexOf('/'); string root = slash > 0 ? bindingPath.Substring(0,slash) : bindingPath;
                if ((root.EndsWith("Stick",StringComparison.OrdinalIgnoreCase) || root.EndsWith("dpad",StringComparison.OrdinalIgnoreCase)) &&
                    new[]{"up","down","left","right"}.All(direction => paths.Contains(root+"/"+direction)))
                    bindingPath = root;
                values.Add(DisplayPath(bindingPath,controller));
                // Short prompts show the first assigned button; every alternate remains visible in Controls.
                if (action.expectedControlType != "Vector2") break;
            }
        return values.Count == 0 ? GameText.Get("Input", "binding.unbound") : string.Join(" / ", values.Distinct());
    }
    public static bool Essential(InputAction action) => action.actionMap.name == "UI" &&
        (action.name == "Navigate" || action.name == "Submit" || action.name == "Cancel") || action.actionMap.name == "System" && action.name == "Menu";
    public static bool CanClear(InputAction action, int index, bool controller)
    {
        if (!Essential(action)) return true;
        var binding = action.bindings[index];
        return action.bindings.Where((b, i) => i != index && Matches(b, controller) && !string.IsNullOrEmpty(b.effectivePath) &&
            (!binding.isPartOfComposite || b.isPartOfComposite && b.name.Equals(binding.name, StringComparison.OrdinalIgnoreCase))).Any();
    }
    public static string Conflict(InputAction action, int index, string path)
    {
        // UI and gameplay deliberately share physical buttons in different scopes.
        foreach (var candidate in Actions)
        {
            bool sameScope = candidate.actionMap == action.actionMap ||
                candidate.actionMap.name != "UI" && action.actionMap.name != "UI" && candidate.name != "Back" && action.name != "Back";
            if (!sameScope) continue;
            for (int i = 0; i < candidate.bindings.Count; i++)
                if (!(candidate == action && i == index) && !candidate.bindings[i].isComposite &&
                    string.Equals(candidate.bindings[i].effectivePath, path, StringComparison.OrdinalIgnoreCase))
                {
                    var descriptor = Settings.descriptions.FirstOrDefault(d => d.action == candidate.actionMap.name + "/" + candidate.name);
                    return descriptor == null ? candidate.name : GameText.Get(descriptor.title);
                }
        }
        return null;
    }
}
