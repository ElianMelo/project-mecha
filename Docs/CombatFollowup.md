# Combat and usability follow-up — 2026-10-19

**Locomotion update, 2026-10-19:** the user subsequently requested restoration of the earlier dash/idle/walk motion. Idle, Run and Dash now use the original supplied Tallgeese motion imported through Unity's native Humanoid pipeline under `Animations/Humanoid/OriginalLocomotion`, shared by both current Humanoid rigs. The old extracted Generic clips freeze on these rigs and were not left assigned. Both characters' three states were previewed with advancing frames and moving bones; the distinct original dash replaces the reused Run clip. Combat/reaction/skill clips, tuning and rig assets are unchanged. This supersedes the locomotion pause/comparison task below, without implying approval of the remaining combat presentation.

## TEMPORARY HANDOFF — REJECTED; revisit next session

The user rejects the current solution as terrible and worse than before. Idle and dash previously had good poses and are now completely broken. The current animation/Humanoid implementation is unfinished and is **not an accepted baseline**. The implementation notes and technical validation below do not constitute visual or gameplay approval.

Work is paused at the user's request. This update changes documentation only; preserve the current working changes for investigation. Do not continue tuning or automatically revert the implementation.

Next session:

- Compare the previously good idle/dash poses with the current replacements. Start at `AgentScripts/HumanoidRigAuthoring.cs` (`NativeLocomotion`), both animation settings/controllers, and the character visual prefabs. Dash currently uses the replacement Run clip.
- Identify whether the regression comes from the Humanoid mapping/skin, source motion, or state setup before making further changes. Valid Avatar flags and successful compilation are insufficient evidence of usable animation.
- Respect the user's rejection of accumulating workarounds: do not add corrective bakes, pose patches, or more VFX tuning to conceal an unresolved rig/animation problem. Review the actual idle/dash result with the user when work resumes.

## Previous implementation record — subject to the rejection above

The user's priority is a maintainable native Humanoid solution. Both players have native Humanoid Avatars and share imported melee motion assets directly. Duplicate baked melee curves and corrective bake tooling are removed. Native Humanoid movement/reaction/skill references replace incompatible Generic states; source clips remain untouched. Legacy Death clips were outside the melee conversion and remain unverified. See CombatCombos.md for the rig boundary.

Extra rifles/cannon remain visible because they are fused into body skins. Neither character provides separate unwanted weapon objects to disable. The filtered Zero mesh was retired and the original derived skin restored. A split model is required to hide those weapons without mesh alteration; no replacement hiding trick was added.

Both SMGs follow left-hand palm sockets. An obsolete separate forearm SMG override is disabled, leaving one active gun per character; the setup helpers preserve this binding. Sword cut particles follow the animated blade plane and position, while the native trail retains its world-space path. Supplied Sword7 emission maps and HDR colors survive the toon conversion; the main camera has HDR/post-processing and the combat Volume's bloom is enabled. Blade point lights provide receiver illumination independently of bloom.

## Options and routing

General adds Show self highlight (default false) and Show players highlight (default true). Each governs name and bottom ring together. Enemy/PVP health bars retain separate behavior. Values persist through GameSave JSON at Mecha.General.SelfHighlight and Mecha.General.PlayersHighlight. Controller navigation is Language → self → players → Back, with wraparound and dropdown-specific Back handling. Native focus borders persist when optional motion is disabled. Labels use the English Settings table and localized fonts.

Random route is a native button in the routing scene. Left at a map row's left boundary enters the button; directional input returns to an available node. Confirm uses the normal button event. Solo's authoritative request fills the remaining connected paths in the current batch (normally five) and enters the first encounter immediately. Multiplayer chooses only the requesting pilot's vote among currently available destinations; it does not choose future fogged rows or bypass squad agreement. Round, roster and repeat-rate checks remain server-owned.

## Bullets and damage feedback

Authored and generated crates use combat depth zero. Previously their front collider surface was Z=0.25, beyond a bullet's 0.065 radius on Z=0. Native Rigidbody/CCD/collider contacts and the existing server damage path remain responsible; no second hit detector was added.

Projectile direction comes from actual flight displacement, with launch direction for an immediate overlapping contact. Resolved velocity may already be zero; collision-relative velocity has callback-relative sign. Direction flows through existing receiver feedback RPCs. DestructibleProp safely reads local health before FishNet initializes its cached NetworkObject.

Damage presentation consumes the captured contact through the existing feedback path. Physics and damage depth remain separate from cosmetic placement, and lethal crate hits can still deliver feedback before destruction.

## Labels and validation

Native Unity Keep/Replace and category labels replace the classification CSV. Keep means retain for development, not final approval. The refresh utility preserves human decisions and known provenance. See ProjectContentClassification.md.

Compilation and operator-driven Play Mode inspection are used; no automated tests are created or run. Both native Avatars report isHuman=true, with standing feet above the floor at the current 3.485 camera scale. SMGs remain under Bip001 L Hand, approximately 0.044/0.046 m from the wrist. Live self-highlight changes showed/hid name and ring together. The route button retained focus, returned rightward to Route 0, selected [0,1,2,5,8] in one observed solo run, and entered Encounter / room 1 without a confirmation timer.

A physical bullet in a local host changed crate health 28 → 20. Advancing frames, contact sparks and the animated light were observed. Same-frame light on/off captures change nearby armor pixels independently of bloom; ignored receipts are under Logs/SwordRevision. Only targeted local-host checks were used for changed authority behavior; no extra clients, builds or Relay sessions. Multiplayer random voting is source-reviewed, not a remote-client runtime claim. Hardware haptics and legacy death playback were not exercised. Artistic acceptance remains with the user.
