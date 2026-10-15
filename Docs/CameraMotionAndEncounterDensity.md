# Camera motion and encounter density — 2026-10-14

Historical camera tuning: the next pass replaces the airborne lock with gentle Y follow, doubles landing duration to 0.96 s, increases zoom amplitudes and adds stronger anti-aliasing. See [latest camera and rendering settings](SoftCameraAndRendering.md). The 18-enemy quota remains current.

The user found the previous 0.16-second vertical correction too abrupt and requested smoother landing motion, three times the enemy quota, skill zoom-out/return and a smaller, smoother dash zoom.

## Tuning in this pass

All values are authored in ScriptableObjects under `Assets/_Project/Settings`.

| Profile | Values |
| --- | --- |
| RoomCamera | Landing blend 0.48 s, native EaseInOut curve with zero slope at both ends. No landing delay. Height tolerance 0.06, feet viewport 0.24, base orthographic size 3.485. |
| RoomCameraZoom | Skill target +8% lens size, SmoothDamp entry 0.12 s and return 0.35 s. Dash target +3%, entry 0.24 s and return 0.5 s. These are damping parameters, not fixed completion durations. A brief dash can finish before reaching its target. |
| EnemySpawning | Quota 18, increased from 6. Initial delay 0.4 s, spawn interval 0.8 s and existing placement safeguards retained. The encounter remains finite. |

Camera Y still holds throughout airborne movement. On landing, the slower curve begins gently, accelerates through the middle and eases to rest. This avoids the previous curve's immediate high starting velocity. The [earlier reference research](LandingCameraRefinement.md) remains relevant for stable stage framing, but does not establish the original game's exact landing interpolation. The new duration is project tuning in response to user feedback.

RoomCameraZoom is a separate presentation component. It observes the focused local player's active skill/dash presentation rather than raw button presses. Buffered inputs wait until an action starts; a server rejection ends that predicted presentation and releases its zoom. Skills take precedence if states overlap. Absolute offsets and retained smoothing velocity blend cancellation/chains without stacking zoom. Ending/cancelling the action releases toward the normal lens size. The component also restores the base lens when disabled.

Zoom runs before RoomCameraRig. The rig compensates its tracking point using the current lens while retaining base-lens grounded composition. Thus a zoom does not itself move the held camera Y. Lens changes invalidate Cinemachine Confiner 2D's lens cache so enlarged framing respects the room. Existing physical walls remain separate. The repeatable camera setup creates/assigns the new component and profile through Unity APIs, preserving authored tuning on later reapplication.

## Process and manual validation

Official Unity CLI 1.0.0-beta.13 was current. An MCP inspection call stopped returning; the CLI confirmed the same Editor was ready and completed the work through the official local Pipeline connection. Scripts compiled successfully. Configuration assets and MainScene were saved with Unity APIs.

Entered Play Mode, confirmed the frame counter advanced and started the local host. Temporary player protection, slow motion and player relocations isolated the camera observations. A recorder collected state from manual actions without supplying inputs or assertions. No automated tests were created or run.

- The spawner reached SpawnedCount=18 and IsFinished=true, and still reported that completed quota at the end of the session.
- In the platform jump, camera Y remained exactly -1.687800 while airborne. On the first grounded sample at time 53.10091 it was -1.687775, showing a gentle start. About 0.08/0.16/0.24/0.32/0.40 seconds later, Y was -1.552726/-1.211823/-0.777437/-0.343301/-0.009238. It reached 0.122200 around 0.48 seconds after landing.
- All three skills were manually activated. Skill lens size rose toward 3.7638 (+8%). After the first skill ended, size returned to approximately 3.485000 and camera Y remained -1.687800.
- An isolated dash peaked around size 3.550297 (+1.87%), below the 3% target because the brief dash ends during its gentler entry. It then returned smoothly.
- Cancelled SwordFire directly into a dash: IsSkillActive changed true to false and IsDashing became true while lens size remained continuous at about 3.763795. The effect then blended toward the smaller dash framing and normal size.
- Inspected the captured live Game View. Near the left boundary, a later zoom-return sample placed the left viewport edge at -36.50001 against the authored -36.5 boundary, within floating-point tolerance. At the end, the lens was exactly 3.485 again.

The runtime console reported zero errors. Final source review and compilation were clean. Unity's scene serialization includes its usual blank YAML fields with trailing spaces; these were not hand-edited. Validation covered the local host and current flat platform arena, without a remote-client or frame-rate benchmark.

Restored the original unpaused Edit Mode, time scale 1 and run-in-background false. Runtime protection, slow motion and relocations were discarded. Reference captures and the observation CSV are kept in ignored Logs outside shipped assets.
