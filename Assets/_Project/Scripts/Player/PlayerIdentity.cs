using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// One server-owned identity supplies world labels and chat sender names.
public sealed class PlayerIdentity : NetworkBehaviour
{
    private readonly SyncVar<string> pilotName = new SyncVar<string>("");
    private readonly SyncVar<int> colorSlot = new SyncVar<int>();
    private readonly SyncVar<PvpTeam> team = new SyncVar<PvpTeam>();
    private bool combatRegistered;
    public PvpTeam Team => team.Value;
    public bool CombatRegistered => combatRegistered;
    private bool named;
    public string PilotName => string.IsNullOrEmpty(pilotName.Value) ? GameText.Get("Lobby", "default.pilot") : pilotName.Value;
    public int ColorSlot => colorSlot.Value;
    public bool LocallyControlled => !IsSpawned || IsOwner;
    public static string LocalName => Object.FindFirstObjectByType<MechaLobbyService>()?.PilotName ?? Object.FindFirstObjectByType<MechaLobbyPanel>()?.PilotName ?? GameText.Get("Lobby", "default.pilot");
    public override void OnStartServer()
    {
        base.OnStartServer();
        colorSlot.Value = Mathf.Max(0, OwnerId);
        pilotName.Value = GameText.Get("Lobby", "pilot.number", colorSlot.Value + 1);
        named = false;
        combatRegistered = false; team.Value = PvpTeam.None;
        if (PvpMatchDirector.Active && PvpMatchDirector.Instance.Phase != PvpMatchPhase.Waiting) Owner.Disconnect(false);
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
        {
            RequestName(LocalName);
            RequestCombatTeam(Object.FindFirstObjectByType<MechaLobbyService>()?.SelectedTeam ?? PvpTeam.Blue);
        }
    }
    [ServerRpc]
    private void RequestCombatTeam(PvpTeam requested)
    {
        if (combatRegistered) return;
        var match = PvpMatchDirector.Instance;
        if (match == null || match.Mode == MechaMatchMode.Cooperative) { combatRegistered = true; return; }
        if (match.Phase != PvpMatchPhase.Waiting || requested != PvpTeam.Blue && requested != PvpTeam.Red) return;
        team.Value = match.Mode == MechaMatchMode.Team ? requested : PvpTeam.None;
        combatRegistered = true;
    }
    [ServerRpc]
    private void RequestName(string value)
    {
        if (named) return;
        pilotName.Value = ChatHistory.Clean(value, 24, GameText.Get("Lobby", "pilot.number", colorSlot.Value + 1));
        named = true;
    }
}
