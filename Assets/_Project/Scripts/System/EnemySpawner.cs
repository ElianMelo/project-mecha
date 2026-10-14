using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    public NetworkObject prefabEnemy;
    public List<Transform> spawnPoints;
    [SerializeField] private float spawnInterval = 4f;
    [SerializeField] private int maximumEnemies = 6;
    private readonly List<NetworkObject> spawned = new List<NetworkObject>();
    private Coroutine routine;
    public override void OnStartServer()
    {
        base.OnStartServer();
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
        while (true)
        {
            spawned.RemoveAll(enemy => enemy == null || !enemy.IsSpawned);
            if (spawned.Count < maximumEnemies)
            {
                Transform point = spawnPoints[Random.Range(0, spawnPoints.Count)];
                if (point != null)
                {
                    NetworkObject enemy = Instantiate(prefabEnemy, point.position, Quaternion.identity);
                    enemy.gameObject.SetActive(true); ServerManager.Spawn(enemy); spawned.Add(enemy);
                }
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
