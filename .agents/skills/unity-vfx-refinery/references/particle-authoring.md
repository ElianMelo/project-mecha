# Particle authoring and interaction

Use relevant sections, not every capability on every effect. Inspect installed systems before custom work. The scanned project uses Unity 6000.3.11f1, URP 17.3.0, Linear color and native Particle System; verify current settings when authoring. Shader Graph and VFX Graph 17.3.0 are installed but are not the default path.

## Existing integration and the concrete gaps

`ParticleBurstEmitter` + `ParticleEffectSettings` already cover small directed bursts, gradients, size, velocity, noise and trails. `PlayerCombatVfx` connects attack pulses and locomotion to these profiles. This emitter constructs a simple burst at runtime; it does not express an authored multi-phase prefab, contact-normal context, collision/light setup or full pooled lifecycle. Use it for simple bursts and authored child systems for richer effects. Do not grow it into a universal combat framework.

`SwordFireVfxEmitter` and existing Graph assets are legacy integration examples, not the desired default. Existing tuning lives under `Assets/_Project/Settings/Effects`; future effects made with this skill keep their own profiles under `Assets/_Project/VFX/<Effect>/Profiles`. Do not move existing assets as collateral work.

`IDamageable.TakeDamage` receives damage, knockback, stun and attacker; it is not a complete visual hit context. `SecondaryWeaponProjectile` and `EnemyLanceProjectile` already own authoritative native physics/projectile behavior. Capture visual event data at their confirmed hit boundary rather than guessing from a target's facing or adding a second projectile motor. FishNet remains responsible for authoritative spawn/contact/damage/despawn; presentation stays cosmetic.

## Decompose a sequence

Prefer a project-authored prefab root with named child systems for individual visual jobs. Native Emission bursts/start delays, Sub Emitters and Animator clips cover ordinary timing. Timeline is useful for an actual authored cinematic; do not add a custom timeline engine. Native subemitters suit particle Birth/Collision/Death events; explicit gameplay contacts trigger root impact effects directly.

An explosion example, relative to confirmed impact, is a starting point rather than fixed tuning:

| Layer | Example onset / lifetime | Job / implementation |
| --- | --- | --- |
| Ignition flash | 0 / 0.02-0.06 s | Brief compact source-mapped billboard or mesh particle; avoid covering the actor |
| Core | 0 / 0.08-0.22 s | Strong hot shape, rapid expansion then erosion; distinct from smoke |
| Directional ejecta | 0.01 / 0.12-0.45 s | Stretched sparks/armor spall aligned to incoming or surface frame |
| Pressure front | 0.01-0.04 / 0.08-0.25 s | Supplied ring/arc texture or mesh; quick scale/alpha curve, restrained distortion if justified |
| Heavy debris | 0.02-0.08 / 0.4-1.2 s | Supplied mesh particles, gravity/drag, native collision and bounce only if visible |
| Smoke | 0.05-0.16 / 0.6-1.8 s | Darker broken silhouettes, slower expansion/drag, lifetime flipbook/dissolve |
| Residual heat | 0.1 / 0.25-0.8 s | Sparse cooled embers or short heat shimmer where useful |

Overlap phases so the flash transfers energy to core/ejecta and then into debris/smoke. A root one-shot's duration must not truncate the latest child or detached trail. Inspect actual start delay, duration, last burst, particle lifetime, subemitter and trail tails. Native `IsAlive(true)` can establish particle completion, but also account for non-particle children. Do not put Destroy stop actions on a root that owns later phases. Controlled replay stops/clears child systems before restarting; pause/disable and pooling must not launch stray emissions.

For burst-and-fade skills, stop rate-emitting fuel layers without cancelling delayed one-shot bursts or cut afterimages. A shared root stop applied before a delayed burst can erase a phase, especially during a slow rendered frame. Keep native finite bursts and continuous-feed stop responsibilities separate.

## Source, target and direction contract

Use a small explicit per-event context at a meaningful integration boundary when needed. Include source/target identity or optional transforms, source-at-release position, receiving target/aim snapshot, actual contact point, normalized surface normal when available, incoming velocity, scale and optional visual seed/event identity. Store values in runtime instance data, not a shared ScriptableObject. Capture positions before the projectile is despawned or source/target can disappear. Live transforms are suitable for a sustained beam; a completed hit remains anchored to its captured contact.

Choose and document frames:

- **Incoming travel/penetration:** use nonzero actual pre-impact velocity; otherwise normalize contact/target minus source snapshot. If degenerate, use the last valid travel direction, then authored actor-facing fallback. Reject NaN/zero vectors before rotation. For melee, source-to-contact and attacker context matter even without a projectile.
- **Surface-facing splash:** normalize the contact normal. Orient a local +Z cone/splash to the normal; do not overwrite the incoming frame with it. Ground effects often use the ground normal while their tangential streaks follow travel.
- **Rebound:** orient to `Vector3.Reflect(incomingDirection, normal)` for a reflective response, while native Rigidbody/PhysicsMaterial or Particle System collision owns actual bounce. Reflection is a presentation direction unless gameplay already defines ricochet. Keep the normal's sign consistent with the contacted surface.
- **Beam:** compute endpoints/length every relevant frame, guard near-zero length, place cast at source, body between endpoints and impact at the actual collision endpoint. Don't aim the hit using a nearly zero final interpolated target difference.

Apply the selected frame to the root pivot **and** local emission/velocity, mesh axis, trails and directional burst sampling. Billboards facing the camera do not by themselves point the emission. Use an authored `Quaternion.FromToRotation` axis correction for meshes authored along +X/+Y rather than assuming +Z. Use a non-collinear up vector for `LookRotation`. Keep gameplay physics on the existing X/Y combat plane; only presentation may use a small depth offset. Review the effect from the actual camera. Avoid negative parent scale mirrored through a character rig; use a unit-scale world root and explicit orientation.

For a hand-fed jet, inspect the actual action clip and hand trajectory before tuning the plume. A reused Hit/reaction clip can swing the emitter through an unintended arc. Prefer an authored Animator pose/clip for a required stable casting interval, and validate left/right roots and emitter clearance under low platforms. Do not move an emitter outside room geometry just to make a larger effect.

For world-space particles, emitter rotation affects spawn directions, not already-emitted velocity. Local-space particles follow the root. Pick local for attached exhaust/charge, world for lingering hit smoke/ejecta; mixed-space children are normal. Avoid feeding world coordinates into an EmitParams path that also transforms them locally. Recompute sustain/beam orientation while alive; freeze one-shot contact axes at spawn. World-space particles intentionally keep their trajectory after launch.

Special interactions get focused scripts, for example `<Weapon>ProjectileVfx` (observe flight/attachment), `<Weapon>ProjectileHitVfx` (contact sequence) and `<Weapon>ProjectileReactionVfx` (shield deflect, surface-specific response). Create only the scripts the effect needs. They reference profiles, native components and supplied event context. They do not own hit detection, damage, target selection or actor locomotion. A reusable helper is warranted only for demonstrated repeated mechanics.

## Collision, bounce and reactions

Distinguish cosmetic particles from gameplay projectiles:

- Use the Particle System Collision module with World/3D (this project uses 3D PhysX), explicit layer masks, appropriate radius scale/quality, bounded collision shapes, damping, bounce and lifetime loss for cosmetic sparks/debris. Planes are suitable only for a deliberately simplified surface. Don't collide every smoke pixel.
- Prefer native subemitters for particle collision sparks when that is all the reaction needs. Use Send Collision Messages and `GetCollisionEvents` only if contact position/normal/velocity is necessary; cap events and avoid spawning a root explosion for every particle contact. Death and Collision subemitters can duplicate reactions unless deliberately authored.
- Native Rigidbody/Collider/PhysicsMaterial handle visible physical fragments and projectile ricochets. Respect the existing collision matrix, gameplay plane and continuous/sweep approach for fast gameplay objects. A presentation script must not apply damage from `OnParticleCollision` or run duplicate server/client damage.
- Keep cast, flight and hit lifetimes independent. Stop flight emission at contact, detach or preserve trails/smoke in world space until drained, play the impact at captured contact+small normal offset, and return/destroy objects once the entire owned tail completes. Cosmetic bounce counts and energy decay are authored data; no unbounded collision chains.

Only add physical interaction when the requested effect benefits. For a beam hit, the gameplay query supplies the endpoint; visuals do not lengthen beyond a wall to reach an intended target.

## Lighting and materials

Emission/bloom does not illuminate the room. Use one short native Light animated with Animator where sufficient, or Particle System Lights with a strict ratio and maximum count. Default to brief unshadowed local light; justify shadows per effect. Check URP additional-light limits and the actual toon material's response; an emissive core alone is not proof of receiver lighting. Tune the light in the existing Linear/HDR scene. Do not change global lightsUseLinearIntensity, exposure, quality assets or volumes to reproduce a vendor demo.

Select an existing compatible native URP Particles Unlit/Simple Lit material before custom code. Additive works for thin hot accents; alpha/premultiplied treatments suit dense smoke. Validate blend factors against source channel content: RGB masks without alpha, packed single-channel masks and colored flipbooks are different inputs. A black background texture is not automatically an alpha texture.

When a special material needs code, prefer a focused URP ShaderLab/HLSL file under the effect's Shaders folder. Typical justified gaps: controlled stepped erosion, packed-channel masks, technical Fresnel, scrolling distortion or custom per-particle cutoff. Inspect URP 17.3 package includes/native particle implementations before coding. Document texture channel, linear/sRGB choice, wrap/filter, blend/depth/cull, shader keywords and exact custom vertex-stream layout. Per-particle Custom Data and shader TEXCOORD packing must agree; don't copy a vendor UV0.Custom.xy hint literally without checking current renderer streams. Configure via Unity APIs.

Soft particles require depth data; distortion requires the pipeline's camera opaque texture and correct sampling. Existing BiRP GrabPass shaders are not a URP solution. Check current renderer/camera capabilities first; provide a compatible non-distortion/soft-particle-disabled fallback rather than making unrequested global changes. Retain depth testing and verify front/back occlusion against characters and scenery. Orthographic soft-particle handling differs from perspective; inspect the current camera rather than assuming either.

Treat erosion/noise/grayscale masks as linear data when that is their shader contract; a scrolling noise map needs Repeat wrapping, while a cell sheet usually needs Clamp. Check the effect at later gameplay times so scrolling noise cannot silently clamp to a constant edge. Preserve vendor importers by configuring project-owned copies.

Texture sheets need verified rows/columns, start frame and normalized lifetime cycles. Do not guess grid layout from an atlas filename or stretch a single frame over the whole sheet. Use supplied mesh/textures for fixed visual forms; do not procedurally recreate rings/icons/symbols with runtime mesh/pixel code. Particle motion/noise simulation and UV animation can compute their native behavior.

## Runtime lifecycle, authority and budgets

For frequent effects, use installed pooling or `UnityEngine.Pool.ObjectPool<T>` when beneficial, not a new generic pool framework. Profile data remains read-only. On reuse restore transforms/parents, stop and clear all Particle Systems/trails, cancel pending invokes/coroutines, reset collision/spent flags and lights, and reclaim owned material instances. Do not schedule Destroy on objects that will return to a pool. Keep a bounded safety lifetime for a genuinely stuck instance without cutting off normal tails.

Spawn cosmetic hit presentation from a single confirmed authoritative event with enough captured data for every observer. Respect existing host `RunLocally` behavior; avoid duplicate local+RPC effects. Do not network every cosmetic particle, add damage authority to presentation, or replace physical GameObject bullets with particles. Seed variation only when observer consistency is needed. Only broaden network validation if this integration actually changes event/replication/spawn behavior.

Track peak systems/particles, screen coverage/overdraw, material variants, colliding debris, dynamic lights, tails and simultaneous skills. Establish an observed budget from the actual target/game scene, not invented universal numbers. Cap per-effect counts and overlaps, prefer shared materials, reduce expensive translucent screen coverage before chasing particle count alone. Inspect multiple overlapping effects if overlap is expected. Low-cost versions must retain the primary impact/tell; reduce smoke fragments, light counts and distortion first.

## Primary API references

Consult version-appropriate installed docs or official Unity pages when uncertain:

- [Particle collision](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysCollisionModule.html)
- [Sub Emitters](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysSubEmitModule.html)
- [Lights](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysLightsModule.html)
- [Custom vertex streams](https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysVertexStreams.html)
- [URP Particles Unlit](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/particles-unlit-shader.html)
