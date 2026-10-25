# Learning notes

Record reusable evidence from real authoring and user feedback. Each entry should identify the observation, the instruction changed, and any remaining uncertainty. Keep effect-specific tuning in the effect's assets.

## 2026-10-25 — explicit preview registration and iterative improvement

The user requested a dedicated scene with a configured effect browser and Space replay, and explicitly chose an empty initial catalog. The workflow now registers only newly skill-authored effects, uses the testing scene for inspection, and updates the skill when observed results or user feedback identify useful corrections. Existing project effects remain references; creating the skill does not make them skill-authored. No effect has received artistic acceptance through this workflow yet.

## 2026-10-25 — inspect list navigation beyond the viewport

Manual inspection with twelve unsaved preview entries showed the shared spatial navigation linking a middle row down to Replay instead of the next clipped row. The browser now applies native explicit links in catalog order while retaining the existing focus feedback and scrolling components. The testing workflow now calls for long-list and footer-route inspection when changing the browser. Temporary inspection entries are not registrations or user-approved effects; the saved catalog remains empty.

## 2026-10-25 — preserve focus when playback controls disable

Replaying with Stop focused briefly disables Stop while replacing the preview. Native Selectable clears focus immediately when disabled, so checking its focus afterward was too late. The browser now captures focus before changing interactability and restores it to a usable control. The workflow's different-focused-control replay inspection exposed this issue. The installed synthetic key command also produced a false-negative Submit observation; normal-frame input confirmed playback without a UI workaround.
