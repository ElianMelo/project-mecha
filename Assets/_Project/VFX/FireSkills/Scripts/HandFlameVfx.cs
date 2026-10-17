using UnityEngine;

// Follows the animated hand only while feeding the jet. World-space tails remain.
public sealed class HandFlameVfx : MonoBehaviour
{
    private Transform hand;
    private float until, depth;
    public void Bind(Transform source, float visualZ, float emissionTime)
    { hand = source; depth = visualZ; until = Time.time + emissionTime; }
    public void Release() => hand = null;
    private void LateUpdate()
    {
        if (hand == null || Time.time >= until) return;
        var point = hand.position; point.z = depth; transform.position = point;
    }
}
