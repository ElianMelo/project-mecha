# A / S / D fire — accepted initial baseline (revision 7)

The latest review corrects preparation to the actual VFX source, asks for a longer D fire fade, and supplies an anime mecha reference for a blend between the earlier graphic treatment and revision 6 texture depth. This pass keeps the existing topology, coverage, cost and damage. The user accepted fire revision 7 as a starting point; content remains provisional rather than final production art.

## Phase and layer breakdown

| Phase / layer | Onset / lifetime | Source and native behavior |
| --- | --- | --- |
| Source preparation | Action start to first fire/contact .28 A/D, .30 S | SourceHeatPreparation follows the shared live source; local ignition and distortion particles, no terrain query. S follows the right hand; A/D use the first emitter offsets. |
| Main fire | .28 A/D, .30 S | FireCells verified 2x2 body/hot pockets; randomized starting cells, HDR heat/color gradient, native noise/rotation/speed curves |
| Outer volume | Alongside each main stroke; D now lingers up to 1.18 s | VolumeFlame uses graded BillowMask plus flowing/heat detail; no C-shaped atlas or 3x3 flipbook in active dependencies |
| Early soot wake | Native fire-particle Birth event / .70 | SootMask; world-space subemitter, probability .40, .16 inherited velocity, neutral charcoal tint |
| Cooling smoke | Native body/outer Death events / .90 / .85 | BillowMask; probabilities .75/.55, slight transparency, restrained growth/drift/buoyancy, rear camera depth and queue 2980 |
| Contact response | Confirmed damage contact | Independently oriented hit prefab, native fire-born soot/death smoke following its local splash path; incoming sparks remain separate |

Warmup is triggered by BeginAction through FireSkillPresentation, follows CombatEmissionOrigin in LateUpdate and ends at first ignition or interruption. FireSourceWarmup replaces FireTerrainWarmup, using native local-space particles at the same source as the main fire rather than a downward ground query. A offset .4, D offset .14 place the charge at their first emission points; S uses its animated hand. Scale is .85 A/D, .70 S. Script, preparation prefab and ignition material were renamed through AssetDatabase with GUIDs preserved. Main onset stays .28/.30 seconds.

D body/core/outer lifetimes now last 1.05/.78/1.18 seconds (previously .56/.42/.64). Native speed curves preserve the original travel interval, then stop rather than allowing longer life to increase reach. Alpha holds longer and cools gradually. Both arms keep the .22-second sequence. A/S fire duration and all damage windows remain. D root cleanup is 2.8 seconds to accommodate its later death-smoke tail; other roots remain 2.2.

Smoke is no longer a delayed static box at the root. Native parent particle birth/death events place it on the fire's actual path, including both sequential cross arms and all S waves. Each child stays in world space and initially inherits .16 of parent motion, with modest later rise. Rear shape offset .72 and queue 2980 keep it behind primary fire while native opaque depth testing preserves scenery occlusion. All systems are cleared/configured before playing only independent systems; event-owned smoke is not manually started. Parent delays are not duplicated on smoke children. Burst cleanup is 2.2 seconds for A/S, 2.8 for D; independent contact remains 1.65 seconds.

## Material and source contract

The user-supplied Build Burning Gundam image guides broad amber/orange flame masses, yellow-gold inner tongues and pale-hot highlights. CellThermal blends .65 graphic shade/heat bands with the existing finer detail, reduces fire grain and increases gold/amber separation. Smoke retains a zero cel blend and softer density. The original image is documentation only; no image pixels, logo or model geometry enter gameplay.

CellThermal remains a focused URP unlit alpha-blended HLSL shader: ZWrite Off, ZTest LEqual, Cull Off. DensityBoost/OpacityPower preserve a dense textured interior and transparent irregular perimeter rather than flattening the source into an opaque cutout. Independent heat/noise sampling adds shaded pockets, hot gradients and per-particle detail offsets. This is texture shading, not physical volumetric self-shadowing. Native light casting and scene bloom remain enabled from revision 5.

Native vertex streams are Position, Color, UV, StableRandomX: UV occupies TEXCOORD0.xy and stable random TEXCOORD0.z. The shader reads that layout explicitly. Atlas starts vary within the verified 2x2 FireCells sheet; single graded volume maps do not use sheet animation. Copied maps remain linear/uncompressed; noise repeats and masks clamp. Source package files/importers remain unchanged. CurlCells, TornCrescent and related old masks/materials are retained as unused historical assets, absent from current effect prefab dependencies. No finished vendor VFX is nested or instantiated. See Docs/VfxEffectOrigins.csv for lineage.

## Validation and limits

Native authoring and both shaders compile with no diagnostics/errors. Read-only native inspection confirms 8/8/14 Particle Systems for A/S/D, with 3/3/6 native smoke subemitters; contact has 7 systems/3 subemitters. All wake children are world-space, delay zero, queue 2980, inherited velocity .16. Active dependencies contain no retired curl/crescent or vendor package assets. The skill passes its static frontmatter validation.

One manual preview per skill sampled .12/.24/.42/.70/1.05/1.40/1.80/2.40/3.60 seconds, advancing 489/555/519 frames. Source preparation is visible at .12/.24 before any fire/contact and gone at first strike. S preparation matches hand X/Y; A/D match authored emission origins. D retains 20 fire particles at 1.05 and 9 at 1.40, with visible luminous fade in the captured scene; fire has ended by 1.80 and smoke then fades. All roots are gone by 3.60. Near hits remain A/S/D 4/3/2 and 84/84/112 damage; far A 1/16, S3/84, D2/112, above/behind untouched. The preview uses an anchored kinematic body, so existing movement calls produce kinematic-velocity warnings; locomotion is not validated here.

No additional art tuning followed the preview. Shader/native authoring and static checks pass. Changed warmup attachment is observed in the manual frame sequence; opposite-facing, airborne and cover cases were source-reviewed only. No automated tests, extra builds/clients or network sessions were used. The temporary preview was removed by leaving Play Mode; MainScene remains clean. Play Mode is restored to its initial state after authoring.

Generated capture images/review sheets were removed after initial-baseline acceptance at the user's request. Runtime JSON reports remain under ignored Logs/FireSkills. User art reference: Docs/References/FireSkills/Feedback07-user-art-direction.png. Historical image paths are intentionally retired.
