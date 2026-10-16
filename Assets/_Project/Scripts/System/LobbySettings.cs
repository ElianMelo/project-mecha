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
    [Header("Lobby presentation")]
    public Color waitingColor = new Color(1f, 0.7f, 0.28f);
    public Color readyColor = new Color(0.3f, 0.94f, 0.68f);
    public Color emptyColor = new Color(0.3f, 0.4f, 0.5f);
    public Color cardColor = new Color(0.035f, 0.06f, 0.09f);
    public Color actionTextColor = new Color(0.015f, 0.04f, 0.055f);
    [Range(1f, 1.3f)] public float textScale = 1.25f;
    [Min(0.05f)] public float transitionSeconds = 0.22f;
    [Range(0f, 0.1f)] public float readyPunch = 0.035f;
}
