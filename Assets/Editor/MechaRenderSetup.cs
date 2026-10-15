using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class MechaRenderSetup
{
    public static void Apply(Camera camera)
    {
        var settings = MechaPresentationSetup.Create<PresentationRenderSettings>("Rendering");
        if (settings.pipeline == null)
        {
            settings.pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            EditorUtility.SetDirty(settings);
        }
        if (settings.pipeline != null)
        {
            settings.pipeline.msaaSampleCount = (int)settings.msaa;
            settings.pipeline.renderScale = settings.renderScale;
            EditorUtility.SetDirty(settings.pipeline);
        }
        camera.allowMSAA = settings.msaa != PresentationRenderSettings.Multisampling.Disabled;
        var data = camera.GetUniversalAdditionalCameraData();
        data.antialiasing = settings.antialiasing;
        data.antialiasingQuality = settings.antialiasingQuality;
        data.renderPostProcessing = settings.postProcessing;
        EditorUtility.SetDirty(data);
    }
}
