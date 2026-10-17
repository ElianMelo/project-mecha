using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

// Author native TMP dropdown templates rather than replacing their controller selection/scroll behavior.
public static class MechaVideoSetup
{
    private static Sprite frame;
    private static TMP_FontAsset font;
    public static VideoConfigurationMenu Build(RectTransform panel)
    {
        frame = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        var card = Rect(panel, "Video", Vector2.zero, new Vector2(900f, 650f));
        card.anchorMin = card.anchorMax = card.pivot = Vector2.one * 0.5f; card.anchoredPosition = Vector2.zero;
        Image(card, new Color(0.025f, 0.05f, 0.085f));
        var group = MechaPresentationSetup.Ensure<CanvasGroup>(card.gameObject);
        group.interactable = group.blocksRaycasts = true;
        // Old controls are buttons. Replace only these three with native dropdown controls.
        foreach (string oldName in new[] { "Quality", "Display", "Sync" })
        { var old = card.Find(oldName); if (old != null) Object.DestroyImmediate(old.gameObject); }
        Label(card, "Title", "VIDEO CONFIGURATION", new Vector2(40f, 22f), new Vector2(820f, 44f), 30f);
        Label(card, "Subtitle", "Select an option to open its list. Changes are staged until Apply.", new Vector2(40f, 68f), new Vector2(820f, 26f), 16f);
        var display = Dropdown(card, "Display Mode", "DISPLAY MODE", 112f);
        var resolution = Dropdown(card, "Resolution", "RESOLUTION", 174f);
        var rate = Dropdown(card, "Refresh Rate", "REFRESH RATE", 236f);
        var quality = Dropdown(card, "Quality Preset", "QUALITY PRESET", 298f);
        var sync = Dropdown(card, "Vertical Sync", "V-SYNC", 360f);
        var limit = Dropdown(card, "Frame Limit", "FRAME LIMIT", 422f);
        Label(card, "Help", "Refresh rate applies to exclusive fullscreen. V-sync uses the display rate.\nD-pad / stick: select   •   Bottom button: open / confirm   •   Right button / Esc: back", new Vector2(40f, 480f), new Vector2(820f, 48f), 14f);
        var apply = Button(card, "Apply", "APPLY", new Vector2(40f, 546f), new Vector2(250f, 48f));
        var defaults = Button(card, "Defaults", "DEFAULTS", new Vector2(310f, 546f), new Vector2(250f, 48f));
        var back = Button(card, "Back", "BACK TO OPTIONS", new Vector2(580f, 546f), new Vector2(280f, 48f));
        var status = Label(card, "Status", "Changes take effect when you choose Apply.", new Vector2(40f, 606f), new Vector2(820f, 26f), 14f);
        var confirmation = Rect(panel, "Display Confirmation", Vector2.zero, Vector2.zero);
        Stretch(confirmation); Image(confirmation, new Color(0.005f, 0.012f, 0.023f, 0.97f), true); confirmation.SetAsLastSibling();
        var prompt = Rect(confirmation, "Card", Vector2.zero, new Vector2(680f, 300f));
        prompt.anchorMin = prompt.anchorMax = prompt.pivot = Vector2.one * 0.5f; prompt.anchoredPosition = Vector2.zero;
        Image(prompt, new Color(0.025f, 0.05f, 0.085f));
        var message = Label(prompt, "Message", "KEEP THESE DISPLAY SETTINGS?", new Vector2(40f, 36f), new Vector2(600f, 90f), 24f);
        message.alignment = TextAlignmentOptions.Center;
        var keep = Button(prompt, "Keep", "KEEP", new Vector2(40f, 190f), new Vector2(280f, 64f));
        var revert = Button(prompt, "Revert", "REVERT", new Vector2(360f, 190f), new Vector2(280f, 64f));
        foreach (var button in new[] { keep, revert }) button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
            selectOnLeft = button == keep ? revert : keep, selectOnRight = button == keep ? revert : keep,
            selectOnUp = button == keep ? revert : keep, selectOnDown = button == keep ? revert : keep };
        confirmation.gameObject.SetActive(false);
        var menu = MechaPresentationSetup.Ensure<VideoConfigurationMenu>(panel.gameObject);
        var settings = MechaPresentationSetup.Create<VideoConfigurationSettings>("VideoConfiguration");
        Assign(menu, "settings", settings); Assign(menu, "controls", group);
        Assign(menu, "display", display); Assign(menu, "resolution", resolution); Assign(menu, "refreshRate", rate);
        Assign(menu, "quality", quality); Assign(menu, "sync", sync); Assign(menu, "frameLimit", limit);
        Assign(menu, "apply", apply); Assign(menu, "defaults", defaults); Assign(menu, "back", back);
        Assign(menu, "confirmation", confirmation.gameObject); Assign(menu, "confirmationText", message); Assign(menu, "status", status);
        Assign(menu, "keep", keep); Assign(menu, "revert", revert);
        return menu;
    }
    private static TMP_Dropdown Dropdown(Transform card, string name, string title, float y)
    {
        Label(card, name + " Label", title, new Vector2(40f, y), new Vector2(230f, 44f), 18f);
        var existing = card.Find(name);
        GameObject root;
        if (existing != null) root = existing.gameObject;
        else
        {
            var resources = new TMP_DefaultControls.Resources {
                standard = frame, background = frame, inputField = frame,
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"), mask = frame };
            root = TMP_DefaultControls.CreateDropdown(resources); root.name = name; root.transform.SetParent(card, false);
        }
        var rect = Rect(card, name, new Vector2(290f, y), new Vector2(570f, 44f));
        var dropdown = root.GetComponent<TMP_Dropdown>(); dropdown.alphaFadeSpeed = 0.15f;
        Image(rect, new Color(0.1f, 0.23f, 0.3f), true);
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) { text.font = font; text.fontSize = 18f; text.color = new Color(0.88f, 0.95f, 0.99f); text.raycastTarget = false; }
        var template = dropdown.template;
        MechaPresentationSetup.Ensure<UiSelectionScroll>(template.gameObject);
        template.sizeDelta = new Vector2(0f, 220f);
        template.GetComponent<UnityEngine.UI.Image>().color = new Color(0.025f, 0.05f, 0.085f);
        var item = template.GetComponentInChildren<UnityEngine.UI.Toggle>(true);
        ((RectTransform)item.transform).sizeDelta = new Vector2(0f, 38f);
        ((RectTransform)item.transform.parent).sizeDelta = new Vector2(0f, 46f);
        item.targetGraphic.color = new Color(0.1f, 0.23f, 0.3f);
        item.graphic.color = new Color(0.36f, 0.9f, 0.92f);
        var colors = item.colors; colors.highlightedColor = new Color(0.5f, 1f, 1f); colors.selectedColor = colors.highlightedColor; item.colors = colors;
        // The existing Sprite border is cloned with each native option, including motion-off feedback.
        MechaUiFeelSetup.Button(item, MechaPresentationSetup.Profile<UiFeelSettings>("UiFeel"));
        return dropdown;
    }
    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var child = parent.Find(name); var rect = child != null ? (RectTransform)child : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(position.x, -position.y); rect.sizeDelta = size; return rect;
    }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static UnityEngine.UI.Image Image(RectTransform rect, Color color, bool raycast = false)
    { var image = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(rect.gameObject); image.sprite = frame; image.type = UnityEngine.UI.Image.Type.Sliced; image.color = color; image.raycastTarget = raycast; return image; }
    private static TMP_Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize)
    { var rect = Rect(parent, name, position, size); var label = MechaPresentationSetup.Ensure<TextMeshProUGUI>(rect.gameObject); label.font = font; label.text = value; label.fontSize = fontSize; label.color = new Color(0.88f, 0.95f, 0.99f); label.raycastTarget = false; label.alignment = TextAlignmentOptions.MidlineLeft; return label; }
    private static UnityEngine.UI.Button Button(Transform parent, string name, string value, Vector2 position, Vector2 size)
    {
        var rect = Rect(parent, name, position, size); var image = Image(rect, new Color(0.1f, 0.23f, 0.3f), true);
        var button = MechaPresentationSetup.Ensure<UnityEngine.UI.Button>(rect.gameObject); button.targetGraphic = image;
        var label = Label(rect, "Label", value, Vector2.zero, size, 20f); label.alignment = TextAlignmentOptions.Center; return button;
    }
    private static void Assign(Component target, string field, Object value) => MechaPresentationSetup.Assign(target, field, value);
}
