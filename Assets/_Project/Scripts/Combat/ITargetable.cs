using UnityEngine;

// Native collider bounds supply screen visibility and a point to aim at.
public interface ITargetable
{
    bool CanTarget { get; }
    int TargetPriority { get; }
    Bounds TargetBounds { get; }
    Vector3 AimPoint { get; }
    Transform TargetTransform { get; }
}
