using TMPro;
using UnityEngine;

public sealed class MechaLobbyPanel : MonoBehaviour
{
    [SerializeField] private MechaLobbyService service;
    [SerializeField] private GameObject panel, browser, room;
    [SerializeField] private TMP_InputField playerName, lobbyName, search, joinCode;
    [SerializeField] private TMP_Text status, roomTitle, members, readyText, pageText;
    [SerializeField] private UnityEngine.UI.Button create, join, ready, leave, close, previous, next;
    [SerializeField] private UnityEngine.UI.Button[] lobbyRows;
    [SerializeField] private LobbyPlayerCard[] playerCards;
    [SerializeField] private TMP_Text readiness;
    [SerializeField] private UnityEngine.UI.Button localHost, localJoin;
    private string[] rowIds;
    private int page;
    private bool wasInGame;
    private ControllerTextEntry textEntry;
    public bool IsVisible => panel.activeSelf;
    public string PilotName => ChatHistory.Clean(playerName.text, 24, GameText.Get(service.Settings.defaultPlayerNameLocalized));
    private void Awake()
    {
        textEntry = GetComponent<ControllerTextEntry>();
        rowIds = new string[lobbyRows.Length];
        playerName.text = GameText.Get(service.Settings.defaultPlayerNameLocalized); lobbyName.text = GameText.Get(service.Settings.defaultLobbyNameLocalized);
        create.onClick.AddListener(() => { _ = service.CreateAsync(lobbyName.text, playerName.text); });
        join.onClick.AddListener(() => { if (!string.IsNullOrWhiteSpace(joinCode.text)) _ = service.JoinAsync(joinCode.text, playerName.text, true); });
        ready.onClick.AddListener(() => { _ = service.ToggleReadyAsync(); });
        leave.onClick.AddListener(() => { _ = service.LeaveAsync(); }); close.onClick.AddListener(Hide);
        localHost.onClick.AddListener(() => GameManager.Instance.StartHost());
        localJoin.onClick.AddListener(() => GameManager.Instance.StartClient());
        search.onValueChanged.AddListener(value => { service.Search = value; page = 0; });
        previous.onClick.AddListener(() => { page = Mathf.Max(0, page - 1); Refresh(); });
        next.onClick.AddListener(() => { page++; Refresh(); });
        for (int i = 0; i < lobbyRows.Length; i++)
        {
            int slot = i;
            lobbyRows[i].onClick.AddListener(() => { if (rowIds[slot] != null) _ = service.JoinAsync(rowIds[slot], playerName.text); });
        }
        service.Changed += Refresh;
        panel.SetActive(false);
    }
    private void OnDestroy() { if (service != null) service.Changed -= Refresh; }
    public void Show() { if (!IsVisible) GameAudio.Play(GameAudioCue.UiOpen); panel.SetActive(true); service.OpenBrowser(); Refresh(); }
    public void Hide()
    {
        if (IsVisible) GameAudio.Play(GameAudioCue.UiCancel);
        if (textEntry != null && textEntry.IsVisible) textEntry.Cancel();
        panel.SetActive(false); service.CloseBrowser();
        GameManager.Instance?.SuppressGameplayInput();
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
    }
    private void Update()
    {
        bool inGame = service.InGame;
        if (inGame && !wasInGame) Hide();
        if (!inGame && wasInGame && service.Session == null) Show();
        wasInGame = inGame;
        if (IsVisible) EnsureSelection();
    }
    private void EnsureSelection()
    {
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events == null || MechaChat.IsTyping || GameManager.Instance != null && GameManager.Instance.OptionsMenuOpen) return;
        var selected = events.currentSelectedGameObject;
        var selectable = selected != null ? selected.GetComponent<UnityEngine.UI.Selectable>() : null;
        if (selectable != null && selectable.IsActive() && selectable.IsInteractable() && selected.transform.IsChildOf(panel.transform)) return;
        UnityEngine.UI.Selectable fallback = service.Session != null ? (ready.IsInteractable() ? ready : leave) : create;
        if (!fallback.IsInteractable()) fallback = close;
        events.SetSelectedGameObject(fallback.gameObject);
    }
    private void Refresh()
    {
        if (status == null) return;
        bool joined = service.Session != null;
        if (joined && textEntry != null && textEntry.IsVisible) textEntry.Cancel();
        browser.SetActive(!joined); room.SetActive(joined);
        status.text = service.Status;
        create.interactable = !service.Busy && !service.InGame;
        join.interactable = !service.Busy && !service.InGame;
        localHost.interactable = localJoin.interactable = !service.Busy && !service.InGame;
        leave.interactable = !service.Busy && !service.Starting;
        ready.interactable = joined && !service.Session.IsLocked && !service.Busy && !service.Starting && !service.InGame;
        readyText.text = service.IsReady ? GameText.Get("Lobby", "ready.cancel") : GameText.Get("Lobby", "ready.confirm");
        var buttonImage = ready.targetGraphic as UnityEngine.UI.Image;
        if (buttonImage != null) buttonImage.color = service.IsReady ? service.Settings.readyColor : service.Settings.waitingColor;
        status.color = service.Busy || service.Starting ? service.Settings.waitingColor : service.Settings.readyColor;
        int pages = Mathf.Max(1, Mathf.CeilToInt((float)service.Lobbies.Count / lobbyRows.Length));
        page = Mathf.Clamp(page, 0, pages - 1); pageText.text = GameText.Get("Lobby", "browser.pagination", (page + 1), (pages));
        previous.interactable = page > 0; next.interactable = page + 1 < pages;
        for (int i = 0; i < lobbyRows.Length; i++)
        {
            int index = page * lobbyRows.Length + i;
            bool exists = index < service.Lobbies.Count;
            lobbyRows[i].gameObject.SetActive(exists); rowIds[i] = null;
            if (!exists) continue;
            var lobby = service.Lobbies[index]; rowIds[i] = lobby.Id;
            lobbyRows[i].GetComponentInChildren<TMP_Text>().text = GameText.Get("Lobby", "browser.row", (lobby.Name), (lobby.MaxPlayers - lobby.AvailableSlots), (lobby.MaxPlayers));
            lobbyRows[i].interactable = !service.Busy && !service.InGame;
        }
        if (!joined) return;
        roomTitle.text = service.Session.Name + GameText.Get("Lobby", "room.code") + service.Session.Code;
        int slotIndex = 0, readyCount = 0;
        foreach (var player in service.Session.Players)
        {
            bool isReady = MechaLobbyService.Read(player, "ready") == "1";
            if (isReady) readyCount++;
            if (slotIndex < playerCards.Length)
                playerCards[slotIndex++].Present(player.Id, MechaLobbyService.Read(player, "name"),
                    player.Id == service.Session.Host ? GameText.Get("Lobby", "role.host") : GameText.Get("Lobby", "role.pilot"), isReady);
        }
        for (; slotIndex < playerCards.Length; slotIndex++) playerCards[slotIndex].Present(null, null, null, false);
        readiness.text = service.Starting ? GameText.Get("Lobby", "deployment.starting") : GameText.Get("Lobby", "pilots.ready", (readyCount), (service.Session.Players.Count));
        readiness.color = service.Starting || readyCount == service.Session.Players.Count ? service.Settings.readyColor : service.Settings.waitingColor;
        members.text = service.Starting ? GameText.Get("Lobby", "deployment.confirmed") : GameText.Get("Lobby", "ready.instructions");
    }
}
