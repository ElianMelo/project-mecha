using UnityEngine;
using UnityEngine.VFX;

public static class SwordFireVfxEmitter
{
    public static GameObject Emit(SwordFireVfxSettings settings, Vector3 position, int facing, int pulse = 0)
    {
        if (settings == null || settings.prefab == null || !SystemInfo.supportsComputeShaders) return null;
        var camera = Camera.main;
        if (camera != null && (camera.transform.position - position).sqrMagnitude > settings.maximumViewDistance * settings.maximumViewDistance) return null;
        // The authored prefab is inactive so all overrides are set before its OnPlay burst.
        var rotation = Quaternion.Euler(0f, facing < 0 ? 180f : 0f, 0f)
            * Quaternion.AngleAxis(settings.firstPulseAngle + pulse * settings.pulseAngleStep, Vector3.forward);
        var instance = Object.Instantiate(settings.prefab, position, rotation);
        instance.name = settings.prefab.name; instance.transform.localScale = settings.scale;
        foreach (var effect in instance.GetComponentsInChildren<VisualEffect>(true))
        {
            if (effect.HasFloat("FlameCount")) effect.SetFloat("FlameCount", Mathf.Clamp(settings.flameCount, 1, 128));
            if (effect.HasFloat("EmberCount")) effect.SetFloat("EmberCount", Mathf.Clamp(settings.emberCount, 1, 128));
            if (effect.HasGradient("FlameGradient")) effect.SetGradient("FlameGradient", settings.flameColors);
            if (effect.HasGradient("EmberGradient")) effect.SetGradient("EmberGradient", settings.emberColors);
        }
        instance.SetActive(true);
        Object.Destroy(instance, settings.lifetime);
        return instance;
    }
}
