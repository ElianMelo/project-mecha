# A / S / D thermal skills - first review pass

User brief: rework all three skills with layered fire that bursts and fades; S must start at the player's hand and travel forward. Keep Mega Slash, Sword Fire and Critical X identities. Visual acceptance is pending the user's feedback.

## Composition

| Layer | Onset / lifetime | Source / frame | Purpose |
| --- | --- | --- | --- |
| Hand ignition | 0 / 0.055 s | IgnitionMask; world | Compact initial energy, not a screen flash |
| Hot core | continuous feed for 0.10-0.14 s / 0.19 s | FireCells 2x2; world, travel axis | Pale amber interior advances with the attack |
| Flame envelope | same feed / 0.22 s | FireCells + ErosionNoise; world | Orange torn fuel/plasma edge, then threshold erosion |
| Shear streaks | burst / 0.15 s | ThermalStreak; velocity aligned | Technical directional acceleration |
| Cut residue (A/D) | 0.08 / 0.38 s | ThermalStreak; native anisotropic billboard | Retains the swept/crossed silhouette while fire decays |
| Cooling soot | 0.055 / up to 0.65 s | SootMask; world | Small dark residual volume, native upward force |
| Embers | 0.02 / 0.44 s | ThermalStreak; travel frame | Native gravity, world collision, damped bounce |
| Residual breakup | 0.02 / 0.35 s | SootMask; world | Quieter warm transition into the tail |
| Final-pulse light | 0 / 0.18 s | Native Light + Animator | Brief unshadowed illumination; one light on the final pulse |

A has four narrow angled sweeps, S has three hand-fed forward jets, and D has two opposed diagonal cuts. A/D use eight native systems per burst; S uses seven. Confirmed contacts use a separate four-system prefab: flare, surface flame, incoming sparks and soot. No finished vendor VFX/Graph/prefab is a runtime dependency.

The custom ShaderLab/HLSL material fills the concrete native-material gap: a texture-driven hot interior and controllable noise/alpha erosion for a restrained cel-like thermal edge. Native Particle System still owns all emission, motion, flipbook playback, fading, collision and bounce. Shader streams are Position/Color/UV; no custom particle data or compute requirement. The source cells are a verified 2x2 sheet. Main/noise textures have their own project copies; vendor assets/importers are untouched.

All copied textures are sampled as linear mask data. Fire cells use Clamp; scrolling erosion noise uses Repeat so it continues varying at later gameplay times rather than clamping to one edge.

## Geometry and integration

`AttackPiece.action` remains the single authored contact definition. `CombatEmissionOrigin` binds the actual right-hand rig transform for S and resolves body-relative origins for A/D. Native capsule queries grow with the visible front, sample the active window and sweep between caster origins, with one hit per receiving IDamageable per pulse. Collider overlap buffers grow when saturated. Obstruction queries prevent damage through solid geometry. Actual source-to-target direction and native collider surface normals feed the separate contact effect.

| Skill | Range / contact window | Cadence / authored duration | Existing damage retained |
| --- | --- | --- | --- |
| A / Mega Slash | 2.6 / 0.24 s per pulse | 4 pulses, 0.08 spacing / 0.78 s | 8+8+8+18 before multiplier |
| S / Sword Fire | 3.5 / 0.28 s per pulse | 3 pulses, 0.11 spacing / 0.85 s | 14 per pulse before multiplier |
| D / Critical X | 3.2 / 0.24 s per pulse | 2 pulses, 0.16 spacing / 0.88 s | 28 per pulse before multiplier |

Impact begins at 0.18 seconds. Front travel is 0.17 seconds (A/D), 0.21 (S). Mana costs/cooldowns and combat identity are retained. A/D afterimages are visual tails and cannot deal damage after their contact windows. Fuel stop affects rate-emitting layers; delayed one-shot bursts retain native timing. All roots clean up at 1.25 seconds, after their bounded tails.

S's old Hit/reaction clip moved its hand through large arcs. A new native Animator clip derived from the existing Idle clip raises/holds the right arm forward and returns to rest. It replaces only the SwordFire state and its animation-settings reference. No runtime bone solver or shared-profile mutation is used. Legacy Graph fields/assets remain for compatibility; the player routes these three actions to the new native particle presentation. Old blue generic charge sticks are skipped for these fire skills.

FishNet retains authoritative damage. One unbuffered server-confirmed observer event carries contact point, source-to-target incoming direction, normal and final-pulse state. Host-local execution uses the existing RunLocally convention; cosmetic particles do not apply damage. Validation uses isolated Play Mode manual receivers and real PlayerAttack; no automated tests, builds or Relay sessions.

## Validation and review

Manual capture helpers and evidence are in `AgentScripts/FireSkillsLab.cs` and ignored `Logs/FireSkills/`. The staged actor/receivers are disposable Play Mode objects using real project models and the gameplay camera's 3.485 orthographic size. Records contain advancing frame/time samples, particle counts, hand positions and received hit totals. Static source/authority and dependency review complements the visual captures; a second network client has not been exercised.

Initial observed issues were corrected: flat red fire, an unstable S hand pose, an upper D origin colliding with a low platform, and delayed cut bursts being stopped with continuous fuel. The skill/learning record captures these lessons. This is an initial review version, not a claim of final art approval.
