using System;
using System.Collections.Generic;
using System.Linq;

public enum RouteVotePhase { Dormant, Choosing, SoftWait, Locked, TieReveal, Encounter }

// Server-owned ballot. Presentation never selects a winner or advances a room.
public sealed class RouteBallot
{
    public RouteVotePhase Phase { get; private set; } = RouteVotePhase.Dormant;
    public readonly Dictionary<int, int> Votes = new Dictionary<int, int>();
    public int[] Candidates { get; private set; } = Array.Empty<int>();
    public int Winner { get; private set; } = -1;
    public double Deadline { get; private set; }
    private int[] roster = Array.Empty<int>();
    private readonly LevelRouteSettings settings;
    public RouteBallot(LevelRouteSettings settings) { this.settings = settings; }
    public void Open(int[] players)
    {
        Votes.Clear(); roster = players; Winner = -1; Candidates = Array.Empty<int>();
        Deadline = 0; Phase = RouteVotePhase.Choosing;
    }
    public void StartEncounter(int node)
    {
        Votes.Clear(); Candidates = new[] { node }; Winner = node;
        Deadline = 0; Phase = RouteVotePhase.Encounter;
    }
    public bool SetRoster(int[] players, double now)
    {
        if (roster.SequenceEqual(players)) return false;
        roster = players;
        foreach (int id in Votes.Keys.Where(id => !players.Contains(id)).ToArray()) Votes.Remove(id);
        // Reconsider agreement whenever a voter joins or leaves.
        if (Phase == RouteVotePhase.Locked || Phase == RouteVotePhase.SoftWait || Phase == RouteVotePhase.Choosing)
        { Phase = RouteVotePhase.Choosing; Winner = -1; Evaluate(now); }
        return true;
    }
    public bool Vote(int player, int node, double now)
    {
        if (!CanVote || !roster.Contains(player)) return false;
        if (node < 0) { if (!Votes.Remove(player)) return false; }
        else if (Votes.TryGetValue(player, out int old) && old == node) Votes.Remove(player);
        else Votes[player] = node;
        Evaluate(now); return true;
    }
    public bool CanVote => Phase == RouteVotePhase.Choosing || Phase == RouteVotePhase.SoftWait;
    private void Evaluate(double now)
    {
        Winner = -1;
        if (Votes.Count == 0) { Phase = RouteVotePhase.Choosing; Deadline = 0; Candidates = Array.Empty<int>(); return; }
        var counts = Votes.Values.GroupBy(x => x).ToArray();
        int most = counts.Max(x => x.Count());
        Candidates = counts.Where(x => x.Count() == most).Select(x => x.Key).OrderBy(x => x).ToArray();
        if (roster.Length > 0 && Votes.Count == roster.Length && most == roster.Length)
        {
            Phase = RouteVotePhase.Locked;
            Winner = Candidates.Length == 1 ? Candidates[0] : -1;
            Deadline = now + (roster.Length == 1 ? settings.soloConfirmSeconds : settings.confirmedVoteSeconds);
        }
        // Split votes remain editable until the whole active roster agrees.
        else { Phase = RouteVotePhase.SoftWait; Deadline = 0; }
    }
    public bool Tick(double now, Random random)
    {
        var before = Phase;
        if (Phase == RouteVotePhase.Locked && now >= Deadline) Phase = RouteVotePhase.Encounter;
        return before != Phase;
    }
}
