# Gameplay refinements — 2026-10-14

Implemented from the supplied gameplay screenshot and the user's adjustment list. The image informed framing only; no models or reference images were created or replaced.

## Result and tuning

| Area | Change | ScriptableObject under `Assets/_Project/Settings` |
| --- | --- | --- |
| Camera | Orthographic size 4.1 → 3.485: visible height reduced 15%, preserving the room framing, dead zone and vertical threshold behavior. | RoomCamera |
| Dash | Every accepted ground/air dash cancels an action immediately, including its startup, recovery and queued follow-ups. Protection lasts while dashing; starting an attack ends it. | PlayerMovement, PlayerCombat |
| Enemy pursuit | Speed 1.7 → 3.6, aggro range 7 → 75, target search 0.35 → 0.18 seconds, windup 0.48 → 0.24 seconds, recovery 1.2 → 0.65 seconds. | EnemyBehavior |
| Platform traversal | Enemies select reachable collider surfaces, jump upward/across gaps and use OneWayPlatform.Drop to follow downward. Jump speed 11.6; asymmetric gravity 24 rising / 40 falling. | EnemyTraversal |
| Crowd | Velocity anticipation and penetration relaxation maintain local spacing. Allowed overlap rises from 20% to 40% with nearby density. Actor-to-actor solid collisions remain disabled. | EnemyCrowd |
| NPC animation | Existing idle/run clips explicitly loop; movement, airborne and attack presentation remain separate. Grounded state is replicated for remote presentation. | EnemyAnimation |
| Arena | Floor expanded from 24 to 72 units, with 20 staggered platforms and separate physical walls/camera limits. Player starts toward the left side. | ArenaLayout, RoomCamera |
| Spawning | 16 markers span the room and its elevations. Initial population 6, cap 16, interval 1.5 seconds; avoid spawning within 3.5 units of a player and prefer space away from existing enemies. | EnemySpawning, ArenaLayout |

Skills cancelled by a dash keep their spent mana and cooldown. Previously emitted particles finish visually, while future damage pulses are cancelled. Protection is tied to an accepted dash window on the server, rather than a separate configurable grace period after the dash.

The crowd overlap target uses capsule width, not the full silhouette of weapons and limbs. Routing prioritizes a successful airborne landing; knockback and airborne crossings can temporarily exceed the soft spacing target.

## Implementation process

Used the official Unity MCP and CLI to inspect the open scene and profiles, compile changed scripts, edit prefabs/material settings through Unity APIs, and operate Play Mode. The CLI update check confirmed 1.0.0-beta.13 was current.

Kept PlayerInputs, PlayerMovement, PlayerAttack, PlayerHealth, animation and effects responsibilities separate. Added EnemyMotor, EnemyPlatformNavigator and EnemyCrowdAvoidance, each with a focused responsibility. Navigation uses the registered one-way colliders plus the actors' support colliders; its reachability model matches jump speed and asymmetric gravity. EnemyController retains server combat authority.

MechaArenaSetup reads the authored ArenaLayout profile. The regular **Tools → Mecha → Apply Mecha Gameplay** and **Apply Character and Presentation** workflows now use that layout rather than the former small-room coordinates. The selected clips, new component references and particle profiles were saved through Unity APIs. Platform shadow casting remains disabled.

During the first crowd observation, enemies compressed farther than intended. Added velocity anticipation so a pursuer slows before contact instead of relying only on a small separation force. A setup call briefly returned HTTP 400 during compilation/reload; the Editor reported ready afterward and the same setup then completed successfully.

## Play Mode observations

- Confirmed the frame counter advanced after entering Play Mode; started the existing local host through GameManager/the network managers.
- A ground Slash1 changed immediately to Ready when dashing. A damage call during that dash left HP at 100; a later call after the dash reduced HP to 90.
- A jumping AirSlash changed immediately to Ready on a left dash, with IsDashing true and horizontal velocity -11.5.
- On the final authority check, the host had IsDashing true while serverActive was false and the server dash window was active. Starting DashSlash then changed IsDashing to false and cleared serverDashUntil to zero.
- An isolated pursuer climbed from the floor to Upper Deck 01 and stood there at approximately y=0.17. When the player returned to the floor, it descended and resumed pursuit on Ground at approximately y=-3.49.
- Observed moving crowds using allowed-overlap values from 0.20 to 0.40. A dense moving sample had adjacent center gaps around 0.267–0.381 units for capsules about 0.415 units wide, consistent with the softened spacing target rather than the previous deep compression.
- Held movement through the expanded room. At the right boundary, the player stopped at x≈35.73, with zero horizontal velocity against the wall at x=36. The camera stopped at x≈30.30, whose right view edge was x≈36.50, matching the configured framing padding.
- Confirmed the enemy Run clip reports looping. Observed dash smoke/streak/debris systems and skill/hit effects in Play Mode, and inspected screenshots of the arena and combat.
- No runtime console errors were reported in the completed passes. No automated tests were created or run. This was local-host validation, not a separate remote-client or latency simulation.

Temporary inspection changes—slow motion, player positioning, isolated/disabled enemies and extended protection for observing pursuit—were confined to Play Mode and discarded. Final state: MainScene active and clean, Edit Mode, unpaused, time scale 1, and the original background-runtime setting restored to false.

## Final inspection

Compilation completed successfully. The final Unity inspection reported orthographic size 3.485, floor width 72, 20 platforms, 16 spawns, zero shadow-casting platforms, zero missing particle profile/material references and zero missing scene scripts. The enemy prefab contains motor, navigation and crowd components; the Run clip loops.

Local inspection captures are retained outside Assets in ignored `Logs/GameplayRefinements/`: `arena-overview.png`, `enemy-climbing.png`, `enemy-upper-deck.png`, and `skill-effects.png`. These are observations from intermediate Play Mode moments, not imported gameplay assets. The isolated upper-deck capture is the clearest record of a successful climb; the earlier climbing capture includes the player's knockback back toward the floor.

Future tuning should edit these ScriptableObjects rather than the temporary inspection helpers in Logs. The saved scene and prefab changes are the delivered implementation; reapplying presentation reads the saved profiles without repeating the one-off tuning migration.
