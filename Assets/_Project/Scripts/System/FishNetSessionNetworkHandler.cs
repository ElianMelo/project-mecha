using System;
using System.Threading.Tasks;
using FishNet.Managing;
using FishNet.Transporting.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;

// Unity Sessions allocates Relay and supplies transport data; FishNet owns gameplay.
public sealed class FishNetSessionNetworkHandler : INetworkHandler
{
    private readonly NetworkManager manager;
    private readonly LobbySettings settings;
    public FishNetSessionNetworkHandler(NetworkManager manager, LobbySettings settings)
    { this.manager = manager; this.settings = settings; }
    public async Task StartAsync(NetworkConfiguration configuration)
    {
        if (configuration.Type != NetworkType.Relay) throw new InvalidOperationException("This lobby requires Unity Relay.");
        var transport = manager.TransportManager.GetTransport<UnityTransport>();
        bool host = configuration.Role == NetworkRole.Host || configuration.Role == NetworkRole.Server;
        // Sessions 2.3 supplies the joined allocation in RelayServerData for FishNet too.
        // RelayClientData is a separate optional Netcode-for-Entities configuration.
        transport.SetRelayServerData(configuration.RelayServerData);
        try
        {
            if (host && !manager.ServerManager.StartConnection()) throw new InvalidOperationException("The Relay host could not start.");
            if (configuration.Role != NetworkRole.Server && !manager.ClientManager.StartConnection())
                throw new InvalidOperationException("The Relay client could not start.");
            float deadline = Time.realtimeSinceStartup + settings.connectionTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if ((!host || manager.ServerManager.Started) &&
                    (configuration.Role == NetworkRole.Server || manager.ClientManager.Started)) return;
                await Task.Delay(50);
            }
            throw new TimeoutException("Relay connection timed out. Check your connection and try again.");
        }
        catch { await StopAsync(); throw; }
    }
    public async Task StopAsync()
    {
        manager.ClientManager.StopConnection(); manager.ServerManager.StopConnection(true);
        float deadline = Time.realtimeSinceStartup + 5f;
        while ((manager.ClientManager.Started || manager.ServerManager.Started) && Time.realtimeSinceStartup < deadline)
            await Task.Delay(50);
    }
}
