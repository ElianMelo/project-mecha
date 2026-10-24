# Visual direction for dark 80s anime mecha effects

This is the project's chosen treatment, not a claim that every 1980s production used one technique. Judge the visible result in context. Supplied references and explicit feedback can narrow the treatment for a particular effect.

## Shape, color and motion

- Lead with a readable silhouette and force direction. Favor asymmetric lobes, tapered jets, broken contours and clear negative space where the mechanism supports them. Keep symmetric pressure rings or beam cores when physically useful; asymmetry is not an obligation for every layer.
- Give hot, middle and shadow regions distinct jobs: a compact bright core, a colored body and darker volume or residue. Choose the palette for the material and scene. Preserve useful grayscale texture density; color bands need not turn every edge into a hard cutout.
- Make the action decisive: fast expansion, directed travel, deliberate deceleration and a legible end. Short flashes can punctuate impact. Stepped visual accents are optional; preserve responsive gameplay and real-time simulation. Do not introduce global frame stepping or time manipulation to imitate cel animation.
- Separate primary force from debris and atmosphere. Directional sparks, fragments and smoke should explain the source, surface or exhaust flow. Bounded cosmetic variation can break repeated stamps without changing gameplay randomness or damage.
- Let smoke carry volume through overlapping lobes, value separation and breakup. Keep it subordinate to the action and inspect depth/sorting. Soft density and fades can serve smoke; avoid using the same transparent cloud treatment for every material.
- Express technical causes such as plasma discharge, combustion, pressure release, hydraulic flow and armor fracture. Do not introduce fantasy symbols or decorative magic circles to fill space.

## Light and readability

Inspect against the actual gameplay background at normal camera scale. Preserve the actor, contact and attack direction through the brightest moment. Use bloom as a halo around readable forms; if highlights merge into a featureless blob, reduce intensity/coverage or improve the layer shapes.

Material emission makes the visible surface self-luminous. Bloom spreads bright pixels. Native lights illuminate compatible receivers. Evaluate these separately: a point light does not prove a material glows, and a bright particle does not prove the floor receives light. When either behavior is requested, inspect it directly without treating pixel overlap or bloom as receiver illumination. Restore temporary diagnostic overrides.

Retain normal world depth for interacting effects. Adjust local cosmetic placement only when required by actual occlusion; do not place all VFX above actors and platforms or remove collision/light behavior to improve a screenshot.

## Adapt the treatment to the mechanism

| Effect | Visual construction | Behavior to inspect |
| --- | --- | --- |
| Explosion | Uneven expanding hot lobes, a compact ignition, directional fragments and distinct cooling smoke. Add a pressure front only when useful. | Burst origin, footprint, overlap with nearby geometry and readable expansion into residue. A confirmed sudden impact may need no separate anticipation. |
| Beam | Anchored emitter, coherent directional core, controlled edge breakup and a separate contact response. Charge/discharge and sustained flow can have different shapes. | Moving source/endpoint, real obstruction/contact, start/hold/release transitions and remaining tails. Do not bake a fixed endpoint or force a beam into an explosion recipe. |
| Armor impact | Brief compressed flash, fragments/sparks guided by incoming travel and the surface normal, with local smoke or scorch only when appropriate. | Actual contact and facing, misses, surface orientation and rapid repeated hits. Flash coverage should preserve the target and hit location. |
| Exhaust | Attached tapered jets, pulsing combustion or pressure bands and cooling breakup downstream. | Moving emitter, world-space residue versus attached core, throttle changes and clean shutdown without detached stamps. |
| Water | Cohesive graphic volumes, saturated flow patterns, luminous highlights where requested and broken foam contours. Use growth, separation and retraction when they express the flow. | Local water-source compatibility, contact, attachment and release. Avoid replacing water with generic fading smoke or using point lights as its self-glow. |

For water, inspect the actual local Gabriel `Prefabs/ToonLiquids` and `Prefabs/TsunamiWaves` effects and dependencies before assembling. Compatible water maps, meshes and shaders can satisfy the treatment without a replacement shader. For explicit fresh rebuilds, use the new brief and independent source examples; do not consult rejected project effects or their authoring records.

## Practical review

Inspect onset, the main action and the end where those phases exist; include a sustained interval for looping effects. Evaluate silhouette, color hierarchy, force direction, repetition, attachment, contact and disappearance together. If the result is weak, identify the visible cause before adding layers or intensity. Choose counts, sizes and durations for the effect and gameplay view rather than copying a fixed budget from another effect.
