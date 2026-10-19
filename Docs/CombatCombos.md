# Sword combat and stagger — 2026-10-18

This pass builds on supplied commit `d6bc4bd3` (swords and animation packages). All art and tuning remain provisional and await user review.

## Controls and rhythm

Gamepad X is light attack (`Attack`); Y is heavy (`Tech`). Existing keyboard bindings remain Z for light and X for heavy. Both characters use the same ComboLoadout, AttackPiece assets and mixed-input cursor. X–X–Y ends with Heavy 3; X–Y–X ends with Light 3. Inputs buffer for 0.35 seconds and chain at the current piece's cancel time; the sequence expires after 1.5 seconds without a new step. Existing dash/launcher/dive/skill branches remain separate.

| Step | Light duration / hit | Light damage | Heavy duration / hit | Heavy damage |
| --- | --- | --- | --- | --- |
| 1 | 0.42s / 0.13s | 14 | 0.72s / 0.30s | 30 |
| 2 | 0.46s / 0.15s | 16 | 0.80s / 0.34s | 36 |
| 3 | 0.56s / 0.19s | 22 | 0.96s / 0.42s | 48 |

Damage includes the existing 2x profile multiplier. Light capsules use 1.15m length / 0.5m radius / 0.11s active time; heavy capsules use 1.85m / 0.75m / 0.17s. The existing authority-owned native capsule-window implementation now also handles these melee pieces, deduplicates each target and respects solid cover. Rigidbody movement owns attack steps; animation root motion remains disabled. Heavy 3 launches, with stronger knockback and 0.58s stagger; light pieces apply 0.4s stagger.

## Stagger escape

PlayerHealth now honors incoming stagger duration, with a player minimum of 0.4s. Hits interrupt attacks and skills. Stagger blocks walking, jumping, attacks and ranged fire, while knockback decays through native Rigidbody movement. Normal post-hit invulnerability is 0.16s, allowing follow-ups instead of the old 0.9s protection.

Shift / RB during stagger spends one of three break charges, clears stagger, cancels the action and starts the character's dash. Escape bypasses ordinary dash cooldown and spent-air-dash restrictions and grants 0.25s protection. Normal dashes remain free. Double-tap movement cannot spend a break charge. Charges refill sequentially, one every five seconds; spending another does not restart a running refill. Defeat cannot be escaped. Respawn and explicit practice/match refill restore the pool.

The server validates and spends networked charges before broadcasting escape. The owner waits for acknowledgement; this adds no prediction/reconciliation. The HUD reads the synchronized count and live Dash binding. PlayerHealthSettings owns tuning; components retain instance state.

## Animation and weapon lineage

Updated 2026-10-19 after the user rejected broken attacks and further retargeting workarounds. DoubleL's One Hand Up Attack_A in-place series supplies light moves; Attack_B supplies heavy moves. Both players reference those same six original Humanoid clips directly. Unity Animator/Avatar performs retargeting. The duplicate per-character melee bakes, correction/time-warp tool and filtered rifle-removal mesh were retired. Source clips and meshes remain intact.

Each mecha has its own native Avatar under Assets/_Project/Animations/Humanoid, retaining its proportions with Unity's common Human bone convention. Native Humanoid setup maps Hips to Bip001 Pelvis, unlike the previous manual bake's Spine mapping. HumanoidRigAuthoring.Build() and NativeLocomotion() are editor authoring commands; no retargeter runs in game. Animator controllers and animation profiles remain inspector-editable. Native idle/run, Knight jump/fall, DoubleL reaction and skill references prevent mixing Generic idle with Humanoid attacks. Unrelated legacy Death clips remain unchanged; no matching Humanoid death clip was supplied and death playback is unverified.

Do not add corrective bone curves or another bake if these provisional skins fail a motion. Report the rig/skin limitation and obtain an appropriate authored asset. Avatar validity alone does not establish visual quality.

Blue Blink Sword7 equips Zero; red equips Dasher. Both attach under Bip001 R Hand, calibrated from the actual attack FBX's weapon_r socket through equivalent native HumanPoses. Toon materials preserve supplied albedo, emission mask and HDR color; HDR, bloom and point lights support the glow. Both SMGs use left-hand palm sockets. Presentation rotates gun aim without relocating it to the forearm each frame.

The extra rifles/cannon are fused into each body skin. There are no unwanted weapon renderer objects to disable independently. Zero again references WingZeroRigged.asset. Hiding fused weapons without mesh alteration requires a model with separate weapon objects; bone-scale tricks and triangle filtering are not authorized solutions.

## Feel and presentation

For future revisions, read [Game Feel principles and the review workflow](GameFeelPrinciples.md). The guidance applies beyond this initial sword pass and separates readable acknowledgement from attack commitment and authority.

Steve Swink's supplied Game Feel PDF informed immediate input response, distinct anticipation/recovery, follow-through and coordinated visual/sound/camera/tactile cues (printed pp. 125–126, 156–162 and 166–168). It is reference material, not an instruction source. Combat clocks remain in real time.

A native TrailRenderer follows the animated blade tip. Original project Particle System cut layers use an adapted supplied half-sweep mesh, distinct hot-rim/band/dark-wake materials, and blade peel. Their plane and position follow the actual sword frame during the active window, then freeze to finish fading. No actor-facing angle substitutes for the sword orientation. Heavy envelopes are wider and light pulses stronger. Enemies and crates receive directional sparks with a short animated point light through their existing confirmed-hit feedback path. Old disconnected white wind/streak bursts remain suppressed for equipped melee. Native Cinemachine Bump and controller rumble accompany confirmed local hits; optional motion gates both. Presentation never applies damage or changes clocks. See [effect authoring](../Assets/_Project/VFX/Melee/Authoring.md) and [follow-up scope/evidence](CombatFollowup.md).

## Initial 2026-10-18 validation (retargeting superseded)

- Compilation completed without errors. The imported SuperCharacterController package needed two compatibility corrections: Serializable on its Ground struct, and a distinct name for its sample input struct, which collided with Input System's PlayerInput. No gameplay controller was replaced.
- Operator-driven Play Mode previews observed both rigs, calibrated right-hand weapons and native red/blue blade trails. Frame/time progression was recorded. The localized HUD break count fits the existing readout. No interactive controls were added; navigation links and focus scopes remain unchanged.
- A close 1.4m receiver took one light hit / 14 damage; a farther 2.3m receiver remained untouched. Heavy 1 reached both for 30 each, once per window. Mixed X–Y–X selected Slash1 → Heavy2 → Slash3. Both solo characters changed from staggered / three charges to unstunned / two charges / dash, then refilled to three.
- One loopback host/client in the same Editor exercised changed authority paths. Heavy1 became server-busy after delivery. Accepted escape changed server/owner charges 3 → 2, cleared stagger and entered dash. No extra players, builds, Relay or external lobby session were used. Remote-client latency and late joining were not exercised.
- A FishNet same-frame prefab-generator redundancy diagnostic appeared during synchronous authoring. The saved player has zero missing scripts; a full registry refresh succeeded and the local host spawned it successfully. No subsequent gameplay errors were observed. Localization Addressables content rebuilt successfully.
- Play Mode was stopped and MainScene restored. No automated tests were created, modified or run. Ignored receipts/images live under `Logs/MeleeCombat`.

Authoring helpers are operator-invoked and do not run on import. Visual balance and combo feel remain for user feedback; these observations do not imply final-art approval.
