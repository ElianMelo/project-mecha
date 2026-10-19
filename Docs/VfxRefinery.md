# VFX Refinery

Established 2026-10-17 from the user's VFX authoring brief. The entry point is [.agents/skills/unity-vfx-refinery/SKILL.md](../.agents/skills/unity-vfx-refinery/SKILL.md). `AGENTS.md` requires it for every VFX creation, revision or diagnosis; automatic invocation is enabled, and the previous Graph skill routes through it.

The visual direction is dark 1980s anime mecha with toon shading, Grand Chase combat readability and Armored Core mechanical language. Particle System handles ordinary and multi-phase effects. Native materials precede focused ShaderLab/HLSL for demonstrated special-material needs; Shader Graph is a fallback and VFX Graph is supplemental. Cryogenic, radiation, plasma, pressure, armor and machinery replace fantasy elemental presentation. New effects and their assets/profiles/scripts live under `Assets/_Project/VFX/<Effect>/`.

## Scan coverage

| Commit | Description | Scope |
| --- | --- | --- |
| `20720549e0d4d6179b3e6c1d61facac469b7cea8` | Import VFX packages | Gabriel Aguiar guides/archive metadata, JMO, PixPlays; Shader Graph manifest/lock change |
| `a25868529b2539e83bb6f0b0e529e25e5757ce1e` | Import more VFX packages | Hovl projectiles, magic circles and shared source library |

Together they changed **2,446 files**: **2,444** library files/metadata and two dependency configuration files. The first commit explicitly adds Shader Graph 17.3.0; the project already has URP/VFX Graph 17.3.0, native Particle System, Timeline, Animator, Cinemachine and Unity physics. Live inspection confirmed Unity 6000.3.11f1, URP, Linear color, clean MainScene and stopped Play Mode.

The scan also reads **nine locally supplied, Git-ignored `.unitypackage` files**, giving **2,453 on-disk files** and **4,696 archived Unity assets**, for **7,149 catalog rows**. Archive assets include folder records and alternate-pipeline duplicates; these are not 4,696 unique imported usable effects. Members were streamed/read without importing demos or extracting whole packages. Non-asset package-manager manifest records were also read and recorded separately.

| Local archive | Assets inside |
| --- | ---: |
| Gabriel Ultra Mega Pack 6.3 HDRP v1.5 | 1,670 |
| Gabriel Ultra Mega Pack 6.3 URP v1.5 | 1,663 |
| JMO Cartoon FX 1 legacy | 636 |
| PixPlays Elemental AOE URP | 125 |
| PixPlays Elemental Auras URP | 78 |
| PixPlays Elemental Beams URP | 119 |
| PixPlays Elemental Blasts URP | 130 |
| PixPlays Elemental Projectiles URP | 149 |
| PixPlays Elemental Shields URP | 126 |

The existing Git rule ignores the package archives while tracking their metadata. Their bytes will not accompany a fresh clone under that rule. The catalog records archive-only availability and SHA-256 identity; the inventory includes these nine local dependencies as a documented exception to the maintained-file rule. This setup does not alter Git policy or stage large archives.

## Reference and source roles

`Reference` and `VFXSource` are independent Unity labels, separate from category, readiness and provenance. Finished vendor VFX prefabs/variants, Graphs, demos, scripts, guides and library folders are Reference. Texture/noise/mask/flipbook assets, compatible shaders/includes/materials and isolated models are VFXSource candidates; demo lightmaps/probes and demo-only content are excluded. The file catalog contains **578 VFXSource assets** and **563 Reference files**, plus metadata. Including folders, native label scope is **1,312 imported assets/folders**: 578 VFXSource and 734 Reference.

Study Reference effects for composition, phase timing, module settings, custom streams and common solutions. Build a new project effect from source components. A recolored vendor prefab, nested final effect or variant is prohibited. A source label does not approve fantasy glyphs or certify compatibility. Check transitive dependencies so sources never bring a finished Reference effect or demo system into gameplay.

Source artwork remains Placeholder; vendor reference/support content remains Others. Unknown upstream AI authorship is unverified. Future project runtime source is Code, assembled prefabs Prefabs, tuning ScriptableObjects Authored Data and unfinished art Placeholder. No supplied content has final-product approval.

## Guides and findings

Read **six PDFs / 43 pages** covering general setup, weapons, ground slashes, level-up/mesh binding, stylized beams and creature demos. General/WeaponFX are image-based and were rendered for reading; copies in both large archives are byte-identical to these outside guides. Read all six PixPlays README files, both Hovl Readmes and the JMO HTML/editor guide; inspected native prefabs/material dependencies and key interaction scripts.

The skill's [source-library reference](../.agents/skills/unity-vfx-refinery/references/source-library.md) records custom vertex streams/packed channels, light costs, Built-in GrabPass incompatibility, pipeline-specific archives, beam endpoint/duration coordination, mesh-mask fitting, source/target snapshots, retained tails, pool reset and demo-script limitations. Package instructions to import everything or replace global rendering settings are not adopted.

## Authoring and learning

Each effect starts with a compact breakdown and layer timeline. Source/target/contact snapshots and incoming velocity distinguish penetration from surface-normal splash and reflected response. Native collision/bounce/light are added when needed. Focused projectile, hit and reaction VFX scripts observe events without taking damage/physics/authority ownership.

The skill requires compilation and observed rendering at gameplay scale, several phases of capture, relevant direction/contact/lifetime checks and iteration on visible defects. Use Play Mode when useful and restore previous editor state. Routine VFX work does not authorize automated tests, extra clients, Relay sessions or player builds.

[VfxIterationNotes.md](VfxIterationNotes.md) stores explicit preferences and demonstrated corrections. Every change request re-evaluates the skill; transferable lessons narrowly update its reference, while effect tuning stays in authored data. Effect-specific origin/validation records are created when effects exist, without fabricated acceptance or source-use rows during setup.

## Maintenance

- [VfxSourceCatalog.csv](VfxSourceCatalog.csv): paths, roles, GUIDs, containers, dimensions, shader/dependency hints, hashes and availability.
- [VfxPackageScan.json](VfxPackageScan.json): commit scope, archive counts, local ignored dependencies and guide coverage.
- [Build-VfxSourceCatalog.py](../AgentScripts/Build-VfxSourceCatalog.py): read-only library/archive scanner; bundled Python requires Pillow/pypdf. Extend explicit roots/commits when adding libraries.
- [Inspect-VfxReference.cs](../AgentScripts/Inspect-VfxReference.cs): native-prefab report through Pipeline, without instantiation or vendor script execution.
- [MechaAssetLabels.cs](../Assets/Editor/MechaAssetLabels.cs) and [ApplyAssetClassification.cs](../AgentScripts/ApplyAssetClassification.cs): native Keep/Replace/category labels preserve provenance and VFX roles; no per-file classification CSV is maintained.

Regenerate the catalog after library changes, regenerate classification, then apply labels through Unity APIs. Do not hand-edit serialized metadata. Preserve unrelated labels, GUIDs and dirty assets. See [classification](ProjectContentClassification.md).

## Initial validation boundary

This task creates the skill, catalog, classification and inspection tools. It does not create or replace a gameplay effect. The inspection helper compiled and reported six native Particle Systems, distinct cast/shockwave/end layers and Custom1/Custom2 stream requirements in PixPlays FireBlast without playing it. Source examples and image-based PDF pages were visually inspected. Runtime VFX quality and user acceptance remain unclaimed during setup.

Final native inspection confirmed **1,312 labeled assets/folders**, **734 Reference**, **578 VFXSource**, with no missing or conflicting expected labels. Saved metadata independently matches all 1,312 roles and current catalog hashes. Existing GUIDs and unrelated labels are preserved. While saving labels, Unity normalized older importer serialization in 458 metadata files (including current texture/model fields and generated CFXR shader-importer data); these are native Editor writes, not hand-edited import settings or vendor source changes.

Final diff review also caught automatic alpha-blend normalization in three loaded PixPlays materials. Targeted Unity serialized APIs restored those source properties, leaving no material changes. The skill and feedback record now capture this inspection caveat.

The skill passes the bundled static frontmatter validator; automatic invocation metadata and local reference links are valid. Both C# helpers compile through Pipeline, and the read-only reference/label reports execute successfully. Unity reports completed compilation with no compilation failure; its fresh console ground truth has zero errors/warnings. MainScene remains the sole loaded scene, clean, with Play Mode stopped. No automated tests, extra builds or network sessions were used. The refreshed classification snapshot has **9,943 files**, including the nine explicitly indexed local archives.
