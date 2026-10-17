using System.Collections;
using UnityEngine;

// Owns the authored one-shot. Native particles/Animator own simulation and light.
public sealed class FireSkillBurst : MonoBehaviour
{
    public void Play(FireSkillSettings settings, Vector3 origin, Vector3 direction, bool final, Transform hand)
    {
        transform.SetPositionAndRotation(origin + Vector3.forward * settings.visualDepth,
            Quaternion.LookRotation(direction, Vector3.forward));
        transform.localScale = Vector3.one * (final ? settings.finalScale : 1f);
        gameObject.SetActive(true);
        foreach (var light in GetComponentsInChildren<Light>(true)) light.enabled = final;
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            // Fire cells are authored pointing up. Rotate the billboard into travel.
            if (ps.name.Contains("Flame"))
            {
                var main = ps.main;
                float angle = (90f - Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                main.startRotation = new ParticleSystem.MinMaxCurve(angle - 0.35f, angle + 0.35f);
            }
            else if (ps.name.Contains("Cut Residue"))
            {
                var main = ps.main;
                main.startRotation = -Mathf.Atan2(direction.y,direction.x);
            }
            ps.Play(false);
        }
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
