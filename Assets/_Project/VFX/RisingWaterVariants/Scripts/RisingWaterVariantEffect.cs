using System;
using System.Collections.Generic;
using UnityEngine;

// One presentation lifecycle for all ten candidates; gameplay supplies pulse callbacks.
public sealed class RisingWaterVariantEffect : CharacterSkillPresentationInstance
{
    [Serializable]
    public struct PulseEmitter
    {
        public ParticleSystem system;
        [Min(1)] public int count;
        [Tooltip("Bit n set: emit on pulse n.")] public int pulseMask;
    }

    [SerializeField] private ParticleSystem[] startupSystems;
    [SerializeField] private PulseEmitter[] pulseEmitters;
    private ParticleSystem[] systems;
    private Animator view;
    private RisingWaterVariantSettings settings;
    private readonly List<Material> ownedMaterials = new List<Material>();
    private float started, duration, expires;
    private int lastPulse = -1;
    private bool ending;
    public int PulseCount { get; private set; }
    public float Elapsed => Time.time - started;
    public bool Ending => ending;
    public readonly List<float> PulseTimes = new List<float>(4);

    private void Awake()
    {
        systems = GetComponentsInChildren<ParticleSystem>(true);
        view = GetComponent<Animator>();
        foreach (var renderer in GetComponentsInChildren<ParticleSystemRenderer>(true))
            ownedMaterials.AddRange(renderer.materials);
    }

    public void Initialize(RisingWaterVariantSettings profile, float actionDuration)
    {
        settings = profile; started = Time.time; duration = actionDuration;
        expires = started + duration + profile.tailSeconds;
        view.Rebind(); view.speed = 1f;
        view.SetFloat("Phase", 0f);
        view.Play("Rise", 0, 0f); view.Update(0f);
        foreach (var system in startupSystems) if (system != null) system.Play(false);
        // Pulse systems have zero automatic emission, but their simulation must run
        // so emitted pieces grow, travel and erode rather than freezing at birth.
        foreach (var emitter in pulseEmitters) if (emitter.system != null) emitter.system.Play(false);
    }

    public override void Pulse(int pulse)
    {
        if (settings == null || ending || pulse <= lastPulse) return;
        lastPulse = pulse; PulseCount++; PulseTimes.Add(Elapsed);
        foreach (var emitter in pulseEmitters)
            if (emitter.system != null && (emitter.pulseMask & (1 << pulse)) != 0)
                emitter.system.Emit(Mathf.Max(1, emitter.count));
    }

    public override void End()
    {
        if (settings == null || ending) return;
        ending = true;
        foreach (var system in systems) if (system != null) system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        if (Elapsed < duration - 0.06f)
        {
            // Interrupted: native Animator erodes every surface through the recovery property.
            view.SetTrigger("Drain");
            expires = Mathf.Min(expires, Time.time + settings.interruptedTail);
        }
    }

    private void Update()
    {
        if (settings == null) return;
        view.SetFloat("Phase", Mathf.Clamp01(Elapsed / Mathf.Max(.01f, duration + settings.tailSeconds)));
        if (Time.time >= expires) Destroy(gameObject);
    }
    private void OnDestroy() { foreach (var material in ownedMaterials) if (material != null) Destroy(material); }
}
