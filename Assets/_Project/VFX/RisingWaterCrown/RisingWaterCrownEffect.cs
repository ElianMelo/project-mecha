using System.Collections.Generic;
using UnityEngine;

// Only owns this cast's particles, native animation and cleanup. Never follows its caster.
public sealed class RisingWaterCrownEffect : CharacterSkillPresentationInstance
{
    [SerializeField] private ParticleSystem[] startupSystems;
    [SerializeField] private ParticleSystem[] pressureSpray;
    [SerializeField] private ParticleSystem surfaceSpray;
    private RisingWaterCrownSettings settings;
    private ParticleSystem[] systems;
    private Animator animationView;
    private readonly List<Material> ownedMaterials = new List<Material>();
    private float started, destroyAt, actionDuration;
    private bool ending;
    private int lastPulse = -1;
    public int PulseCount { get; private set; }
    public float Elapsed => Time.time - started;
    public bool SurfaceActive { get; private set; }

    private void Awake()
    {
        systems = GetComponentsInChildren<ParticleSystem>(true);
        animationView = GetComponent<Animator>();
        // Renderer material curves animate private instances, never authored shared materials.
        foreach (var renderer in GetComponentsInChildren<ParticleSystemRenderer>(true))
            ownedMaterials.AddRange(renderer.materials);
    }

    public void Initialize(RisingWaterCrownSettings profile, float duration)
    {
        settings = profile;
        started = Time.time;
        actionDuration = duration;
        destroyAt = started + profile.lifetime;
        animationView.Rebind();
        animationView.Play("Rise", 0, 0f);
        animationView.Update(0f);
        foreach (var system in startupSystems) if (system != null) system.Play(false);
        SurfaceActive = Physics.Raycast(transform.position + Vector3.up * .08f, Vector3.down,
            profile.surfaceProbeDistance + .08f, profile.surfaceLayers, QueryTriggerInteraction.Ignore);
        if (SurfaceActive && surfaceSpray != null) surfaceSpray.Play(false);
    }

    public override void Pulse(int pulse)
    {
        if (ending || settings == null || pulse <= lastPulse) return;
        lastPulse = pulse;
        PulseCount++;
        foreach (var system in pressureSpray)
            if (system != null) system.Emit(settings.dropletsPerPulse);
    }

    public override void End()
    {
        if (ending || settings == null) return;
        ending = true;
        foreach (var system in systems) if (system != null) system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        if (Elapsed < actionDuration - .06f)
        {
            animationView.SetTrigger("Drain");
            destroyAt = Mathf.Min(destroyAt, Time.time + settings.interruptedTail);
        }
    }

    private void Update()
    {
        if (settings != null && Time.time >= destroyAt) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        foreach (var material in ownedMaterials) if (material != null) Destroy(material);
    }
}
