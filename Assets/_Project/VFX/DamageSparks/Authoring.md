# Directional damage sparks

Original project assembly using native Particle System, collision/trails, Animator and one unshadowed Point Light. Hovl Hit 12 was studied for flash/spark separation; its finished prefab, shaders and behavior are not dependencies.

| Layer | Onset / life | Source and purpose |
| --- | --- | --- |
| Contact ignition | 0 / 0.045–0.065s | Supplied Hovl Mask23; small hot impact read |
| Rebounding metal | 0 / 0.10–0.28s | Supplied Hovl Point11; 11 particles with a 38-degree cone, gravity, drag and short native trails |
| Reflected energy | 0 / 0.095s | Native point light, range 1.35, peak 3.8, native Animator fade; no shadow map |
| Cleanup | up to 0.6s | Native root Particle System stop action includes child tails |

CharacterHitFeedback observes the receiver's existing confirmed damage event/RPC. A cosmetic source-to-contact projection uses native Collider.ClosestPoint and the visible combat-view face; gameplay geometry stays unchanged. Root forward is the outward rebound direction, opposite incoming impulse; local up preserves camera depth. Projectile incoming direction uses measured flight travel and a launch-direction fallback, rather than resolved or callback-relative velocity. Enemy/melee diagonal knockback remains diagonal. Cosmetic collision uses scenery layers 6/7 and cannot deal damage.

Native URP Particles/Unlit additive materials consume existing RGB source masks without source edits. Normal world depth remains enabled. The finite root owns all children, including the animated light, so target destruction cannot truncate the spark tail. Shared CombatVfxSettings contains the prefab reference and remains read-only in play. Crate break hits keep this cue plus existing debris/smoke.

Manual local-host inspection observed physical bullet damage, advancing frames, sparks and the point-light envelope. Same-frame owned-light on/off renders altered nearby mecha armor independently of bloom. Initial captures before native particle birth and with resolved velocity were diagnostic failures, not acceptance evidence; flight direction and visible-face projection were corrected. Native cleanup left no live effect lights after decay. Evidence stays in ignored Logs/SwordRevision. No automated tests, extra clients or builds. Artistic review remains pending.
