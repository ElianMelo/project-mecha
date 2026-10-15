using FishNet.Object;
using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Enemy/Spawning")]
public class EnemySpawnSettings : ScriptableObject
{
    public NetworkObject prefab;
    [Min(0)] public int spawnAmount = 18;
    [Min(0f)] public float initialDelay = 0.4f, interval = 0.8f, minimumPlayerDistance = 3.5f;
    [Min(1)] public int placementAttempts = 6;
}
