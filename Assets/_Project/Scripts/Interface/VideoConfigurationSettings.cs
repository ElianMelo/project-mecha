using UnityEngine;

public enum VideoAntialiasing { Off, Fxaa, Smaa, Msaa2X, Msaa4X, Msaa8X }

[CreateAssetMenu(menuName = "Mecha/Interface/Video Configuration")]
public sealed class VideoConfigurationSettings : ScriptableObject
{
    public int[] frameLimits = { 30, 60, 90, 120, 144, 165, 240, -1 };
    [Min(5f)] public float displayConfirmationSeconds = 15f;
    public int defaultFrameLimit = 60;
    public bool defaultVSync = true;
    public VideoAntialiasing defaultAntialiasing = VideoAntialiasing.Smaa;
    [Range(0, 3)] public int defaultTextureLimit;
}
