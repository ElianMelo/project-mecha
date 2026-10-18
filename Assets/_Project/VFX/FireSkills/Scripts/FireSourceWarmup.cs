using UnityEngine;

// Source preparation follows the same live emission binding as the main fire.
public sealed class FireSourceWarmup : MonoBehaviour
{
    private void Awake() => CombatVfxRendering.Apply(gameObject);
    private FireSkillSettings settings;
    private CombatEmissionOrigin source;
    private PlayerCombatSettings.ActionData action;
    private int facing;
    public void Play(FireSkillSettings profile, CombatEmissionOrigin emission,
        PlayerCombatSettings.ActionData cast, int direction)
    {
        settings = profile; source = emission; action = cast; facing = direction;
        if (source == null) { Destroy(gameObject); return; }
        FollowSource();
        transform.localScale = Vector3.one * settings.warmupScale;
        gameObject.SetActive(true);
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true)) ps.Play(false);
        Destroy(gameObject, 0.50f);
    }
    private void LateUpdate()
    {
        if (source == null) { Destroy(gameObject); return; }
        FollowSource();
    }
    private void FollowSource()
    {
        var point = source.Resolve(action, facing, 0) +
            new Vector3(facing * settings.warmupOffset.x, settings.warmupOffset.y, 0f);
        point.z += settings.visualDepth - 0.05f;
        float angle = settings.facingAngle * Mathf.Deg2Rad;
        var direction = settings.alignToFacing ?
            new Vector3(facing * Mathf.Cos(angle), Mathf.Sin(angle), 0f) :
            CombatEmissionOrigin.Direction(action, facing, 0);
        transform.SetPositionAndRotation(point, Quaternion.LookRotation(direction, Vector3.forward));
    }
}
