using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FishNet.Managing;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Relay.Models;
using UnityEngine;

public sealed class MechaLobbyService : MonoBehaviour
{
    [SerializeField] private LobbySettings settings;
    [SerializeField] private NetworkManager network;
    public event Action Changed;
    public ISession Session { get; private set; }
    public IReadOnlyList<ISessionInfo> Lobbies { get; private set; } = Array.Empty<ISessionInfo>();
    public bool Busy { get; private set; }
    public MechaMatchMode SelectedMode { get; private set; }
    public PvpTeam SelectedTeam { get; private set; } = PvpTeam.Blue;
    public string PilotName { get; set; }
    public bool PvpLobby => SelectedMode != MechaMatchMode.Cooperative;
    private string SessionType => PvpLobby ? settings.sessionType + "-pvp" : settings.sessionType;
    public bool Browsing { get; private set; }
    public bool InGame => network != null && network.ClientManager.Started;
    public string Status { get; private set; } = "";
    public string Search { get; set; } = "";
    public bool IsReady => Session != null && PlayerReady(Session.CurrentPlayer);
    public bool Starting => starting || Session != null && Session.Properties.TryGetValue("phase", out var phase) && phase.Value == "starting" && !InGame;
    private bool querying, refreshing, starting, destroyed, leaving;
    private float nextQuery, nextRoster;
    private int queryFailures;
    private Task initializeTask;
    private FishNetSessionNetworkHandler handler;
    public LobbySettings Settings => settings;
    private void Awake() { handler = new FishNetSessionNetworkHandler(network, settings); Status = GameText.Get("Lobby", "browser.welcome"); }
    public void PrepareLocal(MechaMatchMode mode, PvpTeam team = PvpTeam.Blue)
    {
        if (Session != null || InGame || Busy) return;
        SelectedMode = mode; SelectedTeam = team; Browsing = false;
    }
    public void OpenBrowser(MechaMatchMode mode = MechaMatchMode.Cooperative)
    {
        if (Session == null && !InGame && !Busy) { SelectedMode = mode; Lobbies = Array.Empty<ISessionInfo>(); }
        Browsing = Session == null && !InGame; nextQuery = 0f;
        Changed?.Invoke();
    }
    public Task SelectModeAsync(MechaMatchMode mode) => Operation(async () =>
    {
        if (!PvpLobby || mode != MechaMatchMode.FreeForAll && mode != MechaMatchMode.Team || Starting || InGame) return;
        if (Session == null) { SelectedMode = mode; return; }
        if (!Session.IsHost || Session.IsLocked) return;
        var host = Session.AsHost(); host.SetProperty("mode", new SessionProperty(mode.ToString()));
        host.SetProperty("rules", new SessionProperty(Guid.NewGuid().ToString("N")));
        await host.SavePropertiesAsync(); SelectedMode = mode;
        Session.CurrentPlayer.SetProperty("ready", new PlayerProperty("0"));
        await Session.SaveCurrentPlayerDataAsync();
    });
    public Task SelectTeamAsync(PvpTeam team) => Operation(async () =>
    {
        if (SelectedMode != MechaMatchMode.Team || team != PvpTeam.Blue && team != PvpTeam.Red || Starting || InGame || Session != null && Session.IsLocked) return;
        SelectedTeam = team;
        if (Session == null) return;
        Session.CurrentPlayer.SetProperty("team", new PlayerProperty(team.ToString()));
        Session.CurrentPlayer.SetProperty("ready", new PlayerProperty("0"));
        await Session.SaveCurrentPlayerDataAsync();
    });
    public void CloseBrowser() => Browsing = false;
    private void Update()
    {
        if (destroyed || Busy) return;
        if (Browsing && Session == null && !querying && Time.unscaledTime >= nextQuery) _ = RefreshLobbiesAsync();
        if (Session != null && !refreshing && !Starting && Time.unscaledTime >= nextRoster) _ = RefreshRosterAsync();
    }
    public async Task InitializeAsync()
    {
        if (initializeTask == null) initializeTask = InitializeCoreAsync();
        try { await initializeTask; } catch { initializeTask = null; throw; }
    }
    private async Task InitializeCoreAsync()
    {
        try
        {
            string profile = settings.authenticationProfile + (Application.isEditor ? "_editor" : "_player");
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "--mecha-profile");
            if (at >= 0 && at + 1 < args.Length) profile = args[at + 1];
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync(new InitializationOptions().SetProfile(profile));
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch { initializeTask = null; throw; }
    }
    public async Task RefreshLobbiesAsync()
    {
        if (querying || destroyed || Session != null) return;
        querying = true;
        string queriedType = SessionType;
        try
        {
            await InitializeAsync();
            var filters = new List<FilterOption> {
                new FilterOption(FilterField.StringIndex1, queriedType, FilterOperation.Equal),
                new FilterOption(FilterField.IsLocked, "false", FilterOperation.Equal),
                new FilterOption(FilterField.AvailableSlots, "0", FilterOperation.Greater)};
            if (!string.IsNullOrWhiteSpace(Search)) filters.Add(new FilterOption(FilterField.Name, Search.Trim(), FilterOperation.Contains));
            var result = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions {Count = 100, FilterOptions = filters});
            queryFailures = 0;
            if (!destroyed && Session == null && queriedType == SessionType)
            {
                Lobbies = result.Sessions.ToArray();
                Status = Lobbies.Count == 0 ? GameText.Get("Lobby", "browser.empty") : GameText.Get("Lobby", "browser.count", (Lobbies.Count));
            }
        }
        catch (Exception e) { queryFailures = Mathf.Min(3, queryFailures + 1); Status = Friendly(e); }
        finally { querying = false; nextQuery = Time.unscaledTime + settings.browserRefreshSeconds * (1 << queryFailures); if (!destroyed) Changed?.Invoke(); }
    }
    public Task CreateAsync(string lobbyName, string playerName) => Operation(async () =>
    {
        PilotName = playerName;
        if (CharacterSelectionMenu.Instance != null && !await CharacterSelectionMenu.Instance.EnsureConfirmedAsync()) return;
        await InitializeAsync();
        if (Session != null || InGame) throw new InvalidOperationException(GameText.Get("Lobby", "session.leaveFirst"));
        var options = new SessionOptions {Name = Clean(lobbyName, PvpLobby ? GameText.Get("Lobby", "pvp.defaultName") : GameText.Get(settings.defaultLobbyNameLocalized)), MaxPlayers = settings.maxPlayers,
            Type = SessionType, PlayerProperties = PlayerData(playerName),
            SessionProperties = new Dictionary<string, SessionProperty> {
                {"game", new SessionProperty(SessionType, VisibilityPropertyOptions.Public, PropertyIndex.String1)},
                {"mode", new SessionProperty(SelectedMode.ToString())},
                {"rules", new SessionProperty(Guid.NewGuid().ToString("N"))},
                {"phase", new SessionProperty("waiting")}}};
        options.WithNetworkHandler(handler).WithNetworkOptions(new NetworkOptions {RelayProtocol = RelayProtocol.DTLS});
        Attach(await MultiplayerService.Instance.CreateSessionAsync(options));
    });
    public Task JoinAsync(string id, string playerName, bool byCode = false) => Operation(async () =>
    {
        PilotName = playerName;
        if (CharacterSelectionMenu.Instance != null && !await CharacterSelectionMenu.Instance.EnsureConfirmedAsync()) return;
        await InitializeAsync();
        if (Session != null || InGame) throw new InvalidOperationException(GameText.Get("Lobby", "session.leaveFirst"));
        var options = new JoinSessionOptions {Type = SessionType, PlayerProperties = PlayerData(playerName)};
        options.WithNetworkHandler(handler).WithNetworkOptions(new NetworkOptions {RelayProtocol = RelayProtocol.DTLS});
        string expectedType = SessionType;
        var joined = byCode ? await MultiplayerService.Instance.JoinSessionByCodeAsync(id.Trim().ToUpperInvariant(), options)
            : await MultiplayerService.Instance.JoinSessionByIdAsync(id, options);
        if (!joined.Properties.TryGetValue("game", out var game) || game.Value != expectedType)
        { await joined.LeaveAsync(); throw new InvalidOperationException("Lobby type does not match this browser."); }
        Attach(joined);
    });
    public Task ToggleReadyAsync() => Operation(async () =>
    {
        if (Session == null || Session.IsLocked || Starting || InGame) return;
        Session.CurrentPlayer.SetProperty("ready", new PlayerProperty(IsReady ? "0" : "1"));
        Session.CurrentPlayer.SetProperty("readyMode", new PlayerProperty(SelectedMode.ToString()));
        Session.CurrentPlayer.SetProperty("readyRules", new PlayerProperty(RulesVersion));
        await Session.SaveCurrentPlayerDataAsync();
        Status = GameText.Get("Lobby", "ready.waiting");
    });
    private void Attach(ISession session)
    {
        Session = session; Browsing = false; nextRoster = Time.unscaledTime + settings.rosterRefreshSeconds;
        ReadMode();
        session.Changed += OnSessionChanged; session.RemovedFromSession += OnRemoved;
        session.Deleted += OnRemoved; session.SessionHostChanged += OnHostChanged;
        session.Network.StartFailed += OnNetworkStartFailed; session.Network.StateChanged += OnNetworkStateChanged;
        Status = GameText.Get("Lobby", "session.joined"); Changed?.Invoke();
    }
    private void Detach()
    {
        if (Session == null) return;
        Session.Changed -= OnSessionChanged; Session.RemovedFromSession -= OnRemoved;
        Session.Deleted -= OnRemoved; Session.SessionHostChanged -= OnHostChanged;
        Session.Network.StartFailed -= OnNetworkStartFailed; Session.Network.StateChanged -= OnNetworkStateChanged;
        Session = null;
    }
    private void ReadMode()
    {
        if (Session != null && Session.Properties.TryGetValue("mode", out var value) && Enum.TryParse(value.Value, out MechaMatchMode mode) && Enum.IsDefined(typeof(MechaMatchMode), mode)) SelectedMode = mode;
    }
    private void OnSessionChanged() { ReadMode(); Changed?.Invoke(); if (!Busy) _ = CheckReadyAsync(); }
    private void OnNetworkStateChanged(NetworkState state)
    {
        if (state == NetworkState.Starting) Status = GameText.Get("Lobby", "connection.starting");
        if (state == NetworkState.Started) Status = GameText.Get("Lobby", "deployment.started");
        Changed?.Invoke();
    }
    private async void OnNetworkStartFailed(SessionError error)
    {
        if (destroyed || Session == null || Session.IsHost) return;
        var session = Session; Detach();
        try { await session.LeaveAsync(); } catch (Exception e) { Debug.LogWarning("Lobby cleanup: " + e.Message); }
        await handler.StopAsync();
        Browsing = true; Status = GameText.Get("Lobby", "connection.failed"); Changed?.Invoke();
    }
    private void OnHostChanged(string id) { if (InGame || starting) OnRemoved(); else Changed?.Invoke(); }
    private async void OnRemoved()
    {
        if (leaving || destroyed) return;
        Detach(); starting = false; await handler.StopAsync();
        Status = GameText.Get("Lobby", "session.hostLeft"); Browsing = true; Changed?.Invoke();
    }
    private async Task RefreshRosterAsync()
    {
        refreshing = true;
        try
        {
            var session = Session; await session.RefreshAsync();
            if (session == Session)
            {
                if (!InGame && !Starting) Status = GameText.Get("Lobby", "ready.waiting");
                await CheckReadyAsync();
            }
        }
        catch (Exception e) { if (!leaving) Status = Friendly(e); }
        finally { refreshing = false; nextRoster = Time.unscaledTime + settings.rosterRefreshSeconds; if (!destroyed) Changed?.Invoke(); }
    }
    private async Task CheckReadyAsync()
    {
        var session = Session;
        if (session == null || !session.IsHost || starting || InGame || Busy || session.PlayerCount == 0 ||
            session.Players.Any(player => !PlayerReady(player)) || !PvpRosterValid(session)) return;
        starting = true; Status = GameText.Get("Lobby", "ready.connecting"); Changed?.Invoke();
        try
        {
            var host = session.AsHost(); host.IsLocked = true;
            host.SetProperty("phase", new SessionProperty("starting")); await host.SavePropertiesAsync();
            await Task.Delay(1100);
            await session.RefreshAsync();
            if (session.Players.Any(player => !PlayerReady(player)) || !PvpRosterValid(session))
            { host.IsLocked = false; host.SetProperty("phase", new SessionProperty("waiting")); await host.SavePropertiesAsync(); Status = GameText.Get("Lobby", "ready.waiting"); return; }
            await host.Network.StartRelayNetworkAsync(new RelayNetworkOptions());
            host.SetProperty("phase", new SessionProperty("playing")); await host.SavePropertiesAsync();
            Status = GameText.Get("Lobby", "deployment.started");
        }
        catch (Exception e)
        {
            Status = Friendly(e);
            try
            {
                await handler.StopAsync();
                if (Session == session)
                {
                    var host = session.AsHost(); host.IsLocked = false;
                    host.SetProperty("phase", new SessionProperty("waiting")); await host.SavePropertiesAsync();
                    session.CurrentPlayer.SetProperty("ready", new PlayerProperty("0")); await session.SaveCurrentPlayerDataAsync();
                }
            }
            catch (Exception cleanup) { Debug.LogWarning("Lobby recovery: " + cleanup.Message); }
        }
        finally { starting = false; if (!destroyed) Changed?.Invoke(); }
    }
    public Task LeaveAsync() => Operation(async () =>
    {
        leaving = true;
        var session = Session; Detach(); starting = false;
        try
        {
            if (session != null) { if (session.IsHost) await session.AsHost().DeleteAsync(); else await session.LeaveAsync(); }
        }
        finally { await handler.StopAsync(); leaving = false; Browsing = true; nextQuery = 0f; Status = GameText.Get("Lobby", "session.left"); }
    });
    private string RulesVersion => Session != null && Session.Properties.TryGetValue("rules", out var rules) ? rules.Value : "";
    public bool PlayerReady(IReadOnlyPlayer player) => Read(player, "ready") == "1" && (!PvpLobby || Read(player, "readyMode") == SelectedMode.ToString() && Read(player, "readyRules") == RulesVersion);
    private bool PvpRosterValid(ISession session) => !PvpLobby || session.PlayerCount >= 2 && (SelectedMode != MechaMatchMode.Team ||
        session.Players.All(p => Read(p, "team") == "Blue" || Read(p, "team") == "Red") &&
        session.Players.Any(p => Read(p, "team") == "Blue") && session.Players.Any(p => Read(p, "team") == "Red"));
    private async Task Operation(Func<Task> operation)
    {
        if (Busy || destroyed) return;
        Busy = true; Changed?.Invoke();
        try { await operation(); }
        catch (Exception e) { Status = Friendly(e); Debug.LogWarning("Lobby: " + e.Message); }
        finally { Busy = false; if (!destroyed) { Changed?.Invoke(); _ = CheckReadyAsync(); } }
    }
    private Dictionary<string, PlayerProperty> PlayerData(string playerName) => new Dictionary<string, PlayerProperty> {
        {"name", new PlayerProperty(Clean(playerName, GameText.Get(settings.defaultPlayerNameLocalized)))}, {"ready", new PlayerProperty("0")},
        {"character", new PlayerProperty(CharacterSelectionMenu.SelectedId)}, {"team", new PlayerProperty(SelectedTeam.ToString())},
        {"readyMode", new PlayerProperty(SelectedMode.ToString())}};
    public static string Read(IReadOnlyPlayer player, string key) => player != null && player.Properties.TryGetValue(key, out var value) ? value.Value : "";
    private static string Clean(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().Substring(0, Mathf.Min(28, value.Trim().Length));
    private static string Friendly(Exception error) => error.Message.Contains("429")
        ? GameText.Get("Lobby", "services.busy")
        : GameText.Get("Lobby", "request.failed") + GameText.Get("Lobby", "request.retry");
    private async void OnDestroy()
    {
        destroyed = true; var session = Session; Detach();
        try { if (session != null) { if (session.IsHost) await session.AsHost().DeleteAsync(); else await session.LeaveAsync(); } }
        catch { /* The SDK heartbeat expiry also cleans up an interrupted application. */ }
    }
}
