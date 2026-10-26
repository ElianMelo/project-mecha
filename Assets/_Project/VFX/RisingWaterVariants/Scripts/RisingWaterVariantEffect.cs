using System.Collections.Generic;
using UnityEngine;

// One presentation lifecycle for all ten candidates; gameplay supplies pulse callbacks.
public sealed class RisingWaterVariantEffect : CharacterSkillPresentationInstance
{
    [SerializeField] private ParticleSystem[] startupSystems;
    [SerializeField] private ParticleSystem[] pulseSystems;
    [SerializeField] private ParticleSystem[] pulseMeshSystems;
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
        // These systems have zero automatic emission, but their simulation must run
        // so emitted droplets travel and fade rather than remaining frozen at birth.
        foreach (var system in pulseSystems) if (system != null) system.Play(false);
        foreach (var system in pulseMeshSystems) if (system != null) system.Play(false);
    }

    public override void Pulse(int pulse)
    {
        if (settings == null || ending || pulse <= lastPulse) return;
        lastPulse = pulse; PulseCount++; PulseTimes.Add(Elapsed);
        foreach (var system in pulseMeshSystems)
            if (system != null) system.Emit(1);
        foreach (var system in pulseSystems)
            if (system != null) system.Emit(settings.dropletsPerPulse);
    }

    public override void End()
    {
        if (settings == null || ending) return;
        ending = true;
        foreach (var system in systems) if (system != null) system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        if (Elapsed < duration - 0.06f)
        {
            view.SetTrigger("Drain");
            expires = Mathf.Min(expires, Time.time + settings.interruptedTail);
        }
    }

    private void Update()
    {
        if (settings == null) return;
        // Native Animator evaluates an authored clip against the same clock as the pulses.
        // A long instantiate/editor frame must not move the animation ahead of that clock.
        view.SetFloat("Phase", Mathf.Clamp01(Elapsed / Mathf.Max(.01f, duration + settings.tailSeconds)));
        if (Time.time >= expires) Destroy(gameObject);
    }
    private void OnDestroy() { foreach (var material in ownedMaterials) if (material != null) Destroy(material); }
}
