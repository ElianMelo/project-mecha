# Visual review and skill improvement

## Evidence before acceptance

Keep a compact effect note under `Assets/_Project/VFX/<Effect>/` recording the brief, layer table, source/reference choices, integration event, direction/space decisions and validation. It is an authoring record, not an extra runtime configuration file. Tuning belongs in ScriptableObjects and native authored assets. Put screenshot sequences in ignored Temp/Logs during iteration; preserve only selected useful evidence under Docs/References when warranted.

Preserve the editor's scene setup, dirty state, play/pause state and any temporary preview settings. Preview reference assets in an isolated preview scene without running vendor scripts or saving vendor assets. Inspect new effects in a disposable preview or a controlled runtime instance; don't save preview copies into MainScene. If entering Play Mode is useful, first record the starting state and restore it afterward. Do not stop an already running user's session merely for labeling.

For a new or revised effect:

1. Compile scripts/shaders and inspect console diagnostics attributable to the change. A catalog/label-only task needs no Play Mode or new demo effect.
2. See the whole event in motion at the actual camera and actor scale. Ensure the editor is ticking: compare runtime frame count/time across reads. Sample/capture ignition, peak, secondary motion and fade, rather than one arbitrary frame. Read the CLI skill's playmode-verification-loop when needed.
3. Judge style, silhouette, value hierarchy, phase separation, depth and combat readability. Inspect against both bright and dark backgrounds. A familiar vendor look recolored into cyan is still not a new project effect.
4. Exercise only applicable interactions manually: opposing directions, airborne/downward contacts, source/target movement or despawn, surface normal vs penetration frame, world-space tails, collision/bounce, actual receiver illumination, interruption/replay and pool reuse. Do not call an interaction validated if it was only reviewed in source.
5. If frequent overlap is expected, inspect representative overlapping events and actual cost. Ensure final impact emphasis survives without hiding enemy tells. Performance claims need measured evidence; estimates remain estimates.

When the output fails, identify the failing layer/decision, fix that cause, and re-observe the affected phase plus the whole event. Preserve good layers. Change one coherent visual hypothesis at a time: e.g. narrow sparks along velocity and reduce smoke onset, rather than changing every module without a prediction.

## Learn on every requested change or bad result

Reopen this skill and relevant `Docs/VfxIterationNotes.md` entries before responding to feedback. A change request explicitly includes re-evaluating the workflow/style guidance. Distinguish:

- **Explicit user preference:** record their instruction and scope immediately; do not invent approval for a proposed result.
- **Observed failure:** record the visible symptom and evidence, then a verified cause/correction when established. Unconfirmed causes stay hypotheses.
- **Effect tuning:** keep specific timing/color/count adjustments in its authored profiles, not universal skill rules.
- **Transferable lesson:** narrowly update the relevant skill reference when the observation changes future choices (source selection, directional semantics, phase read, shader stream contract, lifecycle or art direction).

Use `Docs/VfxIterationNotes.md` entries with date/effect, request/symptom, evidence location, cause or hypothesis, change, scope, validation and outcome (pending user feedback / user accepted / replaced). State which skill rule improved. If the correct response is only local tuning, explicitly record why no global instruction changed. This still re-evaluates the skill and preserves learning without making it an ever-growing collection of accidental universal rules.

Don't claim to have learned a visual preference from a file scan alone. Initial package-derived technical findings belong in source-library notes. Don't mark a result accepted because compilation or tool calls succeeded. Don't require a fixed minimum number of iterations; iterate until observed requirements are met, then report honestly.

If tools cannot render/run the result, finish safe authoring/compilation work and identify the exact unverified behavior. Do not silently substitute a vendor final effect or enter an endless tuning loop. Ask for a concrete missing artistic choice or unavailable evidence only when it prevents further meaningful progress.
