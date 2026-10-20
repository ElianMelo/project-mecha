# Equipped sword presentation

Current collision behavior is the later **blade sweep correction** below. The earlier forward-capsule approximation was rejected by the user and is superseded for equipped swords.

Original project presentation assembled with native Particle Systems, TrailRenderer, Light and Cinemachine. No finished vendor effect prefab/graph is a dependency. Sources, materials, profiles and focused scripts stay under this folder; equipped weapon prefabs stay under _Project/Prefabs/Weapons.

| Layer | Onset / life | Source / role |
| --- | --- | --- |
| Sword emission | Persistent | Original Blink Sword7 emission mask and red/blue HDR color, mapped to native toon emission properties/keyword |
| Blade illumination | Persistent plus 0.13s strike envelope | Native point light, range 1.65, idle 0.65 / light 2.5 / heavy 3.5 intensity |
| Blade tip path | Exact contact window | Native animated-tip TrailRenderer, supplied SpeedWind mask, 0.065/0.10s tails and 0.11/0.19 widths |
| Blade body paths | Exact contact window | Two native TrailRenderers at local blade Y=1.02/0.63, 65% tip width, 65%/40% alpha; same source material |
| Blade peel | Active window / 0.10-0.24s particles | Native world-space Particle System and supplied Point11 mask |
| Confirmed hit | Receiver damage event | Original project DamageSparks, brief light, existing audio and flash |
| Impact emphasis | Confirmed local hit | Native Cinemachine Bump and gamepad pulse, gated by optional motion |

SwordSwingSettings owns tuning; shared data is read-only during play. SwordSwingPresentation owns native trail/peel emission gates, light envelope and cleanup. All three native trails retain actual blade trajectories, including motion in depth. Emission starts at action.impact and stops at impact + hitActiveTime; the short native tails fade afterward. New actions, interruption and disable clear stale paths. The independently rotating fixed half-sweep is retired from equipped weapon references. Its earlier authored assets remain available as historical project sources. No custom runtime geometry, pose corrections, animation bake or shader was added.

Melee now uses native Humanoid Avatars and original source clips. Correcting a broken retarget by rotating weapons independently, baking wrist corrections or time-warping motion is retired. The old per-character melee bakes are removed. Both swords use the actual source attack FBX's weapon_r socket calibration under the right hand. Both guns use native left-hand sockets. Integrated extra weapon geometry is preserved until a split asset is available.

Native toon _Emissive_Tex, HDR _Emissive_Color and _EMISSIVE_SIMPLE retain the supplied glow. Main camera HDR/post-processing and the combat Volume Bloom are enabled (intensity 0.7, threshold 0.95); bloom and receiver illumination are separate contributions. Materials use ordinary world depth and the TransparentFX grouping layer, with no clear-depth/foreground overlay. A RenderTexture snapshot made from a copied camera without its URP additional data omits bloom; use the actual camera or final framebuffer for glow review.

Manual preview records advancing frames, standing Humanoid idle, equipped weapons, physical hits, blade presentation and light comparisons at the gameplay camera scale. Visual balance and the provisional rigid skin remain for user review. No automated tests were created or run. Source lineage is in Docs/VfxEffectOrigins.csv, and current behavior/limits in Docs/CombatFollowup.md.

## 2026-10-19 size/brightness revision

The user rejected the current size and brightness. Light/heavy cut radii are now 0.78/1.12 versus 1.30/1.75; native trails are narrower and shorter as listed above. Sword emission retains the supplied mask with HDR color at 0.7x source (previously 1.8x). Band/peel RGB are 55% of prior values; the rim is 40% RGB and 65% alpha. Blade illumination is reduced locally; global bloom/exposure, dark wake and hit sparks are retained. No new source assets are used and existing provenance rows remain valid.

This revision was compiled and inspected in authored data only. Earlier rendered evidence above belongs to the previous balance. No Play Mode preview or automated tests were performed for this revision; the user will evaluate the reduced footprint and brightness during gameplay. See [combat cadence and fairness](../../../../Docs/CombatCadenceAndFairness.md).

## 2026-10-19 blade/contact synchronization revision

The later user feedback rejects the blade/motion/contact mismatch. Native clip inspection found the fixed cut mesh's own angular motion and early emission misleading; full-clip playback also placed several authored impacts after the main fast cut. Three blade-bound native trails replace the fixed half-sweep without increasing the recent tip width, tail length or brightness. Shared ground combo contact starts are now light .17/.26/.30 seconds and heavy .41/.33/.57 seconds; durations, cancels, damage and existing forward capsule geometry remain unchanged. These are authored timing changes, not animation warping or bone corrections.

Local Play Mode inspection at camera size 3.485 observed Dasher Slash1 to the right (14 damage) and Zero Heavy2 to the left (36 damage). Emission was off before contact, on during the authored interval and off during recovery; the receiver's sparks reversed with incoming direction. Each observation advanced frames/time and drained effect roots. Separate same-frame owned-light captures verified armor illumination after reducing the existing white flash to 35ms. Raw inspection images/telemetry are under ignored Logs/DirectionalSparks. One initial staging attempt left the unspawned enemy disabled by FishNet; those no-contact images are diagnostic only. No automated tests or extra clients/builds. Air/branch/combo chaining and moving multiplayer actors were not replayed; the forward capsule remains an authored approximation of reach rather than a swept blade collider. Visual acceptance remains with the user.

## 2026-10-19 blade sweep correction

The user rejected continued spark-direction and collision mismatch. The prior preview inspected emitter axes against knockback, which did not establish the direction of a backhand blade cut. Timing agreement also did not establish spatial agreement with the broad forward damage capsule.

Equipped swords now bind MeleeBlade edge sockets at local Y=.30 and 1.45. MeleeBladeSweep queries native PhysX capsules across that animated segment after Animator evaluation, with intermediate samples at most .075m apart (64-step cap). X/Y follow the real blade; Z remains on the game's combat physics plane. MeleeBlade.asset owns .075m contact radius and sweep limit. Targets are deduplicated per swing and scenery still blocks damage. PlayerAttack owns damage/authority; the blade binding/query has no VFX or damage behavior. The previous forward capsule is skipped for these equipped swords. Unbound legacy attacks and elemental skills retain their existing geometry.

DamageContact carries the actual point and blade travel separately from knockback through normal damage reception and FishNet observer feedback. Sparks emit opposite that travel; projectiles pass their own physical collision/travel. Native blade trails use the same active window and sampled weapon. PlayerMovement.Face now writes native Rigidbody.rotation along with Transform rotation so interpolation cannot restore the previous facing after an Update-time attack turn. No pose, skin, animation clip or rig correction is involved.

Manual preview observed Dasher right Slash1: 14 damage at .199s, incoming (.910,-.415,0), born sparks moving (-2.817,.529,-.127). Zero left Heavy2: 36 damage at .385s, incoming (-.877,-.481,0), sparks (2.818,.827,-.026). A target 2.3m away took zero hits in both inspections. A targeted local host/server session confirmed spawned receiver health 90 to 76 and serialized contact feedback; spark velocity dot incoming was -.998. Host evidence is telemetry only: its capture used the route-map camera and is not VFX visual evidence. Initial host attempts had unpositioned/duplicate staging receivers and are diagnostic only. Cleanup left zero effect roots/lights, compilation passed, and MainScene was restored clean with Play Mode stopped. Raw observations: ignored Logs/BladeCollisionRevision. No automated tests, extra players/builds, remote-latency or dedicated-server replay. Artistic review remains with the user.

## 2026-10-19 melee sparks return toward attacker — rejected

The user explicitly chose sparks returning toward the attacker rather than opposite each cut's instantaneous motion. This supersedes the spark-direction rule in the preceding correction, while preserving its native blade collision and measured travel. DamageContact.Rebound carries the contact-to-attacker body-center snapshot on the combat plane; see [current spark authoring](../DamageSparks/Authoring.md) for implementation and validation. Local close-range right Slash2, left Heavy2 and one spawned local-host contact were observed with advancing frames. Damage, contact coverage, animations and blade trails are unchanged.

## 2026-10-19 explicit direction examples supersede returning sparks

Current melee sparks continue away from the attacker through the hit point, using DamageContact.SprayDirection = contact minus attacker collider center on the combat plane. Player left/enemy right sends sparks right; the reverse sends sparks left. Skills arriving from below send sparks upward. Both the opposite-blade-travel and back-toward-attacker interpretations above are superseded. Native blade collision, physical Incoming and gameplay damage are unchanged. See [current spark authoring](../DamageSparks/Authoring.md) for observed right/left melee and Rising Fountain launch velocities.
