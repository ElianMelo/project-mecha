# VFX testing workflow

Open `Assets/Scenes/VfxTesting.unity`, or use **Tools > Mecha > VFX > Open testing scene**. Enter Play Mode. The left browser lists the explicit skill catalog; the right stage previews the selected effect. The scene is an independent local authoring tool with no lobby or network startup.

- Arrows, D-pad, or stick move focus through the browser and its controls. Focus has a persistent border; the chosen effect also has a persistent marker. Offscreen focused rows scroll into view.
- Selecting a row shows its name and description. Enter or controller confirm on the row plays it; the Replay button also plays it.
- Space replays the chosen effect from any browser control. Space is excluded from UI Submit, so it cannot also activate the focused button.
- Esc, controller back, or Stop clears the preview. Replay replaces the previous instance; changing effects or refreshing also clears it.
- Refresh list rereads the configured catalog. With no valid entries, show the empty state, disable Replay/Stop, and focus Refresh list.

## Register an effect

The canonical `VfxPreviewCatalog` asset is `Assets/_Project/VFX/Testing/Profiles/VfxCreatorCatalog.asset`. It starts empty. Register only effects newly created or adapted through VFX Creator. Existing project effects and vendor packs remain available as references without automatically appearing in this browser.

Create a standalone project-owned prefab under `Assets/_Project/VFX/<Effect>/`. It must preview without a player, lobby, damage authority, or network manager. Native Particle Systems, VFX Graphs, and prefab animation are supported. For a gameplay-dependent effect, author a focused preview wrapper that supplies its visual initialization/attachment in OnEnable and cleans up on disable; reference existing assets rather than duplicating gameplay logic.

In stopped Edit Mode, call the editor helper through the official Unity CLI/Pipeline:

```csharp
VfxTestingAuthoring.Register(
    "Assets/_Project/VFX/Example/Prefabs/Example.prefab",
    "energy.impact",
    "Energy impact",
    "A short directional energy impact.",
    1.2f,
    0.8f);
```

This is an example interface, not an existing effect or a fixed timing recommendation. Use a unique two-word semantic key, display wording appropriate to the request, and durations measured for the actual effect. The helper creates or updates a `VfxPreviewDefinition` in `Testing/Profiles/Entries`, binds its English name/description in the Diagnostics String Table, and adds it once to the catalog. Reuse its key when revising the same effect. Localization capture preserves existing wording; edit the existing English table entries when changing that wording.

Each definition configures its prefab, localized name/description, play time, tail cleanup time, position, rotation, and scale. Edit these through Unity APIs or the Inspector. During preview, emission stops after play time and the entire instance is removed after the configured tail. Custom prefab animation/scripts must cooperate with this lifetime. Tune the offset/orientation and tail duration in the definition rather than changing the shared preview stage to accommodate one effect.

The helper preserves existing native labels and adds VFXCreator to the project prefab and definition. Membership is explicit registration, not a scan of this label. Never register the browser's own UI prefab or mislabel an untouched legacy/vendor effect as newly authored. Do not populate sample effects unless the user requests them.

## Review and iterate

Inspect the effect through the actual browser controls, including replay, interruption, natural cleanup, and list navigation. For browser changes, inspect a list longer than its viewport: every row must remain reachable in order, focus must scroll into view, and Replay/Stop/Refresh must have clear return routes. Inspect Space with different focused controls to catch duplicate Submit behavior. Confirm that frames/time advance. Supplement the neutral stage with actual gameplay-context inspection when attachment, camera scale, surfaces, or contact materially affect the result. Restore the previous editor/Play state afterward.

When a real result exposes a browser or skill limitation, fix the necessary workflow and update the skill's relevant instructions and learning notes. Keep technical observations and user artistic approval distinct. Follow the project's no-unrequested-automated-tests rule; use compilation, source review, static checks, and manual runtime inspection.

During UI inspection, allow synthetic input to pass through a normal Input System frame. The installed Pipeline `simulate_key` command explicitly flushes an extra input update, which can make native UI Submit checks miss an otherwise valid press. Queue a native key state for the next frame when diagnosing that case rather than replacing working UI input behavior.
