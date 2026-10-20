# Original supplied locomotion

Restores the earlier idle, walking/running and dash motion requested on 2026-10-19. Both current Humanoid characters share these native imported clips; the dash is again distinct from running.

| Imported source | Original supplied bytes |
| --- | --- |
| Idle.fbx | Assets/_Project/Models/Player/Mecha.fbx, originally Mecha@Idle_V2.fbx |
| Run.fbx | SourceArchive/NewPlayerUnused-2026-10-14.zip / Mecha@Running.fbx |
| Dash.fbx | SourceArchive/NewPlayerUnused-2026-10-14.zip / Mecha@Dash.fbx |

The files are unchanged source copies; Unity ModelImporter owns Humanoid conversion, generated source Avatars and looping/root bake settings. Physics still owns actor movement. Original source importers, old extracted .anim files, character rigs and combat clips remain unchanged. No per-character pose baking, corrective curves, runtime retargeting component or source mesh edits were introduced. Source provenance remains that of the supplied assets; no AI provenance is inferred.

The prior Generic .anim restoration froze on the current Humanoid rigs and was replaced before delivery. Native source playback was then observed in advancing Play Mode for Idle/Run/Dash on both characters. Hand displacement over the short sample was approximately .018/.263/.429m for Dasher and .015/.230/.414m for Zero. Visual captures are in ignored Logs/LocomotionRevert. All six profile references are humanMotion=true and looping; no new console errors were observed. No automated tests or networking checks. Artistic acceptance remains with the user.

AgentScripts/HumanoidRigAuthoring.cs RestoreLocomotion imports these source files and updates only Idle/Run/Dash in both profiles/controllers. Its broader NativeLocomotion authoring entry also preserves these restored mappings.
