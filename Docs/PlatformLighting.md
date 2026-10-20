# Platform light reception — 2026-10-19

## Current revision: normal-mapped native blocks

The user's next direction replaces the flat platform visuals with shallow 3D blocks using the current sprite art and a surface-response texture. `ScenarioSettings` owns floor/floating block prefabs, visual depth (.6) and front depth (.10). MainScene, ArenaPlatforms, editor setup and procedural rooms use these references. Each visual prefab contains a native Cube body and Quad front with no colliders. Existing gameplay colliders and OneWayPlatform behavior remain independent.

Native URP Lit front materials bind the unchanged IndustrialPlatforms atlas using Sprite.rect texture scale/offset. A copied atlas is imported through Unity's native height-to-normal conversion (Sobel, height .035); it supplies the panel-detail normal map. Metallic/smoothness parameters and dark native metal sides provide light response. This is approximate relief derived from luminance, not reconstructed geometry; baked shading remains in the albedo. No custom shader, renderer migration, package, sword-light tuning or bloom change was needed. The original atlas/importer remain unchanged. New assets live under `Assets/_Project/VFX/SceneSupport/PlatformBlocks`, with Replace labels and inherited AI Generated provenance for the derived map.

Compilation succeeded. A disposable local Play Mode preview used the real procedural builder and actual equipped sword lights. The room contained 17 mesh fronts, zero enabled platform sprites and zero visual-prefab colliders. Advancing frames/time were observed. Same-frame light on/off comparisons in a 21,080-pixel floor crop showed Dasher idle affecting 886 pixels (maximum channel delta 52), Zero idle affecting 1,639 pixels, and Zero's actual Slash1 light at intensity 2.5/action time .134 affecting 3,705 pixels (maximum delta 42). Disabling normal mapping on owned runtime material copies changed 10,232 floor pixels in Zero's idle frame. Both actual textures and localized light patches were visually inspected.

Initial cyan captures were Unity's temporary shader-loading fallback and are invalid evidence. Native material passes were compiled before valid captures. Dasher's warmed strike capture missed the short peak and is not peak-strike evidence; the valid Zero capture includes the native light envelope. Captures: ignored `Logs/PlatformBlocks`; authoring/observation helpers: `AgentScripts/PlatformBlocksAuthoring.cs` and `PlatformBlocksPreview.cs`. Editor restored to stopped, clean MainScene. No automated tests, extra clients or builds. Final appearance awaits user review.

The historical sprite-only fix below remains the fallback when block prefab references are absent; its claim that no normal map was needed describes that earlier implementation only.

## Earlier sprite-only implementation (superseded visually)

The user requested light-reactive platforms after sharing Unity's [Bunny Blitz sample article](https://discussions.unity.com/t/bunny-blitz-2d-and-3d-sample-project-available-now/1731205). The article and [Unity 6.3 compatibility manual](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/2d-renderer-urp-shader-compatibility.html) were studied; the Asset Store sample package was not imported.

Bunny Blitz uses the 2D Renderer, 2D lights and compatible sprite/mesh shaders. This implementation adapts the lit-receiver principle to the project's existing Universal Renderer, toon mechas and native 3D effect lights. It is not a migration to Light2D or the sample's mesh shaders.

## Concrete defect and native solution

Authored platform faces already used IndustrialThermalReceiver (native URP/Simple Lit), but ProceduralRoomBuilder added SpriteRenderers without assigning a material. Generated floors and ledges therefore reverted to default unlit sprites. Prior authored-scene lighting evidence did not cover generated rooms.

ScenarioSettings.platformMaterial now owns the shared receiver reference. The runtime room builder and editor scenario setup consume it; AuthorCombatSceneVfx.Build persists it. Runtime generation does not change shared material properties. The existing atlas, sprite UVs/tiling, alpha clipping, baseline emission, depth, no-shadow behavior and native collider/OneWayPlatform setup remain unchanged. No new shader, renderer feature, normal map, lighting simulation or effect prefab was needed. Background/UI, bloom/exposure and light tuning are unchanged.

## Observed validation

Project recompile completed successfully and editor authoring compiled without diagnostics. A disposable local Play Mode preview invoked the real ProceduralRoomBuilder; all 17 first-room faces referenced the shared lit material.

Same-frame renders toggled only an existing DamageSparks point light: intensity 4.2, range 1.45, position (0,-3.45,-.40). In the 16,864-pixel platform crop, the old default sprite material showed zero changed pixels; the configured native lit material showed 2,605 pixels changing by more than one channel level, maximum difference 103. The localized amber patch was visually inspected. Comparison renders intentionally shared a simulation frame; an independent read confirmed progression from frame 2/time .02 to frame 862/time 14.95 and zero remaining preview effect roots.

Raw captures/receipts: ignored Logs/PlatformLighting. Operator helper: AgentScripts/PlatformLightingRevision.cs. MainScene restored clean in stopped Edit Mode. No automated tests, builds or network sessions. This establishes local light reception, not final art approval or a performance benchmark.
