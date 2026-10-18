using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(EnemySpawner), typeof(ProceduralRoomBuilder))]
public class ProceduralLevelDirector : NetworkBehaviour
{
    [SerializeField] private ProceduralLevelSettings settings;
    private readonly SyncVar<bool> cleared = new SyncVar<bool>();
    private readonly SyncVar<int> remaining = new SyncVar<int>();
    private readonly HashSet<PlayerHealth> placedPlayers = new HashSet<PlayerHealth>();
    private EnemySpawner spawner;
    private LevelPortal portal;
    private float nextRoster;
    private bool changing;
    private bool hasPresentedRoom;
    private LevelRouteDirector routing;
    private RouteEncounter customEncounter;
    private ProceduralLevelSettings activeSettings;
    public static ProceduralLevelDirector Instance { get; private set; }
    public ProceduralLevelSettings Settings => activeSettings != null ? activeSettings : settings;
    public ProceduralLevelLayout Layout { get; private set; }
    public int LevelNumber { get; private set; }
    public int Seed { get; private set; }
    public bool IsClear => cleared.Value;
    public int Remaining => remaining.Value;
    private void Awake() { Instance = this; spawner = GetComponent<EnemySpawner>(); routing = GetComponent<LevelRouteDirector>(); }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public override void OnStopServer()
    {
        hasPresentedRoom = false;
        customEncounter = null; activeSettings = null;
        Layout = null; LevelNumber = 0; placedPlayers.Clear();
        base.OnStopServer();
    }
    public override void OnStopClient() { hasPresentedRoom = false; base.OnStopClient(); }
    public void BeginRun()
    {
        if (!IsServerInitialized || Layout != null) return;
        bool skipOpeningChoice = GameManager.Instance != null && GameManager.Instance.ConsumeQuickHostOpening();
        if (routing != null) { routing.BeginRun(skipOpeningChoice); return; }
        ChangeLevel(1, settings.firstRoomSeed);
    }
    public void EnterRouteEncounter(RouteEncounterDefinition encounter, int seed)
    {
        if (!IsServerInitialized || encounter == null) return;
        ChangeLevel(LevelNumber + 1, seed, encounter);
    }
    private void ChangeLevel(int number, int seed, RouteEncounterDefinition encounter = null)
    {
        changing = true; cleared.Value = false;
        foreach (var projectile in FindObjectsByType<EnemyLanceProjectile>(FindObjectsSortMode.None))
            if (projectile.IsSpawned) ServerManager.Despawn(projectile.NetworkObject);
        // One buffered room description reconstructs deterministic geometry for late joiners.
        if (customEncounter != null && customEncounter.IsSpawned) ServerManager.Despawn(customEncounter.NetworkObject);
        customEncounter = null;
        BuildRoom(number, seed, encounter != null ? encounter.encounterId : "");
        foreach (var prop in FindObjectsByType<DestructibleProp>(FindObjectsInactive.Include, FindObjectsSortMode.None)) prop.RestoreForLevel();
        if (encounter != null && encounter.encounterPrefab != null)
        {
            spawner.SpawnEncounter(System.Array.Empty<Vector3>());
            var content = Instantiate(encounter.encounterPrefab, Vector3.zero, Quaternion.identity);
            ServerManager.Spawn(content); customEncounter = content.GetComponent<RouteEncounter>();
            customEncounter.BeginEncounter(Layout, seed);
        }
        else spawner.SpawnEncounter(Layout.Enemies);
        remaining.Value = customEncounter != null ? customEncounter.Remaining : spawner.LivingCount;
        placedPlayers.Clear(); PlaceNewPlayers(); changing = false;
    }
    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void BuildRoom(int number, int seed, string encounterId = "")
    {
        if (hasPresentedRoom) GameAudio.Play(GameAudioCue.PortalTravel, Layout != null ? Layout.Portal : transform.position);
        hasPresentedRoom = true;
        LevelNumber = number; Seed = seed;
        var encounter = routing != null ? routing.Encounter(encounterId) : null;
        activeSettings = encounter != null ? encounter.roomSettings : settings;
        Layout = ProceduralLevelLayout.Generate(Settings, seed, number, 16f / 9f, encounter != null ? encounter.shape : (LevelShape?)null);
        GetComponent<ProceduralRoomBuilder>().Build(Layout, Settings);
        if (portal == null)
        {
            var go = new GameObject("Exit Portal"); portal = go.AddComponent<LevelPortal>();
            portal.Configure(this);
        }
        portal.transform.position = Layout.Portal;
    }
    private void Update()
    {
        if (!IsServerInitialized || Layout == null || changing) return;
        remaining.Value = customEncounter != null ? customEncounter.Remaining : spawner.LivingCount;
        if (!cleared.Value && (customEncounter != null ? customEncounter.IsComplete : spawner.IsFinished && remaining.Value == 0)) cleared.Value = true;
        if (Time.time >= nextRoster) { nextRoster = Time.time + 0.25f; PlaceNewPlayers(); }
    }
    private void PlaceNewPlayers()
    {
        foreach (var player in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None).OrderBy(player => player.OwnerId))
            if (player.IsSpawned && placedPlayers.Add(player))
            {
                int slot = placedPlayers.Count - 1;
                float offset = ((slot + 1) / 2) * Settings.playerSpawnSpacing * (slot % 2 == 0 ? -1f : 1f);
                Vector3 position = Layout.PlayerStart;
                if (Physics.Raycast(position + Vector3.up * 0.35f, Vector3.down, out var support, 0.8f, (1 << 6) | (1 << 7)))
                    position.x = Mathf.Clamp(position.x + offset, support.collider.bounds.min.x + 0.4f, support.collider.bounds.max.x - 0.4f);
                player.EnterLevel(position);
            }
        placedPlayers.RemoveWhere(player => player == null);
    }
    public bool TryTravel(PlayerHealth player)
    {
        if (!IsServerInitialized || changing || Layout == null || !IsClear || player == null || player.Health <= 0f || player.IsStunned ||
            Vector3.Distance(player.transform.position, Layout.Portal) > Settings.portalInteractionRange) return false;
        if (routing != null) return routing.OpenAfterClear();
        int nextSeed = unchecked(Seed * 1664525 + 1013904223);
        ChangeLevel(LevelNumber + 1, nextSeed); return true;
    }
}
