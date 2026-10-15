using UnityEditor;
using UnityEngine;

public static class MechaHudSetup
{
    public static void Apply(MechaGameplayHud hud)
    {
        var settings = MechaPresentationSetup.Create<GameplayHudLayoutSettings>("GameplayHudLayout");
        var skills = new SerializedObject(hud).FindProperty("skills");
        var bar = hud.transform.Find("Skills Top Bar") as RectTransform;
        if (bar == null)
        {
            bar = new GameObject("Skills Top Bar", typeof(RectTransform)).GetComponent<RectTransform>();
            bar.SetParent(hud.transform, false);
        }
        bar.anchorMin = bar.anchorMax = bar.pivot = settings.skillBarAnchor;
        bar.anchoredPosition = settings.skillBarOffset;
        bar.sizeDelta = new Vector2(skills.arraySize * settings.skillSize.x + Mathf.Max(0, skills.arraySize - 1) * settings.skillSpacing, settings.skillSize.y);
        var layout = MechaPresentationSetup.Ensure<UnityEngine.UI.HorizontalLayoutGroup>(bar.gameObject);
        layout.spacing = settings.skillSpacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        for (int i = 0; i < skills.arraySize; i++)
        {
            var label = skills.GetArrayElementAtIndex(i).objectReferenceValue as TMPro.TMP_Text;
            if (label == null) continue;
            var panel = label.transform.parent as RectTransform;
            if (panel == null || panel == hud.transform) continue;
            panel.SetParent(bar, false);
            panel.SetSiblingIndex(i);
            panel.sizeDelta = settings.skillSize;
            label.fontSize = settings.skillFontSize;
            label.rectTransform.anchoredPosition = settings.skillTextInset;
            label.rectTransform.sizeDelta = settings.skillSize - settings.skillTextPadding;
            var background = panel.GetComponent<UnityEngine.UI.Image>();
            if (background != null) background.raycastTarget = false;
        }
        var tutorial = hud.transform.Find("Controls")?.GetComponent<TMPro.TMP_Text>();
        if (tutorial != null)
        {
            var rt = tutorial.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one;
            rt.anchoredPosition = settings.tutorialOffset; rt.sizeDelta = settings.tutorialSize;
            tutorial.fontSize = settings.tutorialFontSize; tutorial.alignment = TMPro.TextAlignmentOptions.TopRight;
            tutorial.text = settings.tutorialText;
        }
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(bar);
        EditorUtility.SetDirty(bar); EditorUtility.SetDirty(settings);
    }
}
