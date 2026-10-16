using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public static class MechaControllerSetup
{
    [MenuItem("Tools/Mecha/Apply Controller Support")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before applying controller setup.");
        const string path = "Assets/_Project/Inputs/InputSystem_Actions.inputactions";
        var actions = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(path));
        var inputSettings = MechaPresentationSetup.Profile<PlayerInputSettings>("PlayerInput");
        try
        {
            var player = actions.FindActionMap("Player", true);
            void Bind(InputActionMap map, string name, string binding, string group)
            {
                var action = map.FindAction(name, false) ?? map.AddAction(name, InputActionType.Button);
                foreach (var existing in action.bindings) if (existing.path == binding) return;
                action.AddBinding(binding, groups: group);
            }
            Bind(player, "Move", "<Gamepad>/dpad", "Gamepad");
            Bind(player, "Jump", "<Gamepad>/dpad/up", "Gamepad");
            Bind(player, "Down", "<Gamepad>/leftStick/down", "Gamepad");
            Bind(player, "Tech", "<Gamepad>/leftTrigger", "Gamepad");
            Bind(player, "Interact", inputSettings.interactBinding, "Keyboard&Mouse");
            Bind(player, "Interact", inputSettings.alternateInteractBinding, "Keyboard&Mouse");
            Bind(player, "Interact", "<Gamepad>/rightTrigger", "Gamepad");
            var system = actions.FindActionMap("System", false) ?? actions.AddActionMap("System");
            Bind(system, "Menu", "<Keyboard>/escape", "Keyboard&Mouse");
            Bind(system, "Menu", "<Keyboard>/3", "Keyboard&Mouse");
            Bind(system, "Menu", "<Gamepad>/start", "Gamepad");
            Bind(system, "Back", "<Gamepad>/buttonEast", "Gamepad");
            Bind(system, "Diagnostics", "<Keyboard>/f3", "Keyboard&Mouse");
            Bind(system, "Diagnostics", "<Gamepad>/select", "Gamepad");
            Bind(system, "LocalHost", "<Keyboard>/1", "Keyboard&Mouse");
            Bind(system, "LocalJoin", "<Keyboard>/2", "Keyboard&Mouse");
            File.WriteAllText(path, actions.ToJson());
        }
        finally { UnityEngine.Object.DestroyImmediate(actions); }
        AssetDatabase.ImportAsset(path);
        var profile = MechaPresentationSetup.Create<ControllerInterfaceSettings>("ControllerInterface");
        profile.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        EditorUtility.SetDirty(profile);
        const string prefabPath = "Assets/_Project/Prefabs/MechaPlayer.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var playerInput = root.GetComponent<PlayerInput>();
            playerInput.neverAutoSwitchControlSchemes = false;
            playerInput.defaultControlScheme = "";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        foreach (var manager in UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None))
        {
            MechaPresentationSetup.Assign(manager, "controllerSettings", profile);
            MechaPresentationSetup.Ensure<GameInputPresentation>(manager.gameObject);
        }
        var module = UnityEngine.Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (module != null)
        {
            module.moveRepeatDelay = profile.navigationRepeatDelay; module.moveRepeatRate = profile.navigationRepeatRate;
            module.deselectOnBackgroundClick = false;
            EditorUtility.SetDirty(module);
        }
        MechaLobbySetup.Apply(UnityEngine.Object.FindFirstObjectByType<MechaLobbyService>());
        MechaHudSetup.Apply(UnityEngine.Object.FindFirstObjectByType<MechaGameplayHud>());
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }
}
