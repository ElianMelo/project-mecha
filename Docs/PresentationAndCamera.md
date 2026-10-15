# Character, toon, camera and effects pass

Implemented 2026-10-14. Architecture decisions are recorded in [Architecture](Architecture.md) and the repository's AGENTS.md.

## Review the result

Open MainScene, enter Play Mode and press **1** to host locally. Arrows move, Up/Space performs a full jump, held Down descends one-way platforms, Z chains the three attacks, Up during a grounded combo branches to a launcher, Shift/double-tap dashes, and A/S/D perform the existing three skills.

The scene and prefabs are already configured. **Tools → Mecha → Apply Character and Presentation** reapplies presentation from the saved profiles. **Apply Mecha Gameplay** includes that step after the arena/input setup. Existing authored profiles are preserved during reapplication.

## Configuration

All new/touched gameplay tuning is authored under `Assets/_Project/Settings/`:

| Profile | Responsibility |
| --- | --- |
| PlayerMovement | Walk/run/dash, fixed jump impulse, gravity, coyote/buffer timing and ground mask |
| PlayerInput | Double-tap timing and movement dead zone |
| PlayerCombat | Per-action timing, hit volume, damage, launcher, MP/cooldown and buffering |
| PlayerHealth | Health, stun, invincibility and respawn rules |
| PlayerAnimation / EnemyAnimation | Animator controllers and state-to-clip mappings |
| EnemyBehavior / EnemySpawning | Enemy AI, attack timing, reaction lifetime and spawn population |
| CombatVfx | Particle profile references, emission timing, hit/telegraph flash and damage text |
| DashSmoke / AttackWind / HitSparks / HitSmoke / SkillWind | Particle materials, colors, lifetimes, speeds, spread and sizes |
| RoomCamera | Lens, dead/hard zones, damping, vertical thresholds, visual bounds and physical walls |
| ToonLook | Shade bands, outline thickness, lighting, background and visual scale |
| OneWayPlatform | Drop duration and support tolerance |

Components retain runtime state and references. They do not write runtime values into these assets. Unity's Animator controllers, materials and render-pipeline assets retain their native editor workflows, referenced/configured by the presentation profiles.

## Camera and boundaries

Cinemachine **3.1.7** is installed through Unity Package Manager. The main camera has a Cinemachine Brain; Room Camera uses Position Composer and Confiner 2D. The 2D confiner shape is used only to constrain framing; gameplay remains 3D physics.

The camera uses an orthographic size of **4.1** for the supplied character and a straight side view. It has a quiet 12% horizontal central zone, soft damping outside it, and progressively stronger follow near the 74% hard zone. It does not continuously center the player or add facing-direction lookahead.

A runtime tracking point keeps the grounded elevation during ordinary jumps. Landing on a different tier beyond the profile's threshold updates the elevation. Crossing the head/feet safety thresholds allows vertical follow so high travel can remain visible. Cinemachine owns the final camera pose and confining; custom code selects the tracking point and damping.

The view rectangle spans x=-12.5 to 12.5. Confiner damping and slowing distance are zero for firm room-edge stops. Separate invisible 3D BoxColliders stop actors at x=-12 and 12. The camera reaches its limit before the actor reaches the wall; the actor can then continue toward the screen edge without pushing the camera. These bounds and wall dimensions are in RoomCamera.

Gameplay image search returned the Elesis tutorial and multi-tier dungeon references from [GameBlast](https://www.gameblast.com.br/2021/07/impressoes-grand-chase-classic-pc-segue.html) and [the Steam screenshot gallery](https://steamcommunity.com/app/304270/screenshots/). Browser preview was unavailable because automatic approval review reached its usage limit. Framing is an approximation informed by the requested behavior, rather than a claim to have recovered the original game's lens or camera code. Implementation follows Unity's [Position Composer](https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/manual/CinemachinePositionComposer.html) and [Confiner 2D](https://github.com/Unity-Technologies/com.unity.cinemachine/blob/main/com.unity.cinemachine/Documentation~/CinemachineConfiner2D.md) behavior.

## Character and animation assets

The supplied Tallgeese model is now the player visual. Its original mesh and UVs are retained. The body uses the supplied texture; no models or texture images were generated.

The model and albedo are under `Models/Player/Mecha.fbx` and `MechaAlbedo.png`. Selected clips were extracted into `Animations/Player/`: Idle_V2, Running, Dash, Jump, JumpLoop, Attack_A, Attack_B, Attack_C and Death. Root translation is held fixed in the extracted clips so physics owns travel; skeletal posing remains animated.

| Action | Clip |
| --- | --- |
| Idle / Run / Dash | Idle_V2 / Running / Dash |
| Rising / Falling | Jump / JumpLoop |
| Z combo 1 / 2 / 3 | Attack_A / Attack_B / Attack_C |
| Air/dash attack, A | Attack_A |
| S | Attack_B |
| D / combo launcher | Attack_C |
| Player hit / defeat | Attack_B reaction segment / Death |

The former player's rig, materials, weapon and Idle/Run/Attack clips are reused for the enemy through `Prefabs/Characters/EvaVisual.prefab`. Its model/materials are organized under `Models/Enemies/Eva` and clips under `Animations/Enemies`. Enemies now animate movement, attack telegraphs and hit reactions while authoritative physics supplies pushback and launch. The old rig has no dedicated hit/death clips; those states reuse a segment of its existing Attack clip. The new player's Death clip plays before the configured respawn delay, then animation presentation resets for the respawn.

Unused temporary FBXs, Blender files/backups and the unused cyan texture were removed from the Unity asset tree after a recoverable ZIP was written to `SourceArchive/NewPlayerUnused-2026-10-14.zip`. The archive includes original metadata. The selected base model and texture were moved through AssetDatabase to preserve GUIDs. The TEMP_NewPlayer folder is gone.

## Shading and effects

Player, enemy and weapon materials use **Toon/Toon** from Unity Toon Shader 0.15.1-preview. Existing texture colors remain. Two cool shade bands use thresholds 0.58/0.27 with 0.001 feathering, dark outlines and restrained flat highlights. Outline extrusion compensates for each imported model's transform scale. The key light casts hard shadows; ambient fill is reduced. Motion blur is disabled and bloom restrained to keep silhouettes and bands crisp. Platform renderers have shadow casting disabled, so platforms above an actor cannot cast a shadow onto it.

The reference images informed contrast, cel bands, dark recesses and thin ink edges. Baked texture shading and the supplied meshes limit how closely material changes alone can reproduce the references. No reference imagery was used as a texture.

All combat effects use Unity **Particle Systems**, with the supplied wind texture and Unity's built-in particle texture. The circular LineRenderer effects were removed. Dash smoke emits at the feet and moves opposite the dash; attacks produce short directional wind; impacts produce smoke and elongated sparks traveling away from the attacker. MaterialPropertyBlock provides a brief white flash without duplicating character materials. IHitFeedback is shared presentation behavior; ICombatTarget keeps attack detection independent of EnemyController's concrete type.

## Validation process

No automated tests were created or run. Validation uses Unity compilation, source review, actual Game View inspection and manual local-host actions. Temporary slow motion, paused frames, staged actor positions and passive trajectory observations were used only during inspection.

Observed in Play Mode:

- Rightward dash emitted two smoke bursts with negative mean X velocity (approximately -2.24 and -2.32), while the Dash clip played.
- The first hit flashed the enemy white. Hit sparks and smoke traveled right, away from the attacker; observed mean X velocities were approximately +4.90 and +1.59.
- Combo actions selected Attack_A, Attack_B and Attack_C. Client and server reached Slash3; enemy health changed 90 → 81 → 70 → 55. The finisher raised the enemy from the floor to y=-3.16 in the inspected frame.
- A jump with JumpHeld=false rose approximately **2.01 units** and returned to the floor. Camera Y remained **-1.50** for the entire observed jump.
- At the right wall, the player stopped at x=11.73 with zero velocity. Camera X stopped at 5.21 and its right view edge stayed at 12.50; the player remained visible at viewport x=0.95.
- At the left wall, the player stopped at x=-11.73. Camera X stopped at -5.21 and its left view edge stayed at -12.50; the player remained visible at viewport x=0.05.

- A fresh startup framed the floor at camera Y=-1.50 without manual correction. Held Down with the new collider reached y=-3.50 and the solid floor remained blocking.
- Defeat selected the supplied Death clip with HP=0 and movement stunned. After the configured delay, HP returned to 100 and Idle_V2 resumed at the spawn point.
- The A/S/D skills were invoked after the presentation migration; S selected SwordFire and D selected CriticalX. These reuse the mapped supplied attack clips and Particle System profiles.

Final Unity inspection reported no compilation failure, zero missing scene scripts, valid profile references on both character prefabs, and Toon/Toon on their renderers. All six one-way platforms had shadow casting disabled. The console error count was zero. Source whitespace checks passed. The scene is saved in Edit Mode with pause off and time scale 1; temporary input, actor staging and slow motion were discarded. The obsolete custom follow script and temporary connection bootstrap script were removed.

The original Cloud MCP connection mode was restored and the temporary local helper process was stopped after validation.

Only a local host and the current 16:9 Game View were inspected. Separate network clients, controller hardware, unusual aspect ratios and a standalone build remain unverified. Remaining warnings concern FishNet's obsolete editor APIs, Cinemachine's optional HDRP sample assembly reference, and MCP reconnect activity during domain reload.
