using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Enemy/Crowd Spacing")]
public class EnemyCrowdSettings : ScriptableObject
{
    [Range(0f, 0.8f)] public float sparseOverlap = 0.2f;
    [Range(0f, 0.8f)] public float denseOverlap = 0.4f;
    public float neighborRadius = 2f, verticalTolerance = 0.75f;
    public int denseNeighborCount = 5;
    public float separationStrength = 8f, maximumCorrection = 4.5f, anticipation = 0.18f;
    public float minimumSpacing = 1.15f;
}
