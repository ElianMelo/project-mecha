using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using UnityEngine;

[Serializable]
public struct PvpScore
{
    public int playerId, kills, deaths, assists;
    public string name;
    public PvpTeam team;
    public bool connected;
}

// Only the host publishes time, membership and scores. Clients never submit results or damage.
public sealed class PvpMatchDirector : NetworkBehaviour
{
    [SerializeField] private PvpMatchSettings settings;
    public static PvpMatchDirector Instance { get; private set; }
    public static bool Active => Instance != null && Instance.NetworkObject != null && Instance.IsSpawned && Instance.Mode != MechaMatchMode.Cooperative;
    public static bool BlocksCombat => Active && (Instance.Phase != PvpMatchPhase.Running || Instance.IsServerInitialized && Instance.Clock >= Instance.endsAt);
    public PvpMatchSettings Settings => settings;
    public MechaMatchMode Mode { get; private set; }
    public PvpMatchPhase Phase { get; private set; }
    public PvpScore[] Scores { get; private set; } = Array.Empty<PvpScore>();
    public float SecondsRemaining => Phase == PvpMatchPhase.Running ? Mathf.Max(0f, remainingAtSnapshot - (Time.unscaledTime - snapshotAt)) : remainingAtSnapshot;
    public event Action Changed;
    private readonly Dictionary<int, PvpScore> scores = new Dictionary<int, PvpScore>();
    private readonly Dictionary<PlayerHealth, Dictionary<int, float>> contributions = new Dictionary<PlayerHealth, Dictionary<int, float>>();
    private readonly Dictionary<PlayerHealth, Vector3> positions = new Dictionary<PlayerHealth, Vector3>();
    private readonly Dictionary<PlayerHealth, double> positionBlockedUntil = new Dictionary<PlayerHealth, double>();
    private readonly HashSet<PlayerHealth> placed = new HashSet<PlayerHealth>();
    private double endsAt, waitingUntil, nextSnapshot, nextPosition, lastPositionSample;
    private float remainingAtSnapshot, snapshotAt;
    private int expectedPlayers;
    private bool cancelled;
    public bool Cancelled { get; private set; }
    private double Clock => Time.realtimeSinceStartupAsDouble;
    private void Awake() => Instance = this;
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public override void OnStartServer()
    {
        base.OnStartServer();
        var lobby = FindFirstObjectByType<MechaLobbyService>();
        Mode = lobby != null ? lobby.SelectedMode : MechaMatchMode.Cooperative;
        Phase = PvpMatchPhase.Waiting; cancelled = false;
        expectedPlayers = lobby != null && lobby.Session != null ? lobby.Session.PlayerCount : 2;
        waitingUntil = Clock + settings.rosterTimeout; nextSnapshot = nextPosition = lastPositionSample = Clock;
        if (Mode != MechaMatchMode.Cooperative) Publish();
    }
    public override void OnStopNetwork()
    {
        Mode = MechaMatchMode.Cooperative; Phase = PvpMatchPhase.Waiting;
        scores.Clear(); contributions.Clear(); positions.Clear(); positionBlockedUntil.Clear(); placed.Clear();
        Scores = Array.Empty<PvpScore>(); remainingAtSnapshot = 0f; Cancelled = false;
        Changed?.Invoke(); base.OnStopNetwork();
    }
    public bool AreOpponents(PlayerHealth source, PlayerHealth target)
    {
        if (!Active || BlocksCombat || source == null || target == null || source == target ||
            !source.IsSpawned || !target.IsSpawned || source.Health <= 0f || target.Health <= 0f) return false;
        if (!Scores.Any(x => x.playerId == source.OwnerId && x.connected) || !Scores.Any(x => x.playerId == target.OwnerId && x.connected)) return false;
        if (IsServerInitialized && (!PositionAllowed(source) || !PositionAllowed(target))) return false;
        if (Mode == MechaMatchMode.FreeForAll) return true;
        var a = Scores.First(x => x.playerId == source.OwnerId); var b = Scores.First(x => x.playerId == target.OwnerId);
        return a.team != PvpTeam.None && b.team != PvpTeam.None && a.team != b.team;
    }
    public void RecordDamage(PlayerHealth victim, PlayerHealth attacker, float removed)
    {
        if (!IsServerInitialized || Phase != PvpMatchPhase.Running || attacker == null || removed <= 0f || !scores.ContainsKey(attacker.OwnerId)) return;
        if (!contributions.TryGetValue(victim, out var damage)) contributions[victim] = damage = new Dictionary<int, float>();
        damage.TryGetValue(attacker.OwnerId, out float previous); damage[attacker.OwnerId] = previous + removed;
    }
    public void RecordDeath(PlayerHealth victim, PlayerHealth killer)
    {
        if (!IsServerInitialized || Phase != PvpMatchPhase.Running || !scores.TryGetValue(victim.OwnerId, out var dead)) return;
        dead.deaths++; scores[dead.playerId] = dead;
        if (killer != null && killer != victim && scores.TryGetValue(killer.OwnerId, out var winner) && winner.connected)
        {
            winner.kills++; scores[winner.playerId] = winner;
            if (contributions.TryGetValue(victim, out var damage))
                foreach (var pair in damage)
                    if (pair.Key != winner.playerId && pair.Key != dead.playerId &&
                        (pair.Value >= victim.MaxHealth * settings.assistHealthFraction || Mathf.Approximately(pair.Value, victim.MaxHealth * settings.assistHealthFraction)) &&
                        scores.TryGetValue(pair.Key, out var assist) && assist.connected)
                    { assist.assists++; scores[pair.Key] = assist; }
        }
        contributions.Remove(victim); Publish();
    }
    public void ResetLife(PlayerHealth player)
    {
        if (!IsServerInitialized) return;
        contributions.Remove(player); positions[player] = player.transform.position;
        positionBlockedUntil.Remove(player);
    }
    public Vector3 SpawnPoint(PlayerHealth player)
    {
        var levels = ProceduralLevelDirector.Instance;
        if (levels == null || levels.Layout == null) return player.transform.position;
        var bounds = levels.Layout.Bounds;
        int slot = scores.Keys.OrderBy(x => x).ToList().IndexOf(player.OwnerId);
        float fraction = (Mathf.Max(0, slot) + 0.5f) / Mathf.Max(1, scores.Count);
        if (Mode == MechaMatchMode.Team)
        {
            var team = player.GetComponent<PlayerIdentity>().Team;
            var teammates = scores.Values.Where(x => x.team == team).OrderBy(x => x.playerId).ToArray();
            int index = Array.FindIndex(teammates, x => x.playerId == player.OwnerId);
            fraction = team == PvpTeam.Blue ? 0.08f + Mathf.Max(0, index) * 0.06f : 0.92f - Mathf.Max(0, index) * 0.06f;
        }
        else if (Phase == PvpMatchPhase.Running)
        {
            // Pick the floor slot furthest from living opponents instead of reviving in melee.
            float best = -1f;
            for (int i = 0; i < Mathf.Max(4, scores.Count); i++)
            {
                float candidate = (i + 0.5f) / Mathf.Max(4, scores.Count);
                float x = Mathf.Lerp(bounds.xMin + settings.spawnInset, bounds.xMax - settings.spawnInset, candidate);
                float distance = PlayerAttack.ActiveActors.Where(a => a != null && a.IsSpawned && a.OwnerId != player.OwnerId && a.GetComponent<PlayerHealth>().Health > 0f)
                    .Select(a => Mathf.Abs(a.transform.position.x - x)).DefaultIfEmpty(bounds.width).Min();
                if (distance > best) { best = distance; fraction = candidate; }
            }
        }
        return new Vector3(Mathf.Lerp(bounds.xMin + settings.spawnInset, bounds.xMax - settings.spawnInset, fraction), levels.Settings.floorHeight + 0.12f, 0f);
    }
    private PlayerHealth[] Roster() => PlayerAttack.ActiveActors.Where(x => x != null && x.IsSpawned && x.Owner != null && x.Owner.IsActive)
        .Select(x => x.GetComponent<PlayerHealth>()).Where(x => x != null).GroupBy(x => x.OwnerId).Select(x => x.First()).OrderBy(x => x.OwnerId).ToArray();
    private void Update()
    {
        if (!IsServerInitialized || Mode == MechaMatchMode.Cooperative) return;
        double now = Clock;
        // Close the damage gate before taking another simulation step at the deadline.
        if (Phase == PvpMatchPhase.Running && now >= endsAt) { Finish(false); return; }
        if (now < nextSnapshot) return;
        nextSnapshot = now + settings.snapshotInterval;
        var roster = Roster();
        foreach (int id in scores.Keys.ToArray())
        {
            var entry = scores[id]; entry.connected = roster.Any(p => p.OwnerId == id); scores[id] = entry;
        }
        foreach (var player in roster)
        {
            if (!scores.ContainsKey(player.OwnerId) && Phase == PvpMatchPhase.Waiting)
            {
                var identity = player.GetComponent<PlayerIdentity>();
                if (identity == null || !identity.CombatRegistered || !player.GetComponent<CharacterRuntime>().Ready) continue;
                scores[player.OwnerId] = new PvpScore { playerId = player.OwnerId, name = identity.PilotName, team = identity.Team, connected = true };
            }
            if (scores.ContainsKey(player.OwnerId) && placed.Add(player)) { player.EnterLevel(SpawnPoint(player)); positions[player] = player.transform.position; }
        }
        if (Phase == PvpMatchPhase.Waiting)
        {
            bool teamsValid = Mode != MechaMatchMode.Team || scores.Values.Any(x => x.connected && x.team == PvpTeam.Blue) && scores.Values.Any(x => x.connected && x.team == PvpTeam.Red);
            if (scores.Values.Count(x => x.connected) >= expectedPlayers && roster.Length >= 2 && teamsValid)
            {
                Phase = PvpMatchPhase.Running; endsAt = now + settings.durationSeconds;
                foreach (var player in roster) { player.EnterLevel(SpawnPoint(player)); player.GetComponent<PlayerAttack>()?.ResetMatchResources(); player.GetComponent<PlayerSecondaryWeapon>()?.ResetMatchResources(); }
            }
            else if (now >= waitingUntil) { Finish(true); return; }
        }
        if (Phase == PvpMatchPhase.Running && now >= nextPosition)
        {
            float elapsed = (float)Math.Max(settings.positionSampleSeconds, now - lastPositionSample);
            lastPositionSample = now; nextPosition = now + settings.positionSampleSeconds;
            foreach (var player in roster) ValidatePosition(player, elapsed);
        }
        positions.Keys.Where(p => p == null || !roster.Contains(p)).ToList().ForEach(p => { positions.Remove(p); contributions.Remove(p); positionBlockedUntil.Remove(p); placed.Remove(p); });
        Publish();
    }
    private bool PositionAllowed(PlayerHealth player)
    {
        if (positionBlockedUntil.TryGetValue(player, out double until) && Clock < until) return false;
        Vector3 point = player.transform.position;
        if (!float.IsFinite(point.x) || !float.IsFinite(point.y) || !float.IsFinite(point.z) || Mathf.Abs(point.z) > 0.5f) return false;
        return !positions.TryGetValue(player, out var previous) || Vector3.Distance(point, previous) <= settings.maximumTravelPerSecond * (float)Math.Max(settings.positionSampleSeconds, Clock - lastPositionSample) + settings.positionSlack;
    }
    private void ValidatePosition(PlayerHealth player, float elapsed)
    {
        Vector3 point = player.transform.position;
        var layout = ProceduralLevelDirector.Instance?.Layout;
        bool finite = float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);
        bool valid = finite && Mathf.Abs(point.z) < 0.5f && (layout == null || point.x >= layout.Bounds.xMin - 0.5f && point.x <= layout.Bounds.xMax + 0.5f && point.y <= layout.Bounds.yMax + 2f);
        if (valid && positions.TryGetValue(player, out var previous)) valid = Vector3.Distance(point, previous) <= settings.maximumTravelPerSecond * elapsed + settings.positionSlack;
        if (valid) { positions[player] = point; return; }
        positionBlockedUntil[player] = Clock + settings.positionPenaltySeconds;
        player.CorrectPvpPosition(positions.TryGetValue(player, out var safe) ? safe : SpawnPoint(player));
        player.GetComponent<PlayerAttack>()?.Interrupt();
    }
    private void Finish(bool aborted)
    {
        Phase = PvpMatchPhase.Finished; cancelled = aborted;
        foreach (var actor in PlayerAttack.ActiveActors) if (actor != null && actor.IsSpawned) actor.Interrupt();
        foreach (var projectile in FindObjectsByType<SecondaryWeaponProjectile>(FindObjectsSortMode.None)) if (projectile.IsSpawned) ServerManager.Despawn(projectile.NetworkObject);
        Publish();
    }
    private void Publish()
    {
        var ordered = scores.Values.OrderByDescending(x => x.kills).ThenBy(x => x.deaths).ThenByDescending(x => x.assists).ThenBy(x => x.playerId).ToArray();
        Snapshot(Mode, Phase, Phase == PvpMatchPhase.Running ? Mathf.Max(0f, (float)(endsAt - Clock)) : Phase == PvpMatchPhase.Waiting ? settings.durationSeconds : 0f, ordered, cancelled);
    }
    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void Snapshot(MechaMatchMode mode, PvpMatchPhase phase, float seconds, PvpScore[] standings, bool aborted)
    {
        Mode = mode; Phase = phase; remainingAtSnapshot = seconds; snapshotAt = Time.unscaledTime;
        Scores = standings; Cancelled = aborted; Changed?.Invoke();
    }
}
