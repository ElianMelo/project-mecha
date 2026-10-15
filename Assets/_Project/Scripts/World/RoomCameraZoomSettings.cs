using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/World/Room Camera Zoom")]
public class RoomCameraZoomSettings : ScriptableObject
{
    [Range(0f, 0.25f)] public float skillZoomOut = 0.096f, dashZoomOut = 0.033f;
    [Min(0.01f)] public float skillEaseIn = 0.12f, skillEaseOut = 0.35f;
    [Min(0.01f)] public float dashEaseIn = 0.24f, dashEaseOut = 0.5f;
}
