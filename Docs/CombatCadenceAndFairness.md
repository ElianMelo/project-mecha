# Combat cadence and PVE fairness — 2026-10-19

The user's later light-attack revision shortens ground/air light startup, follow-up points and full duration. The light timing table below is historical; current values and the advancing two-character combo preview are recorded in [CombatCombos.md](CombatCombos.md#current-light-timing--2026-10-19-user-revision). Heavy attacks, damage and enemy fairness tuning remain unchanged.

Provisional implementation for user playtesting. The user's [shared hit-reaction discussion](https://chatgpt.com/share/6ad60ff6-6398-83e9-9dd7-ea8866dbb769) informed separation of damage, movement, reaction state and animation. Bounces, grabs and status systems were not added.

## Engagement and fairness

`EnemyEngagement.asset` owns an encounter-wide budget of **two attack tokens**, shared by melee enemies and lancers, including cooperative targets. The existing nearest-hunter count did not gate throws or reserve capacity for live projectiles; `EnemyAttackTokens` fills that concrete gap. Authority queues eligible enemies in FIFO order. An approach lease lasts three seconds; attack starts are separated by 0.30 seconds. Melee retains its token through recovery; a lance retains it until collision, destruction or expiry, even after its thrower is interrupted/killed. Lost targets, stun, passive practice, disable and death release ordinary reservations. Handoff delay is 0.22 seconds. Waiting enemies observe/patrol about 3.5 metres away using existing native navigation and crowd avoidance. Guard leashes remain; the competing nearest-hunter concurrency rule was removed.

Normal melee startup/recovery/cooldown are 0.38/0.42/1.15 seconds. Every third melee preparation can become a special, with a five-second special cooldown. It has a sustained 0.80-second telegraph, distinct heavy animation, 1.5x damage, 7 horizontal / 8 vertical impulse, 0.55 seconds minimum stagger and 0.80 seconds recovery. A staggered target cancels outstanding windups. Lance preparation/cooldown are 0.85/2.8 seconds; prediction is reduced to 0.12 seconds / 0.75 metres maximum.

Ordinary enemy melee/lance hits damage and show feedback, but do not overwrite player velocity, stop movement, interrupt attacks/skills or spend escape charges. A non-attacking pilot can show a cosmetic flinch without a control lock. PVE hit protection is 0.35 seconds; PVP retains 0.16 seconds and authored stagger. Enemy specials explicitly opt into interruption.

`CombatReaction` owns runtime state; `CombatReactions.asset` owns tuning; health owns damage/authority; native Rigidbody motors own impulses/gravity; `CharacterAnimator` presents states. Flow: Flinch / HitReaction → control, or KnockUp → AirHit → Knockdown → StandUp → control. Death overrides recovery. Launch recognition is 3.5 upward velocity; initial launch presentation lasts 0.28 seconds, downed time 0.18 seconds and stand-up 0.48 seconds. Native ground contact starts recovery, with a three-second airborne failsafe. Downed/stand-up reject further damage, and player recovery adds 0.25 seconds protection. Existing stagger escape clears the complete reaction state. Air stagger drag is 3 versus 24 grounded; forced launches ignore the released-jump gravity cut. Global time and hit stop are unchanged.

## Combo weight

Both characters retain shared AttackPieces, mixed light/heavy ordinals, damage and contact geometry. Preparation, contact, follow-through and cancel windows remain synchronized to native animation in real time.

The later 2026-10-19 blade feedback revised **contact starts only** after native clip inspection: light .17/.26/.30s and heavy .41/.33/.57s. The contact column below records the earlier cadence pass; durations/cancels remain current. Blade trails share the contact interval.

The user's subsequent collision rejection supersedes the retained-forward-capsule assumption: equipped swords now query native swept capsules along their animated edge after Animator evaluation, with a .075m blade radius. Damage and temporal tuning remain as above; spatial reach now follows the visible blade. Physical contact travel is separate from knockback for hit feedback. See the current equipped-sword collision record in [CombatCombos.md](CombatCombos.md).

| Step | Light duration / contact / cancel | Heavy duration / contact / cancel |
| --- | --- | --- |
| 1 | 0.78 / 0.24 / 0.58 s | 1.12 / 0.47 / 0.90 s |
| 2 | 0.84 / 0.27 / 0.64 s | 1.24 / 0.53 / 1.00 s |
| 3 | 0.96 / 0.33 / 0.76 s | 1.44 / 0.64 / 1.18 s |

One buffered follow-up lasts 0.95 seconds, allowing early inputs to survive heavy commitment. Sequence input windows are 1.8 seconds. Air slash, dash slash, launcher, dive and critical branches were also retimed; elemental skills were not. Input acknowledgement remains immediate.

## Native animation placeholders

Archived Mecha@Death rendered attack-like poses under native Humanoid import; copying a player Avatar also produced a source bone-length mismatch. Its temporary imported copy was removed; the archive is intact. The user explicitly approved other placeholder animations instead of exact matches.

`Animations/Humanoid/CombatReactions.fbx` copies the supplied native DoubleL Hit_B_4 source. Native importer ranges provide Collapse (frames 0–14), Downed (14–15) and Recover (14–55). These are provisional recoil/death, airborne/downed and stand-up motions, not full prone corpse/get-up performances. Hit_B_5 supplies flinch; full Hit_B_4 supplies heavy reaction. There is no reverse playback, per-character bake, corrective curve or runtime retargeter. Player idle/dash references and source meshes were preserved.

The enemy now has a native Humanoid Avatar. Every clip in its controller, including unused states involved in binding, is Humanoid. Both enemy prefabs and the shared player prefab bind the new profile. Native setup preserves original transforms; Unity may serialize near-identical quaternion values on the enemy prefab.

## Sword revision

Light/heavy cut radii: 1.30/1.75 → 0.78/1.12. Trail widths: 0.11/0.19; tails: 0.065/0.10 seconds. Blade light: 0.65 idle / 2.5 light / 3.5 heavy, range 1.65, decay 0.13 seconds. Emissive color is 0.7 times supplied HDR color, retaining the mask. Cut-band/peel RGB are 55% of previous values; hot-rim RGB 40% with 65% alpha. Dark wake, actual blade orientation, sparks, global bloom and exposure remain. See the effect's authoring note.

## Validation and limits

- Official CLI 1.0.0-beta.13 confirmed current; live commands discovered. All prefab/importer/asset changes used Unity APIs. Compilation completed without errors. Three actor prefabs have zero missing scripts and zero Generic clips in their Humanoid controllers; all required reaction states are bound.
- Operator-driven Edit Mode sampling observed recovery rising and death recoil lowering on both mechas and the enemy. This checks native animation wiring, not gameplay/art acceptance. Receipts: ignored `Logs/CombatCadence`; operator helpers: `AgentScripts/CombatCadenceAuthoring.cs` and `CombatCadenceInspection.cs`.
- No Play Mode transitions, automated tests, extra players/builds, Relay or network simulations. Combat feel, token handoffs, multiplayer timing and reduced sword rendering await user playtesting.
- MainScene remains in stopped Edit Mode. It acquired three unrelated UI RectTransform anchor/position changes during authoring; a native saved-copy comparison identified those differences. The scene was left unsaved to preserve them. Combat changes are saved in assets/prefabs and inherited by the scene player.
- A broad read-only query timed out; focused inspection succeeded. A rejected death reimport disconnected temporarily; the same project recovered ready. Historical tooling/import messages are not current compile failures. Existing GameOptionsMenu member-hiding and FishNet obsolete-API warnings remain outside this task.

## Enemy visibility correction — user Play Mode feedback

The first enemy Avatar was built with the visual prefab's 30x scale already included in its skeleton. Runtime animation then applied that scale again to body translation: spawned enemies had valid colliders at the room floor while their hips/renderer centres were roughly 25 metres higher. This was an Avatar authoring error, not a spawn/token failure.

The native Avatar is now built at unit model scale; the existing 30x visual transform supplies world scale once. The original Avatar GUID, source mesh, prefab scale and native Humanoid clips are retained. `CombatCadenceAuthoring.EnemyAvatar` now normalizes authoring scale, and `AgentScripts/FixEnemyVisibility.cs` provides the focused native rebuild.

At the user's request, one Play Mode session entered the normal local solo host and route flow. Advancing frames were verified (199 → 206 initially; encounter observations at frames 5576 and 11261). Seven enemies spawned. Applying the corrected native Avatar live restored visible melee enemies and lancers, with renderer centres about 0.6–0.7 metres above actor roots instead of 25 metres. The saved Avatar uses the same corrected configuration, with skeleton root scale (1,1,1). Screenshot: ignored `Logs/EnemyVisibility/After.png`. A screenshot-path rejection was tooling-only; there were no gameplay exceptions during this observation. No automated tests or extra clients/builds were used. Stopped Edit Mode and clean MainScene were restored.
