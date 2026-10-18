using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Native URP owns antialiasing; a disposable copy keeps authored pipeline assets read-only.
public sealed class VideoQualityRuntime
{
    private struct CameraValues { public AntialiasingMode mode; public bool msaa; }
    private readonly Dictionary<Camera, CameraValues> cameras = new Dictionary<Camera, CameraValues>();
    private readonly RenderPipelineAsset originalOverride;
    private readonly UniversalRenderPipelineAsset pipeline;
    private VideoAntialiasing selected;
    public VideoAntialiasing Original { get; }

    public VideoQualityRuntime()
    {
        originalOverride = QualitySettings.renderPipeline;
        var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (source != null)
        {
            pipeline = Object.Instantiate(source);
            pipeline.name = source.name + " (Video Preferences)";
            pipeline.hideFlags = HideFlags.DontSave;
            Original = source.msaaSampleCount >= 8 ? VideoAntialiasing.Msaa8X : source.msaaSampleCount >= 4 ? VideoAntialiasing.Msaa4X : source.msaaSampleCount >= 2 ? VideoAntialiasing.Msaa2X : VideoAntialiasing.Off;
        }
        var main = Camera.main != null ? Camera.main.GetComponent<UniversalAdditionalCameraData>() : null;
        if (main != null && main.antialiasing != AntialiasingMode.None)
            Original = main.antialiasing == AntialiasingMode.FastApproximateAntialiasing ? VideoAntialiasing.Fxaa : VideoAntialiasing.Smaa;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    public void Apply(VideoAntialiasing value)
    {
        selected = value;
        if (pipeline != null)
        {
            pipeline.msaaSampleCount = value == VideoAntialiasing.Msaa8X ? 8 : value == VideoAntialiasing.Msaa4X ? 4 : value == VideoAntialiasing.Msaa2X ? 2 : 1;
            QualitySettings.renderPipeline = pipeline;
        }
        ApplyCameras();
    }
    private void SceneLoaded(Scene scene, LoadSceneMode mode) => ApplyCameras();
    private void ApplyCameras()
    {
        foreach (var data in Object.FindObjectsByType<UniversalAdditionalCameraData>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var camera = data.GetComponent<Camera>();
            if (!cameras.ContainsKey(camera)) cameras.Add(camera, new CameraValues { mode = data.antialiasing, msaa = camera.allowMSAA });
            data.antialiasing = selected == VideoAntialiasing.Fxaa ? AntialiasingMode.FastApproximateAntialiasing : selected == VideoAntialiasing.Smaa ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.None;
            camera.allowMSAA = selected >= VideoAntialiasing.Msaa2X;
        }
    }
    public void Dispose()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        foreach (var entry in cameras)
            if (entry.Key != null)
            {
                var data = entry.Key.GetComponent<UniversalAdditionalCameraData>();
                if (data != null) data.antialiasing = entry.Value.mode;
                entry.Key.allowMSAA = entry.Value.msaa;
            }
        if (pipeline != null)
        {
            if (QualitySettings.renderPipeline == pipeline) QualitySettings.renderPipeline = originalOverride;
            Object.Destroy(pipeline);
        }
    }
}
