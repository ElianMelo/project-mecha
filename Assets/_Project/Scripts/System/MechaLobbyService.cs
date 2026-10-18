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
    public bool Browsing { get; private set; }
    public bool InGame => network != null && network.ClientManager.Started;
    public string Status { get; private set; } = "";
    public string Search { get; set; } = "";
    public bool IsReady => Session != null && Read(Session.CurrentPlayer, "ready") == "1";
    public bool Starting => starting || Session != null && Session.Properties.TryGetValue("phase", out var phase) && phase.Value == "starting" && !InGame;
    private bool querying, refreshing, starting, destroyed, leaving;
    private float nextQuery, nextRoster;
    private int queryFailures;
    private Task initializeTask;
    private FishNetSessionNetworkHandler handler;
    public LobbySettings Settings => settings;
    private void Awake() { handler = new FishNetSessionNetworkHandler(network, settings); Status = GameText.Get("Lobby", "browser.welcome"); }
    public void OpenBrowser()
    {
        Browsing = Session == null && !InGame; nextQuery = 0f;
        Changed?.Invoke();
    }
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
        try
        {
            await InitializeAsync();
            var filters = new List<FilterOption> {
                new FilterOption(FilterField.StringIndex1, settings.sessionType, FilterOperation.Equal),
                new FilterOption(FilterField.IsLocked, "false", FilterOperation.Equal),
                new FilterOption(FilterField.AvailableSlots, "0", FilterOperation.Greater)};
            if (!string.IsNullOrWhiteSpace(Search)) filters.Add(new FilterOption(FilterField.Name, Search.Trim(), FilterOperation.Contains));
            var result = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions {Count = 100, FilterOptions = filters});
            queryFailures = 0;
            if (!destroyed && Session == null)
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
        await InitializeAsync();
        if (Session != null || InGame) throw new InvalidOperationException(GameText.Get("Lobby", "session.leaveFirst"));
        var options = new SessionOptions {Name = Clean(lobbyName, GameText.Get(settings.defaultLobbyNameLocalized)), MaxPlayers = settings.maxPlayers,
            Type = settings.sessionType, PlayerProperties = PlayerData(playerName),
            SessionProperties = new Dictionary<string, SessionProperty> {
                {"game", new SessionProperty(settings.sessionType, VisibilityPropertyOptions.Public, PropertyIndex.String1)},
                {"phase", new SessionProperty("waiting")}}};
        options.WithNetworkHandler(handler).WithNetworkOptions(new NetworkOptions {RelayProtocol = RelayProtocol.DTLS});
        Attach(await MultiplayerService.Instance.CreateSessionAsync(options));
    });
    public Task JoinAsync(string id, string playerName, bool byCode = false) => Operation(async () =>
    {
        await InitializeAsync();
        if (Session != null || InGame) throw new InvalidOperationException(GameText.Get("Lobby", "session.leaveFirst"));
        var options = new JoinSessionOptions {Type = settings.sessionType, PlayerProperties = PlayerData(playerName)};
        options.WithNetworkHandler(handler).WithNetworkOptions(new NetworkOptions {RelayProtocol = RelayProtocol.DTLS});
        Attach(byCode ? await MultiplayerService.Instance.JoinSessionByCodeAsync(id.Trim().ToUpperInvariant(), options)
            : await MultiplayerService.Instance.JoinSessionByIdAsync(id, options));
    });
    public Task ToggleReadyAsync() => Operation(async () =>
    {
        if (Session == null || Session.IsLocked || Starting || InGame) return;
        Session.CurrentPlayer.SetProperty("ready", new PlayerProperty(IsReady ? "0" : "1"));
        await Session.SaveCurrentPlayerDataAsync();
        Status = GameText.Get("Lobby", "ready.waiting");
    });
    private void Attach(ISession session)
    {
        Session = session; Browsing = false; nextRoster = Time.unscaledTime + settings.rosterRefreshSeconds;
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
    private void OnSessionChanged() { Changed?.Invoke(); if (!Busy) _ = CheckReadyAsync(); }
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
            session.Players.Any(player => Read(player, "ready") != "1")) return;
        starting = true; Status = GameText.Get("Lobby", "ready.connecting"); Changed?.Invoke();
        try
        {
            var host = session.AsHost(); host.IsLocked = true;
            host.SetProperty("phase", new SessionProperty("starting")); await host.SavePropertiesAsync();
            await Task.Delay(1100);
            await session.RefreshAsync();
            if (session.Players.Any(player => Read(player, "ready") != "1"))
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
    private async Task Operation(Func<Task> operation)
    {
        if (Busy || destroyed) return;
        Busy = true; Changed?.Invoke();
        try { await operation(); }
        catch (Exception e) { Status = Friendly(e); Debug.LogWarning("Lobby: " + e.Message); }
        finally { Busy = false; if (!destroyed) { Changed?.Invoke(); _ = CheckReadyAsync(); } }
    }
    private Dictionary<string, PlayerProperty> PlayerData(string playerName) => new Dictionary<string, PlayerProperty> {
        {"name", new PlayerProperty(Clean(playerName, GameText.Get(settings.defaultPlayerNameLocalized)))}, {"ready", new PlayerProperty("0")}};
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
