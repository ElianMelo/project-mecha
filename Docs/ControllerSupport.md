# Controller support

Implemented 2026-10-16 using the installed Unity Input System 1.19.0. MainScene supports gameplay, the lobby browser/waiting room, local host/join, text entry and the combat lab through a standard Gamepad. Button hints use the active device's display names, including PlayStation layouts exposed by Unity. There is one locally controlled player per game instance; this does not add couch co-op.

The 2026-10-17 [configuration and combat layout follow-up](ConfigurationAndCombatLayout.md) supersedes the original button layout and adds the native video configuration screen. Esc/Start opens the options home; audio has no lobby shortcut.

## Controls

| Action | Xbox-style controls | PlayStation equivalent |
| --- | --- | --- |
| Move | Left stick or D-pad | Left stick or D-pad |
| Jump / launcher branch | A or D-pad Up | Cross or D-pad Up |
| Descend / Mushidon / dive modifier | Left stick Down or D-pad Down | Same |
| Light attack / combo | X | Square |
| Dash / critical branch | RB or double-tap horizontal direction | R1 or double-tap |
| Primary weapon heavy attack (existing magnetic branch) | Y | Triangle |
| Mega Slash / 25 MP | B | Circle |
| Sword Fire / 40 MP | LB | L1 |
| Critical X / 60 MP | LT | L2 |
| Secondary weapon (tap or hold for full auto at weapon cadence) | RT | R2 |
| Show / hide controls | Right-stick click (keyboard F1) | R3 (keyboard F1) |
| Show / hide co-op chat | Keyboard F2 | Keyboard F2 |
| Enter a cleared-room portal | Hold X | Hold Square |
| Open / close menu | Start / Menu | Options |
| Combat lab | Select / View | Share / Create |
| UI navigate | Left stick or D-pad | Same |
| UI select | A | Cross |
| UI back | B | Circle |

Rocket is Dash + Jump within the existing chord window; Down converts Rocket to Mushidon. Down + Attack performs the air dive. Skills, attacks and movement retain their existing commitment, resource, cooldown and authority rules.

The subsequent [game-adjustment pass](GameAdjustments.md) adds keyboard C / RT SMG fire and Enter chat, moves portal interaction off RT, gives input fields the same focus frame as buttons, and rebuilds native directional navigation for current visible/interactable controls. Earlier validation below describes the original controller pass.

Open the menu to choose **SOLO HOST**, **LOCAL JOIN**, or an online lobby. The left stick/D-pad navigates native selectable controls. Submit on a text field opens the on-screen keyboard. It supports letters, digits, punctuation, spaces, erasing, case changes, confirmation and cancellation. Confirmation writes to the original TMP field, preserving its character limit and value-change callbacks. Cancellation preserves the original text. Keyboard and mouse text entry remain available.

## Implementation

`PlayerInputs` converts the existing Input Action asset into gameplay intentions. Portal interaction consumes held Light Attack from the paired Player action map, with a circular progress indicator. It has no stick-click binding. The prefab keeps remote PlayerInput components disabled and permits native control-scheme switching for the owner. Held movement is polled after UI closes. Device loss clears held states and tap history. A native `SwitchCurrentControlScheme` fallback resynchronizes the local player's pairing when Unity keeps waiting for a removed controller after keyboard use. Remote replicas never request pairing.

GameManager enables only a private clone of the System action map for menu, back, combat lab and local connection shortcuts. Menu visibility and a short resume delay suppress gameplay intentions, preventing menu submit/back presses from becoming attacks or skills. Existing movement and combat continue their normal simulation; this is not a network pause.

The existing InputSystemUIInputModule and native Button/TMP navigation own menu movement, submit and selection effects. MechaLobbyPanel supplies a valid selection when the current control becomes hidden or disabled. Keyboard keys have explicit navigation confined to the modal. TMP caret focus is kept out of gamepad navigation, while pointer/keyboard editing still uses TMP normally.

The concrete built-in gap is desktop controller text entry: native desktop TMP fields do not supply a navigable gamepad keyboard. ControllerTextEntry connects native uGUI Buttons and TMP fields to cover that gap; it does not replace text rendering or UI input processing. GameInputPresentation tracks meaningful input and formats native button names. ControllerInterfaceSettings owns help text, keyboard characters, menu resume timing and UI navigation repeats. InputActionAsset owns bindings; shared assets remain read-only in play. **Tools > Mecha > Apply Controller Support** reapplies the saved setup, and the full gameplay setup invokes it.

## Validation

Compilation succeeded and source whitespace checks passed. Manual Play Mode inspection used a temporary Input System Gamepad device because no physical controller was connected. Frames advanced normally. Controller input opened the menu, selected the native text-field keyboard, entered and confirmed `PilotA`, canceled a separate edit without changing its value, and started a local host from a UI button. Native selection returned to the edited field with caret focus off. All ten Player actions resolved to Gamepad controls. Stick input paired the owner with Gamepad and drove movement; Select opened the combat lab and blocked gameplay input, and Back closed it.

Removing a preview controller cleared movement/jump/down state. A replacement gamepad resumed input. The final keyboard fallback check changed the owner from Gamepad to Keyboard&Mouse after removal, with held Left producing `(-1, 0)` movement. Fresh runtime console output contained no errors. Existing FishNet deprecation warnings remain unchanged. One compilation command lost its connection during reload; the following status reported ready and successful compilation.

Temporary devices and preview key states were removed, Play Mode was stopped, the original run-in-background setting was restored, and MainScene remained clean. No automated tests were created, modified or run. Physical Xbox/PlayStation hardware, device-specific glyph names, and a fresh remote Relay session were not exercised.
