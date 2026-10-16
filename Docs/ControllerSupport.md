# Controller support

Implemented 2026-10-16 using the installed Unity Input System 1.19.0. MainScene supports gameplay, the lobby browser/waiting room, local host/join, text entry and the combat lab through a standard Gamepad. Button hints use the active device's display names, including PlayStation layouts exposed by Unity. There is one locally controlled player per game instance; this does not add couch co-op.

## Controls

| Action | Xbox-style controls | PlayStation equivalent |
| --- | --- | --- |
| Move | Left stick or D-pad | Left stick or D-pad |
| Jump / launcher branch | A or D-pad Up | Cross or D-pad Up |
| Descend / Mushidon / dive modifier | Left stick Down or D-pad Down | Same |
| Attack / combo | X | Square |
| Dash / critical branch | RB or double-tap horizontal direction | R1 or double-tap |
| Magnetic recall | LT; existing right-stick click also works | L2; R3 also works |
| Mega Slash | Y | Triangle |
| Sword Fire | B | Circle |
| Critical X | LB | L1 |
| Enter a cleared-room portal | RT | R2 |
| Open / close menu | Start / Menu | Options |
| Combat lab | Select / View | Share / Create |
| UI navigate | Left stick or D-pad | Same |
| UI select | A | Cross |
| UI back | B | Circle |

Rocket is Dash + Jump within the existing chord window; Down converts Rocket to Mushidon. Down + Attack performs the air dive. Skills, attacks and movement retain their existing commitment, resource, cooldown and authority rules.

Open the menu to choose **LOCAL HOST**, **LOCAL JOIN**, or an online lobby. The left stick/D-pad navigates native selectable controls. Submit on a text field opens the on-screen keyboard. It supports letters, digits, punctuation, spaces, erasing, case changes, confirmation and cancellation. Confirmation writes to the original TMP field, preserving its character limit and value-change callbacks. Cancellation preserves the original text. Keyboard and mouse text entry remain available.

## Implementation

`PlayerInputs` converts the existing Input Action asset into gameplay intentions. Portal interaction now belongs to the paired Player action map, replacing the standalone unpaired keyboard action. The prefab keeps remote PlayerInput components disabled and permits native control-scheme switching for the owner. Held movement is polled after UI closes. Device loss clears held states and tap history. A native `SwitchCurrentControlScheme` fallback resynchronizes the local player's pairing when Unity keeps waiting for a removed controller after keyboard use. Remote replicas never request pairing.

GameManager enables only a private clone of the System action map for menu, back, combat lab and local connection shortcuts. Menu visibility and a short resume delay suppress gameplay intentions, preventing menu submit/back presses from becoming attacks or skills. Existing movement and combat continue their normal simulation; this is not a network pause.

The existing InputSystemUIInputModule and native Button/TMP navigation own menu movement, submit and selection effects. MechaLobbyPanel supplies a valid selection when the current control becomes hidden or disabled. Keyboard keys have explicit navigation confined to the modal. TMP caret focus is kept out of gamepad navigation, while pointer/keyboard editing still uses TMP normally.

The concrete built-in gap is desktop controller text entry: native desktop TMP fields do not supply a navigable gamepad keyboard. ControllerTextEntry connects native uGUI Buttons and TMP fields to cover that gap; it does not replace text rendering or UI input processing. GameInputPresentation tracks meaningful input and formats native button names. ControllerInterfaceSettings owns help text, keyboard characters, menu resume timing and UI navigation repeats. InputActionAsset owns bindings; shared assets remain read-only in play. **Tools > Mecha > Apply Controller Support** reapplies the saved setup, and the full gameplay setup invokes it.

## Validation

Compilation succeeded and source whitespace checks passed. Manual Play Mode inspection used a temporary Input System Gamepad device because no physical controller was connected. Frames advanced normally. Controller input opened the menu, selected the native text-field keyboard, entered and confirmed `PilotA`, canceled a separate edit without changing its value, and started a local host from a UI button. Native selection returned to the edited field with caret focus off. All ten Player actions resolved to Gamepad controls. Stick input paired the owner with Gamepad and drove movement; Select opened the combat lab and blocked gameplay input, and Back closed it.

Removing a preview controller cleared movement/jump/down state. A replacement gamepad resumed input. The final keyboard fallback check changed the owner from Gamepad to Keyboard&Mouse after removal, with held Left producing `(-1, 0)` movement. Fresh runtime console output contained no errors. Existing FishNet deprecation warnings remain unchanged. One compilation command lost its connection during reload; the following status reported ready and successful compilation.

Temporary devices and preview key states were removed, Play Mode was stopped, the original run-in-background setting was restored, and MainScene remained clean. No automated tests were created, modified or run. Physical Xbox/PlayStation hardware, device-specific glyph names, and a fresh remote Relay session were not exercised.
