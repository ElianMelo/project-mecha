using System.Collections.Generic;
using UnityEngine;

// Unity owns mixing/spatialization. This admission policy adds per-cue limits,
// protected voices, and oldest-first replacement, which AudioSource.priority alone cannot express.
[DefaultExecutionOrder(-200)]
public sealed class GameAudio : MonoBehaviour
{
    private sealed class Voice
    {
        public AudioSource source;
        public GameAudioSettings.Cue cue;
        public Transform follow;
        public bool attached, releasing;
        public float gain, fade = 1f, started;
        public ulong sequence;
    }
    public static GameAudio Instance { get; private set; }
    [SerializeField] private GameAudioSettings settings;
    private readonly Dictionary<GameAudioCue, GameAudioSettings.Cue> cues = new Dictionary<GameAudioCue, GameAudioSettings.Cue>();
    private readonly Dictionary<GameAudioCue, int> lastClip = new Dictionary<GameAudioCue, int>();
    private readonly Dictionary<GameAudioCue, float> lastUi = new Dictionary<GameAudioCue, float>();
    private readonly Dictionary<GameAudioCue, int> cueCounts = new Dictionary<GameAudioCue, int>();
    private readonly List<Voice> transients = new List<Voice>(), protectedVoices = new List<Voice>();
    private readonly float[] volumes = new float[5];
    private readonly bool[] mixerControls = new bool[5];
    private static readonly string[] Parameters = { "MasterVolume", "UIVolume", "SFXVolume", "AmbientVolume", "MusicVolume" };
    private AudioSource[] musicSources, ambientSources;
    private float[] musicFades = new float[2], ambientFades = new float[2];
    private int musicIndex, ambientIndex, room = int.MinValue;
    private ulong sequence;
    private bool ready;
    public GameAudioSettings Settings => settings;
    public int Played { get; private set; }
    public int Replaced { get; private set; }
    public int Rejected { get; private set; }
    public IReadOnlyDictionary<GameAudioCue, int> CueCounts => cueCounts;
    public int ProtectedActive => protectedVoices.FindAll(v => v.source.isPlaying).Count;
    public bool MixerReady => settings != null && settings.mixer != null && System.Array.TrueForAll(mixerControls, value => value);

    private void Awake()
    {
#if UNITY_SERVER
        enabled = false;
        return;
#else
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (settings == null) { enabled = false; return; }
        foreach (var cue in settings.cues) if (cue != null) cues[cue.id] = cue;
        for (int i = 0; i < volumes.Length; i++) volumes[i] = Mathf.Clamp01(GameSave.GetFloat(GameSaveKeys.AudioVolume((GameAudioBus)i), settings.DefaultVolume((GameAudioBus)i)));
        for (int i = 0; i < settings.transientVoices + settings.releaseVoices; i++) transients.Add(NewVoice("Transient " + i));
        for (int i = 0; i < 16; i++) protectedVoices.Add(NewVoice("Protected " + i));
        musicSources = Beds("Music", GameAudioBus.Music); ambientSources = Beds("Ambient", GameAudioBus.Ambient);
#endif
    }
    private void Start()
    {
        if (settings == null) return;
        ready = true; ApplyVolumes();
        if (!MixerReady) Debug.LogWarning("Mecha audio mixer is incomplete. Run Tools/Mecha/Audio/Connect Mixer after creating the native mixer groups and exposed volumes. Volume controls use source gains until then.", this);
        SetRoom(0);
    }
    private AudioSource Source(string title, GameAudioBus bus)
    {
        var child = new GameObject(title); child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>(); source.playOnAwake = false;
        source.outputAudioMixerGroup = settings.Group(bus); source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = settings.minimumDistance; source.maxDistance = settings.maximumDistance;
        return source;
    }
    private Voice NewVoice(string title) => new Voice { source = Source(title, GameAudioBus.SFX) };
    private AudioSource[] Beds(string title, GameAudioBus bus)
    {
        var sources = new[] { Source(title + " A", bus), Source(title + " B", bus) };
        foreach (var source in sources) { source.loop = true; source.priority = 0; source.spatialBlend = 0f; }
        return sources;
    }
    public float Volume(GameAudioBus bus) => volumes[(int)bus];
    public void SetVolume(GameAudioBus bus, float value)
    {
        volumes[(int)bus] = Mathf.Clamp01(value); GameSave.SetFloat(GameSaveKeys.AudioVolume(bus), volumes[(int)bus]);
        if (ready) ApplyVolumes();
    }
    public void SaveVolumes() => GameSave.Save();
    public void RestoreDefaults()
    {
        for (int i = 0; i < volumes.Length; i++) SetVolume((GameAudioBus)i, settings.DefaultVolume((GameAudioBus)i));
        SaveVolumes();
    }
    private void ApplyVolumes()
    {
        for (int i = 0; i < volumes.Length; i++)
            mixerControls[i] = settings.mixer != null && settings.mixer.SetFloat(Parameters[i], volumes[i] <= 0.0001f ? -80f : 20f * Mathf.Log10(volumes[i]));
        foreach (var voice in transients) RefreshVolume(voice);
        foreach (var voice in protectedVoices) RefreshVolume(voice);
    }
    private float BusGain(GameAudioBus bus)
    {
        // Graceful authoring fallback. Once connected, only the native mixer controls levels.
        bool routed = settings.Group(bus) != null;
        return (routed && mixerControls[0] ? 1f : volumes[0]) * (routed && mixerControls[(int)bus] ? 1f : volumes[(int)bus]);
    }
    private void RefreshVolume(Voice voice)
    { if (voice.cue != null) voice.source.volume = voice.gain * voice.fade * BusGain(voice.cue.bus); }
    public static int Play(GameAudioCue cue, Vector3 position = default, Transform follow = null)
    {
#if UNITY_SERVER
        return 0;
#else
        return Instance != null && Instance.enabled ? Instance.Emit(cue, position, follow) : 0;
#endif
    }
    private int Emit(GameAudioCue id, Vector3 position, Transform follow)
    {
        if (!cues.TryGetValue(id, out var cue) || cue.clips.Length == 0) return 0;
        float now = Time.unscaledTime;
        if (cue.bus == GameAudioBus.UI)
        {
            if (lastUi.TryGetValue(id, out float last) && now - last < cue.minimumInterval) return 0;
            lastUi[id] = now;
        }
        // Do not spend transient voices on inaudible distant actors. Protected cues always finish.
        if (!cue.protect && cue.spatialBlend > 0f && Camera.main != null &&
            (Camera.main.transform.position - position).sqrMagnitude > settings.maximumDistance * settings.maximumDistance) { Rejected++; return 0; }
        var pool = cue.protect ? protectedVoices : transients;
        Voice oldestCue = null, oldestEligible = null, free = null, fading = null;
        int same = 0, active = 0;
        foreach (var voice in pool)
        {
            if (!voice.source.isPlaying) { if (free == null) free = voice; continue; }
            if (voice.releasing)
            {
                if (voice.cue.priority >= cue.priority && (fading == null || voice.sequence < fading.sequence)) fading = voice;
                continue;
            }
            active++;
            if (voice.cue == cue)
            {
                same++;
                if (oldestCue == null || voice.sequence < oldestCue.sequence) oldestCue = voice;
                if (follow != null && voice.follow == follow && now - voice.started < cue.minimumInterval) return 0;
            }
            if (voice.cue.priority >= cue.priority && (oldestEligible == null || voice.cue.priority > oldestEligible.cue.priority ||
                voice.cue.priority == oldestEligible.cue.priority && voice.sequence < oldestEligible.sequence)) oldestEligible = voice;
        }
        if (cue.protect && cue.loop && oldestCue != null && oldestCue.follow == follow) return oldestCue.source.GetInstanceID();
        if (cue.protect)
        {
            // Rare cues never steal one another or truncate on a density spike.
            if (free == null) { free = NewVoice("Protected " + pool.Count); pool.Add(free); }
        }
        else
        {
            Voice victim = same >= cue.maxInstances ? oldestCue : active >= settings.transientVoices ? oldestEligible : null;
            // Reuse an already outgoing tail if an entire burst arrives before the
            // next DSP/update frame. A new important sound should not be dropped
            // merely because the short fade reserve is full.
            if (free == null) free = fading;
            if ((same >= cue.maxInstances || active >= settings.transientVoices) && victim == null || free == null)
            { Rejected++; return 0; }
            if (victim != null) { victim.releasing = true; Replaced++; }
        }
        int index = Random.Range(0, cue.clips.Length);
        if (cue.clips.Length > 1 && lastClip.TryGetValue(id, out int previous) && previous == index) index = (index + 1) % cue.clips.Length;
        lastClip[id] = index;
        var clip = cue.clips[index]; if (clip == null) return 0;
        free.source.Stop(); free.cue = cue; free.follow = follow; free.attached = follow != null;
        free.gain = cue.volume; free.fade = 1f; free.releasing = false; free.started = now; free.sequence = ++sequence;
        var source = free.source; source.clip = clip; source.loop = cue.loop;
        source.outputAudioMixerGroup = settings.Group(cue.bus); source.spatialBlend = cue.spatialBlend;
        source.pitch = Random.Range(Mathf.Max(0.1f, cue.pitch.x), Mathf.Max(0.1f, cue.pitch.y));
        source.priority = cue.protect ? Mathf.Min(24, cue.priority) : cue.priority;
        source.transform.position = position; RefreshVolume(free); source.Play(); Played++;
        cueCounts.TryGetValue(id, out int count); cueCounts[id] = count + 1;
        return source.GetInstanceID();
    }
    public static void Stop(int handle)
    {
        if (Instance == null || handle == 0) return;
        foreach (var voice in Instance.protectedVoices)
            if (voice.source.GetInstanceID() == handle) voice.releasing = true;
    }
    private void Update()
    {
        if (!ready) return;
        Tick(transients); Tick(protectedVoices);
        var director = ProceduralLevelDirector.Instance;
        int number = director != null && director.NetworkObject != null && (director.IsClientInitialized || director.IsServerInitialized) ? director.LevelNumber : 0;
        if (number != room) SetRoom(number);
        FadeBeds(musicSources, musicFades, musicIndex, settings.musicGain, GameAudioBus.Music);
        FadeBeds(ambientSources, ambientFades, ambientIndex, settings.ambientGain, GameAudioBus.Ambient);
    }
    private void Tick(List<Voice> pool)
    {
        foreach (var voice in pool)
        {
            if (!voice.source.isPlaying) continue;
            if (voice.follow != null) voice.source.transform.position = voice.follow.position;
            if (voice.attached && voice.follow == null && voice.source.loop) voice.releasing = true;
            if (voice.releasing)
            {
                voice.fade = Mathf.MoveTowards(voice.fade, 0f, Time.unscaledDeltaTime / settings.releaseSeconds);
                if (voice.fade <= 0f) { voice.source.Stop(); voice.source.clip = null; voice.follow = null; }
            }
            RefreshVolume(voice);
        }
    }
    public void SetRoom(int number)
    {
        room = number;
        SwitchBed(musicSources, settings.musicPlaylist, number, ref musicIndex);
        SwitchBed(ambientSources, settings.ambientPlaylist, number, ref ambientIndex);
    }
    private void SwitchBed(AudioSource[] sources, AudioClip[] playlist, int number, ref int current)
    {
        if (playlist.Length == 0) return;
        var clip = sources == musicSources
            ? number <= 0 && settings.menuMusic != null ? settings.menuMusic : playlist[Mathf.Max(0, number - 1) % playlist.Length]
            : playlist[Mathf.Abs(number) % playlist.Length];
        if (clip == null || sources[current].clip == clip && sources[current].isPlaying) return;
        int next = 1 - current;
        // If a room changes twice within a crossfade, defer until the outgoing bed has faded.
        if (sources[next].isPlaying) return;
        current = next; sources[current].clip = clip; sources[current].volume = 0f; sources[current].Play();
    }
    private void FadeBeds(AudioSource[] sources, float[] fades, int current, float gain, GameAudioBus bus)
    {
        for (int i = 0; i < sources.Length; i++)
        {
            fades[i] = Mathf.MoveTowards(fades[i], i == current ? 1f : 0f, Time.unscaledDeltaTime / settings.crossfadeSeconds);
            sources[i].volume = gain * Mathf.Sin(fades[i] * Mathf.PI * 0.5f) * BusGain(bus);
            if (i != current && fades[i] <= 0f && sources[i].isPlaying) sources[i].Stop();
        }
        // Retry deferred transitions without cutting either bed.
        SwitchBed(sources, bus == GameAudioBus.Music ? settings.musicPlaylist : settings.ambientPlaylist, room, ref current);
        if (bus == GameAudioBus.Music) musicIndex = current; else ambientIndex = current;
    }
    private void OnApplicationPause(bool paused) { if (paused) SaveVolumes(); }
    private void OnDestroy() { if (Instance == this) { SaveVolumes(); Instance = null; } }
}
