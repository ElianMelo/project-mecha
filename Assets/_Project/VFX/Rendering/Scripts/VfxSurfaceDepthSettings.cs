using UnityEngine;

// Optional placement for cosmetic particles in the side-on combat plane.
[CreateAssetMenu(menuName = "Mecha/VFX/Surface Depth")]
public sealed class VfxSurfaceDepthSettings : ScriptableObject
{
    [Min(0f)] public float surfaceClearance = .06f;
    [Min(0f)] public float maximumShift = 1.25f;
    [Min(.01f)] public float probeRadius = .2f;
    public LayerMask platformLayers = (1 << 6) | (1 << 7);
}
