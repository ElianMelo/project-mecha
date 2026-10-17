# Project visual contract

Read for every VFX task. Apply `Docs/VfxIterationNotes.md` as scoped evidence; a rejected treatment for one weapon is not automatically a ban on that technique everywhere.

## Identity before modules

The project combines 1980s anime mecha, modern controlled execution, toon shading and deep shadows. Grand Chase informs readable attack rhythm and side-view combat; Armored Core informs engineered machinery, mass and high-energy weapon systems. These references do not authorize copying artwork. The brief's 80s influence does not mean crude early-era anime, comedy, fantasy or exaggerated rubbery cartoon motion.

Favor angular silhouettes, abrupt ignition, forceful acceleration, tearing metal, hot edges, compressed pressure fronts and deliberate residual settling. Keep actor silhouettes, enemy tells, ground contact, projectiles and HUD legible. Use fine detail to support a strong primary read. A plausible energy source and material response should explain each visible layer.

| Visual decision | Favor | Avoid unless specifically requested |
| --- | --- | --- |
| Value structure | Short white-hot core, saturated middle, dark body and controlled edge | A white screen with bloom hiding all structure |
| Palette | Steel/charcoal shadows; limited amber, cyan, red or weapon-specific accents | Simultaneous rainbow gradients and generic magical purple haze |
| Shapes | Technical streaks, torn fragments, pressure arcs, nozzle cones, bounded fields | Stars, hearts, runes, butterflies, skull symbols, ornate glyph rings |
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
