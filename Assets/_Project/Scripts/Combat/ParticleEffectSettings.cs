using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Presentation/Particle Burst")]
public class ParticleEffectSettings : ScriptableObject
{
    public Material material;
    public int count = 12, maxParticles = 80;
    public Vector2 lifetime = new Vector2(0.12f, 0.35f), speed = new Vector2(1.5f, 4f), size = new Vector2(0.1f, 0.3f);
    public Color color = Color.white;
    public float spread = 0.6f, gravity, growth = 1.4f, emissionRadius = 0.08f, drag = 2f;
    public ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard;
    public float lengthScale = 1f, velocityScale = 0.2f;
    public Vector3 localScale = Vector3.one;
    public Color endTint = Color.white;
    public Vector2 angularSpeed;
    public float noiseStrength, noiseFrequency = 1.5f;
    public bool trails;
    public float trailLifetime = 0.08f, trailWidth = 0.35f;
}
