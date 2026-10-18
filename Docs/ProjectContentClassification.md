# Project content classification

## JSON persistence — 2026-10-18

The save-system pass adds GameSave, GameSaveKeys and LegacySettingsMigration with Unity metadata as provisional Code. Existing audio, video and controls Code now writes device-local JSON, and localization Authored Data gains a shared save-failure message. Save documentation and manual inspection tooling remain Others. The refreshed inventory includes the added sources and documentation. Runtime JSON/backup files live outside the repository under Unity's persistent data path and are generated local output. No artwork, new dependency or provenance claim was introduced. See [save system](SaveSystem.md).

## Controls and localization consolidation — 2026-10-18

The Controls screen adds six runtime sources (Code), an editor authoring tool and localization migration helper (Others), and the `ControlBindings` ScriptableObject (Authored Data), with their Unity metadata. Existing interface Prefabs, scene composition, Input Action data and localization tables retain their current provisional categories. Documentation and localization consolidation/review CSVs are Others. Native uGUI graphics and the existing focus-border Sprite are reused; no artwork or dependency was added. Unknown provenance remains unverified. The refreshed file inventory includes these maintained files. See [controls configuration](ControlsConfiguration.md).

The 2026-10-18 configuration revision adds GeneralConfigurationMenu and VideoQualityRuntime as provisional **Code**, with Unity metadata. Existing menu/lobby prefabs remain **Prefabs**, MainScene remains **Scenes**, and localization tables/video defaults remain **Authored Data**. Editor authoring and documentation remain **Others**. Native TMP dropdowns and existing Sprite graphics are reused; no artwork, locale or external dependency is added. The file inventory includes the new source paths; provenance stays Unverified. See [configuration behavior](ConfigurationAndCombatLayout.md).

The 2026-10-18 localization key refactor changes wording identifiers and existing references, preserving entry IDs, English text, fonts, and locale scope. Existing table/data, Code, Prefabs, Scenes and Others classifications remain unchanged. No artwork or language was added.

## English localization — 2026-10-17

Game wording now uses Unity Localization category String Tables and serialized localized references. The English locale, String/Asset Tables, shared table data and Localization Settings under `Assets/_Project/Localization` are provisional **Authored Data**. `Fonts/EnglishDefault.asset` copies the existing imported Liberation Sans SDF artwork outside Resources for Addressables font loading; its upstream creation provenance is unverified and it remains **Placeholder**. New runtime localization source is **Code**. Editor capture, build policy and migration/inspection scripts are **Others**, as are package-generated Addressables configuration and its metadata. Existing scenes, prefabs and authored game data retain their categories. No final content approval is implied. See [localization authoring and policy](Localization.md).

## Route availability contrast — 2026-10-17

This revision changes existing routing Code, the LevelRouting Authored Data profile and supporting documentation. Native per-renderer material overrides reuse the existing miniature artwork, rings and path sprites; no new artwork, material, shader, prefab or maintained file is added. Categories, provisional readiness, provenance and inventory paths/totals remain unchanged. Manual Play Mode captures stay under ignored Logs/LevelRoutingContrast.

## A/S/D thermal skills — accepted initial baseline, 2026-10-17

Original native Particle System assemblies, an authored S casting clip and light animation, project-owned source-map copies/materials/HLSL, three read-only skill profiles and focused VFX scripts now live under `Assets/_Project/VFX/FireSkills`. Two shared combat-origin/window source files remain in Combat; gameplay contact tuning remains in the existing AttackPiece assets. New effect assemblies are Prefabs, runtime C# is Code, profiles/controller are Authored Data, maps/materials/shader/animation are Placeholder artwork, and authoring/inspection/source records are Others. Supplied map lineage is recorded in [VfxEffectOrigins.csv](VfxEffectOrigins.csv); upstream AI provenance stays unverified and no final art approval is implied.

The snapshot contains **10,055 inventory files**: 22 AI Generated, 218 Authored Data, 290 Code, 3,901 Others (including the archive-status row), 5,550 Placeholder, 66 Prefabs and 8 Scenes. The two retained user-supplied feedback/reference images under Docs/References are Others, not generated game artwork. At initial-baseline acceptance the user requested cleanup: eight generated maintained review images/animations plus 184 ignored raw captures were removed; written iteration lessons, source lineage and 26 ignored JSON runtime reports remain. Earlier passes remain historical review evidence. Revision 5 adds a native terrain-preparation Prefab, a focused VFX script, distortion shader/materials, an authored combat VolumeProfile, native lit terrain material, authoring support/notes and a selected native review sheet, including metadata. The existing IndustrialPlatforms texture retains its confirmed AI Generated/Placeholder provenance; no new source artwork pixels were generated. Scene and platform-prefab changes are intentional configuration/material bindings. Revision 6 adds one Placeholder VolumeFlame material with native metadata, a selected native review sheet and the original user feedback capture; the refreshed inventory also includes current native support metadata. It replaces the active curl mask treatment, binds smoke to native particle events and improves preparation/density. Revision 7 moves the source preparation script/prefab/ignition material through Unity APIs with GUIDs retained, extends D fire fade and blends graphic anime bands with texture detail. The user-supplied art reference is Others; the generated phase sheet was subsequently removed during cleanup. No new gameplay image pixels were generated. Retired source copies remain historical assets. Source libraries retain independent Reference/VFXSource labels and original source files/importers. No finished vendor effect is a dependency. See [effect and validation notes](FireSkillRework.md). No automated tests were added or run.

## VFX Refinery - 2026-10-17

The two latest VFX import commits add 2,444 library files/metadata plus two dependency configuration changes. [VfxSourceCatalog.csv](VfxSourceCatalog.csv) indexes those trees and nine locally supplied, Git-ignored source archives, including 4,696 assets inside the archives. The local archive bytes are explicit inventory exceptions: they are useful supplied dependencies, but the current Git rule will not carry them to another clone. Archive members are catalog entries, not separately imported project files. See [VFX scan and workflow](VfxRefinery.md).

Independent native labels now distinguish **Reference** (finished vendor effects, demos, scripts, guides, archive files and library folders; study only) from **VFXSource** (reusable textures/noise/maps, compatible shaders/materials and isolated models; inspect before reuse). There are 578 source assets and 734 reference files/folders in the 1,312-asset native labeling scope. These roles are not production categories or authorship claims. Source artwork is Placeholder; vendor reference/support is Others; unknown upstream AI origin stays unverified. Finished effects cannot be used, nested or recolored as project effects.

The new project VFX skill, catalog, maintenance and inspection helpers, architecture notes and feedback record are Others support content. Future effect-owned source under `Assets/_Project/VFX` is Code, prefab assemblies Prefabs, ScriptableObject tuning Authored Data, authoring records Others and unfinished art Placeholder. Existing effects were not moved or replaced. The inventory and Unity label tooling preserve these roles on regeneration. No automated tests or gameplay effects were created during this setup.

The refreshed inventory contains **9,943 files**: 22 AI Generated, 208 Authored Data, 274 Code, 3,879 Others (including one archive-status row), 5,496 Placeholder, 56 Prefabs and 8 Scenes. The nine local ignored package files are included in this total; archived members are cataloged separately. Native inspection and saved metadata confirm all 1,312 VFX library roles, unchanged existing GUIDs and retained unrelated labels. Unity also normalized older importer serialization while applying labels; no vendor source files were edited. The earlier totals below are historical snapshots.

## Teammate ring depth follow-up — 2026-10-17

This correction changes existing files only: actor indicator Code, squad Authored Data, the player Prefab, editor/manual inspection support and these notes. The ring now uses a native world-space Canvas/Image with the existing HollowRing Sprite; no new artwork, shader, material or dependency was added. Categories, provisional readiness, unverified provenance and the **7,478-file** inventory totals remain unchanged. Ignored Play Mode captures are retained under Logs/TeammateRing.

## Level routing follow-up — 2026-10-17

The inventory now contains **7,478 maintained files**: **274 Code**, **208 Authored Data**, **56 Prefabs**, **8 Scenes**, **4,398 Placeholder**, **22 AI Generated**, and **2,512 Others** rows, including metadata. This pass adds nine runtime routing/encounter/presentation source files, an Editor authoring tool, a manual session-inspection tool, one routing profile, four encounter definitions, six native prefab assemblies, the dedicated LevelRouting scene, two Animator controllers, five animation clips, seven provisional materials, and research/implementation documentation. The later 3D-scene revision removes the two GUI prefabs and adds the world-node prefab plus scene, keeping 79 added inventory rows. Unverified provenance is retained; no AI-image origin is asserted. Rings/dots reuse the existing hollow-ring and Unity built-in Knob sprites. Code/configuration/prefabs/scenes are provisional; primitive miniatures, materials and animation remain unfinished art. MainScene, LevelRouting, native prefabs and build-scene registration are authored through Unity APIs. Captures, logs and the earlier development build remain ignored artifacts outside maintained game content. See [level routing](LevelRouting.md).

## Current policy — game adjustments, 2026-10-16

The user's classification correction supersedes the historical entries below: **Placeholder applies to unfinished artwork**, including images, models, materials, visual effects, animation and supplied audio. Runtime source is **Code**, reusable assemblies are **Prefabs**, ScriptableObject definitions/profiles and native controller/input/mixer configuration are **Authored Data**, and level compositions are **Scenes**. These four categories have **Provisional** readiness; none implies final approval. **Others** remains for vendor dependencies, maintenance/editor tools, documentation, tests and project configuration. Confirmed generated artwork retains **AI Generated + Placeholder**. Unknown provenance remains unverified.

The game-adjustment pass adds a static hollow-ring image rendered through native SpriteRenderers, native world-space enemy health bars and pilot labels, a secondary weapon using the supplied Sci Fi Gun Heavy art, authored settings, a chat assembly and a targeted chat regression fixture. The 2026-10-17 follow-up replaces the original Particle System bullets with physical GameObject bullet prefabs and native Unity physics. The ring is authoring-time geometric artwork; its generation process is not evidence of AI image-generation provenance. Imported weapon artwork is Placeholder; the package's prefabs are Prefabs. New interface/environment assembly prefabs keep their referenced artwork's provenance separate from assembly configuration.

Current controller/audio follow-up inventory: **7,399 maintained files**, including **256 Code**, **194 Authored Data**, **44 Prefabs**, **6 Scenes**, **4,374 Placeholder**, **22 AI Generated** and **2,503 Others** rows (metadata included). This includes the latest supplied audio package commit, 71 selected clips moved with retained GUIDs, the audio service/footstep/UI/menu code, native mixer and read-only configuration profile, audio/options menu prefabs, editor authoring/inspection support, origin manifest and documentation. Imported audio is Placeholder artwork with supplied/imported origin and unverified upstream AI authorship; folder and package support content is Others. Native mixer configuration is Authored Data. The controller/portal follow-up reuses the existing hollow-ring Sprite and native particle profiles; it adds no artwork or generated graphics. See [audio implementation](FullAudioImplementation.md), [controller and portal refinements](ControllerAudioAndPortalRefinements.md) and [selected clip origins](AudioClipOrigins.csv). The configuration follow-up adds native video settings data, source/editor authoring and manual inspection support; it changes existing menu/lobby prefabs and controller input data without adding artwork. See [configuration and combat layout](ConfigurationAndCombatLayout.md). This snapshot supersedes historical totals below.

`AgentScripts/Update-ProjectClassification.ps1` regenerates the complete inventory using the current categories. `ApplyAssetClassification` removes obsolete managed labels before applying the current labels through Unity APIs. All historical totals and descriptions below describe the earlier policy and are retained as a record, not current classifications.

The latest aim-feedback pass adds three native visual animation clips (Placeholder) and one Animator controller (Authored Data), each with Unity metadata. It reuses the hollow-ring Sprite; no image, mesh, material, shader or external dependency was added. Aim/spear source remains Code, their tuning/controllers remain Authored Data, and actor/projectile assemblies remain Prefabs. The native labels and inventory include these eight new paths. Provenance remains unverified. The preceding projectile platform/foreground revision modified existing content only.

The locked-tracking and settings-organization pass moves 75 existing ScriptableObject profiles into responsibility folders through Unity APIs, retaining GUIDs and Authored Data readiness. New editor organization code, its metadata, the category guide and folder metadata are Others support content. Settings folders are not unfinished artwork; the current inventory corrects their historical folder labels. The existing gun/aim source remains Code and the settled tracking clip remains Placeholder animation. The user explicitly authorized only two existing test path-literal updates required by these moves; no tests were run. See [the category guide](SettingsCategories.md).

The nearest-target follow-up changes existing aim Code, the gun Authored Data profile and support notes/helper only. Its switching delay and distance margin remain in the existing Guns profile. No assets or maintained paths were added or removed; inventory totals and native classifications remain unchanged.

## Controller support — 2026-10-16

The Input Action asset, player prefab, MainScene, HUD/input/menu scripts and new controller interface profile remain **Placeholder**, with their existing unverified provenance preserved. New `ControllerInterfaceSettings`, `GameInputPresentation`, `ControllerTextEntry` and `ControllerTextField` scripts and their metadata are also **Placeholder / Unverified**. `Assets/Editor/MechaControllerSetup.cs`, its metadata and `Docs/ControllerSupport.md` are **Others** support content. Native uGUI/TMP and the existing Unity Input System supply the controls; no artwork or external package was added. The inventory includes these new maintained files. Temporary Gamepad devices used for manual inspection are runtime state and are not maintained content.

## Enemy and UI polish — 2026-10-16

Enemy movement-facing fixes, flatter lance tuning, stationary scenery impacts, the 25% larger HUD and the lobby feedback pass modify existing provisional scripts, profiles, projectile prefab and MainScene. The new `Assets/_Project/Scripts/Interface/LobbyPlayerCard.cs` and its Unity metadata are **Placeholder** content with **Unverified** origin under the existing policy. Bordered cards reuse Unity's built-in UI Sprite; lance impact sparks reuse the existing native Particle System profile. No generated artwork or new external dependency was added. Editor setup and documentation changes remain **Others**. The maintained-file inventory includes the new presenter and metadata; no final-product approval is implied. Temporary local previews are excluded from maintained content.

## Repository lock recovery — 2026-10-16

Removed a confirmed stale, empty `.git/index.lock` after checking that no Git process was running and that the file could be opened exclusively. An index refresh completed without a lock error and released its lock normally; existing working changes and staged selections were preserved. The process that originally left the lock behind could not be identified from the available log.

Git reliability instructions in `AGENTS.md` now require sequential repository writes, inspection with optional locking disabled, waiting for active commands before retrying, and checking lock ownership before recovery. These documentation changes remain **Others** support content. No maintained files or game assets were added; inventory paths and classifications remain unchanged. These precautions reduce recurrence; interrupted Git processes can still leave stale locks.

Recorded at the user's request on 2026-10-16. **All current game content is provisional and used for testing. Nothing is approved as final product content.** This includes imported characters, AI-generated scenery, animations, UI, VFX, scenes, prefabs, authored tuning and gameplay implementations.

## Categories and provenance

[ProjectFileClassification.csv](ProjectFileClassification.csv) contains one row for every maintained file: tracked files plus non-ignored new files, including vendor files and Unity metadata. It records category, readiness/status, origin, kind, Unity labels and evidence. No files are moved or renamed for classification; existing GUIDs and references are retained.

| Category | Meaning | Readiness |
| --- | --- | --- |
| AI Generated | Confirmed generated images and native Sprite assets derived from those images. | Also Placeholder; generation is an origin, not a production approval. |
| Placeholder | Unfinished artwork: models, materials, images, visual effects and animation. | Replace, refine or review before any future final-product approval. |
| Code | Runtime game source. | Provisional. |
| Prefabs | Reusable actor, world and interface assemblies. | Provisional; referenced artwork can remain Placeholder. |
| Authored Data | ScriptableObjects, native animation controllers, input assets and other game configuration. | Provisional. |
| Scenes | Scene composition. | Provisional. |
| Others | Vendor dependencies, Unity/package resources, documentation, tooling, existing tests, configuration, registries and archives. | Support or Archive; this category does not mean final or human-authored. |

Unknown AI origin stays **Unverified**. Commit authors, filenames, Unity serialization and procedural generation do not establish AI authorship. Supplied/imported models and clips retain that lineage, while their original creation method and licensing are not established by this classification. Existing source may have AI assistance; the repository does not provide sufficient per-file evidence to certify all source origins.

Confirmed AI artwork: `CyberpunkCity.png`, `IndustrialPlatforms.png`, `CyberpunkFarSky.png`, `CyberpunkMiddleSkyline.png`, `CyberpunkNearRooftops.png` under `Assets/_Project/Art/Environment/`, plus the six native Sprite assets that reference them. Evidence and prompts are in [scenario art](ScenarioArtPrompts.md) and [three-layer art](ThreeLayerParallaxPrompts.md). Their `.meta` files inherit the classification of their owner; metadata itself is produced by Unity.

Unity's built-in Project labels match the current categories. Confirmed AI art receives both AI Generated and Placeholder. Other existing labels are preserved. Search with `l:Code`, `l:Prefabs`, `l:"Authored Data"`, `l:Scenes`, `l:Placeholder`, `l:"AI Generated"` or `l:Others` in the Project window. Folder `.meta` entries and non-importable files remain covered by the inventory; they do not need synthetic Unity assets. Vendor labels are local project metadata, not changes to vendor source or license notices.

## Scope and maintenance

The maintained-file inventory excludes Git internals and ignored/rebuildable local output: `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, `.vs/`, generated IDE projects and builds. These are **Others / generated local output**, not authored product content. Resolved UPM dependency contents in `Library/PackageCache` are covered by the package manifest/lockfile rather than copied into this inventory. Nothing needs to enumerate or classify every regenerated cache artifact individually.

After adding, moving or deleting maintained files, run from the project root:

```powershell
./AgentScripts/Update-ProjectClassification.ps1
unity command run_script --project-path D:/PersonalProjects/project-mecha --file AgentScripts/ApplyAssetClassification.cs --entry ApplyAssetClassification.Apply
```

Update the explicit provenance rules only when there is supporting evidence. The inventory is a snapshot, not an automatic claim about future files. Changing a placeholder's readiness requires an explicit future content review; the current default remains provisional.

## Image-based objective pointer

The previous `ObjectiveArrowGraphic` built a four-vertex mesh in `OnPopulateMesh`; it did **not** use a sprite. It has been replaced with a native **UnityEngine.UI.Image** using Unity's built-in `DropdownArrow` Sprite. `ProceduralLevelSettings.objectiveArrowSprite` holds the authored reference, so a future downward-facing replacement image can be assigned in the Inspector. Runtime code only positions, rotates, shows/hides and tints the Image; it does not construct its visual shape.

The built-in sprite points down, so the HUD adds 90 degrees when orienting it toward an enemy or portal. Existing objective selection, edge inset, colors and network responsibilities are preserved. **Tools > Mecha > Apply Objective Arrow Sprite** reapplies only this visual setup; the full procedural setup also uses it. The built-in sprite lives in Unity resources rather than a new project image file; its origin is Unity Built-in, its use here is Placeholder, and its project reference is inventoried in the procedural profile and scene.

Use image assets for fixed visual graphics such as arrows, icons and decorative symbols, rendered with native Image/SpriteRenderer components. Direction and placement still require ordinary calculations. Native Particle System/VFX Graph simulation, physics, procedural room layout and gameplay decisions retain their appropriate computation.

## Validation on 2026-10-16

The complete snapshot contains 2,892 maintained files: 22 AI Generated entries (11 image/Sprite assets plus their metadata), 479 Placeholder entries and 2,391 Others entries. Every one of the 1,261 importable file assets has its expected native labels; the inventory also covers folder metadata and files outside Assets. No unverified source authorship was promoted to a confirmed AI claim.

Unity compilation completed with no errors after the Image migration. Live inspection confirmed the Image used DropdownArrow and that no old custom-arrow components remained before the obsolete script was removed through AssetDatabase. Source/reference checks confirm the scene and profile reference the native Image and built-in Sprite. The label operation exceeded Pipeline's 60-second response limit while imports continued, and subsequent main-thread operations timed out. Labels were verified directly from the saved Unity metadata. The maintenance command now batches label changes with StartAssetEditing/StopAssetEditing to avoid per-file reimports on future runs.

Play Mode rendering and directional behavior could not be checked after that editor timeout. The Editor was in stopped Edit Mode before the operation; this pass never entered Play Mode or started a network session. No automated tests were created, changed or run. Existing FishNet obsolete-API warnings and a supplied weapon OBJ self-intersection warning appeared during imports; no vendor source was changed.

## Reference-cadence content update — 2026-10-16

The movement/combat/HUD pass adds three authored attack pieces (Dive Strike, Critical Cross-through and Magnetic Recall), charge/recall particle profiles, a skill-presentation profile/material, and native uGUI diagnostics/skill-focus presentation. These remain **Placeholder** with unverified upstream authorship; no final-product readiness is implied. Setup/extraction/manual-inspection utilities and the frame-analysis document/reference images are **Others**. The recordings and screenshot studies do not establish new AI image provenance. Existing confirmed AI environment assets retain their separate AI Generated + Placeholder classification.

The new cockpit's panels, bars, letterbox and frame meter are native UI Images using Unity's built-in UISprite. The focus backdrop is a native SpriteRenderer using the same built-in sprite. No new generated art or runtime shape generator is involved. The original recordings were read from the user-supplied Downloads paths; decoded frames and intermediate media tooling stay in ignored Temp output. Only selected reference studies and gameplay screenshots are maintained under Docs/References.

The updated maintained-file snapshot contains **2,924 files**: **22 AI Generated**, **499 Placeholder**, and **2,403 Others** (including the preserved archive). The native label pass covered **1,273 importable assets**, updating **12** new assets. All new runtime content retains provisional readiness. The earlier counts above describe the preceding objective-arrow pass.

The follow-up HUD placement and speed-matched locomotion changes modify existing runtime scripts, animation/layout profiles and MainScene, all retaining Placeholder readiness and their existing origin classifications. The refreshed cockpit screenshot and updated documentation remain Others. No maintained files or art assets were added; inventory paths and counts are unchanged. Temporary offline preview objects and capture intermediates are not project content.

The subsequent 50% HUD size reduction updates the same existing layout profile, setup tools, scene and cockpit screenshot. Existing classifications, maintained-file inventory paths and totals remain unchanged.

## Network synchronization update — 2026-10-16

NetworkSimulationSettings, its authored profile and the development-only NetworkValidationProbe remain **Placeholder** prototype content with **Unverified** origin. The focused editor setup, manual inspection helper and networking document are **Others** support files. Existing actor/projectile prefabs remain Placeholder. This pass adds no generated art, changes no provenance claims, and leaves FishNet vendor source untouched. Separate-player builds, local logs and intermediate build output remain ignored.

The refreshed inventory contains **2,934 files**: **22 AI Generated**, **505 Placeholder**, and **2,407 Others** (including the archive). Native labels cover **1,277 importable assets**, with **4** new assets labeled. These totals supersede the earlier historical snapshots.
