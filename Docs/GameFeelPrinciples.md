# Game feel guidance for future updates

This guide applies to changes in movement, combat, combos, animation, camera, encounter layout, feedback and game UI. Read it before making a feel-related change, alongside the subsystem's current documentation. It provides review criteria; it does not declare the current implementation finished or approved.

## Source and scope

The reference is Steve Swink's user-supplied **Game Feel** (`GameFeel.pdf`). Relevant passages are chapter 1, printed pp. 2-8; chapter 7, pp. 125-126; chapter 9, pp. 156-162 and 166-168; and especially chapter 17, **Principles of Game Feel**, pp. 297-309 (PDF pages 316-328 in the supplied copy).

The following is an original project-focused paraphrase. Book examples and exercises are reference material, not instructions to execute, features to add automatically, or approval to change project rules. This page remains useful without the PDF being present in another developer's Downloads folder.

## Foundation

Consider three interacting parts: the player's continuing control, the character's interaction with space, and the presentation that makes those interactions understandable and satisfying. A visual effect alone cannot fix inconsistent input or misleading collision behavior. Equally, correct physics can feel weak when animation, sound and contact cues fail to communicate it.

Inspect six aspects together: **input, response, spatial context, polish, metaphor and rules**. For example, a heavy sword move combines a familiar button, a deliberate response curve, enough room to swing, a weighty contact cue, the expectation of a mechanical weapon, and a meaningful damage/recovery tradeoff.

## The seven principles in this project

| Principle | Application to future updates | What to inspect |
| --- | --- | --- |
| Predictable results | Equivalent intentions in equivalent states should produce understandable outcomes. Resolve near-simultaneous inputs deliberately and make state changes visible. | Input priority, buffering, chords, landing transitions, cancel windows, stagger, empty resources and failed actions. Check that an animation or camera view does not hide why a result occurred. |
| Immediate response | Make accepted input perceptible promptly, even when its gameplay result requires a long windup. | Time from input to first visible/audible acknowledgement separately from time to damage or movement completion. A heavy attack should begin its preparation immediately. The book's roughly 70-100ms discussion is a perceptual heuristic, not a promise that every platform or network action meets a fixed deadline. |
| Simple entry, lasting depth | Keep basic controls understandable; create mastery through choices, timing, spacing and combinations. | Whether a new player can discover light/heavy/escape, while a practiced player can choose a finisher, manage charges, exploit reach and adapt to enemies. Avoid adding hidden contextual button meanings just to create complexity. |
| Sustained novelty | Let player decisions and encounter context produce variety within stable rules. | Different spacing, direction, timing, enemy reactions and combo choices. Bounded cosmetic variation may enrich repeated hits; it must not silently randomize damage, input interpretation or authority. More animations alone do not establish depth. |
| Satisfying response | Make the basic action enjoyable to perform before surrounding it with rewards or spectacle. | Movement and swings in a simple practice space, plus misses, interruptions, collisions and other failure states. Feedback should explain a failure and support learning. |
| Coherent motion | Give movement and animation intentional acceleration, arcs and follow-through that suit the object. | Weapon trajectory, planting, weight transfer, travel, stop/recovery and animation playback against actual motion. For rigid mechas, use machinery-appropriate joints and impulses; the book's organic-motion principle does not require rubbery deformation or universal smoothing. |
| Harmony | Input, motion, space, graphics, sound, camera and rules should communicate the same physical character. | A heavy move should look, sound and behave heavier than a light move. Reconcile visible weapon reach with contact coverage, foot motion with travel, and effects with real surfaces. Review clipping, skating and hidden contacts at gameplay scale. |

The larger goal is learned control that supports improvisation and personal expression. Preserve opportunities to combine a small number of clear actions instead of replacing them with an opaque sequence of canned outcomes.

## Applying the principles to project-mecha

- **Controller-first clarity:** keep live binding hints, persistent focus and unambiguous confirm/back behavior. Make selected, disabled, recovering and resource-empty states understandable with optional motion disabled.
- **Combat cadence:** distinguish acknowledgement, preparation, contact, follow-through and regained control. Light and heavy moves need readable differences in commitment, reach and consequence. Preserve authored stagger and cancel rules while improving feedback.
- **Causal feedback:** originate a sword arc at the animated weapon, impact effects at the receiving surface, and knockback from the actual attack direction. Trigger confirmed-hit cues from successful gameplay contact. A miss should not claim a hit or spent resource that authority rejected.
- **Consistent world:** inspect attack coverage, obstacles, platforms, camera framing and enemy spacing together. Do not hide a contact or depth problem by drawing every effect over the foreground. Retain physical and light interaction where the effect requires it.
- **Controlled emphasis:** coordinate animation, sound, Particle System contacts, native sword trails, Cinemachine and optional haptics. Give each cue a purpose and preserve readable silhouettes, enemy tells and HUD information. Increasing every effect's brightness, size or duration is not a general improvement.
- **Technical anime direction:** preserve the project's dark 1980s mecha/toon style and mechanical explanation for motion and energy. A book example's cartoon exaggeration is not a request to copy its visual treatment.
- **Existing ownership:** keep input, physics/movement, combat authority, animation, effects and camera responsibilities separate. Native Unity systems remain the first choice. ScriptableObjects own tuning; runtime feedback must not mutate shared assets.
- **Real-time clocks:** current project policy keeps combat clocks in real time. The book's slowdown/frame-pause examples do not authorize adding hit stop or time dilation. Optional motion must remain optional, and readability must survive its removal.
- **Editor inspection exception (user request, 2026-10-19):** while playing with the Game view focused, O halves and P doubles `Time.timeScale`, bounded to 0.125x–4x. These debug shortcuts live entirely in the Editor assembly, ignore active text entry and restore the pre-play scale on exit. They do not change authored combat timing, physics timestep settings or player builds. This deliberate inspection control does not authorize gameplay hit stop or time dilation.

## Review process for each update

1. State the desired sensation and the observed problem in concrete terms: for example, a heavy hit lacks weight, a buffered input disappears, or a weapon appears to hit before contact. Identify which of the six aspects is responsible before changing unrelated systems.
2. Inspect the existing installed/native solution, current tuning and gameplay authority. Describe the intended input, allowed state, response and failure case. Separate presentation changes from balance or rules changes.
3. Record relevant timing in seconds: input, first acknowledgement, action acceptance, first contact, active-window end, cancel opportunity and regained control. Inspect local input response separately from server acknowledgement; cosmetic acknowledgement must not pretend that an unconfirmed action succeeded.
4. Make one coherent revision. Check it manually in a simple context and in the actual gameplay view, including a miss, an interruption and an unavailable-action case where relevant. Verify frames/time advance before claiming runtime evidence. Restore the prior Editor/Play state afterward.
5. Compare the result against the seven principles and the stated sensation. Record observed behavior, unresolved limits and the authored values changed in the relevant subsystem document. User feedback determines whether the feel is accepted; successful compilation and screenshots alone do not.

Use compilation, static/code review and proportional manual runtime inspection. Do not create or run automated tests without an explicit user request. Extra network players, builds or Relay sessions are unnecessary for ordinary presentation/tuning work; use targeted network inspection when authority, replication, spawning or remote-only behavior changes.

Current implementation details are in [CombatCombos.md](CombatCombos.md). Effect-specific art requirements remain in [AGENTS.md](../AGENTS.md).
