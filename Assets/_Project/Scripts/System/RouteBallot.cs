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
    private double softDeadline;
    private int[] roster = Array.Empty<int>();
    private readonly LevelRouteSettings settings;
    public RouteBallot(LevelRouteSettings settings) { this.settings = settings; }
    public void Open(int[] players)
    {
        Votes.Clear(); roster = players; Winner = -1; Candidates = Array.Empty<int>();
        softDeadline = Deadline = 0; Phase = RouteVotePhase.Choosing;
    }
    public bool SetRoster(int[] players, double now)
    {
        if (roster.SequenceEqual(players)) return false;
        roster = players;
        foreach (int id in Votes.Keys.Where(id => !players.Contains(id)).ToArray()) Votes.Remove(id);
        // Reconsider a lock when its voters leave. Preserve the original soft deadline.
        if (Phase == RouteVotePhase.Locked || Phase == RouteVotePhase.SoftWait || Phase == RouteVotePhase.Choosing)
        { Phase = RouteVotePhase.Choosing; Winner = -1; Evaluate(now); }
        return true;
    }
    public bool Vote(int player, int node, double now)
    {
        if (!CanVote || !roster.Contains(player)) return false;
        if (node < 0) { if (!Votes.Remove(player)) return false; }
        else { if (Votes.TryGetValue(player, out int old) && old == node) return false; Votes[player] = node; }
        Evaluate(now); return true;
    }
    public bool CanVote => Phase == RouteVotePhase.Choosing || Phase == RouteVotePhase.SoftWait;
    private void Evaluate(double now)
    {
        if (Votes.Count == 0) { Phase = RouteVotePhase.Choosing; Deadline = softDeadline = 0; Candidates = Array.Empty<int>(); return; }
        var counts = Votes.Values.GroupBy(x => x).ToArray();
        int most = counts.Max(x => x.Count());
        Candidates = counts.Where(x => x.Count() == most).Select(x => x.Key).OrderBy(x => x).ToArray();
        if (softDeadline == 0) softDeadline = now + settings.softVoteSeconds;
        if (roster.Length == 1 || most > roster.Length / 2 || Votes.Count == roster.Length || now >= softDeadline)
        {
            Phase = RouteVotePhase.Locked;
            Winner = Candidates.Length == 1 ? Candidates[0] : -1;
            Deadline = now + (roster.Length == 1 ? settings.soloConfirmSeconds : settings.confirmedVoteSeconds);
        }
        else { Phase = RouteVotePhase.SoftWait; Deadline = softDeadline; }
    }
    public bool Tick(double now, Random random)
    {
        var before = Phase;
        if (Phase == RouteVotePhase.SoftWait && now >= Deadline) Evaluate(now);
        else if (Phase == RouteVotePhase.Locked && now >= Deadline)
        {
            if (Winner < 0)
            {
                Winner = Candidates[random.Next(Candidates.Length)];
                Phase = RouteVotePhase.TieReveal; Deadline = now + settings.tieRevealSeconds;
            }
            else Phase = RouteVotePhase.Encounter;
        }
        else if (Phase == RouteVotePhase.TieReveal && now >= Deadline) Phase = RouteVotePhase.Encounter;
        return before != Phase;
    }
}
