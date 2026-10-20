# Industrial platform blocks

User revision: retain the current industrial platform art on shallow 3D blocks, with surface detail reacting to the existing glowing sword. Keep the current Universal Renderer and toon characters.

- FloorBlock/FloatingBlock: native Cube metal body plus Quad textured front; no visual colliders, no shadow casting. Existing room colliders own gameplay.
- Front materials: native URP Lit; unchanged source atlas cropped by Sprite.rect texture scale/offset; alpha cutoff .1; metallic .25; smoothness .32; normal scale .75; restrained atlas emission .18. Side material: dark metal, metallic .5 and smoothness .32.
- IndustrialPlatformNormals.png: unchanged copy of the existing AI-generated atlas with a new GUID, imported as a native NormalMap with height conversion, Sobel filter, height .035, linear and uncompressed. Unity produces the actual normal texture at import; raw source bytes remain atlas colors. This approximate luminance-based relief cannot recover true surface geometry or remove existing baked shading.
- Scenario asset: shared floor/floating prefab references; depth .6 and front depth .10. The front stays just behind the combat plane. Both authored and generated rooms use these references. Original atlas/importer preserved.
- Native asset labels: Replace, category labels, and inherited AI Generated provenance for the derived map. No finished vendor effect is used.

Manual validation used the actual generated room and equipped sword lights. Seventeen mesh faces, zero enabled platform sprites and zero visual colliders. Dasher idle light affected 886 pixels; Zero idle affected 1,639; Zero Slash1 intensity 2.5 at .134 seconds affected 3,705 pixels in a 21,080-pixel floor crop. With normal mapping disabled on owned runtime materials, 10,232 pixels changed, demonstrating surface response. Initial cyan shader-loading captures are invalid; native shader passes were warmed before the valid comparisons. No light-profile tuning was changed. Raw captures: ignored Logs/PlatformBlocks. No automated tests or extra clients/builds. Stopped, clean editor scene restored. Technical validation is not final visual approval.

Reauthor through AgentScripts/PlatformBlocksAuthoring.cs in stopped Edit Mode with a clean MainScene; assets/prefabs/materials/importers are modified through Unity APIs. See Docs/PlatformLighting.md for the reference adaptation and prior sprite fallback.
