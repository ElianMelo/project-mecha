using UnityEngine;
using FishNet.Object;

[CreateAssetMenu(menuName = "Mecha/Guns/Secondary Weapon")]
public sealed class SecondaryWeaponSettings : ScriptableObject
{
    public int capacity = 12;
    public float roundsPerSecond = 14f, refillPerSecond = 5f, refillDelay = 1.5f;
    [Range(0f, 0.05f)] public float arrivalGrace = 0.025f;
    public float damage = 8f, range = 100f;
    public float aimSmoothTime = 0.45f, aimDegreesPerSecond = 100f, acquisitionDelay = 0.3f;
    [Header("Aim capture and feedback")]
    [Range(1f, 45f)] public float lockAngle = 12f;
    [Min(1f)] public float captureDegreesPerSecond = 300f;
    [Min(0.01f)] public float lockedAimSmoothTime = 0.08f, lockedTargetSmoothTime = 0.1f;
    [Min(1f)] public float lockedDegreesPerSecond = 540f;
    public RuntimeAnimatorController reticleController;
    public float reticleVisualDepth = -2f;
    public float aimSyncInterval = 0.1f;
    public float targetScanInterval = 0.12f;
    [Min(0f)] public float targetSwitchDelay = 0.65f;
    [Min(0f)] public float targetSwitchDistanceMargin = 0.15f;
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
