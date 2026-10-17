# Supplied libraries and source reuse

## Find a source or study an effect

`Docs/VfxSourceCatalog.csv` is the searchable asset index; `Docs/VfxPackageScan.json` records scope, archive counts, hashes/commit provenance and guide coverage. The initial scan covers commits `2072054` and `a258685`, all four supplied VFX trees, and nine locally supplied archives. The catalog includes every on-disk file/metadata plus every archived Unity asset. `DependencyGuids`, shader path, layer/name summary, dimensions and technical tags support targeted inspection. Tags are text-derived hints, not proof that a native module is enabled or a shader works in URP.

Use structured CSV reads; a plain text search can help shortlist but rows may contain escaped/multiline content. For example, from the project root:

```powershell
$catalog = Import-Csv Docs/VfxSourceCatalog.csv
$catalog | Where-Object {
    $_.Role -eq 'VFXSource' -and $_.Kind -ne 'Metadata' -and
    $_.Path -match 'Noise|Smoke|Mask|Trail|Gradient|Debris'
} | Select-Object Container,Path,Guid,Dimensions,Shader,Availability
```

For imported final-prefab inspection, discover live commands, then call the official Pipeline `run_script` tool with:

```json
{"file":"AgentScripts/Inspect-VfxReference.cs","entry":"InspectVfxReference.Inspect","args":"[\"Assets/PixPlays/ElementalBlastVFX/FireBlast/Version_BuiltIn/FireBlast.prefab\"]","timeout_ms":60000}
```

The helper reads the prefab asset without instantiating it or executing its scripts. It reports hierarchy, delays/lifetimes/bursts, simulation/scaling, native collision/subemitters, lights, flipbook settings, exact renderer vertex streams, material texture slots and dependencies. Inspect full serialized modules through Unity APIs when a summarized value is insufficient. Record the learning rather than copying the hierarchy wholesale. Imported materials and custom importers can run native version upgrades when loaded, even during an inspection with no explicit setters. Capture the initial working diff, avoid saving loaded references, inspect the final diff, and restore unintended source changes through targeted Unity serialized APIs. In the initial scan, three PixPlays materials gained or changed alpha-blend fields; those changes were restored while retaining the requested labels.

Unity Project labels `Reference` and `VFXSource` make the imported library browsable. If the user explicitly asks to find/show assets in the Editor, follow the installed Unity Search skill. The CSV also covers unopened archive assets that Unity Search cannot yet find.

## Roles and boundaries

| Asset | Role / allowed use |
| --- | --- |
| Supplied package folders, demo scenes, finished VFX prefabs/variants and VFX Graphs | `Reference`: inspect composition, timing, contact handling and implementation patterns; no gameplay use, direct clone, nested dependency or recolored variant |
| Texture/noise/mask/flipbook, compatible shader/include/subgraph, material, isolated mesh/model | `VFXSource`: reuse or adapt after visual and compatibility inspection; output is a new project assembly |
| Demo scripts, runtime controllers, guides, settings, animations, package archives | `Reference`: study only by default; do not install vendor demo behavior as a project system |
| Demo lightmaps/probes and demo-only graphics/models/materials | `Reference`: excluded from source candidates |
| A prefab that appears to contain only a reusable mesh | `Reference` until examined and explicitly reclassified as an isolated building block; extension/name alone cannot prove that |

Folders/archive files are Reference; child source assets receive VFXSource instead. Roles are separate from content category, readiness and provenance: source artwork stays Placeholder, vendor/tooling remains Others, project effect scripts are Code, assemblies Prefabs and tuning Authored Data. Unknown AI origin stays unknown. `VFXSource` does not make a fantasy glyph, heart mesh or creature appropriate for this game.

A new effect's transitive dependencies must not include finished Reference prefabs/graphs/demo controllers. Source materials may themselves reference incompatible shaders or demo resources; inspect and replace those dependencies. Prefer a new project material referencing an unchanged vendor texture over editing the vendor material/importer globally. Preserve native GUIDs when moving existing assets with AssetDatabase; use project copies for effect-specific adaptation.

## Library map and findings

| Library | Best things to study | Reusable source opportunities / caveats |
| --- | --- | --- |
| `Assets/Hovl Studio/AAA Projectiles Vol 2` | Separate Flash/Projectile/Hit phases, world-space detached trails and contact offsets | Final prefabs remain Reference, including `Projectiles with logic`; use `HSFiles` textures/materials/meshes instead |
| `Assets/Hovl Studio/Magic circles Vol 2` | Layered looping emission and shields | Study mechanical layering; exclude literal spell circles, skulls and ornamental glyphs from project effects |
| `Assets/Hovl Studio/HSFiles` | Shader stream assumptions, impact normal and lifecycle | Noise35/43, masks, gradients, smoke, directional trails, debris, BeamMesh/cones/cylinders/hemispheres; check per-asset channels and UVs |
| `Assets/JMO Assets/Cartoon FX Remaster` | Explosion flash/core/sparks/smoke separation, animated light envelope, dissolve, soft particles | Source masks/flipbooks and CFXR shaders/includes can help; rebuild silhouettes/timing for dark mecha. Cartoon final art and particle text are not the project's look |
| `Assets/JMO Assets/Cartoon FX Easy Editor` + legacy archive | Authoring convenience and historical solutions | Reference tooling; legacy effects are not default runtime dependencies |
| `Assets/PixPlays/ElementalProjectiles` | Cast/flight/hit separation and source/target input | Original imported examples are `Version_BuiltIn`; URP replacements are in a local archive. Demo flight interpolation is not authoritative projectile physics |
| `Assets/PixPlays/ElementalBeams` | Tracking endpoints and separate beam/cast/hit/tip | Source textures/material channels and length-relative lifetimes; verify scale/velocity coupling |
| `Assets/PixPlays/ElementalBlastVFX`, `ElementalAOE`, `ElementalAuras`, `ElementalShields` | Multi-stage sequences, sustained start/stop, radius scaling, contact-local shield response, Timeline examples | Isolated textures/meshes/shaders are sources. Replace elemental fantasy language with technical mechanisms |
| `Assets/GabrielAguiarProductions` | Ground slashes, weapon envelopes, beam growth/contact timing, mesh-bound emissions and larger phase compositions | URP/HDRP Ultra Mega Pack archives contain separate models/maps/texture sheets/materials/subgraphs. Final graphs and 600+ prefabs per pipeline remain Reference |

Concrete inspected sources: Hovl `Noise35.png` and `Noise43.png` are grayscale noise fields; `Mask23.png` has a burst silhouette, `Trail21.png` is a directional wedge, `Gradient11.png` is a narrow gradient, and `Explosion2b.png` is a tiled animation sheet. A large image called Smoke is not necessarily a flipbook: inspect layout and material usage. Pixel dimensions alone do not identify tile counts.

### Hovl guide and implementation findings

Both Readme files describe custom per-particle data driving cutoff/center glow through vertex streams. Verify the actual shader TEXCOORD packing and particle stream list; missing Custom Data produces materially wrong output. `Use depth` controls soft intersection behavior. The standalone Distortion shader uses **GrabPass/UnityCG**, a Built-in path; do not transplant it into URP. The package includes RP changer/editor tooling, but running a global converter is unnecessary for source selection. The guide warns about light cost.

`HS_ProjectileMover` separates flash/projectile/hit and detaches tails, disables flight light/collider on impact and restores parents/transforms/particles on pooled reuse. These are useful lifecycle ideas. Its contact orientation can be replaced by fire-point rotation or a fixed offset and then LookAt, so inspect actual axes rather than copying that sequence. Its cleanup based on `hitPS.main.duration` does not necessarily include latest child delay/lifetime/trail tails. Capture incoming velocity before resetting Rigidbody state. The project should retain its existing native/FishNet projectile motor.

### JMO guide and implementation findings

The HTML guide covers packed single-channel textures, dissolve, UV distortion, soft particles, lit/shadowed particles, HDR multipliers, orthographic soft-particle behavior and a custom `.cfxrshader` importer that selects pipeline code. Inspect the generated native Shader asset and includes before reuse; copying only one custom source file can omit its importer/dependencies. Missing depth texture can make soft-particle effects disappear. Washed-out HDR and gamma/linear light intensity mismatches need inspection in the existing scene, not global setting changes.

`CFXR_Effect` handles native particle completion, destroy/disable, animated Light curves and its own camera shake. Learn light envelopes and lifetime ownership; use the project's Animator/Cinemachine/pooling boundaries instead of adding that whole controller. Literal text/stars, bouncy cartoon smoke and color-heavy final prefabs are style counterexamples.

### PixPlays guide and implementation findings

Read all six README files as a family; several copy-paste the AOE archive/demo name into unrelated package instructions. Trust actual catalog paths and package contents, not those copied names. Six specific URP archives exist locally. The shared `VfxData` supports source/target transforms with captured-position fallback plus duration/radius/ground. This is a useful context pattern, but not an instruction to depend on the vendor classes.

`ProjectileVfx` uses a coroutine and Lerp/flight curve with cast, travel and hit slots. `_FlySpeed` is used as a duration divisor, not physical speed. Its final direction calculation can become degenerate near the target. `BeamVfx` updates endpoints, scales the body and tracks the tip; guard zero distance and use the actual contact normal for impact. `ParticleSystemScaleLifetime` derives lifetime from local Z scale; uniform root scaling alone can produce unexpected timing. `BaseVfx` schedules Destroy and stop calls, which must not be copied directly into a pooled lifecycle. Shield `OnHit(point,normal)` is an appropriate presentation boundary; project authority still determines the hit.

The inspected FireBlast prefab has six native Particle Systems, distinct short cast/shockwave/longer end layers, and renderers using UV2 + Custom1XYZW + Custom2XYZW in addition to position/normal/color/UV. Material reuse without that stream contract can fail. Study it structurally; it was not played or approved as project art.

## PDF-derived guidance (all six supplied guides read)

The six PDFs total 43 pages. General and WeaponFX guides are image-based and were rendered/read visually; the other four have extractable text. Both large archives carry byte-identical copies of these six guides, verified by SHA-256.

- **General guide, 6 pages:** choose the matching URP/HDRP version; recompilation may be needed after import/version changes; depth/opaque texture features affect visibility. Ignore instructions to import everything or replace the project's pipeline/Global Volume just to get the demo appearance.
- **WeaponFX, 7 pages (especially p.6):** mesh-envelope materials use Voronoi or gradient noise, main/albedo plus metallic/AO and an authored mask where white exposes the effect. Boundaries/direction/scale fit the weapon. Rebuild with compatible HLSL/Particle System attachment; do not attach the finished vendor graph. Preserve the project's toon material and use a controlled overlay/owned material only where needed.
- **GroundSlashes, 7 pages:** fire-point/camera setup and axis interpretation differ between FPS/TPS demos. Study travel/impact choreography; demo scripts are explicitly demonstration code.
- **LevelUp, 7 pages:** mesh-bound effects require the correct SkinnedMeshRenderer and transform binding. The demo adds a second glow material and animates it with character animation. For project repair/reactor-charge presentation, preserve toon rendering and separate visual state from actor animation/data ownership.
- **StylizedBeams, 8 pages (pp.6-7):** BeamEndPoint binds the beam/impact; normalized length and width curves scale with total duration, and delayed impact should match growth reaching the endpoint. Retiming duration requires retiming those curves/contact onset together. Native equivalents can use separate Particle Systems/LineRenderer/Animator where suitable.
- **SummonCreatures, 8 pages:** orientation constraints differ for ground vs flying motion. Creature assets and demos are Reference; convert intent into drones/technical projection rather than fantasy summons. The guide lists attribution requirements, including separately credited creature sources; retain notices for any reused underlying asset. Record relevant credit evidence, without treating the guide as a complete license audit.

## Archive-only sources

Nine `.unitypackage` files are locally present but ignored by the existing `*.unitypackage` Git rule. Their metadata is tracked. The catalog distinguishes `Container` and `Availability`; **a catalog entry does not mean its bytes will exist on another clone**. Report a missing archive if a needed source is unavailable; do not pretend it is imported or silently fetch an unrelated substitute.

Use the catalog's GUID/path/hash to read a selected member with Python `tarfile` into ignored `Temp/VfxRefinery`, without `extractall` or importing the full package. Review dependencies recursively. Prefer URP content; HDRP's final graphs/materials are study-only for this renderer. Before importing a selected source, check `AssetDatabase.GUIDToAssetPath`: reuse an already present compatible asset instead of introducing a duplicate GUID. Stage only needed source assets and source dependencies, reject paths outside the intended root, and import/copy via a focused Unity API authoring action into the effect's source folder. Preserve source bytes/GUID lineage in the origin record; use a fresh GUID for an adapted copy when the original already exists. Never overwrite the project's settings, scenes, manifest or vendor assets using archive paths.

## Maintenance and origin record

Regenerate after source library additions/deletions with `AgentScripts/Build-VfxSourceCatalog.py` using bundled Python with Pillow/pypdf. This inventories and hashes files/archives; it does not import them. Then update `AgentScripts/Update-ProjectClassification.ps1` and apply labels through `ApplyAssetClassification` in bounded Unity batches. Catalog and native labels are snapshots; new packages require review and regeneration. Keep new package roots/commits in the scanner's explicit scope when expanding the library.

When the first effect is produced, create `Docs/VfxEffectOrigins.csv` with columns:

`Effect,OutputPath,SourcePath,SourceGuid,Container,Role,Use,Changes,LicenseEvidence,ValidationEvidence`

Use `Role=VFXSource` for actual reused assets and `Role=Reference` with `Use=Study only` for studied finished effects. No runtime Reference dependency is allowed. Record source materials/maps and any transitive shader/include or mesh reuse, including copies/adaptations. For untouched shared source assets record `Changes=None`; for authored project artwork keep provenance unverified unless actual generation evidence exists. This record is created when real effects exist; no fictitious origin rows belong in the initial skill setup.
