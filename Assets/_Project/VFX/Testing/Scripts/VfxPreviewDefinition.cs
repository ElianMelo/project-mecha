using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Mecha/VFX/Preview definition")]
public sealed class VfxPreviewDefinition : ScriptableObject
{
    public GameObject prefab;
    [Tooltip("The prefab starts and schedules its own visual systems. The browser still owns replacement and cleanup.")]
    public bool prefabControlsPlayback;
    public LocalizedString displayName = new LocalizedString();
    public LocalizedString description = new LocalizedString();
    [Min(0.05f)] public float playSeconds = 3f;
    [Min(0f)] public float tailSeconds = 1f;
    public Vector3 position;
    public Vector3 eulerAngles;
    [Min(0.01f)] public float scale = 1f;
}
