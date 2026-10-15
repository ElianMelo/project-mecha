using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private EnemySpawnSettings settings;
    public NetworkObject prefabEnemy => settings != null ? settings.prefab : null;
    public List<Transform> spawnPoints;
    private readonly List<NetworkObject> spawned = new List<NetworkObject>();
    private Coroutine routine;
    private int nextPoint;
    public int SpawnedCount { get; private set; }
    public bool IsFinished { get; private set; }
    public override void OnStartServer()
    {
        base.OnStartServer();
        SpawnedCount = 0; nextPoint = 0; IsFinished = false;
        if (prefabEnemy != null && spawnPoints != null && spawnPoints.Count > 0) routine = StartCoroutine(SpawnEnemy());
        else Debug.LogWarning("EnemySpawner needs a prefab and spawn points.", this);
    }
    public override void OnStopServer()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null; spawned.Clear(); base.OnStopServer();
    }
    private IEnumerator SpawnEnemy()
    {
        if (settings.initialDelay > 0f) yield return new WaitForSeconds(settings.initialDelay);
        for (int slot = 0; slot < settings.spawnAmount; slot++)
        {
            spawned.RemoveAll(enemy => enemy == null || !enemy.IsSpawned);
            Transform point = null;
            for (int attempt = 0; attempt < settings.placementAttempts && point == null; attempt++)
            {
                point = ChooseSpawnPoint();
                if (point == null) yield return new WaitForSeconds(settings.interval);
            }
            if (point != null)
            {
                NetworkObject enemy = Instantiate(prefabEnemy, point.position, Quaternion.identity);
                enemy.gameObject.SetActive(true); ServerManager.Spawn(enemy); spawned.Add(enemy); SpawnedCount++;
            }
            if (slot + 1 < settings.spawnAmount) yield return new WaitForSeconds(settings.interval);
        }
        IsFinished = true; routine = null;
        if (SpawnedCount < settings.spawnAmount) Debug.LogWarning($"Encounter placed {SpawnedCount}/{settings.spawnAmount} enemies; remaining locations were blocked.", this);
    }
    private Transform ChooseSpawnPoint()
    {
        var players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        Transform best = null;
        float bestSpacing = -1f;
        int bestIndex = nextPoint;
        for (int offset = 0; offset < spawnPoints.Count; offset++)
        {
            int index = (nextPoint + offset) % spawnPoints.Count;
            var point = spawnPoints[index];
            if (point == null) continue;
            bool nearPlayer = false;
            foreach (var player in players)
                if (player.Health > 0f && Vector3.Distance(player.transform.position, point.position) < settings.minimumPlayerDistance) { nearPlayer = true; break; }
            if (nearPlayer) continue;
            float spacing = float.MaxValue;
            foreach (var enemy in spawned)
                if (enemy != null) spacing = Mathf.Min(spacing, (enemy.transform.position - point.position).sqrMagnitude);
            if (spacing <= bestSpacing) continue;
            best = point; bestSpacing = spacing; bestIndex = index;
        }
        nextPoint = (bestIndex + 1) % spawnPoints.Count;
        return best;
    }
}
