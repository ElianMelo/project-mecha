using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public sealed class VfxPreviewPlayer : MonoBehaviour
{
    [SerializeField] private Transform origin;
    private GameObject instance;
    private Coroutine lifetime;
    public bool IsPlaying => instance != null;
    public int PlaybackCount { get; private set; }
    public event Action Changed;

    public void Play(VfxPreviewDefinition definition)
    {
        StopPreview();
        if (definition == null || definition.prefab == null) return;

        // Configure an inactive parent before OnEnable/play-on-awake scripts run.
        instance = new GameObject("VFX preview instance");
        instance.SetActive(false);
        instance.transform.SetParent(origin != null ? origin : transform, false);
        instance.transform.localPosition = definition.position;
        instance.transform.localRotation = Quaternion.Euler(definition.eulerAngles);
        instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, definition.scale);
        var effect = Instantiate(definition.prefab, instance.transform, false);
        effect.SetActive(true);
        instance.SetActive(true);

        var particles = instance.GetComponentsInChildren<ParticleSystem>();
        var subemitters = new HashSet<ParticleSystem>();
        foreach (var particle in particles)
        {
            var module = particle.subEmitters;
            for (int i = 0; i < module.subEmittersCount; i++)
                subemitters.Add(module.GetSubEmitterSystem(i));
        }
        foreach (var particle in particles)
            if (!subemitters.Contains(particle)) particle.Play(false);
        foreach (var graph in instance.GetComponentsInChildren<VisualEffect>())
        {
            // Reinit sends the graph's initial event; a second Play would duplicate it.
            graph.Reinit();
        }

        PlaybackCount++;
        lifetime = StartCoroutine(Finish(definition.playSeconds, definition.tailSeconds));
        Changed?.Invoke();
    }

    private IEnumerator Finish(float playSeconds, float tailSeconds)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, playSeconds));
        if (instance != null)
        {
            foreach (var particle in instance.GetComponentsInChildren<ParticleSystem>())
                particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            foreach (var graph in instance.GetComponentsInChildren<VisualEffect>()) graph.Stop();
        }
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, tailSeconds));
        lifetime = null;
        ReleaseInstance();
        Changed?.Invoke();
    }

    public void StopPreview()
    {
        if (lifetime != null) { StopCoroutine(lifetime); lifetime = null; }
        ReleaseInstance();
        Changed?.Invoke();
    }

    private void ReleaseInstance()
    {
        if (instance == null) return;
        instance.SetActive(false);
        Destroy(instance);
        instance = null;
    }

    private void OnDisable() => StopPreview();
}
