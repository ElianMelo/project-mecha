using UnityEngine;

public class CharacterHitFeedback : MonoBehaviour, IHitFeedback
{
    [SerializeField] private CombatVfxSettings settings;
    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private float until;
    private Color tint;
    private bool emissiveFlash;
    private bool applied;
    private void Awake() { renderers = GetComponentsInChildren<Renderer>(true); block = new MaterialPropertyBlock(); }
    public void Hit(float damage, Vector3 incomingDirection)
    {
        // Crates have their own material-specific cue.
        if (GetComponent<DestructibleProp>() == null)
            GameAudio.Play(GetComponent<PlayerHealth>() != null ? GameAudioCue.PlayerHit : GameAudioCue.Hit, transform.position, transform);
        Flash(settings.flashColor, settings.flashDuration, true);
        Vector3 position = transform.position + settings.hitOffset;
        // Follow the incoming impulse, away from the attacker.
        Vector3 direction = new Vector3(Mathf.Sign(incomingDirection.x), settings.hitSprayLift, 0f);
        ParticleBurstEmitter.Emit(settings.hitSparks, position, direction);
        ParticleBurstEmitter.Emit(settings.hitSmoke, position, direction);
        ParticleBurstEmitter.Emit(settings.hitCore, position, direction);
        ParticleBurstEmitter.Emit(settings.hitDebris, position, direction);
        if (damage > 0f) CombatEffect.DamageNumber(position + settings.damageTextOffset, damage, settings);
    }
    public void Telegraph() => Flash(settings.telegraphColor, settings.telegraphDuration);
    private void Flash(Color color, float duration, bool emissive = false) { tint = color; until = Time.time + duration; emissiveFlash = emissive; }
    private void LateUpdate()
    {
        if (settings == null) return;
        bool active=Time.time<until;
        if(!active && !applied)return;
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            if (!active) { renderer.SetPropertyBlock(null); continue; }
            block.Clear();
            block.SetColor("_BaseColor", tint); block.SetColor("_Color", tint);
            block.SetColor("_1st_ShadeColor", tint); block.SetColor("_2nd_ShadeColor", tint);
            block.SetColor("_Emissive_Color", emissiveFlash ? tint : Color.black);
            renderer.SetPropertyBlock(block);
        }
        applied=active;
    }
}
