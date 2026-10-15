using UnityEngine;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(menuName = "Mecha/Presentation/Rendering")]
public class PresentationRenderSettings : ScriptableObject
{
    public enum Multisampling { Disabled = 1, Two = 2, Four = 4, Eight = 8 }
    public UniversalRenderPipelineAsset pipeline;
    public Multisampling msaa = Multisampling.Four;
    [Range(0.1f, 2f)] public float renderScale = 1f;
    public AntialiasingMode antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
    public AntialiasingQuality antialiasingQuality = AntialiasingQuality.High;
    public bool postProcessing = true;
}
