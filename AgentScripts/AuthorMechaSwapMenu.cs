using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;

// Focused, repeatable native authoring for the Configuration swap entry.
public static class AuthorMechaSwapMenu
{
    public static string Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
        var menu = UnityEngine.Object.FindFirstObjectByType<GameOptionsMenu>();
        var selection = UnityEngine.Object.FindFirstObjectByType<CharacterSelectionMenu>();
        if (menu == null || selection == null) throw new InvalidOperationException("Open MainScene first.");
        var serialized = new SerializedObject(menu);
        var source = (UnityEngine.UI.Button)serialized.FindProperty("controlsButton").objectReferenceValue;
        var button = (UnityEngine.UI.Button)serialized.FindProperty("swapMecha").objectReferenceValue;
        if (button == null)
        {
            button = UnityEngine.Object.Instantiate(source, source.transform.parent);
            button.name = "Swap Mecha";
            Undo.RegisterCreatedObjectUndo(button.gameObject, "Add Swap Mecha");
        }
        var rect = (RectTransform)button.transform;
        rect.anchoredPosition = new Vector2(40f, -556f);
        var options = (RectTransform)source.transform.parent;
        options.sizeDelta = new Vector2(options.sizeDelta.x, 706f);
        var help = options.Find("Help") as RectTransform;
        if (help != null) help.anchoredPosition = new Vector2(40f, -628f);
        if (options.GetComponent<UiNavigationScope>() == null) options.gameObject.AddComponent<UiNavigationScope>();
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        var label = button.GetComponentInChildren<TMP_Text>(true);
        var reference = MechaLocalizationAuthoring.Capture("Settings", "mecha.swap", "SWAP MECHA");
        label.GetComponent<LocalizeStringEvent>().StringReference = reference;
        label.GetComponent<LocalizeStringEvent>().RefreshString();
        serialized.FindProperty("swapMecha").objectReferenceValue = button;
        serialized.ApplyModifiedProperties();

        // The modal now confirms a saved preference rather than launching a connection.
        var confirm = (UnityEngine.UI.Button)new SerializedObject(selection).FindProperty("confirm").objectReferenceValue;
        var binding = confirm.GetComponentInChildren<LocalizeStringEvent>(true);
        var collection = LocalizationEditorSettings.GetStringTableCollection(binding.StringReference.TableReference);
        var table = collection.StringTables.Single(t => t.LocaleIdentifier.Code == "en");
        table.GetEntry("selection.confirm").Value = "CONFIRM MECHA";
        table.GetEntry("selection.hint").Value = "Choose a frame, then confirm your selection.  /  Back keeps your current mecha.";
        EditorUtility.SetDirty(table);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        EditorSceneManager.SaveScene(menu.gameObject.scene);
        AssetDatabase.SaveAssets();
        return "Authored Swap Mecha with existing focus feedback and localized English labels.";
    }
}
