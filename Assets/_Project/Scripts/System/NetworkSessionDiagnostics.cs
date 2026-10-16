using System;
using System.Linq;
using UnityEngine;

// Read-only development logging for manual multi-process inspection. Opens no endpoint.
public sealed class NetworkSessionDiagnostics : MonoBehaviour
{
    private float nextReport;
    private bool reporting;
    private void Awake()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        reporting = Environment.GetCommandLineArgs().Contains("--mecha-network-log");
#endif
    }
    private void Update()
    {
        if (!reporting || Time.unscaledTime < nextReport) return;
        nextReport = Time.unscaledTime + 2f;
        var lobby = GetComponent<MechaLobbyService>();
        var level = ProceduralLevelDirector.Instance;
        var players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        Debug.Log("MECHA_NETWORK " + JsonUtility.ToJson(new Snapshot {
            lobby = lobby?.Session?.Name ?? "", lobbyPlayers = lobby?.Session?.PlayerCount ?? 0,
            status = lobby?.Status ?? "", room = level?.LevelNumber ?? 0, seed = level?.Seed ?? 0,
            remaining = level?.Remaining ?? 0, clear = level != null && level.IsClear,
            platforms = OneWayPlatform.ActivePlatforms.Count, projectiles = FindObjectsByType<EnemyLanceProjectile>(FindObjectsSortMode.None).Length,
            players = players.Select(player => $"owner={player.OwnerId} local={player.IsOwner} hp={player.GetComponent<PlayerHealth>().Health:0} grounded={player.IsGrounded} pos={player.transform.position} action={player.GetComponent<PlayerAttack>().CurrentAction}").ToArray()}));
    }
    [Serializable] private class Snapshot
    {
        public string lobby, status;
        public int lobbyPlayers, room, seed, remaining, platforms, projectiles;
        public bool clear;
        public string[] players;
    }
}
