using UnityEngine;

// Native animated sockets define the blade, shared by combat and presentation.
public sealed class MeleeBlade : MonoBehaviour
{
    public Transform edgeBase, edgeTip;
    public MeleeBladeSettings settings;
    public bool IsReady => isActiveAndEnabled && edgeBase != null && edgeTip != null && settings != null;
    public void Read(out Vector3 start, out Vector3 end, float combatDepth)
    {
        start=edgeBase.position;end=edgeTip.position;
        // Combat is 2.5D. Preserve the visible X/Y blade path, query at physics depth.
        start.z=end.z=combatDepth;
    }
}
