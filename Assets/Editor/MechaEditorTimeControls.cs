using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Editor assembly only: no scene component, saved setting or player-build shortcut.
[InitializeOnLoad]
internal static class MechaEditorTimeControls
{
    private const string OriginalScaleKey = "Mecha.Editor.Debug.OriginalTimeScale";
    private const float MinimumScale = 0.125f;
    private const float MaximumScale = 4f;

    static MechaEditorTimeControls()
    {
        InputSystem.onAfterUpdate += ReadShortcuts;
        EditorApplication.playModeStateChanged += PlayModeChanged;
    }

    private static void PlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            SessionState.SetFloat(OriginalScaleKey, Time.timeScale);
        else if (state == PlayModeStateChange.ExitingPlayMode)
            Time.timeScale = SessionState.GetFloat(OriginalScaleKey, 1f);
    }

    private static void ReadShortcuts()
    {
        // Run once per gameplay input update, not again during fixed/editor updates.
        if (InputState.currentUpdateType != InputUpdateType.Dynamic ||
            !EditorApplication.isPlaying || EditorApplication.isPaused ||
            EditorWindow.focusedWindow == null ||
            EditorWindow.focusedWindow.GetType().Name != "GameView" ||
            EditorGUIUtility.editingTextField || MechaChat.IsTyping)
            return;

        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != null &&
            (selected.GetComponent<TMP_InputField>()?.isFocused == true ||
             selected.GetComponent<UnityEngine.UI.InputField>()?.isFocused == true))
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        bool slower = keyboard.oKey.wasPressedThisFrame;
        bool faster = keyboard.pKey.wasPressedThisFrame;
        if (slower == faster) return;

        Time.timeScale = Mathf.Clamp(Time.timeScale * (slower ? 0.5f : 2f), MinimumScale, MaximumScale);
        Debug.Log($"Editor playback speed: {Time.timeScale:0.###}x (O slower / P faster)");
    }
}
