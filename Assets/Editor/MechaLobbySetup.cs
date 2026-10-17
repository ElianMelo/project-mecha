using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class MechaLobbySetup
{
    private static TMP_FontAsset font;
    private static LobbySettings style;
    private static Sprite panelSprite;
    private static readonly Color Card = new Color(0.055f, 0.08f, 0.13f, 0.98f);
    private static readonly Color Accent = new Color(0.12f, 0.65f, 0.76f);
    public static void Apply(MechaLobbyService service)
    {
        style = service.Settings;
        panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        font = Object.FindFirstObjectByType<MechaGameplayHud>().GetComponentInChildren<TMP_Text>().font;
        var root = GameObject.Find("Mecha Lobby UI") ?? new GameObject("Mecha Lobby UI");
        var canvas = MechaPresentationSetup.Ensure<Canvas>(root); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 150;
        var scaler = MechaPresentationSetup.Ensure<UnityEngine.UI.CanvasScaler>(root);
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f); scaler.matchWidthOrHeight = 0.5f;
        MechaPresentationSetup.Ensure<UnityEngine.UI.GraphicRaycaster>(root);
        var ui = MechaPresentationSetup.Ensure<MechaLobbyPanel>(root);
        var panel = Rect(root.transform, "Lobby Panel", Vector2.zero, Vector2.zero);
        MechaPresentationSetup.Ensure<CanvasGroup>(panel.gameObject).alpha = 1f;
        panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one; panel.offsetMin = panel.offsetMax = Vector2.zero;
        MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(panel.gameObject).color = new Color(0.015f, 0.025f, 0.05f, 1f);
        Label(panel, "Title", "MECHA  /  ONLINE EXPEDITIONS", new Vector2(40f, 24f), new Vector2(1000f, 44f), 28f);
        Label(panel, "Subtitle", "ASSEMBLE YOUR SQUAD   /   READY UP   /   DEPLOY TOGETHER", new Vector2(40f, 76f), new Vector2(1040f, 30f), 16f);
        var close = Button(panel, "Close", "CLOSE", new Vector2(1110f, 28f), new Vector2(130f, 40f));
        var browser = Rect(panel, "Browser", new Vector2(40f, 120f), new Vector2(1200f, 500f));
        var browseCard = Rect(browser, "Browse Card", Vector2.zero, new Vector2(640f, 500f));
        MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(browseCard.gameObject).color = Card;
        Label(browseCard, "Browse Title", "FIND A LOBBY", new Vector2(20f, 16f), new Vector2(590f, 28f), 20f);
        var search = Input(browseCard, "Search", "Search lobby names…", new Vector2(20f, 57f), new Vector2(600f, 40f));
        var rows = new List<UnityEngine.UI.Button>();
        for (int i = 0; i < 6; i++) rows.Add(Button(browseCard, "Lobby Row " + i, "", new Vector2(20f, 111f + i * 43f), new Vector2(600f, 37f)));
        var prev = Button(browseCard, "Previous", "<", new Vector2(20f, 381f), new Vector2(42f, 32f));
        var next = Button(browseCard, "Next", ">", new Vector2(578f, 381f), new Vector2(42f, 32f));
        var page = Label(browseCard, "Page", "1 / 1  ·  AUTO REFRESH 1s", new Vector2(80f, 382f), new Vector2(480f, 30f), 14f);
        page.alignment = TextAlignmentOptions.Center;
        var code = Input(browseCard, "Join Code", "Or enter a lobby code", new Vector2(20f, 438f), new Vector2(400f, 40f));
        var join = Button(browseCard, "Join Code Button", "JOIN CODE", new Vector2(436f, 438f), new Vector2(184f, 40f));
        var createCard = Rect(browser, "Create Card", new Vector2(664f, 0f), new Vector2(536f, 500f));
        MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(createCard.gameObject).color = Card;
        Label(createCard, "Create Title", "YOUR EXPEDITION", new Vector2(24f, 16f), new Vector2(480f, 30f), 20f);
        Label(createCard, "Pilot Label", "PILOT NAME", new Vector2(24f, 68f), new Vector2(480f, 22f), 14f);
        var playerName = Input(createCard, "Player Name", "Pilot", new Vector2(24f, 100f), new Vector2(488f, 44f));
        Label(createCard, "Room Label", "NEW LOBBY NAME", new Vector2(24f, 169f), new Vector2(480f, 22f), 14f);
        var lobbyName = Input(createCard, "Lobby Name", "Mecha Expedition", new Vector2(24f, 200f), new Vector2(488f, 44f));
        var create = Button(createCard, "Create", "CREATE LOBBY", new Vector2(24f, 269f), new Vector2(488f, 48f), Accent);
        var localHost = Button(createCard, "Local Host", "SOLO HOST", new Vector2(24f, 341f), new Vector2(236f, 44f));
        var localJoin = Button(createCard, "Local Join", "LOCAL JOIN", new Vector2(276f, 341f), new Vector2(236f, 44f));
        Label(createCard, "Help", "Up to 4 pilots. Every pilot must be ready.\n\nD-pad / stick: navigate. Bottom button: select.\nRight button: back. MENU: close.", new Vector2(24f, 404f), new Vector2(488f, 92f), 13f);
        var room = Rect(panel, "Waiting Room", new Vector2(40f, 120f), new Vector2(1200f, 500f));
        MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(room.gameObject).color = Card;
        var roomTitle = Label(room, "Room Title", "EXPEDITION", new Vector2(40f, 24f), new Vector2(760f, 78f), 24f);
        var readiness = Label(room, "Readiness", "SQUAD STANDBY", new Vector2(830f, 35f), new Vector2(330f, 60f), 17f);
        readiness.alignment = TextAlignmentOptions.TopRight;
        var members = Label(room, "Members", "", new Vector2(40f, 386f), new Vector2(1120f, 32f), 16f);
        var cards = new List<LobbyPlayerCard>();
        int count = service.Settings.maxPlayers;
        float stride = count <= 4 ? 128f : 66f;
        for (int i = 0; i < count; i++)
            cards.Add(PlayerCard(room, i, new Vector2(40f + (i % 2) * 572f, 116f + (i / 2) * stride), count <= 4 ? 108f : 60f));
        var ready = Button(room, "Ready", "READY", new Vector2(800f, 426f), new Vector2(360f, 50f), Accent);
        var leave = Button(panel, "Leave", "LEAVE LOBBY / GAME", new Vector2(40f, 650f), new Vector2(250f, 40f));
        var status = Label(panel, "Status", "", new Vector2(310f, 646f), new Vector2(930f, 54f), 15f);
        Assign(ui, "service", service); Assign(ui, "panel", panel.gameObject); Assign(ui, "browser", browser.gameObject); Assign(ui, "room", room.gameObject);
        Assign(ui, "playerName", playerName); Assign(ui, "lobbyName", lobbyName); Assign(ui, "search", search); Assign(ui, "joinCode", code);
        Assign(ui, "status", status); Assign(ui, "roomTitle", roomTitle); Assign(ui, "members", members); Assign(ui, "readyText", ready.GetComponentInChildren<TMP_Text>());
        Assign(ui, "readiness", readiness);
        Assign(ui, "localHost", localHost); Assign(ui, "localJoin", localJoin);
        Assign(ui, "pageText", page); Assign(ui, "create", create); Assign(ui, "join", join); Assign(ui, "ready", ready); Assign(ui, "leave", leave);
        Assign(ui, "close", close); Assign(ui, "previous", prev); Assign(ui, "next", next);
        var so = new SerializedObject(ui); var array = so.FindProperty("lobbyRows"); array.arraySize = rows.Count;
        for (int i = 0; i < rows.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        var cardArray = so.FindProperty("playerCards"); cardArray.arraySize = cards.Count;
        for (int i = 0; i < cards.Count; i++) cardArray.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        Assign(Object.FindFirstObjectByType<GameManager>(), "lobbyPanel", ui);
        var keyboard = BuildKeyboard(panel);
        foreach (var field in new[] { playerName, lobbyName, search, code })
            Assign(MechaPresentationSetup.Ensure<ControllerTextField>(field.gameObject), "keyboard", keyboard);
        MechaUiFeelSetup.ApplyLobby(ui);
        panel.gameObject.SetActive(false);
    }
    private static ControllerTextEntry BuildKeyboard(RectTransform parent)
    {
        var profile = MechaPresentationSetup.Create<ControllerInterfaceSettings>("ControllerInterface");
        var overlay = Rect(parent, "Controller Keyboard", Vector2.zero, Vector2.zero);
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(overlay.gameObject).color = new Color(0.005f, 0.01f, 0.025f, 0.98f);
        var card = Rect(overlay, "Keyboard Card", Vector2.zero, new Vector2(800f, 480f));
        card.anchorMin = card.anchorMax = profile.keyboardPanelAnchor; card.pivot = new Vector2(0.5f, 0.5f); card.anchoredPosition = Vector2.zero;
        MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(card.gameObject).color = Card;
        var title = Label(card, "Title", "EDIT TEXT", new Vector2(24f, 16f), new Vector2(752f, 40f), 22f);
        var preview = Label(card, "Preview", "", new Vector2(24f, 68f), new Vector2(752f, 48f), 22f);
        preview.overflowMode = TextOverflowModes.Ellipsis; preview.textWrappingMode = TextWrappingModes.NoWrap;
        var grid = Rect(card, "Keys", new Vector2(24f, 132f), new Vector2(752f, 236f));
        var layout = MechaPresentationSetup.Ensure<UnityEngine.UI.GridLayoutGroup>(grid.gameObject);
        layout.cellSize = new Vector2(68f, 48f); layout.spacing = new Vector2(8f, 8f);
        layout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 10;
        var keys = new List<UnityEngine.UI.Button>();
        for (int i = 0; i < profile.keyboardCharacters.Length; i++)
            keys.Add(Button(grid, "Key " + i, profile.keyboardCharacters[i].ToString(), Vector2.zero, layout.cellSize));
        var erase = Button(card, "Erase", "ERASE", new Vector2(24f, 402f), new Vector2(140f, 48f));
        var space = Button(card, "Space", "SPACE", new Vector2(176f, 402f), new Vector2(140f, 48f));
        var changeCase = Button(card, "Case", "abc", new Vector2(328f, 402f), new Vector2(120f, 48f));
        var cancel = Button(card, "Cancel", "CANCEL", new Vector2(460f, 402f), new Vector2(148f, 48f));
        var confirm = Button(card, "Confirm", "DONE", new Vector2(620f, 402f), new Vector2(156f, 48f), Accent);
        var footer = new[] { erase, space, changeCase, cancel, confirm };
        for (int i = 0; i < keys.Count; i++)
        {
            int column = i % 10, row = i / 10, columns = Mathf.Min(10, keys.Count - row * 10);
            var navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnLeft = keys[row * 10 + (column + columns - 1) % columns], selectOnRight = keys[row * 10 + (column + 1) % columns],
                selectOnUp = row > 0 ? keys[i - 10] : footer[column / 2],
                selectOnDown = i + 10 < keys.Count ? keys[i + 10] : footer[column / 2] };
            keys[i].navigation = navigation;
        }
        for (int i = 0; i < footer.Length; i++) footer[i].navigation = new UnityEngine.UI.Navigation {
            mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnLeft = footer[(i + footer.Length - 1) % footer.Length],
            selectOnRight = footer[(i + 1) % footer.Length], selectOnUp = keys[Mathf.Clamp(keys.Count - 10 + i * 2, 0, keys.Count - 1)], selectOnDown = keys[Mathf.Min(i * 2, keys.Count - 1)] };
        var keyboard = MechaPresentationSetup.Ensure<ControllerTextEntry>(parent.parent.gameObject);
        Assign(keyboard, "panel", overlay.gameObject); Assign(keyboard, "settings", profile); Assign(keyboard, "title", title); Assign(keyboard, "preview", preview);
        Assign(keyboard, "erase", erase); Assign(keyboard, "space", space); Assign(keyboard, "changeCase", changeCase);
        Assign(keyboard, "cancel", cancel); Assign(keyboard, "confirm", confirm);
        var serialized = new SerializedObject(keyboard); var array = serialized.FindProperty("keys"); array.arraySize = keys.Count;
        for (int i = 0; i < keys.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = keys[i];
        serialized.ApplyModifiedPropertiesWithoutUndo(); overlay.gameObject.SetActive(false); return keyboard;
    }
    private static LobbyPlayerCard PlayerCard(Transform parent, int index, Vector2 offset, float height)
    {
        var rect = Rect(parent, "Pilot Card " + index, offset, new Vector2(548f, height));
        var border = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(rect.gameObject);
        border.sprite = panelSprite; border.type = UnityEngine.UI.Image.Type.Sliced;
        border.color = style.emptyColor; border.raycastTarget = false;
        var inset = Rect(rect, "Surface", new Vector2(2f, 2f), new Vector2(544f, height - 4f));
        var background = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(inset.gameObject);
        background.sprite = panelSprite; background.type = UnityEngine.UI.Image.Type.Sliced;
        background.color = style.cardColor; background.raycastTarget = false;
        bool compact = height < 80f;
        var name = Label(inset, "Pilot Name", "OPEN SLOT", new Vector2(18f, compact ? 4f : 20f), new Vector2(330f, 34f), 21f);
        name.overflowMode = TextOverflowModes.Ellipsis; name.textWrappingMode = TextWrappingModes.NoWrap;
        var role = Label(inset, "Role", "INVITE A PILOT", new Vector2(18f, compact ? 32f : 62f), new Vector2(330f, 24f), compact ? 11f : 13f);
        var state = Label(inset, "State", "AWAITING PILOT", new Vector2(354f, compact ? 16f : 40f), new Vector2(170f, 30f), 14f);
        state.alignment = TextAlignmentOptions.MidlineRight; state.color = style.emptyColor;
        var card = MechaPresentationSetup.Ensure<LobbyPlayerCard>(rect.gameObject);
        Assign(card, "settings", style); Assign(card, "border", border); Assign(card, "background", background);
        Assign(card, "pilotName", name); Assign(card, "role", role); Assign(card, "state", state);
        return card;
    }
    private static void Assign(Component component, string name, Object value) => MechaPresentationSetup.Assign(component, name, value);
    private static RectTransform Rect(Transform parent, string name, Vector2 offset, Vector2 size)
    {
        var go = MechaScenarioSetup.Child(parent, name);
        var rect = MechaPresentationSetup.Ensure<RectTransform>(go);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(offset.x, -offset.y); rect.sizeDelta = size; return rect;
    }
    private static TMP_Text Label(Transform parent, string name, string text, Vector2 offset, Vector2 size, float fontSize)
    {
        var rect = Rect(parent, name, offset, size); var label = MechaPresentationSetup.Ensure<TextMeshProUGUI>(rect.gameObject);
        label.font = font; label.text = text; label.fontSize = fontSize * style.textScale; label.color = new Color(0.88f, 0.93f, 0.98f);
        label.richText = false; label.raycastTarget = false; label.alignment = TextAlignmentOptions.TopLeft; return label;
    }
    private static UnityEngine.UI.Button Button(Transform parent, string name, string title, Vector2 offset, Vector2 size, Color? color = null)
    {
        var rect = Rect(parent, name, offset, size); var image = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(rect.gameObject);
        image.color = color ?? new Color(0.12f, 0.18f, 0.26f);
        image.sprite = panelSprite; image.type = UnityEngine.UI.Image.Type.Sliced;
        var button = MechaPresentationSetup.Ensure<UnityEngine.UI.Button>(rect.gameObject); button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f); colors.pressedColor = new Color(0.72f, 0.85f, 0.92f);
        colors.selectedColor = new Color(1.8f, 2.1f, 2.2f); colors.fadeDuration = 0.1f; button.colors = colors;
        var label = Label(rect, "Label", title, new Vector2(10f, 2f), size - new Vector2(20f, 4f), 17f);
        if (color.HasValue) { label.color = style.actionTextColor; label.fontStyle = FontStyles.Bold; }
        label.alignment = TextAlignmentOptions.Center; return button;
    }
    private static TMP_InputField Input(Transform parent, string name, string hint, Vector2 offset, Vector2 size)
    {
        var rect = Rect(parent, name, offset, size); var image = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(rect.gameObject);
        image.color = new Color(0.025f, 0.045f, 0.075f);
        var field = MechaPresentationSetup.Ensure<TMP_InputField>(rect.gameObject);
        var viewport = Rect(rect, "Viewport", new Vector2(12f, 0f), size - new Vector2(24f, 0f));
        MechaPresentationSetup.Ensure<UnityEngine.UI.RectMask2D>(viewport.gameObject);
        var label = Label(viewport, "Text", "", Vector2.zero, viewport.sizeDelta, 18f); label.alignment = TextAlignmentOptions.MidlineLeft;
        var placeholder = Label(viewport, "Placeholder", hint, Vector2.zero, viewport.sizeDelta, 18f);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft; placeholder.color = new Color(0.45f, 0.54f, 0.64f);
        field.textViewport = viewport; field.textComponent = (TMP_Text)label; field.placeholder = placeholder; field.targetGraphic = image;
        field.characterLimit = 28; field.caretColor = Color.white; return field;
    }
}
