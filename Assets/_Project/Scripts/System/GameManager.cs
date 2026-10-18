using FishNet.Managing;
using FishNet.Transporting.UTP;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public enum GameState { Paused, Moving, Drawing }
    public GameState currentGameState = GameState.Moving;
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private LobbySettings lobbySettings;
    [SerializeField] private MechaLobbyPanel lobbyPanel;
    [SerializeField] private ControllerInterfaceSettings controllerSettings;
    [SerializeField] private ControlBindingSettings controlBindings;
    private InputActionAsset interfaceActions;
    private InputActionMap systemActions;
    private GameplayDiagnostics diagnostics;
    private ControllerTextEntry textEntry;
    private MechaGameplayHud hud;
    private AudioConfigurationMenu audioMenu;
    private GameOptionsMenu optionsMenu;
    private float resumeInputAt;
    private bool quickHostOpening;
    private int lastBackFrame = -1;
    private string localAddress = "127.0.0.1";
    public static GameManager Instance { get; private set; }
    public bool MenuOpen => CharacterSelectionMenu.Instance != null && CharacterSelectionMenu.Instance.IsVisible || OptionsMenuOpen || audioMenu != null && audioMenu.IsVisible || lobbyPanel != null && lobbyPanel.IsVisible || diagnostics != null && diagnostics.Visible || textEntry != null && textEntry.IsVisible;
    public bool GameplayInputBlocked => MenuOpen || LevelRouteDirector.Instance != null && LevelRouteDirector.Instance.IsRouting || MechaChat.IsTyping || Time.unscaledTime < resumeInputAt;
    public ControllerInterfaceSettings ControllerSettings => controllerSettings;
    public NetworkManager Network => _networkManager;
    public bool LobbyOpen => optionsMenu != null ? optionsMenu.LobbyVisible : lobbyPanel != null && lobbyPanel.IsVisible;
    public bool AudioMenuOpen => audioMenu != null && audioMenu.IsVisible;
    public bool OptionsMenuOpen => optionsMenu != null && optionsMenu.IsVisible;
    private void Awake()
    {
        Instance = this;
        diagnostics = FindFirstObjectByType<GameplayDiagnostics>();
        textEntry = FindFirstObjectByType<ControllerTextEntry>();
        hud = FindFirstObjectByType<MechaGameplayHud>();
        audioMenu = FindFirstObjectByType<AudioConfigurationMenu>();
        optionsMenu = FindFirstObjectByType<GameOptionsMenu>();
        GameControlBindings.Initialize(controlBindings);
        interfaceActions = Instantiate(controllerSettings.actions);
        GameControlBindings.Register(interfaceActions);
        var uiModule = UnityEngine.EventSystems.EventSystem.current?.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (uiModule != null) uiModule.actionsAsset = interfaceActions;
        systemActions = interfaceActions.FindActionMap("System", true);
        systemActions.FindAction("LocalHost").performed += _ => { if (AcceptShortcut()) StartQuickHost(); };
        systemActions.FindAction("LocalJoin").performed += _ => { if (AcceptShortcut()) StartClient(); };
        systemActions.FindAction("Menu").performed += context => {
            if (GameControlBindings.Capturing) return;
            if ((MenuOpen || context.control.device is Gamepad || AcceptShortcut()) && lastBackFrame != Time.frameCount)
            { lastBackFrame = Time.frameCount; ToggleMenu(); }
        };
        systemActions.FindAction("Back").performed += _ => Back();
        systemActions.FindAction("Diagnostics").performed += _ => {
            if (!OptionsMenuOpen && AcceptShortcut() && (lobbyPanel == null || !lobbyPanel.IsVisible)) diagnostics?.Toggle();
        };
        systemActions.FindAction("ControlsHelp").performed += _ => { if (AcceptShortcut()) hud?.ToggleControls(); };
        systemActions.FindAction("ChatVisibility").performed += _ => { if (AcceptShortcut()) MechaChat.Instance?.ToggleVisibility(); };
        systemActions.FindAction("Chat").performed += _ => { if (AcceptShortcut() && !OptionsMenuOpen) MechaChat.Instance?.Open(); };
    }
    private bool AcceptShortcut()
    {
        if (CharacterSelectionMenu.Instance != null && CharacterSelectionMenu.Instance.IsVisible) return false;
        if (GameControlBindings.Capturing) return false;
        var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
        return selected == null || selected.GetComponent<TMPro.TMP_InputField>() == null;
    }
    private void OnEnable() => systemActions.Enable();
    public void SuppressGameplayInput() => resumeInputAt = Time.unscaledTime + controllerSettings.menuResumeDelay;
    private void ToggleMenu()
    {
        if (CharacterSelectionMenu.Instance != null && CharacterSelectionMenu.Instance.IsVisible) { CharacterSelectionMenu.Instance.Cancel(); return; }
        if (MechaChat.IsTyping) return;
        if (optionsMenu != null)
        {
            if (textEntry != null && textEntry.IsVisible) textEntry.Cancel();
            else if (optionsMenu.IsVisible) optionsMenu.Back(); else optionsMenu.Open();
            return;
        }
        if (audioMenu != null)
        {
            if (audioMenu.IsVisible) audioMenu.Hide();
            else if (textEntry != null && textEntry.IsVisible) textEntry.Cancel();
            else audioMenu.Show();
        }
        else if (MenuOpen) Back(); else lobbyPanel?.Show();
    }
    public void ShowLobby() => optionsMenu.OpenLobby();
    private void Back()
    {
        if (GameControlBindings.Capturing) return;
        if (lastBackFrame == Time.frameCount) return;
        lastBackFrame = Time.frameCount;
        if (CharacterSelectionMenu.Instance != null && CharacterSelectionMenu.Instance.IsVisible) { CharacterSelectionMenu.Instance.Cancel(); return; }
        if (MechaChat.IsTyping) { MechaChat.Instance.Close(); return; }
        if (textEntry != null && textEntry.IsVisible) textEntry.Cancel();
        else if (optionsMenu != null && optionsMenu.IsVisible) optionsMenu.Back();
        else if (audioMenu != null && audioMenu.IsVisible) audioMenu.Hide();
        else if (diagnostics != null && diagnostics.Visible) diagnostics.Toggle();
        else if (lobbyPanel != null && lobbyPanel.IsVisible) optionsMenu.ShowHome();
        else if (LevelRouteDirector.Instance != null && LevelRouteDirector.Instance.IsRouting) LevelRouteDirector.Instance.Withdraw();
    }
    private async void Start()
    {
        // Optional launch parameters exercise the same public lobby flow as the menu.
        string[] args = System.Environment.GetCommandLineArgs();
        int pvp = System.Array.IndexOf(args, "--mecha-pvp");
        if (pvp >= 0)
        {
            var service = FindFirstObjectByType<MechaLobbyService>();
            int team = System.Array.IndexOf(args, "--mecha-team");
            service.PrepareLocal(pvp + 1 < args.Length && args[pvp + 1] == "team" ? MechaMatchMode.Team : MechaMatchMode.FreeForAll,
                team + 1 < args.Length && team >= 0 && args[team + 1] == "red" ? PvpTeam.Red : PvpTeam.Blue);
        }
        int join = System.Array.IndexOf(args, "--mecha-join-lobby");
        if (join >= 0 && join + 1 < args.Length)
        {
            var service = FindFirstObjectByType<MechaLobbyService>(); ShowLobby();
            CharacterSelectionMenu.Instance.ConfirmLaunchArgument();
            if (!await CharacterSelectionMenu.Instance.EnsureConfirmedAsync()) return;
            await service.JoinAsync(args[join + 1], "Remote Pilot", true);
            if (service.Session != null && System.Array.IndexOf(args, "--mecha-ready") >= 0) await service.ToggleReadyAsync();
        }
        else if (System.Array.IndexOf(args, "--mecha-local-client") >= 0) { StartClient(); CharacterSelectionMenu.Instance.ConfirmLaunchArgument(); }
    }
    private void OnDisable() => systemActions?.Disable();
    private void OnDestroy() { GameControlBindings.Unregister(interfaceActions); if (interfaceActions != null) Destroy(interfaceActions); if (Instance == this) Instance = null; }
    public void StartHost() => StartHost(false);
    public void StartQuickHost() { FindFirstObjectByType<MechaLobbyService>()?.PrepareLocal(MechaMatchMode.Cooperative); StartHost(true); }
    public bool ConsumeQuickHostOpening()
    {
        bool requested = quickHostOpening; quickHostOpening = false; return requested;
    }
    private void StartHost(bool skipOpeningChoice)
    {
        if (MenuOpen && CharacterSelectionMenu.Instance != null && CharacterSelectionMenu.Instance.IsVisible) return;
        if (_networkManager.ServerManager.Started || _networkManager.ClientManager.Started || FindFirstObjectByType<MechaLobbyService>()?.Session != null) return;
        optionsMenu.Close();
        CharacterSelectionMenu.Instance.Open(() => StartHostSelected(skipOpeningChoice));
    }
    private void StartHostSelected(bool skipOpeningChoice)
    {
        if (_networkManager.ServerManager.Started || _networkManager.ClientManager.Started) return;
        if (FindFirstObjectByType<MechaLobbyService>()?.Session != null) return;
        _networkManager.TransportManager.GetTransport<UnityTransport>().SetConnectionData("127.0.0.1", lobbySettings != null ? lobbySettings.localPort : (ushort)7777, "0.0.0.0");
        quickHostOpening = skipOpeningChoice;
        optionsMenu.Close();
        if (_networkManager.ServerManager.StartConnection()) _networkManager.ClientManager.StartConnection();
        else quickHostOpening = false;
    }
    public void StartServer() => _networkManager.ServerManager.StartConnection();
    public void StartClient()
    {
        if (_networkManager.ClientManager.Started || _networkManager.ServerManager.Started || FindFirstObjectByType<MechaLobbyService>()?.Session != null) return;
        optionsMenu.Close();
        CharacterSelectionMenu.Instance.Open(StartClientSelected);
    }
    private void StartClientSelected()
    {
        if (_networkManager.ClientManager.Started || _networkManager.ServerManager.Started || FindFirstObjectByType<MechaLobbyService>()?.Session != null) return;
        _networkManager.TransportManager.GetTransport<UnityTransport>().SetConnectionData(localAddress, lobbySettings != null ? lobbySettings.localPort : (ushort)7777);
        optionsMenu.Close(); _networkManager.ClientManager.StartConnection();
    }
    public void SetIPAddress(string text) { localAddress = text; _networkManager.TransportManager.Transport.SetClientAddress(text); }
    // Kept for the existing manual Relay-code UI, using the same authentication service.
    public async Task<string> StartHostWithRelay(int maxConnections, string connectionType)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        _networkManager.TransportManager.GetTransport<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        if (!_networkManager.ServerManager.StartConnection()) return null;
        _networkManager.ClientManager.StartConnection(); return code;
    }
    public async Task<bool> StartClientWithRelay(string joinCode, string connectionType)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());
        _networkManager.TransportManager.GetTransport<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        return _networkManager.ClientManager.StartConnection();
    }
}
