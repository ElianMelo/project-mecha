using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class MechaLobbySetup
{
    private static TMP_FontAsset font;
    private static readonly Color Card = new Color(0.055f, 0.08f, 0.13f, 0.98f);
    private static readonly Color Accent = new Color(0.12f, 0.65f, 0.76f);
    public static void Apply(MechaLobbyService service)
    {
        font = Object.FindFirstObjectByType<MechaGameplayHud>().GetComponentInChildren<TMP_Text>().font;
        var root = GameObject.Find("Mecha Lobby UI") ?? new GameObject("Mecha Lobby UI");
        var canvas = MechaPresentationSetup.Ensure<Canvas>(root); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 150;
        var scaler = MechaPresentationSetup.Ensure<UnityEngine.UI.CanvasScaler>(root);
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f); scaler.matchWidthOrHeight = 0.5f;
        MechaPresentationSetup.Ensure<UnityEngine.UI.GraphicRaycaster>(root);
        var ui = MechaPresentationSetup.Ensure<MechaLobbyPanel>(root);
        var panel = Rect(root.transform, "Lobby Panel", Vector2.zero, Vector2.zero);
        panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one; panel.offsetMin = panel.offsetMax = Vector2.zero;
        MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(panel.gameObject).color = new Color(0.015f, 0.025f, 0.05f, 0.98f);
        Label(panel, "Title", "MECHA  /  ONLINE EXPEDITIONS", new Vector2(40f, 24f), new Vector2(950f, 36f), 28f);
        Label(panel, "Subtitle", "Create a team, ready up, and clear rooms together through Unity Relay.", new Vector2(40f, 68f), new Vector2(1000f, 26f), 16f);
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
        Label(createCard, "Help", "Up to 4 pilots\n\nEveryone presses READY before the game starts.\n\n1  Local host     2  Local join     3  Online lobbies", new Vector2(24f, 342f), new Vector2(488f, 142f), 16f);
        var room = Rect(panel, "Waiting Room", new Vector2(40f, 120f), new Vector2(1200f, 500f));
        MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(room.gameObject).color = Card;
        var roomTitle = Label(room, "Room Title", "EXPEDITION", new Vector2(40f, 24f), new Vector2(1100f, 64f), 24f);
        var members = Label(room, "Members", "", new Vector2(40f, 115f), new Vector2(1100f, 290f), 21f);
        var ready = Button(room, "Ready", "READY", new Vector2(800f, 426f), new Vector2(360f, 50f), Accent);
        var leave = Button(panel, "Leave", "LEAVE LOBBY / GAME", new Vector2(40f, 650f), new Vector2(250f, 40f));
        var status = Label(panel, "Status", "", new Vector2(310f, 646f), new Vector2(930f, 54f), 15f);
        Assign(ui, "service", service); Assign(ui, "panel", panel.gameObject); Assign(ui, "browser", browser.gameObject); Assign(ui, "room", room.gameObject);
        Assign(ui, "playerName", playerName); Assign(ui, "lobbyName", lobbyName); Assign(ui, "search", search); Assign(ui, "joinCode", code);
        Assign(ui, "status", status); Assign(ui, "roomTitle", roomTitle); Assign(ui, "members", members); Assign(ui, "readyText", ready.GetComponentInChildren<TMP_Text>());
        Assign(ui, "pageText", page); Assign(ui, "create", create); Assign(ui, "join", join); Assign(ui, "ready", ready); Assign(ui, "leave", leave);
        Assign(ui, "close", close); Assign(ui, "previous", prev); Assign(ui, "next", next);
        var so = new SerializedObject(ui); var array = so.FindProperty("lobbyRows"); array.arraySize = rows.Count;
        for (int i = 0; i < rows.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        Assign(Object.FindFirstObjectByType<GameManager>(), "lobbyPanel", ui);
        panel.gameObject.SetActive(false);
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
        label.font = font; label.text = text; label.fontSize = fontSize; label.color = new Color(0.88f, 0.93f, 0.98f);
        label.richText = false; label.raycastTarget = false; label.alignment = TextAlignmentOptions.TopLeft; return label;
    }
    private static UnityEngine.UI.Button Button(Transform parent, string name, string title, Vector2 offset, Vector2 size, Color? color = null)
    {
        var rect = Rect(parent, name, offset, size); var image = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(rect.gameObject);
        image.color = color ?? new Color(0.12f, 0.18f, 0.26f);
        var button = MechaPresentationSetup.Ensure<UnityEngine.UI.Button>(rect.gameObject); button.targetGraphic = image;
        var label = Label(rect, "Label", title, new Vector2(10f, 2f), size - new Vector2(20f, 4f), 17f);
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
