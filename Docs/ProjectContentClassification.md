# Unity asset labels

Unity asset labels are the classification record. Select an asset in the Project window and inspect its labels at the bottom of the Inspector; search with `l:Replace` or `l:Keep` in Unity Search. The former per-file classification CSV and generator were removed at the user's request on 2026-10-19.

| Label | Meaning |
| --- | --- |
| Replace | Unfinished artwork or an asset marked for replacement. |
| Keep | Retain for current development; not final-product or art approval. |
| Placeholder | Unfinished models, materials, textures, images, animation and other art. |
| Code | Project runtime source. |
| Prefabs | Project assemblies. |
| Authored Data | ScriptableObject tuning, definitions and native configuration. |
| Scenes | Unity scenes. |
| Others | Dependencies, tooling and supporting documentation. |

All current game content remains provisional. Origin is separate: preserve confirmed AI Generated labels alongside Placeholder/Replace; unknown authorship remains unknown. Never infer AI or human origin from file format or appearance.

VFX libraries retain independent Reference and VFXSource labels describing their source roles. These labels do not prohibit adapting complete effects into project-owned copies or variants. Source lineage remains in VfxEffectOrigins.csv and the VFX source catalog; these are not readiness inventories. Their historical use descriptions and evidence paths, including retired authoring documents, record earlier work rather than current reuse restrictions or documentation links. VFX authoring guidance lives in [VFX Creator](../.agents/skills/vfx-creator/SKILL.md).

Use **Tools → Mecha → Content → Refresh Project Labels** for newly authored project assets. The native editor utility preserves existing categories, provenance, library roles and explicit Keep/Replace decisions. It adds a default only when neither decision is present: Placeholder → Replace, other categories → Keep. Humans can change the decision directly in Unity. There is no import callback, runtime classifier, file relocation or GUID rewrite.

Humanoid Avatars and Animator setup are Authored Data/Keep; animation art remains Placeholder/Replace. New spark and sword assemblies are Prefabs/Keep, their artwork Placeholder/Replace, focused presentation source Code/Keep, and authoring notes/tooling Others/Keep. Labels describe retained function and replacement intent, not artistic acceptance.
