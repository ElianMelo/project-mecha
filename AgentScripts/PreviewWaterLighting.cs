using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// Operator-invoked native skill presentation and same-frame light comparison; no test runner/assertions.
public static class PreviewWaterLighting
{
    const string Root = "Assets/_Project/VFX/ZeroWater/";
    const string Output = "Logs/WaterLighting";
    static async Task Wait(float seconds)
    {
        float until = Time.time + seconds;
        while (Time.time < until) { await Awaitable.NextFrameAsync(); await Awaitable.EndOfFrameAsync(); }
    }
    static Color32[] Capture(Camera camera, string name)
    {
        Directory.CreateDirectory(Output);
        var old = camera.targetTexture; var active = RenderTexture.active;
        var rt = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGBHalf);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            File.WriteAllBytes(Output + "/" + name + ".png", texture.EncodeToPNG()); return texture.GetPixels32();
        }
        finally { camera.targetTexture = old; RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(texture); }
    }
    static object Difference(Camera camera, Color32[] a, Color32[] b, Vector2 lower, Vector2 upper)
    {
        var lo = camera.WorldToViewportPoint(lower); var hi = camera.WorldToViewportPoint(upper);
        int x0 = Mathf.Clamp((int)(lo.x * 1280), 0, 1279), x1 = Mathf.Clamp((int)(hi.x * 1280), 0, 1279);
        int y0 = Mathf.Clamp((int)(lo.y * 720), 0, 719), y1 = Mathf.Clamp((int)(hi.y * 720), 0, 719), changed = 0, max = 0;
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
        {
            int i = y * 1280 + x, d = Mathf.Max(Math.Abs(a[i].r - b[i].r), Mathf.Max(Math.Abs(a[i].g - b[i].g), Math.Abs(a[i].b - b[i].b)));
            if (d > 1) changed++; max = Mathf.Max(max, d);
        }
        return new { pixels = (x1 - x0 + 1) * (y1 - y0 + 1), changed, maxChannelDelta = max };
    }
    static object Pair(Camera camera, string phase)
    {
        var effects = UnityEngine.Object.FindObjectsByType<WaterEffect>(FindObjectsSortMode.None);
        var lights = effects.SelectMany(e => e.GetComponentsInChildren<Light>()).ToArray();
        var enabled = lights.Select(l => l.enabled).ToArray();
        foreach (var light in lights) light.enabled = false;
        var off = Capture(camera, phase + "-light-off");
        for (int i = 0; i < lights.Length; i++) lights[i].enabled = enabled[i];
        var on = Capture(camera, phase + "-light-on");
        return new { phase, frame = Time.frameCount, time = Time.time, effects = effects.Select(e => e.name).ToArray(),
            lights = lights.Select(l => new { l.intensity, l.range, position = l.transform.position.ToString("F3") }).ToArray(),
            floor = Difference(camera, off, on, new Vector2(-4, -4.6f), new Vector2(4, -3.6f)),
            actor = Difference(camera, off, on, new Vector2(-.6f, -3.5f), new Vector2(.6f, -1.4f)) };
    }
    public static async Task<object> Observe()
    {
        if (!EditorApplication.isPlaying || UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None).Any())
            throw new InvalidOperationException("Use a disposable Play Mode menu session.");
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
        foreach (var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (c is RoomCameraRig || c is RoomCameraZoom || c is EnemySpawner || c.GetType().Name == "CinemachineBrain") c.enabled = false;
        var settings = AssetDatabase.LoadAssetAtPath<ProceduralLevelSettings>("Assets/_Project/Settings/World/ProceduralLevels.asset");
        var builder = UnityEngine.Object.FindFirstObjectByType<ProceduralRoomBuilder>();
        if (builder == null) builder = new GameObject("Water Lighting Preview Room").AddComponent<ProceduralRoomBuilder>();
        builder.Build(ProceduralLevelLayout.Generate(settings, settings.firstRoomSeed, 1, 16f / 9f), settings);
        var camera = Camera.main; camera.transform.SetPositionAndRotation(new Vector3(0, -2.1f, -12), Quaternion.identity); camera.orthographicSize = 3.485f;
        var actor = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/MechaPlayer.prefab"));
        try
        {
            actor.GetComponent<CharacterRuntime>().enabled = false; actor.GetComponent<CharacterRuntime>().Apply(1);
            actor.GetComponent<UnityEngine.InputSystem.PlayerInput>().enabled = false; actor.GetComponent<PlayerInputs>().enabled = false;
            await Wait(.2f); actor.SetActive(true); var body = actor.GetComponent<Rigidbody>(); body.position = actor.transform.position = new Vector3(0, -3.5f, 0); body.linearVelocity = Vector3.zero;
            actor.GetComponent<PlayerMovement>().Face(1); await Wait(.15f);
            foreach (var material in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct())
                for (int pass = 0; pass < material.passCount; pass++) ShaderUtil.CompilePass(material, pass, true);
            await Wait(.2f);
            var skills = actor.GetComponent<CharacterSkills>(); var observations = new List<object>();
            int frame = Time.frameCount; float began = Time.time;
            var bubble = AssetDatabase.LoadAssetAtPath<WaterBubbleSkill>(Root + "Profiles/BubbleBurst.asset");
            bubble.Begin(skills, 1); await Wait(.15f); observations.Add(Pair(camera, "BubblePreparation"));
            await Wait(.25f); bubble.PresentPulse(skills, 1, 0); await Wait(.08f); observations.Add(Pair(camera, "BubbleDetonation"));
            await Wait(1.5f);
            var fountain = AssetDatabase.LoadAssetAtPath<WaterFountainSkill>(Root + "Profiles/RisingFountain.asset");
            fountain.Begin(skills, 1); await Wait(.15f); observations.Add(Pair(camera, "FountainPreparation"));
            await Wait(.25f); fountain.PresentPulse(skills, 1, 0); await Wait(.27f); observations.Add(Pair(camera, "RisingFountain"));
            await Wait(1.7f);
            skills.ActivateModifier(8, 1.3f, 1.4f); await Wait(.25f); observations.Add(Pair(camera, "WaterModeAura"));
            var aura = UnityEngine.Object.FindObjectsByType<WaterEffect>(FindObjectsSortMode.None).First(e => e.name.StartsWith("WaterModeAura"));
            var lightBefore = aura.GetComponentInChildren<Light>().transform.position;
            body.position = actor.transform.position = new Vector3(1, -3.5f, 0); await Wait(.35f);
            var lightAfter = aura.GetComponentInChildren<Light>().transform.position;
            skills.ClearModifier(); await Wait(.15f); var fading = aura.GetComponentInChildren<Light>().intensity;
            await Wait(.35f); var drained = aura.GetComponentInChildren<Light>().intensity;
            await Wait(1.3f);
            var report = new { frame, finalFrame = Time.frameCount, began, time = Time.time, observations,
                auraFollowDelta = (lightAfter - lightBefore).ToString("F3"), fading, drained,
                remainingEffects = UnityEngine.Object.FindObjectsByType<WaterEffect>(FindObjectsSortMode.None).Length };
            return report;
        }
        finally
        {
            foreach (var effect in UnityEngine.Object.FindObjectsByType<WaterEffect>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(effect.gameObject);
            UnityEngine.Object.Destroy(actor);
        }
    }
}
