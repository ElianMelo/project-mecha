using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Network/Lobby")]
public sealed class LobbySettings : ScriptableObject
{
    [Range(1, 8)] public int maxPlayers = 4;
    public string sessionType = "project-mecha-v1", authenticationProfile = "mecha";
    public string defaultLobbyName = "Mecha Expedition", defaultPlayerName = "Pilot";
    [Min(1f)] public float browserRefreshSeconds = 1f, rosterRefreshSeconds = 3f;
    [Min(5f)] public float connectionTimeoutSeconds = 25f;
    public ushort localPort = 7777;
}
