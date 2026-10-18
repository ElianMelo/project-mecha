using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// Operator-driven local inspection. No assertions, scenarios, or automated test runners.
public static class PvpSessionInspection
{
    public static object Snapshot()
    {
        var match = PvpMatchDirector.Instance;
        return new {frame = Time.frameCount, time = Time.time, active = PvpMatchDirector.Active,
            mode = match.Mode.ToString(), phase = match.Phase.ToString(), match.SecondsRemaining, match.Cancelled,
            match.Scores, routing = LevelRouteDirector.Instance?.IsRouting,
            players = PlayerAttack.ActiveActors.Where(x => x != null && x.IsSpawned).OrderBy(x => x.OwnerId).Select(x => new {
                x.OwnerId, x.ObjectId, x.IsOwner, health = x.GetComponent<PlayerHealth>().Health,
                respawn = x.GetComponent<PlayerHealth>().RespawnRemaining, position = x.transform.position.ToString("F3"),
                name = x.GetComponent<PlayerIdentity>().PilotName, team = x.GetComponent<PlayerIdentity>().Team.ToString(),
                registered = x.GetComponent<PlayerIdentity>().CombatRegistered,
                aim = x.GetComponent<MechaAutoAim>().Target?.TargetTransform.name,
                bars = x.GetComponentsInChildren<ActorWorldHud>(true).Select(h => h.GetComponentsInChildren<UnityEngine.UI.Image>().Count(i => i.name == "Health Fill")).ToArray()
            }).ToArray(), enemies = EnemyController.ActiveEnemies.Count(x => x != null && x.IsSpawned) };
    }
    public static object Host(string mode = "free", string team = "blue")
    {
        var lobby = UnityEngine.Object.FindFirstObjectByType<MechaLobbyService>();
        GameOptionsMenu.Instance.Close();
        lobby.PrepareLocal(mode == "team" ? MechaMatchMode.Team : MechaMatchMode.FreeForAll, team == "red" ? PvpTeam.Red : PvpTeam.Blue);
        lobby.PilotName = "Host";
        GameManager.Instance.StartHost(); CharacterSelectionMenu.Instance.Confirm();
        return new {frame = Time.frameCount, requested = mode};
    }
    public static object Peer(string team = "red", string name = "Peer")
    {
        string folder = Path.GetFullPath("Logs/PvpLobby"); Directory.CreateDirectory(folder);
        var start = new ProcessStartInfo(Path.GetFullPath("Builds/PvpLobby/Mecha.exe")) {
            Arguments = "-batchmode -nographics -logFile \"" + Path.Combine(folder, name + ".log") + "\" --mecha-local-client --mecha-character dasher --mecha-network-log --mecha-pvp team --mecha-team " + team,
            WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        var process = Process.Start(start);
        return new {processId = process.Id, team, log = Path.Combine(folder, name + ".log")};
    }
    public static object Place(int ownerId, float x)
    {
        var player = PlayerAttack.ActiveActors.First(p => p.IsSpawned && p.OwnerId == ownerId).GetComponent<PlayerHealth>();
        player.CorrectPvpPosition(new Vector3(x, ProceduralLevelDirector.Instance.Settings.floorHeight, 0f));
        PvpMatchDirector.Instance.ResetLife(player);
        return new {ownerId, position = player.transform.position.ToString("F3")};
    }
    public static async Task<object> CreateLobby(string title)
    {
        await UnityEngine.Object.FindFirstObjectByType<MechaLobbyService>().CreateAsync(title, "Host");
        return LobbySnapshot();
    }
    public static object LobbySnapshot()
    {
        var lobby = UnityEngine.Object.FindFirstObjectByType<MechaLobbyService>();
        return new {lobby.Busy,lobby.Status,lobby.IsReady,mode=lobby.SelectedMode.ToString(),team=lobby.SelectedTeam.ToString(),
            exists=lobby.Session!=null,players=lobby.Session?.PlayerCount,locked=lobby.Session?.IsLocked,
            properties=lobby.Session?.Properties.ToDictionary(x=>x.Key,x=>x.Value.Value)};
    }
    public static async Task<object> LobbyAction(string action)
    {
        var lobby=UnityEngine.Object.FindFirstObjectByType<MechaLobbyService>();
        switch(action)
        {
            case "ready": await lobby.ToggleReadyAsync(); break;
            case "team": await lobby.SelectModeAsync(MechaMatchMode.Team); break;
            case "free": await lobby.SelectModeAsync(MechaMatchMode.FreeForAll); break;
            case "blue": await lobby.SelectTeamAsync(PvpTeam.Blue); break;
            case "red": await lobby.SelectTeamAsync(PvpTeam.Red); break;
            case "leave": await lobby.LeaveAsync(); break;
            default: throw new ArgumentException("Unknown lobby action.");
        }
        return LobbySnapshot();
    }
}
