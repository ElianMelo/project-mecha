using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/World/One Way Platform")]
public class OneWayPlatformSettings : ScriptableObject
{
    public float dropDuration = 0.45f, surfaceTolerance = 0.06f, risingThreshold = 0.1f;
}
