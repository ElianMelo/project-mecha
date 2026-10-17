using UnityEngine;
using FishNet.Object;

[CreateAssetMenu(menuName = "Mecha/Player/Secondary Weapon")]
public sealed class SecondaryWeaponSettings : ScriptableObject
{
    public int capacity = 25;
    public float roundsPerSecond = 10f, refillPerSecond = 5f, refillDelay = 0.65f;
    [Range(0f, 0.05f)] public float arrivalGrace = 0.025f;
    public float damage = 5f, range = 100f;
    public float aimSmoothTime = 0.7f, aimDegreesPerSecond = 65f, acquisitionDelay = 0.3f;
    public float aimSyncInterval = 0.1f;
    public float targetScanInterval = 0.12f;
    public LayerMask targetLayers = (1 << 9) | (1 << 10);
    public NetworkObject projectile;
    public float projectileLifetime = 4f, projectileRadius = 0.065f;
    public float projectileVisualDepth = -1.2f;
    public Vector3 armMountOffset = Vector3.zero;
    public Vector3 mountOffset = new Vector3(0f, 0.95f, -0.22f);
    public float boltSpeed = 30f;
    public Color shotColor = new Color(5f, 3.8f, 0.12f, 1f);
    public LayerMask hitLayers = (1 << 7) | (1 << 9) | (1 << 10);
}
