using System.Collections;
using UnityEngine;

// Replays the actual shared S timeline without actors, damage or a network session.
public sealed class RisingWaterVariantPreview : MonoBehaviour
{
    [SerializeField] private RisingWaterVariantSettings settings;
    private RisingWaterVariantEffect effect;
    public RisingWaterVariantEffect Effect => effect;

    private void OnEnable()
    {
        if (!Application.isPlaying || settings == null || settings.previewAction == null) return;
        var action = settings.previewAction.action;
        effect = settings.SpawnAt(transform.position, transform.rotation, action.duration);
        if (effect == null) return;
        effect.transform.SetParent(transform, true);
        StartCoroutine(Sequence(action));
    }

    private IEnumerator Sequence(PlayerCombatSettings.ActionData action)
    {
        float started = Time.time;
        for (int pulse = 0; pulse < action.pulses; pulse++)
        {
            float target = action.impact + pulse * action.pulseInterval;
            while (Time.time - started < target) yield return null;
            if (effect != null) effect.Pulse(pulse);
        }
        while (Time.time - started < action.duration) yield return null;
        if (effect != null) effect.End();
    }

    private void OnDisable() { StopAllCoroutines(); if (effect != null) effect.End(); }
}
