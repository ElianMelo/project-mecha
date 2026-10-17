# Controller, audio and portal refinements — 2026-10-17

The later [configuration and combat layout revision](ConfigurationAndCombatLayout.md) removes the audio Lobby/Squad footer, renames local hosting Solo Host, replaces the basic video buttons with complete native settings, and updates the combat bindings. Earlier observations below describe this document’s original pass.

## Standing UI policy

The user's controller-first requirement is recorded in AGENTS.md and Architecture.md: always show the currently selected control, retain selection feedback with motion disabled, and review directional links, initial focus, confirm/back, modal ownership, disabled/hidden controls and complete paths between screens. Native uGUI Selectable/navigation, Image sprites and Animator remain responsible for interaction and presentation. No runtime-generated shapes or new UI framework were introduced.

## Menus and navigation

Esc / controller Menu opens the main configuration screen with Resume Game, Lobby/Squad, Audio and Video. Audio retains its five mixer volume controls and saved values; Back returns to options and restores focus to Audio. Video offers native quality, windowed/borderless display and V-sync settings, saved through PlayerPrefs. Back returns focus to Video. The main Resume or Back action closes the menu and restores gameplay input through the existing resume grace. Settings never stop the co-op clock.

Every current selectable receives a native sliced-Sprite selection border through UiInteractionFeedback. This border is driven by actual EventSystem selection independently of optional animation. Audio's vertical route includes all five sliders followed by Defaults → Lobby/Squad → Back; footer left/right navigation also remains available. Sliders retain native left/right adjustment.

The lobby now has an explicit UiNavigationScope that rebuilds around visible, interactable controls. The separate controller keyboard keeps its own existing navigation. The neighbour ranking admits controls whose rectangles overlap on the perpendicular axis, even if their centres fall outside the usual directional cone. This fixes the wide Create Lobby control skipping the narrower Local Host / Local Join row. Create → Down reaches Local Host; Right then reaches Local Join. Underlying lobby selection does not steal focus from configuration screens.

GameOptionsMenu coordinates screens, AudioConfigurationMenu owns audio preferences, and VideoConfigurationMenu wraps native display/quality settings. MechaMenuRefinementSetup authors the native menu and portal UI through Unity APIs and saves the reusable GameOptions prefab. Canonical audio/controller/lobby setup paths also retain the corrected navigation and focus rules.

## Sound selection and impact

The selected audio library grows from 65 to 71 clips and from 41 to 44 cues. Supplied source paths remain recorded in AudioClipOrigins.csv, and native AssetDatabase moves preserve GUIDs. No audio synthesis or external audio package was introduced.

Gameplay now cycles Sci-Fi 2 Loop, Sci-Fi 4 Loop and Sci-Fi 7 Loop; the quieter Sci-Fi 1 Loop remains menu music. A read-only comparison of the supplied tracks measured mid-track signal level and spectral attack activity. Loops 2, 4 and 7 showed more rhythmic activity than the preceding selections; this is an energy-based selection, not a formal musicological tempo assessment. Gameplay music source gain increases from 0.35 to 0.6 without changing the user's saved Music slider.

MegaSlash and CriticalX startup gains rise to 0.8; SwordFire rises to 0.75 with a lower pitch range. Their spatial blends are 0.45 for clearer close combat presence. SkillPulse gain rises from 0.25 to 0.38. Each skill also gets a distinct protected final-pulse impact: heavy sci-fi weapon boom, short explosion/fire burst, or shield-deflection boom. These retain the existing non-stealing protected audio path. The authored GameAudio profile owns the new clips, priorities, gain and pitch.

## Spear contact

A player contact still applies authoritative damage once. Instead of immediately despawning, EnemyLanceProjectile uses the same native stationary-pose path as a scenery impact: stop velocity/interpolation, buffer its position and rotation, play the existing particle/audio impact, and retain the spear for the EnemyLanceSettings-authored 0.8-second playerImpactLifetime. Embedded state prevents repeated sweeps or damage. Scenery keeps its separate 1.5-second authored lifetime. Impact effects are unbuffered; the stationary pose remains buffered for observers.

## Portal hold

Portals use the existing Attack action: hold Z on keyboard or X/the platform-specific west-face button on controller. PlayerInputs polls AttackHeld and consumes a portal-eligible attack press instead of starting a melee combo. The old Interact action has no bindings and no portal subscription; neither stick press activates travel. R3 remains only the unrelated existing controls-help shortcut.

ProceduralLevelSettings authors a 0.9-second hold duration, ring Sprite, size and position. LevelObjectiveHud displays the actual button name in a native radial Image using the existing HollowRing Sprite; fillAmount follows owner hold progress. Releasing, moving out of range, dying, stun, opening a menu or changing rooms resets progress. The server receives press/release intentions and measures its own elapsed hold time, continuously checking the existing life, stun, room-clear and distance conditions. It does not accept an owner-supplied completion time. The existing authoritative room transition remains the final action.

## Validation

Validation uses compilation, static review and manual runtime inspection through the official Unity Pipeline. No automated tests were added, modified or run. No extra player builds or Relay sessions were used; remote-client delivery is not measured here.

- Controller Menu opened options with Resume selected and a visible border. Controller confirmation opened Audio with Master selected. Audio's footer reached Lobby/Squad, and confirmation opened the squad browser.
- Controller Down from Create reached Local Host with its border visible; Right linked to Local Join. Confirming Local Host started the local FishNet host.
- Video displayed native quality, display mode and V-sync values with clear focus. Controller Back returned to options with Video selected.
- In local gameplay the first combat music was Sci-Fi 2 Loop. A real MegaSlash presentation emitted one startup, four ordinary pulse layers and one protected MegaSlashImpact.
- A spear contact changed player health from 64 to 48, left its stopped spear alive with 37 native impact particles at the inspected frame, then despawned after resuming the authored delay. No second damage was observed.
- A controller portal hold paused for inspection at 41.2% with the radial Image at the same value, room 1 unchanged and melee state Ready. Releasing cancelled progress and kept room 1. A complete hold then advanced to room 2. The live HUD showed the circular X prompt within the viewport.
- Temporary controller inspection input and the longer inspection-only UI repeat delay were removed/restored. Native settings and all content changes were saved through Unity APIs. The final Editor state is stopped and unpaused, with its original audio mute setting retained. Final compilation and inventory checks complete the pass.
