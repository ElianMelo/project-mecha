# FishNet synchronization review and upgrade plan

Implemented 2026-10-16 for the existing side-view co-op prototype, using FishNet **4.7.2 free**, FishyUnityTransport, and the existing Unity Sessions/Relay adapter. Existing gameplay work in the working directory was preserved.

## Findings

The player, both enemy types and lance projectile already had NetworkObject and NetworkTransform. The player also had NetworkAnimator, but CharacterAnimator called Animator.CrossFadeInFixedTime directly. Enemies did not have NetworkAnimator: each observer independently inferred locomotion and replayed combat animation callbacks. Remote actors combined transform smoothing with Rigidbody interpolation, because NetworkTransform's Component Configuration was Disabled. That gives two systems responsibility for the same displayed pose and can leave physics queries using a different pose.

There was no authored TimeManager in MainScene, so FishNet created one with its default 30 Hz tick rate. Physics and movement use Unity FixedUpdate at 50 Hz. Transforms sent every tick, with a two-tick interpolation buffer. Automatic teleport at a three-unit separation could turn gaps during fast motion or packet starvation into snaps. Player hit queries run in Update while physics automatic transform synchronization is disabled.

PlayerSettings Run In Background was disabled. FishNet's NetworkManager already forces it on during operation, so this is **not a confirmed root cause** of the reported lag. The project setting is now consistent with the manager.

## Changes

| Area | Current configuration / behavior |
| --- | --- |
| Timing | MainScene has a native TimeManager at **50 Hz**; Unity remains responsible for physics. This matches the nominal existing 0.02-second physics cadence; it does not convert FixedUpdate gameplay into tick prediction. |
| Transform updates | Every tick when changed; position sensitivity remains 0.001. Two interpolation ticks now represent approximately **40 ms**, versus 67 ms at 30 Hz, before transport/frame scheduling delays. |
| Actor physics | NetworkTransform Component Configuration = Rigidbody. Native FishNet configures controllers as dynamic and replicas as kinematic with Rigidbody interpolation disabled. Local controllers retain native Rigidbody interpolation. Removed competing player/enemy lifecycle assignments. |
| Projectiles | Preserve intentional server kinematic MovePosition simulation; clients disable Rigidbody interpolation and display NetworkTransform motion. Do not use automatic Rigidbody configuration, which would make the authoritative projectile dynamic. |
| Facing / scale | Actor Y rotation snaps so a left/right flip does not interpolate through an incorrect facing. Static root scale is not synchronized. |
| Teleports | Disable distance-triggered teleport. Existing explicit NetworkTransform.Teleport calls on revive/room travel remain active; spawning initializes enemy/projectile poses normally. |
| Animation | Player NetworkAnimator remains owner-authoritative. Both enemy prefabs now have server-authoritative NetworkAnimator. CharacterAnimator writes states, speed and parameters only on the animation authority, and calls NetworkAnimator.CrossFadeInFixedTime for spawned actors. Observer callbacks still provide VFX/UI but cannot overwrite replicated animation. |
| Hit queries | Refresh native physics transforms immediately before each authoritative melee overlap query. Damage, range, cooldown, mana and stun rules remain server-validated. No client-declared hits or enlarged hitboxes were introduced. |
| Background | PlayerSettings and NetworkManager both enable background operation. |
| Frame budget | Native ClientManager and ServerManager cap rendering at the profile's **120 FPS**, replacing their implicit 500 FPS defaults. Network simulation remains 50 Hz. This leaves more CPU/GPU capacity for peers running on the same machine. |
| Input ownership | PlayerInput starts disabled in the network prefab and is enabled only for the owning client. Ownership changes and client shutdown reset input. Unspawned offline previews enable input at Start. This prevents a newly instantiated remote replica from pairing with local devices before FishNet ownership callbacks. |
| Authoring | NetworkSimulation.asset is the ScriptableObject profile. **Tools > Mecha > Apply Network Synchronization** applies it through Unity APIs. The existing full online setup also calls this step. No runtime profile mutation or replacement networking stack. |

The profile is an **editor setup profile**: after editing it, apply the menu command and rebuild every peer. Tick rates must match. Old clients/builds should not be mixed with the updated host.

## What this can and cannot fix

Removing duplicate smoothing, giving animation one authority, reducing buffering time and querying refreshed collider poses address concrete implementation/configuration faults. The 50 Hz setting increases the opportunity to send changed transforms by about 67% relative to 30 Hz; CPU and bandwidth must still be measured on target hardware. It is a starting configuration for this small co-op encounter, not a universal optimum.

An observer still sees server-owned enemies in the past. The server evaluates attacks after commands arrive, using current authoritative colliders. A fast enemy can therefore leave the displayed swing volume before evaluation. Interpolation tuning cannot guarantee that every visually aligned swing hits under latency. This pass deliberately does not implement historical collider recording, rewind, extrapolation, client-reported hit approval or latency-based hitbox inflation.

Player motion remains the project's existing **owner-authoritative movement**, not FishNet replicate/reconcile prediction. The server validates combat but trusts the owner transform stream, and a host can observe a buffered remote-player pose. Full server-authoritative movement with FishNet's **free prediction** is a separate future migration: move input/movement, impulses, collision exceptions, hit stop and support/drop state onto one tick simulation with reconcile state, then remove owner-authoritative NetworkTransform. It is not a Pro unlock and should not be confused with buying lag compensation.

## FishNet Pro integration slots

No Pro-only feature was implemented or replaced. Extrapolation is explicitly zero in all configured transforms.

1. **Collider rollback / lag compensation — first priority.** Add the licensed native RollbackManager configuration and ColliderRollback to enemy hitboxes. Provide the client's validated command/pulse tick to server-side hit resolution. Integrate the native rollback/restore scope around PlayerAttack.ApplyHits immediately before its Physics.OverlapBoxNonAlloc call. Keep range, damage, cooldown and target eligibility authoritative. Match rollback interpolation to the actual transform buffer and use a bounded rewind window. Cover melee overlap/multi-pulse attacks, death/despawn, late arrival and room changes; do not assume a raycast sample alone validates melee.
2. **NetworkTransform extrapolation.** After purchasing, evaluate a small native extrapolation window under measured jitter/loss. Enemy collision, platform landing, sudden hit knockback and direction reversals can make extrapolated poses wrong. Revisit interpolation with rollback enabled rather than simply maximizing both buffers.
3. **NetworkAnimator parameter selection.** The free component synchronizes supported parameters. Pro can filter selected parameters if profiling shows a useful traffic reduction. Gameplay authority must remain independent of animation values.
4. **Network level of detail.** Consider Pro LOD for substantially larger encounters. Nearby combatants must retain enough updates for precise interaction; the current small room does not need a custom substitute.

## Validation

The final Windows development build succeeded with **zero errors**. Its one warning states that Pipeline is disabled in player builds; no runtime control endpoint was enabled. Compilation and source whitespace checks passed. Existing general regression fixtures were not run.

Two independent standalone players exercised the ordinary local Unity Transport/FishNet path. A bounded opt-in development probe started the host, joined the client, observed ordinary enemy AI for the opening seconds, staged a passive target at close range, and requested owner attacks through PlayerAttack.HandleAttack. Combat continued through real ServerRpc validation, authoritative overlap/damage/knockback, replicated health, hit confirmation and enemy despawn. It does not inject accepted hits or replace the combat transport.

| Final build run | Recorded client interval | Confirmed client damage | Stable rendering | Network conditions |
| --- | --- | --- | --- | --- |
| Ordinary local connection | About 35 s | 326 | About 120 FPS | No injected impairment |
| Impaired connection | About 35 s | 326 | About 120 FPS | Native FishNet simulator adds 80 ms on each peer's outgoing traffic, 2% unreliable loss and 2% reordering; client RTT settled around 164 ms |

Both runs confirmed enemy replicas were kinematic with Rigidbody interpolation None. Enemy animation state hashes changed with movement/reaction and replicated with health/knockback. The impaired client's ordinary session log also showed the remote host's position, grounded state and health. Neither final run logged managed exceptions, malformed animator data, missing prefab failures or the earlier input control-scheme pairing warning. Unrelated graphics info-queue and blocked Unity analytics-upload messages occurred in the player logs.

The uncapped initial check ran near 500 FPS; the final measured cap was approximately 120 FPS. Initial frame-loading spikes and the **deliberate staging teleport** are included in the probe's cumulative maximum-step/frame/collider-offset fields, so those maxima are not presented as proof of zero warping or zero hitbox error. These checks establish concrete synchronization, hit registration and stable session behavior, not a guarantee about every moving-target hit over the Internet. Physical keyboard/gamepad gestures were not driven in the standalone probes; input activation was checked by native prefab configuration and disappearance of device-pairing warnings.

The Unity Editor entered Play Mode but froze at frame 2 with isPaused=false and timeScale=1. Rechecking frame advancement after enabling background execution, auto-tick and Editor/Game View focus still timed out. The network checks therefore used standalone processes. The first build succeeded but captured an inspection timeout while its main thread was busy; both later builds succeeded with zero errors. The Editor was restored to stopped, clean MainScene, original input-background behavior, and the previous 16 ms auto-tick interval. Validation processes were closed and simulator settings were never saved into game assets. A Unity bug report was not sent.

Receipts are ignored under Logs/NetworkSynchronization, including final host/client logs and results.json. The development build is under Builds/NetworkSynchronization. NetworkValidationProbe is compiled only for Editor/development, activates only with --mecha-network-probe, stops its scripted work after 35 seconds, and exposes no control endpoint. The optional host, attack and latency launch flags are validation controls, not game features. AgentScripts/NetworkSynchronizationLab.cs provides operator-invoked Editor inspection/staging through the official Pipeline.

Scope: one host and one remote process on the same Windows computer; seven initial enemies; local transport plus simulated adverse conditions. Live Relay, four players, different machines, platform variants, long-duration soak, prediction/reconciliation and Pro rollback were not exercised. Lobby/Relay membership code was unchanged.

## References and interpretation

- [Valve: Source Multiplayer Networking](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking): distinguishes simulation/update cadence, buffering, local prediction and server hit compensation. The applicable lesson is that a smooth buffered view is delayed and hit evaluation must account for that delay. Source's example constants are not copied into this Unity game.
- [FishNet NetworkTransform](https://fish-networking.gitbook.io/docs/fishnet-building-blocks/components/network-transform): native Rigidbody configuration, interpolation, send interval, snapping and teleport behavior.
- [FishNet NetworkAnimator](https://fish-networking.gitbook.io/docs/fishnet-building-blocks/components/networkanimator): explicit Play/CrossFade calls must use the network component; parameter/speed synchronization alone does not capture those commands.
- [FishNet Pro feature boundaries](https://fish-networking.gitbook.io/docs/overview/readme/pro-projects-and-support) and [RollbackManager](https://fish-networking.gitbook.io/docs/fishnet-building-blocks/components/managers/rollbackmanager-pro-feature): collider rewind, transform extrapolation, parameter filtering and LOD are upgrade work.

The Valve page rejected direct fetches (403); its indexed article text was available through search and reviewed. FishNet documentation was fetched directly and compared with the installed source. Observed defects and configuration decisions above are project-specific findings; the remaining-latency explanation follows those networking principles.
