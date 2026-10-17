# Landing camera refinement — 2026-10-14

Historical pass: the user subsequently found this 0.16-second ease-out too abrupt. Further passes added longer easing, airborne follow and skill/dash zoom; see [current settings and validation](SoftCameraAndRendering.md). The research and measurements below describe this earlier implementation.

The user rejected the previous slow vertical recenter. The requested behavior is to freeze camera Y during the jump, then adjust almost instantly on landing with a smooth finish. This replaces the camera portion of EncounterStatesAndCamera.md.

## Reference research

Reviewed KOG's [official game page](https://www.koggames.com/game/grandchaseclassic), [guide index](https://gcp.koggames.com/guides/) and [official Steam media gallery](https://store.steampowered.com/app/985810/GrandChase/). The linked YouTube guide was stopped by a verification screen, so no conclusions were drawn from that video. Public Steam trailer metadata supplied official media for local frame inspection, stored only in ignored Logs; no reference art was imported into Unity.

- **5_intro** is an animated cinematic, so it is unsuitable for inferring gameplay camera motion.
- **6_circus** (35.9 seconds) shows stage combat, but places gameplay in moving, tilted presentation panels. Its trailer framing must not be confused with the in-game camera.
- **New Character Iris** (51.1 seconds) contains ordinary side-view gameplay. Inspected its overview and consecutive frames at 0.25-second intervals from 18–22 seconds. Within continuous shots, platform/background framing stays stable while the character rises during attacks; characters can occupy different vertical positions rather than being held at screen center. Scene cuts and skill presentation changes were excluded from camera-follow conclusions.

The observed stable stage framing supports holding Y through ordinary airborne movement. These edited clips do **not** establish the original game's exact landing interpolation, duration, or behavior across all rooms and camera modes. The immediate 0.16-second landing blend is this project's tuning to implement the user's direction, not a claimed reconstruction of proprietary camera code.

Official media identifiers: Iris 257286231, Circus 256823067, Intro 256823066, returned by the [public Steam application metadata](https://store.steampowered.com/api/appdetails?appids=985810&filters=movies). The Iris frames came from [KOG's Steam-hosted media stream](https://video.akamai.steamstatic.com/store_trailers/985810/147870267/bb804505941e30705482f97b63d0ab8616d82e42/1770952915/dash_h264.mpd?t=1770956682).

## Implementation and tuning

RoomCameraRig cancels its vertical adjustment on takeoff and holds the actual output camera height. On the first grounded update, it targets the supporting collider's top, applying the existing feet framing and room limits. Ground-probe and collider-settling offsets no longer shift that target. This support-height policy applies to the current flat platforms.

RoomCameraSettings owns the authored values in `Assets/_Project/Settings/Camera/RoomCamera.asset`:

| Setting | Value / purpose |
| --- | --- |
| Landing blend duration | 0.16 seconds, starting immediately on landing |
| Landing blend curve | Native Unity AnimationCurve; cubic ease-out, 87.5% correction halfway through, zero slope at completion |
| Height tolerance | 0.06 world units; suppresses support-height noise |
| Settled feet viewport | 0.24; preserves existing grounded composition |
| Orthographic size | 3.485; unchanged |

Removed the settle-delay timer, SmoothDamp tail and maximum vertical speed. Jumping during an unfinished blend freezes its current visible height; the next landing starts a new blend. Returning to the same surface height produces no vertical correction once framing has settled. Cinemachine Position Composer continues horizontal follow, and Confiner 2D continues room confinement.

Changes were applied through the official Unity MCP, with the current Unity CLI (1.0.0-beta.13). C# edits compiled successfully. The profile and scene were updated and saved through Unity Editor APIs using the existing camera setup helper.

## Manual Play Mode observations

Started the local host and confirmed the frame counter advanced. Temporarily protected the player and cleared the finite encounter to isolate camera movement. Used slow motion for observing individual contact/blend frames, then checked ordinary speed. A temporary telemetry recorder observed the manual session; it supplied no gameplay inputs or assertions. No automated tests were created or run.

| Manual action | Observed camera behavior |
| --- | --- |
| Floor jump to Lower Deck 01 | Airborne Y stayed exactly -1.687800. First grounded sample at game time 23.96029 already moved to -1.675581. It reached 0.122194 by 24.11747, within 0.00001 of the 0.122200 target after about 0.157 s. |
| Descend through Lower Deck 01 | Airborne Y stayed exactly 0.122200. Ground contact at 24.44032 began the downward correction immediately; it reached -1.687794 by 24.59759. |
| Jump before a landing blend finished | The incomplete correction stopped at -1.088363 and stayed exactly there through the following airborne interval, 25.88066–26.40063. |
| Next landing on Upper Deck 01 | Correction started on contact and reached 1.972196 by 26.55815, about 0.158 s after the first grounded sample. |
| Ordinary-speed lower landing | From camera Y=1.972200 while airborne, the first sampled contact had already corrected to 1.573173. It reached the 0.122200 target about 0.150 s later. |

Also manually checked a floor jump returning to the floor, inspected the rendered Game View and reviewed the final source/configuration. The runtime and final console reported no errors. This pass validates the local-host camera with current flat arena surfaces; it does not validate slopes, moving platforms or separate remote clients.

Stopped Play Mode and restored the original unpaused Edit Mode state, time scale 1 and run-in-background false. MainScene remained active and clean. Temporary runtime positions, slow motion, enemy removals and player protection were discarded. Detailed observation CSV and reference frames remain in ignored Logs, outside shipped assets.
