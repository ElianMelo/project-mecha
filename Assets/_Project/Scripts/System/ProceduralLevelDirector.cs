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
    public static ProceduralLevelDirector Instance { get; private set; }
    public ProceduralLevelSettings Settings => settings;
    public ProceduralLevelLayout Layout { get; private set; }
    public int LevelNumber { get; private set; }
    public int Seed { get; private set; }
    public bool IsClear => cleared.Value;
    public int Remaining => remaining.Value;
    private void Awake() { Instance = this; spawner = GetComponent<EnemySpawner>(); }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public override void OnStopServer()
    {
        Layout = null; LevelNumber = 0; placedPlayers.Clear();
        base.OnStopServer();
    }
    public void BeginRun()
    {
        if (!IsServerInitialized || Layout != null) return;
        ChangeLevel(1, settings.firstRoomSeed);
    }
    private void ChangeLevel(int number, int seed)
    {
        changing = true; cleared.Value = false;
        foreach (var projectile in FindObjectsByType<EnemyLanceProjectile>(FindObjectsSortMode.None))
            if (projectile.IsSpawned) ServerManager.Despawn(projectile.NetworkObject);
        // One buffered room description reconstructs deterministic geometry for late joiners.
        BuildRoom(number, seed);
        foreach (var prop in FindObjectsByType<DestructibleProp>(FindObjectsInactive.Include, FindObjectsSortMode.None)) prop.RestoreForLevel();
        spawner.SpawnEncounter(Layout.Enemies);
        remaining.Value = spawner.LivingCount;
        placedPlayers.Clear(); PlaceNewPlayers(); changing = false;
    }
    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void BuildRoom(int number, int seed)
    {
        LevelNumber = number; Seed = seed;
        Layout = ProceduralLevelLayout.Generate(settings, seed, number, 16f / 9f);
        GetComponent<ProceduralRoomBuilder>().Build(Layout, settings);
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
        remaining.Value = spawner.LivingCount;
        if (!cleared.Value && spawner.IsFinished && remaining.Value == 0) cleared.Value = true;
        if (Time.time >= nextRoster) { nextRoster = Time.time + 0.25f; PlaceNewPlayers(); }
    }
    private void PlaceNewPlayers()
    {
        foreach (var player in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None).OrderBy(player => player.OwnerId))
            if (player.IsSpawned && placedPlayers.Add(player))
            {
                int slot = placedPlayers.Count - 1;
                float offset = ((slot + 1) / 2) * settings.playerSpawnSpacing * (slot % 2 == 0 ? -1f : 1f);
                Vector3 position = Layout.PlayerStart;
                if (Physics.Raycast(position + Vector3.up * 0.35f, Vector3.down, out var support, 0.8f, (1 << 6) | (1 << 7)))
                    position.x = Mathf.Clamp(position.x + offset, support.collider.bounds.min.x + 0.4f, support.collider.bounds.max.x - 0.4f);
                player.EnterLevel(position);
            }
        placedPlayers.RemoveWhere(player => player == null);
    }
    public bool TryTravel(PlayerHealth player)
    {
        if (!IsServerInitialized || changing || !IsClear || player == null || player.Health <= 0f || player.IsStunned ||
            Vector3.Distance(player.transform.position, Layout.Portal) > settings.portalInteractionRange) return false;
        int nextSeed = unchecked(Seed * 1664525 + 1013904223);
        ChangeLevel(LevelNumber + 1, nextSeed); return true;
    }
}
