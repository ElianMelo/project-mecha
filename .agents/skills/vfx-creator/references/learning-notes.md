# Learning notes

Record reusable evidence from real authoring and user feedback. Each entry should identify the observation, the instruction changed, and any remaining uncertainty. Keep effect-specific tuning in the effect's assets.

## 2026-10-25 — explicit preview registration and iterative improvement

The user requested a dedicated scene with a configured effect browser and Space replay, and explicitly chose an empty initial catalog. The workflow now registers only newly skill-authored effects, uses the testing scene for inspection, and updates the skill when observed results or user feedback identify useful corrections. Existing project effects remain references; creating the skill does not make them skill-authored. No effect has received artistic acceptance through this workflow yet.

## 2026-10-25 — inspect list navigation beyond the viewport

Manual inspection with twelve unsaved preview entries showed the shared spatial navigation linking a middle row down to Replay instead of the next clipped row. The browser now applies native explicit links in catalog order while retaining the existing focus feedback and scrolling components. The testing workflow now calls for long-list and footer-route inspection when changing the browser. Temporary inspection entries are not registrations or user-approved effects; the saved catalog remains empty.

## 2026-10-25 — preserve focus when playback controls disable

Replaying with Stop focused briefly disables Stop while replacing the preview. Native Selectable clears focus immediately when disabled, so checking its focus afterward was too late. The browser now captures focus before changing interactability and restores it to a usable control. The workflow's different-focused-control replay inspection exposed this issue. The installed synthetic key command also produced a false-negative Submit observation; normal-frame input confirmed playback without a UI workaround.

## 2026-10-25 — timed sequences and real visual alternatives

The rising-water family required the same four pulse callbacks in every preview. Generic native startup would have played scheduled systems early, so preview definitions now opt into prefab-owned playback while the browser retains cleanup ownership. A first rendered comparison also exposed a repeated heavy foam mask, inverted fin tips and an unclear splash silhouette. The candidates were revised using actual source orientation and different material/layer constructions. The comparison guidance now asks for matched-phase inspection of the full set. These corrections are technical/visual observations; the user's preferred water treatment remains undecided.

The revised curtain's flat source plane was initially culled by its single-sided material; reversing the source face corrected it without changing the vendor shader. Timing captures also included command compilation in the first frame, producing an apparent late first pulse. The review tool now starts after a fresh frame, and the testing guidance records that distinction.

## 2026-10-25 — first-set water rejection and a second authoring pass

The user rejected the first ten as finished art: dense connected bodies lacked depth and transparent openings; texture noise, simple blue/light-blue colors, rectangular or blade silhouettes, misplaced splashes and odd sequencing weakened water identity. Motion in 01/03 and the multiple-stream potential of 08 were useful partial directions. The new [water direction](water-direction.md) preserves the precise critique and four permanent references. The skill now calls for deliberate negative space, spatial roles, hue/value composition, and a draft/revision review instead of a single main-phase render.

The second attempt replaced the old graphic construction with a generated painted-water sprite atlas, layered curved lobes, native Cartoon FX ripple/spray sources and authored Animator clips. Native phase captures exposed two concrete integration faults: creation-frame delta could advance Animator ahead of pulse time, and partially bound transform curves could reset unbound position/depth components. Rise now evaluates the clip through a Phase parameter derived from the presentation clock; animated transforms explicitly retain their other authored axes. Pulse-only systems run with zero automatic emission so manually emitted particles simulate and fade. These are observed workflow improvements, not evidence of user artistic approval. The new set remains awaiting assessment.

## 2026-10-25 — replacing an illustration is not rebuilding the effect

The user rejected the second set as bouncing PNGs. Whole painted water lobes on SpriteRenderers, even with growth/lean/fade clips and small supporting particles, did not deliver the requested evolving VFX. The skill now chooses actual simulation/material/texture roles before artwork and asks for interior-motion review at steady overall scale. The water-family direction explicitly excludes that rejected atlas construction. It does not ban intentionally requested 2D VFX or genuine animated flipbooks.

The native rebuild adapts PixPlays water shader/material controls, mesh particles, foam masks, scrolling/displacement and custom vertex streams, with separate ballistic particles and trails. Inspecting the first render exposed a concrete compatibility fault: the closed-beam material culled open Gabriel wave surfaces from the testing view. Appropriate two-sided material settings exposed their animated water surfaces. Mesh bounds also needed rebasing at author time so particle size curves grow from contact instead of lifting the bottom above the floor. Native shader particle-data channels must be understood and retained; a material dissolve slider can be ignored when the graph takes dissolve from Custom1. The authored alpha envelope controls interruption independently. These corrections establish technique and integration, not user approval of the new art.

The final runtime console review exposed repeated native velocity-mode errors: an animated X curve was paired with constant Y/Z values. The authored linear axes now all use Curve mode, including explicit zero curves. Orbital axes use matching modes as well. The family was rebuilt and inspected again; the corrected run reported no new runtime errors. Native material compatibility and native module validity are separate checks; an attractive render is not sufficient evidence for either.

## 2026-10-26 — user accepts the native direction for continued iteration

The user stated that the native rebuild follows a good direction. Retain its real mesh/material/particle construction for the next feedback pass. No individual candidate has been selected and Zero's live S remains unchanged. The user requested removal of transient screenshots and previews before continuing; captures, animated comparison exports and temporary inspection helpers are cleaned while the ten playable browser entries and their required wrappers remain.

## 2026-10-28 — third pass: phases, cel shader and review loop

The user asked for better shape, motion, warmup, action, recovery, opacity and shape change, and layering, with *The Unity Shaders Bible* and seven more references. The skill now plans an explicit phase timeline and layer roles before artwork ([technique guide](technique-guide.md)) and reviews fixed phases side by side from an elevated camera.

Observed during authoring (agent observations, not user approval):

- The first cel render was mostly white: foam lines covered about half the body and HDR foam bloomed. Restricting foam lines to the light band, biasing tone toward the body band and lowering foam HDR fixed it. Too much white is the first thing to check.
- Flat tube openings repeated the earlier "flat top" criticism. A tear threshold on `1 - uv.y` with its own sample fixed it; round cells gave egg-shaped lobes, vertically stretched cells gave pointed tongues closer to the eruption references.
- `Voronoi01` holds about 15 cells per tile, so tiling near 1 eroded into speckle. Tiling about 0.3 opened a few large foam-edged holes. Check a mask's feature count before choosing tiling.
- Build reliability: eval timeouts re-issued a build that deleted generated folders, leaving prefabs with broken references; a stale in-memory prefab then rendered grey quads. Builds now update assets in place and force-reimport prefabs.
- `capture_game_view` writes under `Assets/`; an existing tracked empty `Assets/Logs` folder was deleted during cleanup and restored from git with its original GUID. Check tracked status before deleting.

Remaining uncertainty: user evaluation of the ten; whether the pool's dark recovery patches read well on real gameplay floors; ribbon trails are the busiest silhouette in recovery.
