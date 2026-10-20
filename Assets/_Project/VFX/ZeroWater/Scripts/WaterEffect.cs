using UnityEngine;

// Native systems own particles and light envelopes. This component owns lifetime/attachment.
public sealed class WaterEffect : MonoBehaviour
{
    [SerializeField] private Animator sustainedLighting;
    private Transform follow;
    private CharacterModifiers mode;
    private float expires, started;
    private void Awake() => CombatVfxRendering.Apply(gameObject);
    public void Play(float lifetime, Transform anchor = null, CharacterModifiers modifier = null)
    { expires = Time.time + lifetime; started = Time.time; follow = anchor; mode = modifier; GetComponent<VfxSurfaceDepth>()?.Place(); }
    private void LateUpdate()
    {
        if (follow != null) { transform.position = follow.position; GetComponent<VfxSurfaceDepth>()?.Place(); }
        if (Time.time >= expires || mode != null && Time.time - started > 0.5f && !mode.Active)
        {
            foreach (var ps in GetComponentsInChildren<ParticleSystem>()) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (sustainedLighting != null) sustainedLighting.SetTrigger("Release");
            Destroy(gameObject, 1.5f); enabled = false;
        }
    }
}
