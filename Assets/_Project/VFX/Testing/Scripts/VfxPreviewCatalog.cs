using UnityEngine;

// Explicit registration records effects created or adapted with VFX Creator.
// Existing project and vendor effects are not automatically included.
[CreateAssetMenu(menuName = "Mecha/VFX/Preview catalog")]
public sealed class VfxPreviewCatalog : ScriptableObject
{
    public VfxPreviewDefinition[] effects = System.Array.Empty<VfxPreviewDefinition>();
}
