using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Read-only asset/runtime inspection. No Test Runner, fixtures or assertions.
public static class InspectAudio
{
    public static object Assets()
    {
        var profile = AssetDatabase.LoadAssetAtPath<GameAudioSettings>("Assets/_Project/Settings/Audio/GameAudio.asset");
        var paths = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
        var rows = paths.Select(path => {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var sample = importer.defaultSampleSettings;
            return $"\"{path}\",{clip.length.ToString(System.Globalization.CultureInfo.InvariantCulture)},{clip.channels},{clip.frequency},{sample.loadType},{sample.compressionFormat},{importer.forceToMono},{sample.preloadAudioData},{importer.loadInBackground}";
        }).ToArray();
        File.WriteAllLines("Logs/audio-import-inspection.csv", new[] { "Path,Seconds,Channels,Frequency,LoadType,Compression,Mono,Preload,Background" }.Concat(rows));
        return new {
            clips = paths.Length,
            cues = profile.cues.Length,
            missingClips = profile.cues.Where(c => c.clips.Length == 0 || c.clips.Any(clip => clip == null)).Select(c => c.id.ToString()).ToArray(),
            protectedCues = profile.cues.Where(c => c.protect).Select(c => c.id.ToString()).ToArray(),
            groups = profile.mixer.FindMatchingGroups("").Select(g => g.name).ToArray(),
            parameters = Enum.GetNames(typeof(GameAudioBus)).Select(n => profile.mixer.GetFloat(n + "Volume", out float value) ? n + "Volume" : "MISSING: " + n).ToArray(),
            config = "Real voices=" + AudioSettings.GetConfiguration().numRealVoices + ", virtual voices=" + AudioSettings.GetConfiguration().numVirtualVoices,
            playerPrefabs = AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponent<ActorFootstepAudio>() != null).ToArray(),
            report = "Logs/audio-import-inspection.csv"
        };
    }
    public static object Runtime()
    {
        var audio = GameAudio.Instance;
        return new {
            frame = Time.frameCount, clock = Time.time, audio.MixerReady, audio.Played, audio.Replaced, audio.Rejected,
            cues = audio.CueCounts.OrderBy(p => p.Key).Select(p => p.Key + ": " + p.Value).ToArray(),
            playing = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Where(s => s.isPlaying)
                .Select(s => $"{s.name}: {s.clip.name}, route={s.outputAudioMixerGroup?.name}, priority={s.priority}, pitch={s.pitch:F3}, virtual={s.isVirtual}").ToArray(),
            listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.enabled),
            menu = UnityEngine.Object.FindFirstObjectByType<AudioConfigurationMenu>()?.IsVisible
        };
    }
}
