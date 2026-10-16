using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class PlayerLedgeAssist : MonoBehaviour
{
    private Rigidbody body;
    private CapsuleCollider capsule;
    private float nextAssist;
    private readonly Collider[] blockers = new Collider[16];
    public int AssistedLandings { get; private set; }
    private void Awake() { body = GetComponent<Rigidbody>(); capsule = GetComponent<CapsuleCollider>(); }

    public bool TryAssist(PlayerMovementSettings settings, PlayerInputs inputs)
    {
        if (inputs.DownHeld || Time.time < nextAssist || body.isKinematic || body.linearVelocity.y > settings.vaultMaxRiseSpeed) return false;
        var bounds = capsule.bounds;
        OneWayPlatform best = null; Vector3 correction = Vector3.zero;
        float nearest = float.MaxValue;
        foreach (var platform in OneWayPlatform.ActivePlatforms)
        {
            if (platform == null || platform.Surface == null || !platform.Surface.enabled || platform.IsDropping(capsule)) continue;
            var deck = platform.Surface.bounds;
            float lift = deck.max.y - bounds.min.y;
            if (lift <= 0.025f || lift > settings.vaultHeight || bounds.max.z < deck.min.z || bounds.min.z > deck.max.z) continue;
            float inset = Mathf.Min(bounds.extents.x * 0.65f, deck.extents.x * 0.5f);
            float x = Mathf.Clamp(bounds.center.x, deck.min.x + inset, deck.max.x - inset);
            float shift = x - bounds.center.x;
            if (Mathf.Abs(shift) > settings.vaultReach ||
                Mathf.Abs(shift) > 0.04f && inputs.MoveInput.x * shift <= 0f) continue;
            Vector3 delta = new Vector3(shift, lift + 0.025f, 0f);
            if (!HasClearance(bounds, delta, settings.groundLayers)) continue;
            float score = delta.sqrMagnitude;
            if (score >= nearest) continue;
            best = platform; correction = delta; nearest = score;
        }
        if (best == null) return false;
        body.position += correction;
        body.linearVelocity = new Vector3(body.linearVelocity.x, 0f, 0f);
        Physics.SyncTransforms(); best.ConfirmSupport(capsule);
        nextAssist = Time.time + settings.vaultCooldown; AssistedLandings++;
        return true;
    }

    private bool HasClearance(Bounds bounds, Vector3 delta, int layers)
    {
        float radius = bounds.extents.x * 0.9f;
        Vector3 center = bounds.center + delta;
        Vector3 up = Vector3.up * Mathf.Max(0f, bounds.extents.y - radius);
        int count = Physics.OverlapCapsuleNonAlloc(center - up, center + up, radius, blockers, layers | 1, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (blockers[i] != capsule && blockers[i].GetComponent<OneWayPlatform>() == null) return false;
        return true;
    }
}
