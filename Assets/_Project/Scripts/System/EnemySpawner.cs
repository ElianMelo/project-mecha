using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    public NetworkObject prefabEnemy;
    public List<Transform> spawnPoints;

    public override void OnStartServer()
    {
        StartCoroutine(SpawnEnemy());
    }

    IEnumerator SpawnEnemy()
    {
        while(true)
        {
            Vector3 position = spawnPoints[Random.Range(0, spawnPoints.Count)].position;
            NetworkObject netObj = Instantiate(prefabEnemy, position, Quaternion.identity);
            ServerManager.Spawn(netObj, base.Owner);
            yield return new WaitForSeconds(0.5f);
        }
    }
}
