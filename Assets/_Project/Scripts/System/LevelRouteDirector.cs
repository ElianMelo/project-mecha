using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

[Serializable]
public struct RoutePilotVote
{
    public int playerId, colorSlot, nodeId;
    public string name;
}

[RequireComponent(typeof(ProceduralLevelDirector))]
public sealed class LevelRouteDirector : NetworkBehaviour
{
    [SerializeField] private LevelRouteSettings settings;
    public static LevelRouteDirector Instance { get; private set; }
    public LevelRouteSettings Settings => settings;
    public LevelRouteNode[] Nodes { get; private set; } = Array.Empty<LevelRouteNode>();
    public RoutePilotVote[] Pilots { get; private set; } = Array.Empty<RoutePilotVote>();
    public int[] Visited { get; private set; } = Array.Empty<int>();
    public int[] Candidates { get; private set; } = Array.Empty<int>();
    public int[] Planned { get; private set; } = Array.Empty<int>();
    public int PlanningTarget { get; private set; }
    public RouteVotePhase Phase { get; private set; }
    public int CurrentNode { get; private set; } = -1;
    public int Winner { get; private set; } = -1;
    public int Round { get; private set; }
    public int Sector { get; private set; }
    public bool IsRouting => Phase >= RouteVotePhase.Choosing && Phase <= RouteVotePhase.TieReveal;
    public bool CanVote => Phase == RouteVotePhase.Choosing || Phase == RouteVotePhase.SoftWait;
    public int ChoiceOrigin => IsRouting && Planned.Length > 0 ? Planned[Planned.Length - 1] : CurrentNode;
    public float SecondsRemaining => Mathf.Max(0f, secondsAtSnapshot - (Time.unscaledTime - snapshotAt));
    public event Action Changed;
    private readonly Dictionary<string, RouteEncounterDefinition> catalogue = new Dictionary<string, RouteEncounterDefinition>(StringComparer.Ordinal);
    private readonly Dictionary<int, double> nextVote = new Dictionary<int, double>();
    private readonly List<int> visited = new List<int>();
    private readonly List<int> planned = new List<int>();
    private RouteBallot ballot;
    private ProceduralLevelDirector rooms;
    private System.Random random;
    private int generation, current = -1, sector, planCursor, planningTarget;
    private float secondsAtSnapshot, snapshotAt;
    private double nextSnapshot;
    private double Clock => Time.realtimeSinceStartupAsDouble;
    private void Awake()
    {
        Instance = this; rooms = GetComponent<ProceduralLevelDirector>();
        foreach (var definition in Resources.LoadAll<RouteEncounterDefinition>("RouteEncounters")) Register(definition);
        if (settings != null && settings.openingEncounter != null) Register(settings.openingEncounter, true);
    }
    private void Register(RouteEncounterDefinition room, bool allowSame = false)
    {
        if (room == null || string.IsNullOrWhiteSpace(room.encounterId) || room.roomSettings == null)
        { Debug.LogError("Route encounter needs a unique ID and room settings.", room); return; }
        if (room.encounterPrefab != null && room.encounterPrefab.GetComponent<RouteEncounter>() == null)
        { Debug.LogError("Custom encounter prefab needs a RouteEncounter component.", room); return; }
        if (catalogue.TryGetValue(room.encounterId, out var existing))
        { if (!allowSame || existing != room) Debug.LogError("Duplicate route encounter ID: " + room.encounterId, room); return; }
        catalogue.Add(room.encounterId, room);
    }
    public RouteEncounterDefinition Encounter(string id) => id != null && catalogue.TryGetValue(id, out var room) ? room : null;
    public bool IsAvailable(int id)
    {
        if (id < 0 || id >= Nodes.Length) return false;
        return ChoiceOrigin < 0 ? id == 0 : Nodes[ChoiceOrigin].next.Contains(id);
    }
    public bool IsRevealed(int id)
    {
        if (id < 0 || id >= Nodes.Length) return false;
        if (Visited.Contains(id) || Planned.Contains(id) || id == CurrentNode) return true;
        int ahead = Mathf.Max(1, settings.revealedStepsAhead) - 1;
        if (ChoiceOrigin < 0) return IsWithinReveal(0, id, ahead);
        if (ChoiceOrigin >= Nodes.Length) return false;
        return Nodes[ChoiceOrigin].next.Any(choice => IsWithinReveal(choice, id, ahead));
    }
    private bool IsWithinReveal(int from, int id, int ahead)
    {
        if (from < 0 || from >= Nodes.Length || Nodes[from].depth > Nodes[id].depth) return false;
        if (from == id) return true;
        return ahead > 0 && Nodes[from].next.Any(next => IsWithinReveal(next, id, ahead - 1));
    }
    public void BeginRun()
    {
        if (!IsServerInitialized || ballot != null || settings == null) return;
        random = new System.Random(unchecked(Environment.TickCount * 397 ^ GetInstanceID()));
        ballot = new RouteBallot(settings); sector = 0; visited.Clear();
        NewSector(true);
        BeginPlanning();
    }
    private void NewSector(bool opening)
    {
        sector++; current = -1; visited.Clear(); planned.Clear(); planCursor = 0;
        var graph = LevelRouteGraph.Generate(settings, catalogue.Values.OrderBy(x => x.encounterId, StringComparer.Ordinal).ToArray(), random.Next(), opening);
        PresentGraph(graph, sector);
    }
    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void PresentGraph(LevelRouteNode[] nodes, int sectorNumber)
    {
        Nodes = nodes; Sector = sectorNumber;
        CurrentNode = Winner = -1; Visited = Planned = Candidates = Array.Empty<int>();
        Changed?.Invoke();
    }
    private PlayerHealth[] Roster() => FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None)
        .Where(x => x.IsSpawned && x.Owner != null && x.Owner.IsActive).OrderBy(x => x.OwnerId).GroupBy(x => x.OwnerId).Select(x => x.First()).ToArray();
    private void OpenChoice()
    {
        generation++; nextVote.Clear();
        ballot.Open(Roster().Select(x => x.OwnerId).ToArray());
        Publish();
    }
    private void BeginPlanning()
    {
        planned.Clear(); planCursor = 0;
        int depths = Nodes.Max(node => node.depth) + 1;
        int completed = current < 0 ? 0 : Nodes[current].depth + 1;
        planningTarget = Mathf.Min(Mathf.Max(1, settings.pathsPerBatch), depths - completed);
        OpenChoice();
    }
    public bool OpenAfterClear()
    {
        if (!IsServerInitialized || ballot == null || ballot.Phase != RouteVotePhase.Encounter || !rooms.IsClear) return false;
        if (planCursor < planned.Count) EnterEncounter();
        else
        {
            if (Nodes[current].next.Length == 0) NewSector(false);
            BeginPlanning();
        }
        return true;
    }
    public void Select(int node)
    {
        if (!IsClientInitialized || !CanVote || !IsAvailable(node)) return;
        RequestVote(Round, node);
    }
    public void Withdraw()
    {
        if (IsClientInitialized && CanVote) RequestVote(Round, -1);
    }
    public void RandomRoute()
    {
        if (IsClientInitialized && CanVote) RequestRandomRoute(Round);
    }
    [ServerRpc(RequireOwnership = false)]
    private void RequestRandomRoute(int round, NetworkConnection sender = null)
    {
        if(sender == null || ballot == null || !ballot.CanVote || round != generation) return;
        var roster = Roster();
        if(!roster.Any(p=>p.OwnerId==sender.ClientId)) return;
        double now=Clock;
        if(nextVote.TryGetValue(sender.ClientId,out double next) && now<next) return;
        if(roster.Length==1)
        {
            // Complete the current connected batch, then deploy immediately without confirmation timers.
            while(planned.Count<planningTarget)
            {
                var choices=Nodes.Where(n=>ServerAvailable(n.id)).ToArray();
                if(choices.Length==0)break;
                planned.Add(choices[random.Next(choices.Length)].id);
            }
            if(planCursor<planned.Count){generation++;EnterEncounter();}
        }
        else
        {
            // Multiplayer randomizes only this pilot's vote among the current available destinations.
            var choices=Nodes.Where(n=>ServerAvailable(n.id)).ToArray();
            if(choices.Length==0)return;
            int chosen=choices[random.Next(choices.Length)].id;
            ballot.SetRoster(roster.Select(p=>p.OwnerId).ToArray(),now);
            if(!ballot.Votes.TryGetValue(sender.ClientId,out int vote)||vote!=chosen)ballot.Vote(sender.ClientId,chosen,now);
            nextVote[sender.ClientId]=now+settings.voteRepeatSeconds;Publish();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void RequestVote(int round, int node, NetworkConnection sender = null)
    {
        if (sender == null || ballot == null || !ballot.CanVote || round != generation || node < -1 || node >= Nodes.Length || node >= 0 && !ServerAvailable(node)) return;
        double now = Clock;
        if (nextVote.TryGetValue(sender.ClientId, out double next) && now < next) return;
        // Sender is injected by FishNet. A supplied player id is never trusted.
        ballot.SetRoster(Roster().Select(x => x.OwnerId).ToArray(), now);
        if (!ballot.Vote(sender.ClientId, node, now)) return;
        nextVote[sender.ClientId] = now + settings.voteRepeatSeconds; Publish();
    }
    private bool ServerAvailable(int id)
    {
        int origin = planned.Count > 0 ? planned[planned.Count - 1] : current;
        return origin < 0 ? id == 0 : Nodes[origin].next.Contains(id);
    }
    private void Update()
    {
        if (!IsServerInitialized || ballot == null || ballot.Phase == RouteVotePhase.Encounter || ballot.Phase == RouteVotePhase.Dormant) return;
        double now = Clock;
        if (now < nextSnapshot) return;
        nextSnapshot = now + settings.snapshotInterval;
        ballot.SetRoster(Roster().Select(x => x.OwnerId).ToArray(), now);
        ballot.Tick(now, random);
        if (ballot.Phase == RouteVotePhase.Encounter)
        {
            planned.Add(ballot.Winner);
            if (planned.Count < planningTarget) OpenChoice();
            else EnterEncounter();
        }
        else Publish();
    }
    private void EnterEncounter()
    {
        current = planned[planCursor++];
        ballot.StartEncounter(current); visited.Add(current); Publish();
        var node = Nodes[current];
        rooms.EnterRouteEncounter(Encounter(node.encounterId), node.seed);
    }
    private void Publish()
    {
        var pilots = Roster().Select(x => {
            var identity = x.GetComponent<PlayerIdentity>();
            return new RoutePilotVote { playerId = x.OwnerId, name = identity != null ? identity.PilotName : GameText.Get("Lobby", "pilot.number", x.OwnerId + 1),
                colorSlot = identity != null ? identity.ColorSlot : x.OwnerId, nodeId = ballot.Votes.TryGetValue(x.OwnerId, out int vote) ? vote : -1 };
        }).ToArray();
        PresentState(generation, current, visited.ToArray(), planned.ToArray(), planningTarget, ballot.Phase, ballot.Winner, ballot.Candidates,
            (float)Math.Max(0, ballot.Deadline - Clock), pilots);
    }
    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void PresentState(int round, int node, int[] history, int[] plan, int target, RouteVotePhase phase, int winner, int[] candidates, float seconds, RoutePilotVote[] pilots)
    {
        Round = round; CurrentNode = node; Visited = history; Phase = phase; Winner = winner; Candidates = candidates;
        Planned = plan; PlanningTarget = target;
        secondsAtSnapshot = seconds; snapshotAt = Time.unscaledTime; Pilots = pilots; Changed?.Invoke();
    }
    private void ResetPresentation()
    {
        Phase = RouteVotePhase.Dormant; Nodes = Array.Empty<LevelRouteNode>(); Pilots = Array.Empty<RoutePilotVote>();
        Visited = Candidates = Planned = Array.Empty<int>(); PlanningTarget = 0; CurrentNode = Winner = -1; Changed?.Invoke();
    }
    public override void OnStopServer() { ballot = null; current = -1; visited.Clear(); planned.Clear(); planCursor = 0; nextVote.Clear(); ResetPresentation(); base.OnStopServer(); }
    public override void OnStopClient() { ResetPresentation(); base.OnStopClient(); }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
