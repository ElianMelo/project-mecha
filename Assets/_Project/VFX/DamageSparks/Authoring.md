# Directional damage sparks

Current sparks continue the hit through the receiver: attacker left/receiver right sends sparks right, and an impact from below sends sparks upward. Melee snapshots attacker-body-center-to-contact in DamageContact.SprayDirection. Skills/projectiles default to positive Incoming, and legacy contacts use positive supplied impact direction. Both prior opposite-travel and back-toward-attacker rules below are superseded by the user's explicit examples.

Original project assembly using native Particle System, collision/trails, Animator and one unshadowed Point Light. Hovl Hit 12 was studied for flash/spark separation; its finished prefab, shaders and behavior are not dependencies.

| Layer | Onset / life | Source and purpose |
| --- | --- | --- |
| Contact ignition | 0 / 0.035–0.055s | Supplied Hovl Mask23; compact 0.12–0.20m hot contact |
| Rebounding metal | 0 and 0.018s / 0.14–0.32s | Supplied Hovl Point11; 23+7 particles with a 36-degree cone, 2.6–5.6m/s, gravity, native noise/drag/collision and short native trails |
| Cooling fragments | 0.018s / 0.24–0.43s | Same Point11 source at lower alpha; 9 slower fragments in a 52-degree cone, stronger gravity, amber-to-dark cooling |
| Reflected energy | 0 / 0.15s | Native point light, range 1.45, peak 4.2, native Animator fade; no shadow map |
| Cleanup | up to 0.9s | Native root Particle System stop action includes all child tails |

CharacterHitFeedback observes the receiver's existing confirmed damage event/RPC. A cosmetic source-to-contact projection uses native Collider.ClosestPoint and the visible combat-view face; gameplay geometry stays unchanged. Root forward uses the captured spray direction; local up preserves camera depth. Projectile incoming direction uses measured flight travel and a launch-direction fallback, rather than resolved or callback-relative velocity. Legacy contacts without explicit context use their supplied impact direction without negation. Cosmetic collision uses scenery layers 6/7 and cannot deal damage.

Native URP Particles/Unlit additive materials consume existing RGB source masks without source edits. Normal world depth remains enabled. The finite root owns all children, including the animated light, so target destruction cannot truncate the spark tail. Shared CombatVfxSettings contains the prefab reference and remains read-only in play. Crate break hits keep this cue plus existing debris/smoke.

Manual local-host inspection observed physical bullet damage, advancing frames, sparks and the point-light envelope. Same-frame owned-light on/off renders altered nearby mecha armor independently of bloom. Initial captures before native particle birth and with resolved velocity were diagnostic failures, not acceptance evidence; flight direction and visible-face projection were corrected. Native cleanup left no live effect lights after decay. Evidence stays in ignored Logs/SwordRevision. No automated tests, extra clients or builds. Artistic review remains pending.

## 2026-10-19 directional fan revision

User supplied a metal-grinding spark image as visual reference: concentrate energy at contact, fan opposite incoming travel and keep a small local light. The denser primary fan and slower gravity-curved fragments use the existing project materials and unchanged source masks; no stock effect is instantiated. The full-body white hit flash now ends at 35ms (previously 100ms), exposing the small amber illumination while it fades. Bloom/exposure and blade emission remain unchanged.

Manual local preview observed 14/36 damage through the normal enemy path with right/left melee, 30 primary particles and reversed rebound vectors (-1,0,0)/(1,0,0). Separate diagonal incoming inspections at 67/70ms measured opposite directions (-.981,-.196,0)/(.981,-.196,0), native light intensity 2.16/2.12 and 9 cooling fragments. Same-frame light on/off images changed 3,847 pixels in the receiving-area crop, maximum channel difference 95. Final runtime cleanup left zero spark roots or contact lights. A first operator contact sample included script-compilation time in its first frame and skipped the short light; scheduling the event after a native frame corrected that inspection artifact. Compilation and prefab missing-script/dependency inspection passed; no new gameplay authority or network behavior. Raw captures remain in ignored Logs/DirectionalSparks, pending user visual acceptance.

## 2026-10-19 blade-contact direction correction

The user rejected the previous direction. An emitter axis opposite knockback can still match actual blade travel during a returning/backhand cut. Melee now supplies its physical query point and measured blade travel as DamageContact, separate from impulse. The receiving feedback snapshots this through its existing authority/RPC path and launches opposite travel. Native cone, world-space trails, local light and materials remain unchanged. The simple-burst fallback also uses the opposing direction. Secondary projectiles now preserve their actual collision point and flight direction in the same contact value.

Born particle velocity was inspected, not just root.forward. Right Slash1 incoming (.910,-.415,0) produced mean (-2.817,.529,-.127); left Heavy2 incoming (-.877,-.481,0) produced (2.818,.827,-.026). A spawned local-host receiver received 14 damage and a contact-spark velocity dot incoming of -.998. Frame progression and cleanup were observed; no new gameplay errors. This supersedes the earlier direction claim based on knockback/root orientation. Raw receipts: Logs/BladeCollisionRevision; main implementation note: ../Melee/Authoring.md. No new source artwork/material/shader or automated tests.

## 2026-10-19 attacker-facing interpretation — rejected

The user clarified that hit sparks should spray **back toward the attacker**. Opposing instantaneous blade motion did not satisfy that rule on returning/backhand cuts. The earlier opposite-travel acceptance claims are superseded for melee sparks.

MeleeBladeSweep now snapshots contact-to-attacker collider center on the X/Y plane into DamageContact.Rebound. The physical point and measured Incoming remain separate and unchanged. Coincident points fall back opposite the attacker's facing. CharacterHitFeedback uses Rebound for both the authored prefab and simple-burst fallback. Projectile constructors default Rebound to opposite Incoming. The existing confirmed damage/observer RPC carries the snapshot, so moving actors cannot steer an already emitted fan. Native cone variation, gravity, cooling trails, light, hit timing and gameplay damage remain unchanged.

Compilation passed. An advancing local preview observed Dasher right Slash2 at close range: 16 damage, initial mean spark direction dot contact-to-attacker .987; Zero left Heavy2: 36 damage, dot .983. Both distant receivers took no hits. A targeted local-host contact reduced spawned enemy health 90 to 76; incoming blade travel (.308,-.951,0) differed from born spark direction (.918,.375,.124), which returned toward the right-side attacker as intended. Raw captures/receipts: ignored Logs/SparkConsistency. Remote-client playback was not exercised; no automated tests or extra players/builds. MainScene restored clean, Play Mode stopped. User visual acceptance remains pending.

## 2026-10-19 corrected direction: continue through the receiver

The user rejected the previous reversal with explicit examples: player left/enemy right means sparks point right; attacks arriving from below mean sparks point up. This is the current rule for melee, skills and projectile hit feedback. DamageContact.SprayDirection replaces the misleading Rebound field. Melee snapshots contact minus attacker body center; other contacts default to positive Incoming. Legacy feedback uses its supplied impact vector directly. The captured direction stays fixed after emission; native cone spread, gravity and surface collisions still control individual particles.

Compilation passed. Advancing local preview: right Slash2 produced mean velocity (2.642,-1.713,.110), left Heavy2 (-2.997,-1.712,.157). Real Rising Fountain gameplay produced an upward emitter (0,1,0) and initial mean velocities (.118,3.421,.007)/(.176,3.313,.118). Distant melee receivers remained untouched. Raw receipts and captures: ignored Logs/SparkThroughDirection. Play Mode stopped and MainScene restored clean. No automated tests, builds or extra network sessions. Remote-client playback was not repeated for this direction-only correction; visual acceptance remains pending.
