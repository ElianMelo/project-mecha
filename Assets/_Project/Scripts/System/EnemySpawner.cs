using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private EnemySpawnSettings settings;
    public NetworkObject prefabEnemy => settings != null ? settings.prefab : null;
    public List<Transform> spawnPoints = new List<Transform>();
    private readonly List<NetworkObject> spawned = new List<NetworkObject>();
    public int SpawnedCount { get; private set; }
    public bool IsFinished { get; private set; }
    public int LivingCount
    {
        get
        {
            int count = 0;
            foreach (var enemy in spawned)
                if (enemy != null && enemy.IsSpawned && !enemy.GetComponent<EnemyController>().IsDead) count++;
            return count;
        }
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        var levels = GetComponent<ProceduralLevelDirector>();
        if (levels != null) levels.BeginRun();
        else
        {
            var positions = new List<Vector3>();
            foreach (var point in spawnPoints) if (point != null) positions.Add(point.position);
            SpawnEncounter(positions);
        }
    }
    public void SpawnEncounter(IReadOnlyList<Vector3> positions)
    {
        if (!IsServerInitialized) return;
        foreach (var old in spawned) if (old != null && old.IsSpawned) ServerManager.Despawn(old);
        spawned.Clear(); SpawnedCount = 0; IsFinished = false;
        if (prefabEnemy == null) { Debug.LogError("Encounter enemy prefab is missing.", this); return; }
        int amount = Mathf.Min(positions.Count, settings.spawnAmount);
        for (int i = 0; i < amount; i++)
        {
            bool ranged = settings.lancePrefab != null && i % 3 == 2;
            var enemy = Instantiate(ranged ? settings.lancePrefab : prefabEnemy, positions[i], Quaternion.identity);
            enemy.GetComponent<EnemyController>().AssignRole(ranged ? EnemyRole.Lancer : i % 2 == 0 ? EnemyRole.Guard : EnemyRole.Hunter);
            enemy.gameObject.SetActive(true); ServerManager.Spawn(enemy);
            spawned.Add(enemy); SpawnedCount++;
        }
        IsFinished = true;
    }
    public override void OnStopServer()
    {
        spawned.Clear(); IsFinished = false; base.OnStopServer();
    }
}
