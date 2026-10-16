using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Presentation/Combat Effects")]
public class CombatVfxSettings : ScriptableObject
{
    public SwordFireVfxSettings swordFire;
    public SwordFireVfxSettings megaSlashFire, criticalXFire;
    public ParticleEffectSettings dashSmoke, wind, hitSparks, hitSmoke, skillWind;
    public ParticleEffectSettings dashStreaks, dashDebris, slashEdges, hitCore, hitDebris, landingSmoke;
    public float landingSpeedThreshold = 4f;
    public Vector3 dashOffset = new Vector3(0f, 0.08f, -0.15f), strikeOffset = new Vector3(0.7f, 0.85f, -0.35f);
    public Vector3 hitOffset = new Vector3(0f,0.8f,-0.35f), damageTextOffset = Vector3.up * 0.5f;
    public float hitSprayLift = 0.25f;
    public float dashEmissionInterval = 0.045f, flashDuration = 0.1f, telegraphDuration = 0.35f;
    public Color flashColor = new Color(2f, 2f, 2f), telegraphColor = new Color(1.4f, 0.3f, 0.05f);
    public float damageNumberLifetime = 0.65f, damageNumberSpeed = 1.2f, damageNumberSize = 4f;
    public Color damageNumberColor = new Color(1f, 0.85f, 0.35f);
}
