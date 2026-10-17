# Project visual contract

Read for every VFX task. Apply `Docs/VfxIterationNotes.md` as scoped evidence; a rejected treatment for one weapon is not automatically a ban on that technique everywhere.

## Identity before modules

The project combines 1980s anime mecha, modern controlled execution, toon shading and deep shadows. Grand Chase informs readable attack rhythm and side-view combat; Armored Core informs engineered machinery, mass and high-energy weapon systems. These references do not authorize copying artwork. The brief's 80s influence does not mean crude early-era anime, comedy, fantasy or exaggerated rubbery cartoon motion.

Favor angular silhouettes, abrupt ignition, forceful acceleration, tearing metal, hot edges, compressed pressure fronts and deliberate residual settling. Keep actor silhouettes, enemy tells, ground contact, projectiles and HUD legible. Use fine detail to support a strong primary read. A plausible energy source and material response should explain each visible layer.

| Visual decision | Favor | Avoid unless specifically requested |
| --- | --- | --- |
| Value structure | Short white-hot core, saturated middle, dark body and controlled edge | A white screen with bloom hiding all structure |
| Palette | Steel/charcoal shadows; limited amber, cyan, red or weapon-specific accents | Simultaneous rainbow gradients and generic magical purple haze |
| Shapes | Torn fragments, broken curved pressure fronts, turbulent tongues, bounded fields | Stars, hearts, runes, butterflies, skull symbols, ornate glyph rings |
| Smoke | Layered dark cells, harder leading breaks and quieter fade | Round candy-colored puffs or uniformly soft stock smoke |
| Timing | Fast decisive strike, staggered secondary fragments, readable decay | Every child starting/ending together; endlessly wobbling loops |
| Surface response | Sparks, armor spall, scorch/heat, appropriate reflected ejecta | Identical radial spark ball regardless of incoming direction |
| Scale | Actor-relative and camera-readable | Source-demo scale copied into the combat scene |

Prefer a dominant hue plus one functional accent, with neutral soot/metal. A warm kinetic weapon can have an orange core and cold steel debris; a cryogenic shot can have a narrow pale core with low-saturation vapor. The actual game's lighting and post-processing are the reference exposure. Retain recognizable shapes with bloom reduced; bloom adds energy rather than constructing the effect.

## Convert elemental requests into machine behavior

| Requested element | Project interpretation | Useful layers and boundaries |
| --- | --- | --- |
| Fire | Fuel ignition, plasma discharge, reactor exhaust or thermal cutting | Hot core, directional fuel/plasma envelope, cooling embers, soot; no spell sigils |
| Ice/frost | Cryogenic jet, rapid coolant expansion, flash-frozen armor | Nozzle direction, sharp condensation front, cold vapor, brittle mechanical fragments; no floating fantasy snowflakes |
| Poison | Ionizing radiation, reactor leak or toxic industrial contamination | Bounded hazard read, restrained particulate/heat ionization, mechanical emitter; color alone does not establish the hazard |
| Lightning | Electrical arcing, capacitive discharge or EMP | Conductive endpoints, branched short arcs, grounded spark response; avoid decorative wandering bolts |
| Earth/rock | Armor breakup, concrete fracture, excavation or rail impact | Material-specific debris and dust, gravity/contact, no summoned floating boulders |
| Wind | Pressure wave, thrust wake, vacuum impulse or sonic shear | Short pressure distortion, source-aligned streaks and displaced grit |
| Water | Coolant/hydraulic fluid/steam | Local spill or jet, droplet breakup and evaporation; no magical ocean wave |
| Healing/nature | Repair drones, nanomachine service or a maintenance field | Technical delivery, brief convergence and localized weld/repair cues; no leaves/hearts |
| Shield/aura | Directional armor field, reactor envelope or countermeasure system | Controlled surface interference, localized impact ripple and emitter anchors; no ornamental magic circle |
| Summon | Drone deployment, fabricated hardware or projected tactical system | Mechanism and mass cues; vendor creatures remain study references |

Do not reduce this table to renaming a fantasy prefab. Change its silhouette, movement, phase structure and material response, and build a new assembly from source components.

## Art review at gameplay distance

View the effect against both dark and lit portions of the actual environment. Check the first readable frame, peak force, secondary motion and final fade. Compare the effect with the actor's body size and weapons; no hardcoded universal meter scale. A contact flash must communicate direction while leaving the receiver readable. Smoke can have a dark core while keeping a controllable alpha perimeter.

For skills with several damage pulses, distinguish buildup, repeated contact and the final impact. Do not introduce hit-stop/time dilation: the project's clocks run in real time and stun is gameplay state. Camera emphasis goes through existing Cinemachine/presentation facilities, with authored amplitude and optional-motion preferences respected. Avoid vendor camera-shake components.

Treat team/hostile colors and status meaning as functional signals before decorative palette choices. A visually attractive friendly effect that hides an enemy telegraph is a failed effect.

## Fire brief and current user preferences

The feedback history lives in Docs/VfxIterationNotes.md; these are the current decisions, not a new universal style for every effect. A is a broad instant upward thermal explosion, S/B advances in rolling layered waves from the actual hand, and D/C grows bottom-to-top then top-to-bottom as a cross. Preserve that sequence, including when facing reverses. A needs width before/beyond contact; costly D needs broad coverage without extra cost or indiscriminate height. Damage pulses need not equal visual explosion roots. Establish event topology before emission settings.

“Flamethrower,” “explosion” and “fantasy burst” are soft delivery/phase references. They do not prescribe three identical continuous fuel jets, a round bomb, magical art or repeated hooked crescents. The middle skill's wave direction and D's ordered cross were liked; preserve those choices while revising the rejected treatment. Earlier sweeping-crescent and simultaneous-cross treatments are superseded.

### Visible layers, density and silhouette diversity

Multiple child systems and multiple filenames do not prove layering or variety. Review actual source silhouettes at gameplay scale. The revision-5 screenshot repeats the same recognizable C-shaped sprite across fire lobes: rotating, recoloring or adding noise to that icon does not solve the repetition. Replace the dominant recognizable mask when necessary; use genuinely different body, hot-pocket, outer-volume and soot densities. Randomize verified flipbook starting frames and bounded texture sampling where useful, while preserving the attack silhouette.

Differentiate depth, scale, value/color, opacity, motion and onset. Preserve holes between lobes and irregular edges. A dense interior can have textured dark pockets, pale-hot accents and a saturated middle while the perimeter fades transparently. Preserve grayscale source density through alpha and interior shading; aggressive thresholding can turn a detailed source into a flat opaque cutout. Check both isolated masks and accumulated overlap. Do not use bloom or a uniform white fill to fake density. Texture-based darkening is not physical self-shadowing; describe the actual shader behavior accurately.

The user rejects straight decorative VFX lines: no rigid slash bars, stretched straight streaks or linear afterimages. Supplied irregular/curved masks, native noise, rotation, breakup and erosion are suitable. Straight source-to-target gameplay geometry remains valid. Fire rolls, oscillates and flickers at different scales; smoke drifts and expands more slowly. Motion belongs to the source mechanism, with directional force and machinery weight rather than rubbery wobble. Bounded visual-only seeds, gradients and scroll variation must not consume gameplay randomness or alter damage/aim/timing.

### Blended anime fire direction (revision 7)

The supplied Build Burning Gundam image, preserved in Docs/References/FireSkills/Feedback07-user-art-direction.png, is a visual reference for this fire pass: broad sweeping amber/orange flame shapes, bright yellow-gold inner tongues, pale-hot focal highlights and clear value separation around machinery. Extract those relationships, not logos, model geometry or artwork pixels. The user requests a blend between the old graphic solution and the current detailed solution; neither photorealistic mottled fire nor repeated flat C-shaped stamps satisfies that direction.

Use broad controlled color/shade bands as the primary read, with restrained textured density, transparent irregular edges and softer rear smoke. Fine noise should support the flame mass rather than turning it into realistic cloudy grain. Preserve the current source diversity, depth, random motion and causal smoke while restoring stronger anime shape/color hierarchy. Apply the reference to the VFX; it does not replace the project's broader art direction or authorize copying the image.

D needs a longer luminous fire tail and more gradual cooling/fade while keeping its rising-then-falling cross. A longer smoke lifetime alone does not meet this request. Extend the relevant fire phase and alpha/color decay, retain overlap between arms and stop forward travel earlier in normalized lifetime so added life does not accidentally extend reach. Treat exact durations as local tuning. Do not prolong every other skill or fog layer automatically.

### Preparation and causal smoke

For these skills use source preparation, main fire and a visible smoke aftermath. The user explicitly corrected the terrain interpretation in revision 7: warmup belongs at the point where the VFX starts, including the animated hand for S. Terrain preparation applies only to an effect actually originating on terrain. Warmup needs a perceptible lead-in before main emission/contact, enough contrast/scale at the actual camera and the same source frame/depth as the main effect. A nonzero warmup particle count or a named object does not prove the player can see preparation. Local ignition/distortion should build energy at the source and follow it until release, with no generic spell charge or sigils. Do not raycast to ground or project every warmup into a floor strip. Balance readability against the earlier late-start complaint; local tuning belongs in authored profiles.

Smoke must follow the fire's movement behind it, with slight transparency, then remain briefly and fade after burnout. Rear camera depth and path alignment are separate requirements. A fixed delayed box at the root does not trace moving fire or a delayed second stroke. Prefer native particle birth/death/collision subemitters when the fire particle is the correct event anchor; use captured gameplay contact for actual impacts. Keep tails in world space with restrained inherited motion and later buoyancy. Account for repeated S pulses, both D arms and contact smoke together. Do not overcorrect long foreground fog by removing all visible aftermath.

### Rendering and light

Inspect the actual particle shader, active renderer features, camera post-processing and source importers before attributing color/blur to toon shading. Unlit particles do not inherit character toon materials; camera effects still affect them. Check mask compression/filtering, alpha accumulation, source channels, erosion and HDR color. Prefer local material fixes for local defects.

Real light casting and scene post-processing/bloom/glow are explicitly authorized where appropriate. Receiver illumination must be visible on relevant mecha/terrain, not merely represented by a Light component or emissive particles. Check enable gates, envelope, range/position and native receiver compatibility. Same-frame owned-light and bloom-disabled comparisons can distinguish these contributions when needed. Preserve dark technical-anime contrast, readable silhouettes and baseline exposure; avoid whole-screen blur or converting distant backgrounds/UI solely to show lighting. Bloom enhances energy without hiding density or negative space.
