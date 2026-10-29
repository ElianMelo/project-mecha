---
name: vfx-creator
description: Create, adapt, or visually refine Unity VFX using supplied anime references, project effects, and installed packs; register authored effects in the VFX testing browser and improve this workflow from observed results. Exclude unrelated gameplay logic and general skill management.
---

# VFX Creator

Create effects whose visual language comes from the user's images. Read the [visual guide and seven references](references/visual-guide.md) before choosing the treatment. Follow [project architecture and validation rules](../../../AGENTS.md) for implementation.

## Establish the visual target

- Use the current request and its supplied references first, then the seven bundled images as the default anime direction. Relevant project effects can supply construction, integration, or explicitly requested visual references; their appearance does not silently become the target.
- Describe the useful qualities of the selected references: dominant silhouette, value/color regions, edge treatment, glow, supporting shapes, and force direction. Choose the palette and shape vocabulary for this effect. Avoid a fixed decade, mechanical explanation, universal palette, or repeated effect recipe.
- Inspect the gameplay trigger, normal camera distance, background, source/contact frame, footprint, attachment, and start/stop behavior. Distinguish a requested visual revision from a change to gameplay reach or timing.
- Images establish appearance, not measured animation. Study motion in the installed effects and choose timing for the action; do not invent frame counts or durations supposedly demonstrated by a still image.

## Find and adapt installed effects

Inspect relevant effects under `Assets/_Project/VFX` and discover locally installed packs. Available starting locations include:

| Library | Location |
| --- | --- |
| GabrielAguiarProductions | `Assets/GabrielAguiarProductions` |
| Hovl Studio | `Assets/Hovl Studio` |
| Cartoon FX | `Assets/JMO Assets` |
| PixPlays elemental effects | `Assets/PixPlays` |
| Free Slash VFX | `Assets/Free Slash VFX` |

This is a starting list, not an exclusive supplier list. Inspect actual local assets and dependencies; source catalogs can help discovery but do not prove availability or compatibility.

- Reuse textures/masks, materials, shaders, meshes, flipbooks, UV animation, trails, particle shapes, renderer arrangements, curves, subemitters, and motion patterns where they serve the target.
- Complete pack effects, prefabs, and subeffects are allowed starting points. Study their actual hierarchy, simulation, materials, timing, and shutdown. Preserve vendor originals; adapt project-owned copies or prefab variants with compatible dependencies.
- Check the active render pipeline, shader support, material properties, blend/depth behavior, mesh orientation, simulation space, and required scripts before reuse. Retain only the layers and dependencies the result needs.
- Match the reference through shape, layering, value separation, movement, and timing as well as color. A recolored stock effect is sufficient only if its resulting appearance and behavior actually meet the request.
- Preserve source lineage and native provenance/category/Reference/VFXSource labels. These source-role labels describe assets; they do not prohibit adapting a complete effect. Do not infer artistic approval or authorship from a label.

## Plan phases and construction

The user confirmed this approach as the direction for anime water on 2026-10-28; treat it as the baseline and iterate from it.

Before building, read the [technique guide](references/technique-guide.md) and write the effect's timeline as data: warmup/anticipation ending on the impact frame, action with an overshoot snap and a reaction on every pulse, recovery that breaks the form apart, and a shorter interruption path. Derive every size, rotation and erosion curve from that timeline and the gameplay action's real timings. Change shape in every phase (height, width, tilt, spin, tear depth, erosion holes); fading opacity alone is not motion.

Assign layer roles front to back (ground/pool, deep shell, body, foam/crest/spray) with explicit sort order. Use cel tone bands (step/smoothstep thresholds), foam bands on erosion edges, torn tops instead of flat rims, back-face shading and flowing UVs. Check texture feature frequency before choosing tiling.

## Compose and integrate

Choose a dominant readable shape and supporting layers appropriate to the action. Plan deliberate empty/low-opacity regions, front-to-back layer roles, and a hue/value progression; do not rely on dense texture noise or two brightnesses of one color. For the current water family, read the [water references and user critique](references/water-direction.md); it indexes eleven reference images. Decide which forms stay attached, travel, expand, collide, break apart, or leave a tail. Keep the result legible against the actual scene at gameplay scale.

Choose the actual effect construction and texture roles before making artwork. For evolving water, use flow/foam/dissolve masks, scrolling or distorted UVs, mesh surfaces, particle motion, trails and suitable animated flipbooks. An entire splash illustration on a scaled or bouncing card does not substitute for those systems. Intentionally requested 2D effects can use genuine animated VFX textures; do not confuse a still illustration with a flipbook or flow mask. Use installed native Unity systems and compatible pack capabilities before introducing a custom replacement. Keep effect-owned prefabs, materials, profiles, and focused presentation scripts under `Assets/_Project/VFX/<Effect>/`. Store authored tuning in ScriptableObject assets or the relevant native Unity asset; runtime components hold instance state without modifying shared configuration.

Use the official [Unity CLI](../unity-cli/SKILL.md) and [Pipeline](../unity-pipeline/SKILL.md) workflows: discover live commands and modify scenes, prefabs, materials, and import settings through Unity APIs. Keep combat collision, damage, and network authority in gameplay systems; presentation consumes their source/contact data. Existing effects may be referenced or reused without rebuilding unrelated content.

Inspect material emission, bloom, and receiver lighting according to the requested result. Tune the effect's own materials and layers first; changes to shared cameras or scene rendering must serve the actual request. Preserve meaningful world placement and interaction.

## Inspect and deliver

Register every effect newly created or adapted with this skill in the [VFX testing browser](references/testing-workflow.md). Keep the catalog limited to skill-authored effects; do not bulk-register older project effects or vendor libraries. The catalog was initially empty; populate it with requested new effects. Use its list and replay controls for inspection before reporting completion.

For multiple alternatives to one action, follow [candidate comparison](references/candidate-comparison.md): preserve a shared gameplay/timing contract, vary the actual graphic construction, compare at consistent phases, and wait for the user's selection before replacing live content when requested.

Check relevant compilation/shader diagnostics, then inspect the rendered effect in motion at normal gameplay scale. Check that interior flow, erosion, foam and detached particles evolve while the overall transform is steady; growth and fading alone do not establish the requested motion. For a motion-focused revision, deliver a native motion capture alongside playable previews when practical, rather than relying on a peak still. Check onset, main action or sustained phase, ending, and tails; include facing, moving sources, contact, repetition, interruption, and cleanup where applicable. Compare shape, palette, layering, glow, and motion against the selected references before adding more particles or brightness. Treat the first render as a draft: identify visible mismatches, revise them, and inspect the changed phases again. Keep the review focused on the requested qualities rather than treating added detail as progress.

Review from an elevated, gameplay-like camera; a floor-level stage hides ground layers. Capture fixed phases (late warmup, impact, mid-action, recovery) on a slowed clock and compare them side by side; see the [testing workflow](references/testing-workflow.md).

Use proportional local runtime inspection. Follow the project's prohibition on unrequested automated tests and unnecessary extra players/builds/network sessions. Verify frames/time advance before claiming runtime observations, restore the previous editor/Play state, and remove temporary preview objects.

Report the references and pack ingredients used, the browser entry, what changed, what was observed, and any remaining limits. Keep technical validation separate from the user's visual acceptance.

## Improve from results

The user has requested an iterative skill: update these instructions and relevant supporting references when practical use or user feedback reveals missing guidance, a misleading assumption, or a repeatable improvement. Address necessary gaps in the testing workflow alongside the effect, then reflect the corrected workflow here. No separate approval is needed for these skill improvements within the VFX request.

Use the observed result and the user's assessment as evidence. Record a concise observation, the resulting instruction change, and remaining uncertainty in [learning notes](references/learning-notes.md). Revise or remove advice that proved unhelpful instead of accumulating conflicting rules. Preserve demonstrated successful approaches when reusable. Keep one effect's palette, timing, and taste feedback in its own authored data unless the feedback establishes a broader preference. Do not claim user acceptance from compilation or a screenshot.
