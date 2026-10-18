using UnityEngine;

// Editor-baked presentation tuning. Physics and movement keep their existing scale.
[CreateAssetMenu(menuName = "Mecha/Characters/Wing Zero Rig")]
public sealed class WingZeroRigSettings : ScriptableObject
{
    [Range(.1f, 1f)] public float visualScale = .5f;
    [Range(.1f, 1f)] public float wingScale = .6f;
}
