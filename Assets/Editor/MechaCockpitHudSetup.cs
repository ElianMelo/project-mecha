using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

public static class MechaCockpitHudSetup
{
    private static GameplayHudLayoutSettings palette;
    private static TMP_FontAsset font;
    public static void Apply(MechaGameplayHud hud)
    {
        palette = MechaPresentationSetup.Create<GameplayHudLayoutSettings>("GameplayHudLayout");
        palette.panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        Assign(hud, "settings", palette);
        MechaPresentationSetup.Ensure<UnityEngine.UI.GraphicRaycaster>(hud.gameObject);
        var panel = Rect(hud.transform, "Pilot Telemetry", palette.cockpitAnchor, palette.cockpitOffset, palette.cockpitSize);
        panel.localScale = Vector3.one * palette.pilotHudScale;
        Panel(panel, palette.panelColor);
        Line(panel, "Accent", new Vector2(0f, -1f), new Vector2(palette.cockpitSize.x, 2f), palette.accentColor);
        Move(hud.transform.Find("Status") as RectTransform, panel, new Vector2(16f, -12f), new Vector2(316f, 22f));
        var status = panel.Find("Status").GetComponent<TMP_Text>(); status.fontSize = 14f; status.color = palette.textColor;
        RepositionBar(hud, panel, "Health", new Vector2(16f, -62f), new Vector2(316f, 7f), palette.textColor);
        RepositionBar(hud, panel, "Mana", new Vector2(16f, -96f), new Vector2(316f, 5f), palette.accentColor);
        Assign(hud, "healthValue", Label(panel, "Armor Value", "AP   100 / 100", 16, 34, 300, 25, 23));
        Assign(hud, "energyValue", Label(panel, "Energy Value", "EN   100 / 100", 16, 72, 300, 22, 18));
        Assign(hud, "motion", Label(panel, "Motion", "SYSTEM STANDBY", 16, 116, 316, 18, 11));
        var mark = Label(hud.transform, "Expedition Mark", "PROJECT MECHA  /  COMBAT SYSTEMS\nMK-01    //    FIELD OPERATIONS", 28, 22, 340, 40, 12);
        mark.rectTransform.localScale = Vector3.one * palette.pilotHudScale;
        var link = Label(hud.transform, "Link", "OFFLINE  /  F3 DIAGNOSTICS", 0, 0, 330, 24, 12 * palette.interfaceTextScale);
        Position(link.rectTransform, Vector2.one, new Vector2(-28f, -22f), new Vector2(330f, 24f) * palette.interfaceTextScale);
        link.alignment = TextAlignmentOptions.TopRight; Assign(hud, "link", link);
        var combo = Label(hud.transform, "Combo Readout", "", 0, 0, 260, 94, 16 * palette.interfaceTextScale);
        Position(combo.rectTransform, new Vector2(1f, 0.5f), new Vector2(-32f, 0f), new Vector2(260f, 94f) * palette.interfaceTextScale);
        combo.alignment = TextAlignmentOptions.TopRight; Assign(hud, "comboReadout", combo);
        var controls = hud.transform.Find("Controls")?.GetComponent<TMP_Text>();
        if (controls != null)
        {
            Assign(hud, "controls", controls);
            Position(controls.rectTransform, Vector2.one, new Vector2(-28f, -55f), new Vector2(470f, 66f) * palette.interfaceTextScale);
            controls.fontSize = 11f * palette.interfaceTextScale; controls.color = new Color(0.64f, 0.75f, 0.78f);
            controls.text = palette.tutorialText;
        }
        var room = hud.GetComponentInChildren<LevelObjectiveHud>() ?? hud.GetComponent<LevelObjectiveHud>();
        if (room != null)
        {
            var objective = new SerializedObject(room).FindProperty("objective").objectReferenceValue as TMP_Text;
            if (objective != null)
            {
                Position(objective.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(520f, 38f) * palette.interfaceTextScale);
                objective.fontSize = 12f * palette.interfaceTextScale; objective.alignment = TextAlignmentOptions.Top; objective.color = palette.textColor;
            }
        }
        var skillBar = hud.transform.Find("Skills Top Bar");
        if (skillBar != null)
            foreach (var image in skillBar.GetComponentsInChildren<UnityEngine.UI.Image>())
            { image.sprite = palette.panelSprite; image.type = UnityEngine.UI.Image.Type.Sliced; image.color = palette.panelColor; }
        Diagnostics(hud);
        SkillFocus(hud);
        MechaUiFeelSetup.ApplyHud(hud);
        EditorUtility.SetDirty(palette); EditorUtility.SetDirty(hud);
    }
    private static void Diagnostics(MechaGameplayHud hud)
    {
        var diagnostics = MechaPresentationSetup.Ensure<GameplayDiagnostics>(hud.gameObject);
        var panel = Rect(hud.transform, "F3 Diagnostics", new Vector2(0f, 1f), palette.diagnosticsOffset, palette.diagnosticsSize);
        panel.localScale = Vector3.one * palette.interfaceTextScale;
        Panel(panel, new Color(0.015f, 0.025f, 0.035f, 0.97f));
        Line(panel, "Accent", Vector2.zero, new Vector2(palette.diagnosticsSize.x, 2f), palette.accentColor);
        Label(panel, "Title", "F3  //  COMBAT LAB", 16, 12, 450, 26, 18);
        Label(panel, "Phase Key", "BLUE Startup    RED Active    GREEN Recovery", 16, 46, 620, 20, 11);
        var strip = Rect(panel, "Frame Meter", new Vector2(0f, 1f), new Vector2(16f, -73f), new Vector2(624f, 12f));
        var layout = MechaPresentationSetup.Ensure<UnityEngine.UI.HorizontalLayoutGroup>(strip.gameObject);
        layout.spacing = 1f; layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        var cells = new UnityEngine.UI.Image[104];
        for (int i = 0; i < cells.Length; i++)
        {
            var cell = Rect(strip, "Frame " + i.ToString("000"), Vector2.zero, Vector2.zero, new Vector2(5f, 10f));
            cells[i] = Panel(cell, palette.neutralColor);
        }
        Label(panel, "Meter Help", "60 Hz display samples  /  wraps left to right  /  rendered-frame telemetry", 16, 89, 620, 18, 10);
        var readout = Label(panel, "Telemetry", "", 16, 114, 468, 230, 11);
        readout.lineSpacing = 10f;
        var inputs = Label(panel, "Input History", "", 494, 114, 152, 230, 11); inputs.lineSpacing = 4f;
        Assign(diagnostics, "panel", panel.gameObject); Assign(diagnostics, "readout", readout);
        Assign(diagnostics, "inputHistory", inputs); Assign(diagnostics, "settings", palette);
        Assign(diagnostics, "notice", Label(panel, "Notice", "Offline practice: refill. Hit volumes appear in Scene view.", 16, 378, 620, 24, 10));
        var serialized = new SerializedObject(diagnostics); var array = serialized.FindProperty("frameMeter"); array.arraySize = cells.Length;
        for (int i = 0; i < cells.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = cells[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Button(panel, "Reset", "RESET", 16, diagnostics.ResetCounters);
        Button(panel, "Copy", "COPY DATA", 120, diagnostics.CopySnapshot);
        Button(panel, "Refill", "REFILL", 224, diagnostics.RefillPractice);
        Button(panel, "Hitboxes", "HITBOXES", 432, diagnostics.ToggleHitboxes);
        Button(panel, "Enemy Hold", "HOLD AI", 536, diagnostics.ToggleEnemyPractice);
        panel.gameObject.SetActive(false);
    }
    private static void SkillFocus(MechaGameplayHud hud)
    {
        var settings = MechaPresentationSetup.Create<SkillPresentationSettings>("SkillPresentation");
        settings.backdropSprite = palette.panelSprite;
        if (settings.backdropMaterial == null)
        {
            const string path = "Assets/_Project/Materials/SkillBackdrop.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(material, path); }
            settings.backdropMaterial = material;
        }
        var focus = MechaPresentationSetup.Ensure<SkillFocusPresentation>(hud.gameObject);
        var root = GameObject.Find("Skill Focus Backdrop") ?? new GameObject("Skill Focus Backdrop");
        var backdrop = MechaPresentationSetup.Ensure<SpriteRenderer>(root); backdrop.sprite = settings.backdropSprite;
        backdrop.sharedMaterial = settings.backdropMaterial; backdrop.sortingOrder = -90; backdrop.color = Color.clear;
        root.transform.position = new Vector3(0f, 0f, settings.backdropDepth);
        var bars = Rect(hud.transform, "Skill Letterbox", Vector2.zero, Vector2.zero, Vector2.zero);
        bars.anchorMin = Vector2.zero; bars.anchorMax = Vector2.one; bars.offsetMin = bars.offsetMax = Vector2.zero;
        var group = MechaPresentationSetup.Ensure<CanvasGroup>(bars.gameObject); group.alpha = 0f; group.blocksRaycasts = false;
        var top = Rect(bars, "Top", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, settings.letterboxHeight.x));
        top.anchorMin = new Vector2(0f, 1f); top.anchorMax = Vector2.one; Panel(top, Color.black);
        var bottom = Rect(bars, "Bottom", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, settings.letterboxHeight.y));
        bottom.anchorMin = Vector2.zero; bottom.anchorMax = new Vector2(1f, 0f); Panel(bottom, Color.black);
        var title = Label(bars, "Skill Title", "", 0, 0, 600, 22, 12);
        Position(title.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(600f, 22f)); title.alignment = TextAlignmentOptions.Center;
        var flash = Rect(hud.transform, "Skill Impact Flash", Vector2.zero, Vector2.zero, Vector2.zero);
        flash.anchorMin = Vector2.zero; flash.anchorMax = Vector2.one; flash.offsetMin = flash.offsetMax = Vector2.zero;
        var image = Panel(flash, Color.clear);
        Assign(focus, "settings", settings); Assign(focus, "backdrop", backdrop); Assign(focus, "letterbox", group);
        Assign(focus, "title", title); Assign(focus, "flash", image);
        EditorUtility.SetDirty(settings);
    }
    private static void RepositionBar(MechaGameplayHud hud, RectTransform parent, string name, Vector2 position, Vector2 size, Color tint)
    {
        var existing = parent.Find(name) ?? hud.transform.Find(name);
        if (existing == null) return;
        Move(existing as RectTransform, parent, position, size);
        var background = existing.GetComponent<UnityEngine.UI.Image>(); background.sprite = palette.panelSprite;
        background.color = new Color(0.15f, 0.2f, 0.23f); background.raycastTarget = false;
        var fill = existing.Find("Fill")?.GetComponent<UnityEngine.UI.Image>();
        if (fill != null)
        {
            Position(fill.rectTransform, Vector2.zero, Vector2.zero, size); fill.color = tint;
            fill.sprite = palette.panelSprite; fill.raycastTarget = false;
        }
    }
    private static void Move(RectTransform rect, Transform parent, Vector2 position, Vector2 size)
    { if (rect == null) return; rect.SetParent(parent, false); Position(rect, new Vector2(0f, 1f), position, size); }
    private static void Assign(Component target, string field, Object value) => MechaPresentationSetup.Assign(target, field, value);
    private static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        var child = parent.Find(name);
        var rect = child != null ? child as RectTransform : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); Position(rect, anchor, offset, size); return rect;
    }
    private static void Position(RectTransform rect, Vector2 anchor, Vector2 offset, Vector2 size)
    { rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.anchoredPosition = offset; rect.sizeDelta = size; }
    private static UnityEngine.UI.Image Panel(RectTransform rect, Color color)
    {
        var image = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(rect.gameObject);
        image.sprite = palette.panelSprite; image.type = UnityEngine.UI.Image.Type.Sliced; image.color = color; image.raycastTarget = false; return image;
    }
    private static void Line(Transform parent, string name, Vector2 offset, Vector2 size, Color color) => Panel(Rect(parent, name, new Vector2(0f, 1f), offset, size), color);
    private static TMP_Text Label(Transform parent, string name, string value, float x, float y, float w, float h, float size)
    {
        var rect = Rect(parent, name, new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));
        var label = MechaPresentationSetup.Ensure<TextMeshProUGUI>(rect.gameObject);
        label.font = font; label.fontSize = size; label.color = palette.textColor; label.text = value; label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.TopLeft; return label;
    }
    private static void Button(Transform parent, string name, string label, float x, UnityAction action)
    {
        var rect = Rect(parent, name, new Vector2(0f, 1f), new Vector2(x, -344f), new Vector2(100f, 28f));
        var image = Panel(rect, new Color(0.12f, 0.23f, 0.28f)); image.raycastTarget = true;
        var button = MechaPresentationSetup.Ensure<UnityEngine.UI.Button>(rect.gameObject); button.targetGraphic = image;
        while (button.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(button.onClick, 0);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        Label(rect, "Label", label, 0, 0, 100, 28, 11).alignment = TextAlignmentOptions.Center;
    }
}
