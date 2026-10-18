using UnityEngine;

public enum MechaMatchMode { Cooperative, FreeForAll, Team }
public enum PvpTeam { None, Blue, Red }
public enum PvpMatchPhase { Waiting, Running, Finished }

[CreateAssetMenu(menuName = "Mecha/Network/PVP Match")]
public sealed class PvpMatchSettings : ScriptableObject
{
    [Min(1f)] public float durationSeconds = 300f;
    [Min(1f)] public float respawnSeconds = 5f;
    [Range(0.01f, 1f)] public float assistHealthFraction = 0.2f;
    [Min(0.1f)] public float snapshotInterval = 0.25f;
    [Min(5f)] public float rosterTimeout = 45f;
    [Min(0.1f)] public float spawnInset = 2f;
    [Min(0.1f)] public float positionSampleSeconds = 0.2f;
    [Min(0.1f)] public float positionSlack = 2f;
    [Min(1f)] public float maximumTravelPerSecond = 45f;
    [Min(1f)] public float positionPenaltySeconds = 1f;
    public Color blue = new Color(0.22f, 0.62f, 1f);
    public Color red = new Color(1f, 0.3f, 0.28f);
}
