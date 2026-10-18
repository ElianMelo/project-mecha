using UnityEngine;

// TransparentFX is included in the normal world camera by default.
// Collider roots retain their gameplay layers; only presentation goes here.
public static class CombatVfxRendering
{
    public const int Layer = 1;
    public static void Apply(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            if (renderer.GetComponent<Collider>() == null) renderer.gameObject.layer = Layer;
    }
}
