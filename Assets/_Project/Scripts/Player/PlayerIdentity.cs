using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// One server-owned identity supplies world labels and chat sender names.
public sealed class PlayerIdentity : NetworkBehaviour
{
    private readonly SyncVar<string> pilotName = new SyncVar<string>("Pilot");
    private readonly SyncVar<int> colorSlot = new SyncVar<int>();
    private bool named;
    public string PilotName => pilotName.Value;
    public int ColorSlot => colorSlot.Value;
    public bool LocallyControlled => !IsSpawned || IsOwner;
    public static string LocalName => Object.FindFirstObjectByType<MechaLobbyPanel>()?.PilotName ?? "Pilot";
    public override void OnStartServer()
    {
        base.OnStartServer();
        colorSlot.Value = Mathf.Max(0, OwnerId);
        pilotName.Value = "Pilot " + (colorSlot.Value + 1);
        named = false;
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner) RequestName(LocalName);
    }
    [ServerRpc]
    private void RequestName(string value)
    {
        if (named) return;
        pilotName.Value = ChatHistory.Clean(value, 24, "Pilot " + (colorSlot.Value + 1));
        named = true;
    }
}
