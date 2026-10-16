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
    private string[] rowIds;
    private int page;
    private bool wasInGame;
    public bool IsVisible => panel.activeSelf;
    private void Awake()
    {
        rowIds = new string[lobbyRows.Length];
        playerName.text = service.Settings.defaultPlayerName; lobbyName.text = service.Settings.defaultLobbyName;
        create.onClick.AddListener(() => { _ = service.CreateAsync(lobbyName.text, playerName.text); });
        join.onClick.AddListener(() => { if (!string.IsNullOrWhiteSpace(joinCode.text)) _ = service.JoinAsync(joinCode.text, playerName.text, true); });
        ready.onClick.AddListener(() => { _ = service.ToggleReadyAsync(); });
        leave.onClick.AddListener(() => { _ = service.LeaveAsync(); }); close.onClick.AddListener(Hide);
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
    public void Show() { panel.SetActive(true); service.OpenBrowser(); Refresh(); }
    public void Hide() { panel.SetActive(false); service.CloseBrowser(); UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null); }
    private void Update()
    {
        bool inGame = service.InGame;
        if (inGame && !wasInGame) Hide();
        if (!inGame && wasInGame && service.Session == null) Show();
        wasInGame = inGame;
    }
    private void Refresh()
    {
        if (status == null) return;
        bool joined = service.Session != null;
        browser.SetActive(!joined); room.SetActive(joined);
        status.text = service.Status;
        create.interactable = !service.Busy && !service.InGame;
        join.interactable = !service.Busy && !service.InGame;
        leave.interactable = !service.Busy && !service.Starting;
        ready.interactable = joined && !service.Session.IsLocked && !service.Busy && !service.Starting && !service.InGame;
        readyText.text = service.IsReady ? "NOT READY" : "READY";
        int pages = Mathf.Max(1, Mathf.CeilToInt((float)service.Lobbies.Count / lobbyRows.Length));
        page = Mathf.Clamp(page, 0, pages - 1); pageText.text = $"{page + 1} / {pages}  ·  AUTO REFRESH 1s";
        previous.interactable = page > 0; next.interactable = page + 1 < pages;
        for (int i = 0; i < lobbyRows.Length; i++)
        {
            int index = page * lobbyRows.Length + i;
            bool exists = index < service.Lobbies.Count;
            lobbyRows[i].gameObject.SetActive(exists); rowIds[i] = null;
            if (!exists) continue;
            var lobby = service.Lobbies[index]; rowIds[i] = lobby.Id;
            lobbyRows[i].GetComponentInChildren<TMP_Text>().text = $"{lobby.Name}    {lobby.MaxPlayers - lobby.AvailableSlots}/{lobby.MaxPlayers}    JOIN →";
            lobbyRows[i].interactable = !service.Busy && !service.InGame;
        }
        if (!joined) return;
        roomTitle.text = service.Session.Name + "\nLOBBY CODE  " + service.Session.Code;
        var text = new System.Text.StringBuilder();
        foreach (var player in service.Session.Players)
        {
            bool isReady = MechaLobbyService.Read(player, "ready") == "1";
            text.Append(player.Id == service.Session.Host ? "HOST  " : "PILOT  ");
            text.Append(MechaLobbyService.Read(player, "name"));
            text.AppendLine(isReady ? "     READY" : "     WAITING");
        }
        members.text = text.ToString() + "\nEvery pilot must be ready. The game starts automatically.";
    }
}
