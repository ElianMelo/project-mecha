using System.Collections.Generic;
using UnityEngine;

// Bias only cosmetic emitter branches. Collision, lights and gameplay stay at
// their actual world positions and use the normal scene rendering/depth pass.
public sealed class VfxSurfaceDepth : MonoBehaviour
{
    [SerializeField] private VfxSurfaceDepthSettings settings;
    private readonly List<Transform> visuals = new List<Transform>();
    private readonly List<Vector3> positions = new List<Vector3>();
    private readonly Collider[] surfaces = new Collider[32];
    private bool cached;

    public void Place()
    {
        if (settings == null) return;
        if (!cached) Cache();
        float offset = 0f;
        // This project's physics plane is XY; the camera views it from negative Z.
        int count = Physics.OverlapBoxNonAlloc(transform.position,
            new Vector3(settings.probeRadius, settings.probeRadius, settings.maximumShift),
            surfaces, Quaternion.identity, settings.platformLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            float shift = surfaces[i].bounds.min.z - settings.surfaceClearance - transform.position.z;
            if (shift >= -settings.maximumShift) offset = Mathf.Min(offset, shift);
            surfaces[i] = null;
        }
        for (int i = 0; i < visuals.Count; i++)
            if (visuals[i] != null)
                visuals[i].localPosition = positions[i] + visuals[i].parent.InverseTransformVector(Vector3.forward * offset);
    }

    private void Cache()
    {
        cached = true;
        foreach (var system in GetComponentsInChildren<ParticleSystem>(true))
        {
            if (system.transform == transform) continue;
            bool nested = false;
            for (var parent = system.transform.parent; parent != transform && parent != null; parent = parent.parent)
                if (parent.GetComponent<ParticleSystem>() != null) { nested = true; break; }
            if (nested || HasInteraction(system.transform)) continue;
            visuals.Add(system.transform); positions.Add(system.transform.localPosition);
        }
    }

    private static bool HasInteraction(Transform branch)
    {
        if (branch.GetComponentInChildren<Collider>(true) != null ||
            branch.GetComponentInChildren<Rigidbody>(true) != null ||
            branch.GetComponentInChildren<Light>(true) != null) return true;
        foreach (var system in branch.GetComponentsInChildren<ParticleSystem>(true))
            if (system.collision.enabled || system.trigger.enabled || system.lights.enabled) return true;
        return false;
    }
}
