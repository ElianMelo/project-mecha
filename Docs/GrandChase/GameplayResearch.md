# Grand Chase gameplay research and Mecha adaptation

Research date: October 13, 2026. Target: the PC side-scrolling Grand Chase / GrandChase Classic, using Elesis's Knight job as the combat reference.

This project recreates a playable slice of its movement and melee combat. It retains the project's mecha characters, models and FishNet setup. The implementation is an interpretation with explicit, editable timings; the sources do not establish the original game's physics constants or a universal set of frame data.

## Evidence and version boundaries

KOG describes the PC game as a side-scrolling action RPG with character-specific dungeon and PvP play. Its character roster, co-op context and vertically layered arenas distinguish this reference from the mobile GrandChase party battler. The project therefore uses an X/Y gameplay plane with 3D character presentation. [KOG's Steam description](https://store.steampowered.com/app/985810/GrandChase/).

The community introductory guide describes arrow-key movement, Up to jump, Down to descend platforms, Z to attack, and double-tapping a horizontal direction to dash. These bindings inform the default controls. A/S/D must be reserved for skills, so the previous WASD movement bindings were replaced. [An Introductory Guide to Grand Chase](https://steamcommunity.com/sharedfiles/filedetails/?id=291417716).

The Knight reference describes separate ground, jump and dash attacks, a dash that can continue into running, and air dashing. Mega Slash combines quick thrusts with a heavier ending, Sword Fire uses a sword energy wave, and Critical X emphasizes upward energy and a downward follow-through. The mecha adaptation retains those roles using simplified hit volumes and original procedural effects. It does not duplicate every original animation or hit count. [Knight job reference](https://grandchase.fandom.com/wiki/Knight).

The Knight skill tree documents A/S/D shortcuts and charged Z alternatives, as well as combo branches, launchers, critical/double attacks and job-specific abilities. Its current and historical sections differ. The adaptation selects three direct shortcut skills, three basic combo stages and explicit cancel windows; it leaves charged skills, the complete skill tree and PvP grabs outside this slice. [Knight Skill Tree](https://grandchase.fandom.com/wiki/Knight_Skill_Tree).

The 2010 Elesis movement guide is useful historical evidence for rocket step (dash followed closely by jump), shadow step (adding Down), direction reversals and platform-dependent techniques. It also describes Otto as a drop-and-air-attack interaction that can return the player to a platform. These are timing and state interactions, rather than independent buttons. Here dash-jump momentum and Down fast-fall support an approachable rocket/shadow-step interpretation. Exact legacy exploit timing, Otto loops and the complete named-step taxonomy are not reproduced. These historical descriptions are not evidence that every technique behaves identically in current Classic. [Historical Elesis movement guide](https://www.webcheats.com.br/topic/410954-tutorial-guia-de-user-elesis/).

The affliction reference distinguishes launches, knockdown/recovery and MP-consuming defensive counters. That distinction matters: hit reactions affect what an opponent can do next. The implementation provides health, interruption, hitstun and launch knockback; it does not implement defensive MP counters or a full knockdown/get-up state machine. [Game Afflictions](https://grandchase.fandom.com/wiki/Game_Afflictions).

Sword Master's description shows that another Elesis job has different attack coverage and can dash after a heavy cleave. This supports keeping cancel rules explicit per action, but it is not treated as proof that all Knight attacks share that job's behavior. [Sword Master reference](https://grandchase.fandom.com/wiki/Sword_Master).

Fandom's full pages were blocked by robots.txt during research. The indexed excerpts were available and used; claims above stay within those excerpts. Historical forum material and community documentation supplement KOG's official product description. Original numeric acceleration, gravity, invulnerability frames and collision tolerances remain unknown.

## Mechanical priorities

| Feature | Why it matters | Project decision |
| --- | --- | --- |
| Immediate horizontal control | Position determines whether a melee hit connects | Accelerated walking, quick direction changes and no depth movement |
| Double-tap dash | Makes rapid approach and retreat part of ordinary input | Direction-specific tap tracking; Shift and controller bumper alternatives |
| Ground/air distinction | Different attacks and transitions create traversal/combat choices | Ground combo, dash slash and air slash selected from movement state |
| Dash-jump chaining | Preserves approach speed while changing height | Short horizontal momentum window after a dash jump |
| Down during flight | Lets the player lower a jump arc and reach another level | Fast-fall that can cut a rising jump |
| One-way platforms | Vertical routes remain accessible without solid undersides | Per-actor collision pairs; rising/below-platform actors pass through |
| Down while supported | Lets the player change levels deliberately | Ignore only the supporting platform; floor stays solid |
| Action commitment | Hits need meaningful startup, impact and recovery | Damage pulses have explicit times; startup cannot be canceled |
| Recovery cancellation | Movement and skills can connect without a long animation lock | Basic attacks cancel after 0.19 seconds; skills after their last pulse plus 0.08 seconds |
| Enemy interruption | Combos should change enemy behavior | Hits cancel a telegraphed attack, stun and apply knockback |
| Finisher launch | Gives combo endings a distinct role | Third normal slash, final Mega Slash pulse and Critical X launch |
| MP and cooldown | Prevents skills from replacing all basic combat | Server-owned MP, costs and cooldowns; normal hits recover MP |

These decisions prioritize readable, repeatable controls. They are design choices for this prototype, not claims about exact Grand Chase constants.

## Component responsibilities

MechaPlayer originally contained `PlayerInputs`, `PlayerMovement`, `PlayerAttack`, a Rigidbody, CapsuleCollider, Input System PlayerInput, FishNet NetworkObject, NetworkAnimator and NetworkTransform. The root was inactive in the asset and a disabled scene instance also existed. Its sword child had a trigger collider; the enemy despawned immediately when that trigger touched it.

The revised prefab retains the original separation:

- **PlayerInputs:** input callbacks, ownership, double-tap detection, jump-held state and suppression while editing a relay code.
- **PlayerMovement:** fixed-step horizontal velocity, facing, support detection, jumps, dash/run states, gravity and platform descent.
- **PlayerAttack:** action selection, combo buffer, cancel windows, skill costs/cooldowns, server hit queries and observer presentation.
- **PlayerHealth:** health, damage immunity, ordinary-hit interruption, skill super armor and respawn.
- **PlayerCombatVfx / CombatEffect:** original arcs, sparks, dash traces and floating damage numbers; no damage authority.
- **OneWayPlatform:** independent collision-pair management for players and enemies, including cleanup after disable/despawn.
- **EnemyController:** server-owned pursuit, attack telegraph, damage, stun, launch and death.
- **EnemyPresentation:** flashes, sparks, damage numbers and death shrink.
- **ChaseGameplayHud:** local player's health, MP, action and skill readiness display.

The old sword trigger is disabled. Damage comes from scheduled overlap queries, so holding an overlapping sword does not continuously damage or instantly despawn an enemy.

## Controls

| Action | Keyboard / mouse | Gamepad |
| --- | --- | --- |
| Move | Left / Right | Left stick |
| Jump | Up / Space; hold for full height | South face button |
| Descend / fast-fall | Down | D-pad Down |
| Attack | Z / left mouse | West face button |
| Dash | Double-tap Left/Right or Shift | Right bumper; double-tap stick direction also works |
| Mega Slash | A | North face button |
| Sword Fire | S | East face button |
| Critical X | D | Left bumper |
| Start local host | 1 | Existing connection UI |
| Join configured server | 2 | Existing connection UI |

Press 1 after entering Play Mode to start the local FishNet host and spawn the player. The existing relay connection flow is retained. Jump → dash, dash → jump → Down, and attack → dash after the hit window are useful practice sequences. One air dash is available between landings; there is no double jump.

## Editable prototype tuning

| Setting | Value |
| --- | --- |
| Walk / continued run / dash | 4.5 / 7 / 11.5 units per second |
| Dash duration / post-dash cooldown | 0.18 / 0.12 seconds |
| Horizontal double-tap window | 0.23 seconds |
| Initial jump velocity | 10 units per second |
| Rising / falling gravity | 24 / 40 units per second squared |
| Released-jump / apex gravity | 52 / 14 units per second squared |
| Maximum falling speed | 20 units per second |
| Coyote time / jump buffer | 0.10 / 0.12 seconds |
| Rocket momentum window | 0.28 seconds |
| Down fast-fall / platform-drop initial velocity | -6 / -4 units per second |
| Platform drop grace | 0.45 seconds, with below-platform collision handling continuing as needed |
| Enemy health / player health | 90 / 100 |
| Enemy approach speed / attack warning | 1.7 units per second / 0.48 seconds |
| Enemy hit damage / player damage immunity | 12 / 0.9 seconds |
| Enemy population / spawn interval | 6 maximum / 4 seconds |
| MP capacity / passive regeneration / basic-hit recovery | 100 / 7 per second / 4 per target hit |

All gravity is applied by the actor components; Rigidbody built-in gravity is disabled. Rotation and Z movement are constrained. The arena's adjacent platform levels are about 1.85 units apart so a held jump reaches the next tier. There are three lower platforms, two middle platforms, a summit and a solid floor.

## The three skills

| Shortcut | Action | Pulses / damage | MP / cooldown | Presentation and use |
| --- | --- | --- | --- | --- |
| A | Mega Slash | 8 + 8 + 8 + 18 = 42 | 25 / 3 seconds | Ruby arcs and sparks; close-range sequence with a launching finisher |
| S | Sword Fire | 14 × 3 = 42 | 40 / 5 seconds | Amber wave pulses advancing forward; wider horizontal reach |
| D | Critical X | 28 × 2 = 56 | 60 / 7 seconds | Violet and white opposing arcs; taller coverage and launch |

The damage total assumes the enemy remains inside every pulse. Knockback, height, target movement and spacing can cause later pulses to miss. Skills grant protection from hit interruption, while damage still reduces health. They do not provide global invulnerability or freeze other players. Existing attack animation is retimed for each action; effects and hit pulses provide the different skill identities. Bespoke Elesis animations and audio were not imported.

## Networking and limits

Player motion retains the project's client-owned NetworkTransform approach. Input is enabled only on the owner, remote player physics is kinematic, and enemy simulation belongs to the server. Attack RPCs require ownership and validate the action, direction, recovery window, cooldown and MP. Only the server changes network enemy health, queries hits and awards hit MP. Effects are also presented to observing clients.

This prevents client-supplied enemy damage amounts, but it is not a complete anti-cheat solution: player positions and mobility remain client-owned. Dedicated-server latency, multiple independent clients and Relay over the internet were not exercised in this session. The local host path was exercised in the running Editor.

Not included in this gameplay slice: full dungeon progression, bosses, equipment/loot, character jobs, the complete Knight skill tree, PvP grabs and counters, frame-perfect historical step exploits, Otto loops, new character art, bespoke animation sets or production VFX pooling. Current gravity curves and cancel times can be tuned independently of the input and presentation components.

See [Implementation and runtime validation](ProcessAndValidation.md) for the actual work log and observations.

The subsequent [refinement pass](Enhancements.md) adds held descent, a closer follow camera, the rising combo branch, improved buffers and landing cancellation. Runtime script names now use Mecha terminology, and Unity Toon Shader is installed.
