# Soft airborne camera and anti-aliasing — 2026-10-14

The next pass adds immediate support-contact framing at floor/mid/high heights, a compact HUD, scenario art, destructibles and Prediction enemies. See [current implementation](ScenarioAndPrediction.md). Airborne smoothing, the 0.96-second landing ease, action zoom and PC anti-aliasing below remain in use.

The user requested slight smooth camera Y movement during jumps, twice the landing transition time, smoother pixels and larger action zoom effects. These settings supersede the airborne lock and 0.48-second landing transition in CameraMotionAndEncounterDensity.md. The encounter quota remains 18.

## Authored configuration

Profiles are under `Assets/_Project/Settings`.

| Profile | Current values |
| --- | --- |
| RoomCamera | Airborne follow weight 0.18, offset cap 0.55 world units, SmoothDamp time 0.35 s, maximum speed 1.2 units/s. |
| RoomCamera | Landing duration 0.96 s, exactly twice the previous 0.48 s. Native EaseInOut curve with zero slope at both ends; no contact delay. |
| RoomCameraZoom | Skill target +9.6% lens size: previous 8% × 1.20. Dash target +3.3%: previous 3% × 1.10. These are relative amplitude increases, not extra percentage points. Existing entry/return damping remains unchanged. |
| Rendering | PresentationRenderSettings referencing `Assets/Settings/PC_RPAsset.asset`: 4× MSAA, render scale 1.0, high-quality SMAA, camera post-processing enabled. |

Each takeoff captures the visible camera height and prior supporting surface height. Only a small portion of subsequent player displacement contributes to the camera target, bounded and damped through Unity's SmoothDamp. Falling follows the same rule. Landing starts the long eased correction from the current camera height, preserving position continuity. A subsequent takeoff reanchors the soft follow at the current visible height.

The existing Cinemachine composition, room confinement, action zoom component and tracking-point lens compensation remain active. Airborne targets are clamped to the room's valid height for the current lens. Grounded composition remains based on the normal lens so action zoom does not independently shift its framing target.

The active PC renderer previously used one sample and camera FXAA. The new Rendering profile applies native 4× MSAA plus high-quality SMAA, retaining the hard toon shade bands. MechaRenderSetup applies the profile from the repeatable camera setup through Unity APIs. Rendering configuration is applied in Edit Mode; runtime camera code does not modify shared pipeline assets. The existing mobile pipeline retains its own tuning; this pass configures the active PC presentation.

## Process and manual validation

Used the official Unity CLI and local Pipeline connection; the update check confirmed 1.0.0-beta.13 was current. Scripts compiled successfully. Updated the profiles, native PC pipeline asset and MainScene through Unity APIs.

Entered Play Mode, confirmed the frame counter advanced and started the local host. A temporary recorder observed manually triggered jumps, a skill, a dash and a platform descent. Slow motion, temporary player protection, runtime repositioning and clearing the completed encounter isolated camera motion. No automated tests were created or run.

- During the first jump, player foot height moved from approximately -3.5 to -1.65. Camera Y moved gently from -1.687800 to about -1.445399, approximately 0.24 units of follow.
- First grounded sample at time 31.00152 showed camera Y=-1.444929. At about 0.16/0.32/0.48/0.64/0.80 s after contact it was -1.327151/-1.035750/-0.659305/-0.281256/0.006693. At 0.9605 s it reached the 0.122200 target.
- During descent, camera Y moved from 0.122200 toward 0.083054 before ground contact. The subsequent long landing ease reached approximately -1.687799 after 0.96 s.
- The increased skill effect reached lens size 3.819553, close to the configured 3.819560 target (base 3.485 × 1.096). The dash observation reached approximately 3.557066 during its entry/return interval; brief dashes need not reach their full 3.3% target. Both effects returned to base size 3.485.
- Inspected the captured live Game View. Runtime reads confirmed the active pipeline was PC_RPAsset with sample count 4 and scale 1, and camera SMAA quality High with post-processing enabled. The device's supported MSAA sample-count query returned 4.
- Also manually jumped at ordinary time scale. The encounter still completed its quota of 18. Runtime console reported zero errors and no rendering warnings; existing FishNet obsolete-API compilation warnings were unrelated to this pass.

Restored unpaused Edit Mode, time scale 1, background runtime false and the original Editor quality level 1. MainScene remained clean. Temporary runtime changes were discarded. Reference capture and observation CSV are stored in ignored Logs outside shipped assets. Validation covers the current PC pipeline/local host and flat arena; no remote-client or performance benchmark was performed. Four-sample rendering adds GPU cost compared with the previous single sample.
