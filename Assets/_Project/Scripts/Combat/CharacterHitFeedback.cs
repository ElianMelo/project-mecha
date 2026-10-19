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
        Vector3 direction = incomingDirection.sqrMagnitude > .0001f ? incomingDirection.normalized : Vector3.up;
        Vector3 position = transform.position + settings.hitOffset;
        if(settings.damageSparkPrefab != null)
        {
            var body=GetComponent<Collider>();
            if(body != null)
            {
                position=body.ClosestPoint(body.bounds.center-direction*(body.bounds.extents.magnitude+.1f));
                // Project a cosmetic contact onto the visible face in the X/Y combat view.
                // Physics, damage and the receiving collider retain their original depth.
                position.z=body.bounds.min.z+Mathf.Min(-.025f,settings.hitOffset.z);
            }
            // Rebound from the receiving face, preserving vertical/diagonal incoming hit direction.
            Instantiate(settings.damageSparkPrefab,position,Quaternion.LookRotation(-direction,Mathf.Abs(direction.z)<.95f?Vector3.forward:Vector3.up));
        }
        else
        {
            ParticleBurstEmitter.Emit(settings.hitSparks, position, direction);
            ParticleBurstEmitter.Emit(settings.hitSmoke, position, direction);
            ParticleBurstEmitter.Emit(settings.hitCore, position, direction);
            ParticleBurstEmitter.Emit(settings.hitDebris, position, direction);
        }
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
