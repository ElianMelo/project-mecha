using UnityEngine;

// Unity ParticleSystem owns simulation, fading, billboarding and cleanup.
public static class ParticleBurstEmitter
{
    public static ParticleSystem Emit(ParticleEffectSettings settings, Vector3 position, Vector3 direction)
    {
        if (settings == null || settings.material == null) return null;
        var go = new GameObject(settings.name);
        go.layer = CombatVfxRendering.Layer;
        go.transform.position = position; go.transform.localScale = settings.localScale;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false; main.duration = settings.lifetime.y; main.startLifetime = settings.lifetime.y;
        main.startSpeed = 0f; main.maxParticles = settings.maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = settings.gravity;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var emission = ps.emission; emission.enabled = false;
        var shape = ps.shape; shape.enabled = false;
        var color = ps.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0f),new GradientColorKey(settings.endTint,1f)}, new[]{new GradientAlphaKey(1f,0f),new GradientAlphaKey(0.8f,0.5f),new GradientAlphaKey(0f,1f)});
        color.color = gradient;
        var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f,1f,1f,settings.growth));
        var limit = ps.limitVelocityOverLifetime; limit.enabled = true; limit.drag = settings.drag;
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = settings.angularSpeed != Vector2.zero;
        rotation.z = new ParticleSystem.MinMaxCurve(settings.angularSpeed.x * Mathf.Deg2Rad, settings.angularSpeed.y * Mathf.Deg2Rad);
        var noise = ps.noise; noise.enabled = settings.noiseStrength > 0f;
        noise.strength = settings.noiseStrength; noise.frequency = settings.noiseFrequency;
        noise.quality = ParticleSystemNoiseQuality.Low;
        var trails = ps.trails; trails.enabled = settings.trails;
        trails.lifetime = settings.trailLifetime; trails.widthOverTrail = settings.trailWidth;
        trails.dieWithParticles = true;
        var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = settings.material;
        renderer.renderMode = settings.renderMode; renderer.lengthScale = settings.lengthScale; renderer.velocityScale = settings.velocityScale;
        if (settings.trails) renderer.trailMaterial = settings.material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
        ps.Play();
        Vector3 axis = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.right;
        for (int i = 0; i < settings.count; i++)
        {
            Vector2 jitter = Random.insideUnitCircle * settings.spread;
            Vector3 velocity = (axis + new Vector3(jitter.x,jitter.y,0f)).normalized * Random.Range(settings.speed.x,settings.speed.y);
            var p = new ParticleSystem.EmitParams { position = position + (Vector3)Random.insideUnitCircle * settings.emissionRadius, velocity = velocity,
                startLifetime = Random.Range(settings.lifetime.x,settings.lifetime.y), startSize = Random.Range(settings.size.x,settings.size.y),
                startColor = settings.color, rotation = Mathf.Atan2(velocity.y,velocity.x) * Mathf.Rad2Deg };
            ps.Emit(p,1);
        }
        return ps;
    }
}
