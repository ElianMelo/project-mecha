# VFX testing workflow

Open `Assets/Scenes/VfxTesting.unity`, or use **Tools > Mecha > VFX > Open testing scene**. Enter Play Mode. The left browser lists the explicit skill catalog; the right stage previews the selected effect. The scene is an independent local authoring tool with no lobby or network startup.

- Arrows, D-pad, or stick move focus through the browser and its controls. Focus has a persistent border; the chosen effect also has a persistent marker. Offscreen focused rows scroll into view.
- Selecting a row shows its name and description. Enter or controller confirm on the row plays it; the Replay button also plays it.
- Space replays the chosen effect from any browser control. Space is excluded from UI Submit, so it cannot also activate the focused button.
- Esc, controller back, or Stop clears the preview. Replay replaces the previous instance; changing effects or refreshing also clears it.
- Refresh list rereads the configured catalog. With no valid entries, show the empty state, disable Replay/Stop, and focus Refresh list.

## Register an effect

The canonical `VfxPreviewCatalog` asset is `Assets/_Project/VFX/Testing/Profiles/VfxCreatorCatalog.asset`. It was created empty and now holds requested skill-authored effects. Register only effects newly created or adapted through VFX Creator. Existing project effects and vendor packs remain available as references without automatically appearing in this browser.

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

Set `prefabControlsPlayback` to true for a wrapper that schedules its own startup/pulses. This prevents the browser from immediately playing every independent Particle System or reinitializing graphs inside that sequence. The browser still owns replay replacement, stop, and final cleanup. Leave the flag false for ordinary native play-on-start prefabs. A timed preview must read the action's real data and invoke the same presentation callbacks without applying damage or recreating combat authority.

The helper preserves existing native labels and adds VFXCreator to the project prefab and definition. Membership is explicit registration, not a scan of this label. Never register the browser's own UI prefab or mislabel an untouched legacy/vendor effect as newly authored. Do not populate sample effects unless the user requests them.

## Review and iterate

Inspect the effect through the actual browser controls, including replay, interruption, natural cleanup, and list navigation. For browser changes, inspect a list longer than its viewport: every row must remain reachable in order, focus must scroll into view, and Replay/Stop/Refresh must have clear return routes. Inspect Space with different focused controls to catch duplicate Submit behavior. Confirm that frames/time advance. Supplement the neutral stage with actual gameplay-context inspection when attachment, camera scale, surfaces, or contact materially affect the result. Restore the previous editor/Play state afterward.

When a real result exposes a browser or skill limitation, fix the necessary workflow and update the skill's relevant instructions and learning notes. Keep technical observations and user artistic approval distinct. Follow the project's no-unrequested-automated-tests rule; use compilation, source review, static checks, and manual runtime inspection.

During UI inspection, allow synthetic input to pass through a normal Input System frame. The installed Pipeline `simulate_key` command explicitly flushes an extra input update, which can make native UI Submit checks miss an otherwise valid press. Queue a native key state for the next frame when diagnosing that case rather than replacing working UI input behavior.

When a freshly compiled inspection command starts a timed preview, wait for a new advancing frame before starting the cast. Synchronous command compilation can inflate the current frame's delta and make the first observed pulse appear late. Record the actual callback times and separate this inspection artifact from the action's authored timing.

For timed animated presentations, compare the animation pose against the pulse clock at startup as well as the main phase. If native Animator delta advances a creation frame ahead of the action, use an authored motion-time parameter driven by the presentation clock. When authoring transform curves programmatically, explicitly retain the other position/scale axes; inspect actual runtime offsets rather than assuming prefab values survived binding. A pulse-only particle system should simulate with zero automatic emission before manual Emit calls so particles travel and age.

### Phase captures

Pipeline `capture_game_view` writes only under `Assets/`; move each PNG to the ignored root `Logs/` immediately and remove any temporary asset folder through `AssetDatabase` afterwards. Check `git ls-files` before deleting a folder under `Assets/`: an empty tracked folder must be restored with its original `.meta`.

The browser removes previews on a real-time timer, so for slowed captures instantiate the preview wrapper directly under `Effect origin`. Set `Time.timeScale` to about 0.03, poll the effect's `Elapsed`, freeze with `timeScale = 0` at each target phase, capture, then resume. CLI round-trips take about a second, which is too slow for real-time checks of interruption or pulse timing; use a slowed clock for those as well. In Play Mode, temporarily move the preview camera to an elevated 3/4 view; Play Mode changes are discarded. Stitch crops into contact sheets (Texture2D in an eval) to compare phases and candidates side by side.

### Authoring builds

Long authoring builds exceed the eval main-thread limit (about 5 s). The pipeline may then re-issue the request, so builds must be idempotent: update generated materials and meshes in place instead of deleting folders, and remove only stale leftovers at the end. Force-reimport saved prefabs at the end of the build; otherwise Play Mode can be served a stale in-memory prefab with null materials and meshes. Run builds only after compilation and shader import settle, then verify material texture bindings before capturing.

When authoring native velocity curves, keep the X/Y/Z modes compatible, including axes whose values are zero. Inspect the current runtime console after playing the full family; repeated native module errors can coexist with an apparently animated render. For imported open water sheets, check culling from both sides, UV orientation and particle custom-data/vertex-stream contracts before changing textures to compensate for a rendering mismatch.
