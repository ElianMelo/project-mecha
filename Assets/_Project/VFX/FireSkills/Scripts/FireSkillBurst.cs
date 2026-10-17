using System.Collections;
using UnityEngine;

// Owns the authored one-shot. Native particles/Animator own simulation and light.
public sealed class FireSkillBurst : MonoBehaviour
{
    private static readonly System.Random VisualRandom = new System.Random();
    public void Play(FireSkillSettings settings, Vector3 origin, Vector3 direction, bool final, Transform hand)
    {
        transform.SetPositionAndRotation(origin + Vector3.forward * settings.visualDepth,
            Quaternion.LookRotation(direction, Vector3.forward));
        transform.localScale = Vector3.one * (final ? settings.finalScale : 1f);
        // Keep the first stroke rising when the facing frame reverses its local X axis.
        if (settings.kind == PlayerAttack.ActionKind.CriticalX && direction.x < 0f)
            foreach (Transform arm in transform)
                if (arm.name == "Rising Blast" || arm.name == "Falling Blast")
                {
                    var position = arm.localPosition; position.x = -position.x; arm.localPosition = position;
                    arm.localRotation = Quaternion.Inverse(arm.localRotation);
                }
        gameObject.SetActive(true);
        foreach (var light in GetComponentsInChildren<Light>(true)) light.enabled = true;
        var systems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false;
            ps.randomSeed = (uint)VisualRandom.Next(1, int.MaxValue);
            var noise = ps.noise;
            if (noise.enabled && ps.name.Contains("Flame"))
                noise.frequency *= 1f + ((float)VisualRandom.NextDouble() * 2f - 1f) * settings.motionVariation;
            // Fire cells are authored pointing up. Rotate the billboard into travel.
            if (ps.name.Contains("Flame"))
            {
                var main = ps.main;
                Vector3 axis = ps.transform.forward;
                float angle = (90f - Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                main.startRotation = new ParticleSystem.MinMaxCurve(angle - 0.35f, angle + 0.35f);
            }
            else if (ps.name.Contains("Crescent"))
            {
                var main = ps.main;
                float angle = Mathf.PI - Mathf.Atan2(direction.y, direction.x);
                main.startRotation = new ParticleSystem.MinMaxCurve(angle - 0.18f, angle + 0.18f);
            }
        }
        // Native subemitters spawn only from their parent particles, never at the root.
        foreach (var ps in systems)
            if (!ps.name.StartsWith("Rear Wake")) ps.Play(false);
        if (hand != null && TryGetComponent<HandFlameVfx>(out var jet))
            jet.Bind(hand, origin.z + settings.visualDepth, settings.emissionTime);
        StartCoroutine(Drain(settings.emissionTime));
        Destroy(gameObject, settings.cleanupTime);
    }

    private IEnumerator Drain(float emissionTime)
    {
        yield return new WaitForSeconds(emissionTime);
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            if (ps.emission.rateOverTime.constantMax > 0f)
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    public void StopEmission()
    {
        StopAllCoroutines();
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            if (ps.emission.rateOverTime.constantMax > 0f)
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        if (TryGetComponent<HandFlameVfx>(out var jet)) jet.Release();
    }
}
