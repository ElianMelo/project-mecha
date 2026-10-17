# VFX feedback and learning

Read relevant entries before each VFX creation or revision. Follow `.agents/skills/unity-vfx-refinery/references/review-and-learning.md`. Distinguish explicit preferences, observed defects, hypotheses, local tuning and transferable skill improvements. Never infer visual approval from compilation or the absence of feedback.

## 2026-10-17 - Standing visual and workflow brief

- **Source:** user's VFX Refinery request; explicit standing preferences, not a critique of a rendered iteration produced in this pass.
- **Scope:** all future project VFX. The game combines 1980s anime mecha, toon shading, deep shadows, Grand Chase combat readability and Armored Core machinery. Avoid crude early-anime, goofy/cartoon and fantasy-spell treatments. Ice becomes cryogenics; poison becomes radiation/industrial contamination.
- **Workflow change:** Particle System first; HLSL/ShaderLab for justified special materials; source/reference catalog; original project effects under `Assets/_Project/VFX`; separate focused interaction scripts; explicit source/target/contact direction; native collision/bounce/light as appropriate; observed multi-phase iteration.
- **Skill improvement:** created the project entry point and progressive art/source/authoring/review references. Future feedback must re-evaluate these instructions and record narrow improvements with evidence.
- **Validation:** package and archive inventory, six guides read (43 pages), structural native-prefab inspection and tooling static/Unity compilation checks. Native inspection confirms 734 Reference + 578 VFXSource labels with no mismatches; existing GUIDs and unrelated labels are preserved. No new effect was requested or visually accepted during this setup. No automated tests were created or run.
- **Outcome:** standing brief recorded. Effect-specific visual acceptance remains pending future creation requests.

## Recording future iterations

For each request to change an effect or each observed bad result, add an entry with date/effect, exact request or symptom, evidence path, cause or hypothesis, change made, scope, verification, outcome and the specific skill improvement. If only effect tuning changes, state why the general skill remains appropriate and retain the lesson here. Do not add fictitious example failures or accepted outcomes.

## 2026-10-17 - A/S/D fire skills, first user review version

- **Request:** replace A/S/D presentation with layered fire bursts and fading fuel/ember tails, preserve each skill's proposition, launch S from the player's hand and improve hit detection. The user will evaluate the result; acceptance is pending.
- **Observed issues:** the original implementation used one box sample per pulse and disconnected Graph spawn offsets. In the first replacement capture the fire was flat red, S's old reaction clip moved its hand through a large arc, D's upper emitter intersected a low platform, and stopping continuous fuel could cancel a delayed cut afterimage.
- **Changes:** native 8/7/8-system A/S/D assemblies with 4-system contact reactions; hotter texture-based cores/erosion; a native forward-hand casting clip; lower/wider D crossing geometry; rate-emission stop separated from finite delayed bursts. Advancing native capsule windows share source geometry with presentation, deduplicate targets per pulse, sweep caster motion and respect solid cover. Actual collider contact normals and source-to-target incoming directions orient impacts.
- **Source handling:** copied supplied maps have linear sampling; erosion noise repeats while the verified 2x2 cells clamp. Original vendor maps and finished effects remain unchanged. See `Docs/VfxEffectOrigins.csv`.
- **Evidence:** ignored `Logs/FireSkills/` captures and per-frame JSON; real PlayerAttack plus existing manual training receivers, actual models/background and gameplay camera size. Advancing frames were observed. S registered three hits in either facing direction, zero through a wall, and caught a receiver entering after ignition. D registered two hits on the adjusted front footprint and excluded behind/above receivers. A's upper sweep can be stopped by overhead cover; per-pulse contact depends on actual geometry, not a blanket hit count.
- **Skill improvement:** the particle-authoring reference now explicitly distinguishes fuel stop from delayed bursts, requires the actual animated hand pose to be reviewed, checks emitter clearance against room geometry and specifies linear/repeating data-map treatment. A shared-profile edit in Play Mode was rejected by automatic review and was applied through stopped Edit Mode authoring instead; no runtime asset mutation was performed.
- **Scope/outcome:** these are demonstrated technical corrections and effect-specific art choices, not universal requirements for every VFX or user-approved final art. Pending user visual feedback. No automated tests were created or run.

## 2026-10-17 - Reference loading can normalize source materials

- **Observed issue:** final diff inspection found three PixPlays materials with automatic alpha-blend-field additions/changes after structural prefab/material inspection and label application. These were not requested art changes.
- **Cause/scope:** Unity's loaded material/pipeline validation can normalize older imported sources even when the inspection code has no explicit setters. A read-only authoring intention does not guarantee a byte-identical imported source after later editor saves.
- **Correction:** restored only the added/changed saved material float properties through Unity SerializedObject APIs and SaveAssetIfDirty. Kept all requested labels, existing GUIDs and unrelated labels. Git diff confirms no remaining material changes.
- **Skill improvement:** source-library guidance now requires before/after diff review around reference inspection and targeted API restoration of unintended source upgrades. Older importer metadata serialization remains native engine output accompanying label edits, documented separately.
- **Validation/outcome:** static diff review and targeted Unity API restoration completed. This is a tooling lesson, not evidence of a rendered effect's quality or user approval.
