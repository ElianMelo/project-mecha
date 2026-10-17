# Configuration and combat layout — 2026-10-17

Esc/Start opens Resume, Lobby/Squad, Audio and Video. The audio section now contains only Defaults and Back to Options; lobby access stays on the home screen. Its persistent Sprite focus outline surrounds the full label/slider/value row, independent of optional motion. Native Slider handles stretch to the rail height rather than adding their own height to it. Canonical authoring preserves both changes.

The lobby's existing direct-host entry is visibly **Solo Host**. It calls the same `GameManager.StartHost` as keyboard 1, using local UnityTransport/FishNet connections without a Relay allocation. Controller Down from Create Lobby reaches Solo Host. Local Join remains available.

## Confirmed controller layout

| Action | Xbox-style button | PlayStation equivalent |
| --- | --- | --- |
| Primary light attack / combo | X | Square |
| Primary heavy attack | Y | Triangle |
| Mega Slash / 25 MP | B | Circle |
| Sword Fire / 40 MP | LB | L1 |
| Critical X / 60 MP | LT | L2 |

Heavy uses the existing primary loadout branch internally named Tech (currently MagneticPull). This is an input/presentation refactor; it preserves existing action profiles, resource costs, cooldowns and authority. Keyboard Z/X/A/S/D and other controls remain. Portals continue to consume held Light Attack (Z/X), with no stick-click travel binding. Central device hints, HUD skill hints, authored controller help, diagnostic text and repeatable input authoring use the new layout. B remains Back in menus; normal menu/input suppression prevents it also casting the 25 MP skill when navigating.

## Native video configuration

Native TMP dropdowns provide display mode, resolution, refresh rate, quality preset, VSync and frame limit. Supported resolutions come from `Screen.resolutions`, deduplicated by dimensions and sorted. A current custom window size remains selectable and can be restored. Refresh options retain the reported fractional refresh ratio for each resolution; only Windows exclusive fullscreen exposes refresh selection. Borderless/windowed use the desktop refresh. Modes unavailable on the device are omitted. The current desktop build exposes one PC quality preset, so its selector is disabled and skipped rather than offering artificial presets.

Values are staged until Apply. Defaults stages the native desktop size, borderless mode, current authored quality preset and profile defaults. Confirmed device preferences use PlayerPrefs; quality is stored by name because build-time stripping changes quality indices. Existing windowed/VSync preferences migrate. Saved dimensions/rates/modes are checked against current capabilities on startup. Changing quality is followed by applying VSync and frame pacing so a preset cannot overwrite the chosen synchronization setting. VSync controls frame pacing when enabled; the frame-limit selector is disabled with an explicit caption. With VSync off, the native target frame rate supports 30/60/90/120/144/165/240 FPS and Unlimited.

Display changes open a Keep/Revert overlay with a realtime 15-second countdown and initial focus on Revert. Timeout, Back, or closing the screen before confirming restores the prior display, quality and frame-pacing state. Only Keep persists display changes. Underlying controls are disabled while the overlay is open; navigation stays within the two confirmation buttons. Back first closes an expanded native list, then returns from the section. Lists, active settings and footers have controller focus borders; disabled controls are excluded from navigation. Footer buttons support horizontal and vertical navigation. Audio routes and volume preferences are independent of video settings.

`VideoConfigurationSettings` in Settings/Interface owns frame limits/defaults and confirmation duration; runtime drafts never mutate it. No package or artwork was added. Native TMP/ScrollRect lack automatic viewport tracking of an offscreen controller selection, observed during manual inspection. The small `UiSelectionScroll` adapter fills that specific gap by adjusting native scroll content after selection changes; native Toggle selection and dropdown behavior remain responsible for interaction. Existing sliced Sprite assets supply every focus outline and dropdown graphic.

## Research basis

Unity's [Screen.SetResolution](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Screen.SetResolution.html) documents deferred application, monitor-supported resolution fallback, Windows-only exclusive fullscreen and refresh switching only in exclusive mode. [Screen.resolutions](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Screen-resolutions.html) supplies the supported modes. [QualitySettings.vSyncCount](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/QualitySettings-vSyncCount.html) and [Application.targetFrameRate](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-targetFrameRate.html) explain VSync precedence and recommend hardware synchronization for smooth desktop frame pacing. [QualitySettings.SetQualityLevel](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/QualitySettings.SetQualityLevel.html) explains stripped platform levels and why runtime indices must be discovered. These sources informed the supported options and application behavior; the timed confirmation is a project UX choice.

## Validation

Compilation succeeded. Manual Play Mode inspection used individual controller presses through a temporary Input System device, with no assertions, fixtures or automated tests. Native first focus, list selection, Back routing and disabled-field skipping were observed. The resolution dropdown initially exposed an offscreen selection; after the adapter was added, a live Game view capture showed the selected item scrolled into view with its border visible. A resolution change opened the confirmation, focused Revert, and timed out to the previous state. A separate controller Keep confirmed a display-mode draft. VSync off made the frame-limit selector available; Apply set native VSync to 0 and targetFrameRate to 144 and showed saved status. The original device preferences were snapshotted and restored after this inspection.

The audio capture shows the full-row border enclosing the rail and correctly sized handle, two footer buttons and readable navigation instructions. A lobby capture shows Create Lobby Down selecting Solo Host; confirm started server/client on 127.0.0.1:7777. Owner input resolved X/Y/B/LB/LT and hints matched. Individual presses produced Slash1, the primary MagneticPull branch, MegaSlash, SwordFire and CriticalX, with authored costs 25/40/60. Enemy practice-passive mode and a native player revive were used transiently to observe the final skill without stun interrupting input; these runtime changes end with Play Mode.

Editor-only display preview deliberately avoids changing the user's desktop/window. Actual operating-system fullscreen/resolution transitions and physical controller hardware were not exercised; these need a standalone desktop run. Temporary controller input, repeat-delay overrides and video preference changes were restored, and Play Mode was stopped. No automated tests were created, modified or run. Pipeline inspection startup timeouts and a manual trigger-state helper error were corrected; fresh product runtime output had no errors.
