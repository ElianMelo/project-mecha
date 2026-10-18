using System;
using System.Linq;
using TMPro;
using UnityEngine;

// A persistent, read-only table below chat; native menu navigation keeps its own focus.
public sealed class PvpScoreboard : MonoBehaviour
{
    [Serializable] public struct Row { public GameObject root; public TMP_Text rank, pilot, kills, deaths, assists; }
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text clock, summary, respawn;
    [SerializeField] private Row[] rows;
    private static PvpScoreboard instance;
    public static float ReservedHeight => instance != null && PvpMatchDirector.Active ? ((RectTransform)instance.panel.transform).rect.height + 8f : 0f;
    private void Awake() => instance = this;
    private void Update()
    {
        bool visible = PvpMatchDirector.Active;
        panel.SetActive(visible);
        if (!visible) return;
        var match = PvpMatchDirector.Instance;
        int seconds = Mathf.CeilToInt(match.SecondsRemaining);
        clock.text = GameText.Get("Lobby", match.Mode == MechaMatchMode.Team ? "pvp.teamClock" : "pvp.freeClock", $"{seconds / 60:0}:{seconds % 60:00}");
        if (match.Phase == PvpMatchPhase.Waiting) summary.text = GameText.Get("Lobby", "pvp.waiting");
        else if (match.Cancelled) summary.text = GameText.Get("Lobby", "pvp.cancelled");
        else if (match.Mode == MechaMatchMode.Team)
        {
            int blue = match.Scores.Where(x => x.team == PvpTeam.Blue).Sum(x => x.kills);
            int red = match.Scores.Where(x => x.team == PvpTeam.Red).Sum(x => x.kills);
            summary.text = match.Phase == PvpMatchPhase.Finished
                ? blue == red ? GameText.Get("Lobby", "pvp.draw") : GameText.Get("Lobby", "pvp.winner", GameText.Get("Lobby", blue > red ? "team.blue" : "team.red"))
                : GameText.Get("Lobby", "pvp.teamScore", blue, red);
        }
        else if (match.Phase == PvpMatchPhase.Finished && match.Scores.Length > 0)
        {
            var leader = match.Scores[0];
            bool tied = match.Scores.Skip(1).Any(x => x.kills == leader.kills && x.deaths == leader.deaths && x.assists == leader.assists);
            summary.text = tied ? GameText.Get("Lobby", "pvp.draw") : GameText.Get("Lobby", "pvp.winner", leader.name);
        }
        else summary.text = GameText.Get("Lobby", "pvp.ranking");
        var local = PlayerAttack.ActiveActors.FirstOrDefault(x => x != null && x.IsSpawned && x.IsOwner)?.GetComponent<PlayerHealth>();
        respawn.text = match.Phase == PvpMatchPhase.Finished ? GameText.Get("Lobby", "pvp.finished") :
            local != null && local.IsDead ? GameText.Get("Lobby", "pvp.respawn", Mathf.CeilToInt(local.RespawnRemaining)) : "";
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].root.SetActive(i < match.Scores.Length);
            if (i >= match.Scores.Length) continue;
            var entry = match.Scores[i]; var row = rows[i];
            row.rank.text = (i + 1).ToString();
            row.pilot.text = entry.connected ? entry.name : GameText.Get("Lobby", "pvp.disconnected", entry.name);
            row.pilot.color = match.Mode == MechaMatchMode.Team ? entry.team == PvpTeam.Blue ? match.Settings.blue : match.Settings.red : Color.white;
            row.kills.text = entry.kills.ToString(); row.deaths.text = entry.deaths.ToString(); row.assists.text = entry.assists.ToString();
        }
    }
    private void OnDestroy() { if (instance == this) instance = null; }
}
