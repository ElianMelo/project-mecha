# Encounter states and landing camera — 2026-10-14

This pass replaces continuous enemy replenishment and global aggression with finite encounters and range-driven enemy states. Its camera and six-enemy quota were subsequently revised; see [current camera tuning](SoftCameraAndRendering.md) and [18-enemy encounter update](CameraMotionAndEncounterDensity.md). Enemy state tuning remains current. Implementation and observations later in this document describe this historical pass.

## Authored configuration

All profiles are in `Assets/_Project/Settings`.

| Profile | Current tuning |
| --- | --- |
| PlayerAwareness | Hard radius 3.5, soft radius 8, large radius 16 world units. Ranges are centered on each player and use distance in the side-view XY plane. Selected player objects show native Unity wire-sphere gizmos. |
| EnemyStates | Wander hold 2–3.5 s; Suspicious retention 1.5–2.8 s; Alert retention 3–4.5 s. Boundary delay 0.35–0.8 s and exit hysteresis 0.45 units. Durations are sampled per enemy on entering a state. |
| EnemyStates | Wander speed 1.25, suspicious speed 2.1; patrol radius 7 and vertical radius 4 around the enemy's spawn home. Patrol pauses 0.8–1.8 s, travel decisions every 3–5 s, and 50% chance to consider another reachable platform. |
| EnemyBehavior | Alert keeps the previous chase speed 3.6 and existing attack timings. |
| EnemySpawning | Eighteen total enemies, initial delay 0.4 s, spacing between spawns 0.8 s. A completed encounter does not replace defeated enemies. |
| RoomCamera | Current: gentle airborne follow and 0.96 s ease-in/ease-out landing blend, height tolerance 0.06, settled feet at 24% of viewport height, base size 3.485. Separate skill/dash zoom profile. Earlier camera timing below is historical. |

The large range selects Wander; outside all ranges, enemies also wander rather than seeking a distant player. A hard-range entry immediately enters Alert even during another state's hold time. Other transitions wait for the randomized dwell/retention time and boundary delay. Alert and Suspicious retention refresh while their corresponding range is still sensed, so a long-active enemy retains its state for a few moments after losing that range. Damage also alerts the enemy toward a known/nearest living player.

Wander pauses and chooses local walking/platform goals, including upward and downward routes. Suspicious moves toward the sensed player position without starting attacks. Alert pursues and attacks. Crowd avoidance remains shared across states. An in-progress airborne route can finish its landing before replanning.

Finite spawning counts lifetime placements rather than current living enemies. Invalid or player-blocked spawn markers are retried a bounded number of times; if every permitted attempt is blocked, that slot is skipped with one completion warning. A new server session starts a new finite encounter.

## Implementation

- Added PlayerAwareness and PlayerAwarenessSettings to the player prefab. A lightweight active-player registry supplies server perception without creating extra physics trigger colliders.
- Added EnemyStateMachine, EnemyStateSettings, IEnemyState, WanderState, SuspiciousState and AlertState. State objects hold runtime memory; shared ScriptableObjects hold authoring data. EnemyController retains damage, windup and attack authority, and replicates the current state to clients.
- Generalized EnemyPlatformNavigator with MoveTo and reachability queries so patrol and pursuit share the same Unity collider routes. Wander uses Unity physics queries to choose local surfaces.
- Changed EnemySpawner to place the configured quota once, then finish. SpawnedCount is cumulative for the encounter and IsFinished exposes completion.
- Changed RoomCameraRig to hold the actual output height during airborne motion, wait for stable support, and smoothly move toward a grounded framing height. Cinemachine continues horizontal composition and room confinement. Vertical composition is supplied through the tracking point, so the old vertical edge snap cannot pull the camera during a jump. Only X of the existing dead-zone/hard-limit vectors is used; the new landing fields govern Y.
- Updated repeatable character/presentation setup to assign the new profiles and components through Unity APIs. Saved MainScene and prefab changes through Unity APIs.

The official Unity CLI update check reported 1.0.0-beta.13 as current. Unity initially imported the new source files before their companion API changes were complete, producing temporary compilation messages; the completed source compiled successfully. One recompile request overlapped that reload and returned HTTP 400, while the subsequent state/compilation reads confirmed ready with no compile failure. No automated tests were created or run.

## Manual Play Mode observations

Confirmed the player loop was advancing and started the existing local host. The first encounter placed six enemies and reported IsFinished=true. Distant enemies were observed in Wander, walking and routing between elevations; one began at approximately (12, 2.16) and was later on an upper platform around y=0.17 while still wandering. Suspicious movement was observed at about 1.62 horizontal units/s on the floor; ground friction means measured motion can be below the authored speed.

For the corrected camera, a floor jump began with camera Y=-1.6877923. While player Y rose from -3.5 to -2.3382 and then -1.6978, camera Y remained exactly -1.6877923. After landing on Lower Deck 01 at Y≈-1.69, camera height initially stayed fixed, then progressed gently through -1.68354 and -1.60310. It subsequently settled near Y=0.1222, matching the configured grounded viewport framing.

For state timing, an enemy reset to Wander showed a fresh hold of 2.7777 s; moving the player into its hard range caused Alert immediately on the following simulation update, with a new randomized Alert hold of about 4.15 s. At a distance of 6.5, a separate observation changed Wander to Suspicious and showed retention around 2.33 s. With movement temporarily held still for inspecting timing, an Alert enemy retained Alert at distance≈11.99 with about 2.99 s remaining, then returned to Wander after that retention expired. These observation holds and player relocations were temporary runtime changes.

All six enemies were then defeated. The spawner remained finished with cumulative SpawnedCount=6 and zero remaining enemies in later reads at game times 155.71 and 162.89. It did not replenish them.

The completed runtime pass and final Editor console reported zero errors. Compilation completed with no errors; the final scene inspection reported zero missing scripts. Validation used a local host, not a second remote client or latency simulation.

Stopped Play Mode and restored Edit Mode, unpaused, time scale 1 and background runtime false. MainScene remained active and clean. Temporary positions, protection, stun holds and defeated enemies were discarded with Play Mode. Future tuning should use the profiles above; the setup helper in ignored Logs records the one-off migration rather than a runtime dependency.
