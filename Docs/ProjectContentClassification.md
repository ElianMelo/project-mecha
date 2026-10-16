# Project content classification

## Repository lock recovery — 2026-10-16

Removed a confirmed stale, empty `.git/index.lock` after checking that no Git process was running and that the file could be opened exclusively. An index refresh completed without a lock error and released its lock normally; existing working changes and staged selections were preserved. The process that originally left the lock behind could not be identified from the available log.

Git reliability instructions in `AGENTS.md` now require sequential repository writes, inspection with optional locking disabled, waiting for active commands before retrying, and checking lock ownership before recovery. These documentation changes remain **Others** support content. No maintained files or game assets were added; inventory paths and classifications remain unchanged. These precautions reduce recurrence; interrupted Git processes can still leave stale locks.

Recorded at the user's request on 2026-10-16. **All current game content is provisional and used for testing. Nothing is approved as final product content.** This includes imported characters, AI-generated scenery, animations, UI, VFX, scenes, prefabs, authored tuning and gameplay implementations.

## Categories and provenance

[ProjectFileClassification.csv](ProjectFileClassification.csv) contains one row for every maintained file: tracked files plus non-ignored new files, including vendor files and Unity metadata. It records category, readiness/status, origin, kind, Unity labels and evidence. No files are moved or renamed for classification; existing GUIDs and references are retained.

| Category | Meaning | Readiness |
| --- | --- | --- |
| AI Generated | Confirmed generated images and native Sprite assets derived from those images. | Also Placeholder; generation is an origin, not a production approval. |
| Placeholder | Current prototype game code, art, scenes, prefabs, materials, profiles and other game content without confirmed AI provenance. | Replace, refine or review before any future final-product approval. |
| Others | Vendor dependencies, Unity/package resources, documentation, tooling, existing tests, configuration, registries and archives. | Support or Archive; this category does not mean final or human-authored. |

Unknown AI origin stays **Unverified**. Commit authors, filenames, Unity serialization and procedural generation do not establish AI authorship. Supplied/imported models and clips retain that lineage, while their original creation method and licensing are not established by this classification. Existing source may have AI assistance; the repository does not provide sufficient per-file evidence to certify all source origins.

Confirmed AI artwork: `CyberpunkCity.png`, `IndustrialPlatforms.png`, `CyberpunkFarSky.png`, `CyberpunkMiddleSkyline.png`, `CyberpunkNearRooftops.png` under `Assets/_Project/Art/Environment/`, plus the six native Sprite assets that reference them. Evidence and prompts are in [scenario art](ScenarioArtPrompts.md) and [three-layer art](ThreeLayerParallaxPrompts.md). Their `.meta` files inherit the classification of their owner; metadata itself is produced by Unity.

Unity's built-in Project labels are applied to importable file assets: **AI Generated**, **Placeholder**, **Others**. Confirmed AI art receives both AI Generated and Placeholder. Other existing labels are preserved. Search with `l:Placeholder`, `l:"AI Generated"` or `l:Others` in the Project window. Folder `.meta` entries and non-importable files remain covered by the inventory; they do not need synthetic Unity assets. Vendor labels are local project metadata, not changes to vendor source or license notices.

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
