using UnityEngine;

// Authoring profile consumed by the editor setup. Runtime uses native FishNet components.
[CreateAssetMenu(menuName = "Mecha/Network Simulation")]
public sealed class NetworkSimulationSettings : ScriptableObject
{
    [Range(1, 240)] public ushort tickRate = 50;
    [Range(50, 500)] public ushort maximumFrameRate = 120;
    [Range(1, 20)] public ushort interpolationTicks = 2;
    [Range(1, 255)] public byte sendInterval = 1;
    [Range(0.00001f, 0.1f)] public float positionSensitivity = 0.001f;
    [Tooltip("Ordinary motion stays interpolated. Revive/travel explicitly call NetworkTransform.Teleport.")]
    public bool automaticDistanceTeleport;
}
