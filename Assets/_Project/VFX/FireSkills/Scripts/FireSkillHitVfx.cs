using UnityEngine;

// Contact presentation only. Damage has already been confirmed by combat authority.
public sealed class FireSkillHitVfx : MonoBehaviour
{
    public void Play(Vector3 point, Vector3 incoming, Vector3 normal, bool final, float depth)
    {
        if (normal.sqrMagnitude < 0.01f) normal = -incoming;
        point += normal.normalized * 0.025f;
        point.z += depth;
        transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal.normalized, Vector3.forward));
        transform.localScale = Vector3.one * (final ? 1.15f : 0.8f);
        gameObject.SetActive(true);
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            // The penetration spray follows the incoming shot; contact flare faces outward.
            if (ps.name.Contains("Sparks")) ps.transform.rotation = Quaternion.LookRotation(incoming, Vector3.forward);
            ps.Play(false);
        }
        Destroy(gameObject, 1.2f);
    }
}
