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
    private InputAction localHost, localClient, lobbies;
    private string localAddress = "127.0.0.1";
    public static GameManager Instance { get; private set; }
    public bool MenuOpen => lobbyPanel != null && lobbyPanel.IsVisible;
    public NetworkManager Network => _networkManager;
    private void Awake()
    {
        Instance = this;
        localHost = new InputAction("Local host", InputActionType.Button, "<Keyboard>/1");
        localClient = new InputAction("Local client", InputActionType.Button, "<Keyboard>/2");
        lobbies = new InputAction("Online lobbies", InputActionType.Button, "<Keyboard>/3");
        localHost.performed += _ => { if (AcceptShortcut()) StartHost(); };
        localClient.performed += _ => { if (AcceptShortcut()) StartClient(); };
        lobbies.performed += _ => { if (AcceptShortcut()) lobbyPanel?.Show(); };
    }
    private bool AcceptShortcut()
    {
        var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
        return selected == null || selected.GetComponent<TMPro.TMP_InputField>() == null;
    }
    private void OnEnable() { localHost.Enable(); localClient.Enable(); lobbies.Enable(); }
    private async void Start()
    {
        // Optional launch parameters exercise the same public lobby flow as the menu.
        string[] args = System.Environment.GetCommandLineArgs();
        int join = System.Array.IndexOf(args, "--mecha-join-lobby");
        if (join >= 0 && join + 1 < args.Length)
        {
            var service = FindFirstObjectByType<MechaLobbyService>(); lobbyPanel?.Show();
            await service.JoinAsync(args[join + 1], "Remote Pilot", true);
            if (service.Session != null && System.Array.IndexOf(args, "--mecha-ready") >= 0) await service.ToggleReadyAsync();
        }
        else if (System.Array.IndexOf(args, "--mecha-local-client") >= 0) StartClient();
    }
    private void OnDisable() { localHost.Disable(); localClient.Disable(); lobbies.Disable(); }
    private void OnDestroy() { localHost.Dispose(); localClient.Dispose(); lobbies.Dispose(); if (Instance == this) Instance = null; }
    public void StartHost()
    {
        if (_networkManager.ServerManager.Started || _networkManager.ClientManager.Started) return;
        if (FindFirstObjectByType<MechaLobbyService>()?.Session != null) return;
        _networkManager.TransportManager.GetTransport<UnityTransport>().SetConnectionData("127.0.0.1", lobbySettings != null ? lobbySettings.localPort : (ushort)7777, "0.0.0.0");
        lobbyPanel?.Hide();
        if (_networkManager.ServerManager.StartConnection()) _networkManager.ClientManager.StartConnection();
    }
    public void StartServer() => _networkManager.ServerManager.StartConnection();
    public void StartClient()
    {
        if (_networkManager.ClientManager.Started || _networkManager.ServerManager.Started || FindFirstObjectByType<MechaLobbyService>()?.Session != null) return;
        _networkManager.TransportManager.GetTransport<UnityTransport>().SetConnectionData(localAddress, lobbySettings != null ? lobbySettings.localPort : (ushort)7777);
        lobbyPanel?.Hide(); _networkManager.ClientManager.StartConnection();
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
