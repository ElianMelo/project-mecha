---
name: unity-anime-vfx
description: Create or visually refine project-mecha VFX with a dark 1980s anime mecha aesthetic using Unity Particle System and local Gabriel source ingredients. Use for effect composition, timing, materials, motion and visual diagnosis; exclude unrelated gameplay logic, tooling and skill management.
---

# 80s anime VFX

Create original effects that communicate mechanical force through bold shapes, decisive motion and controlled light. Follow [project rules](../../../AGENTS.md); this skill supplies art direction, not permission to change combat, cameras or global rendering.

## Establish the effect

1. Inspect the actual gameplay trigger, camera scale and backdrop. For explicit fresh rebuilds, exclude the rejected effect's implementation, assets, tuning, captures and notes; inspect only shared gameplay interfaces and independent permitted sources. Otherwise inspect the relevant implementation. Identify the physical source, target/contact frame, intended footprint, attachment and stop/release behavior. Preserve accepted parts when refining an effect.
2. Read [visual direction](references/visual-direction.md) and relevant supplied references. Consult [game feel guidance](../../../Docs/GameFeelPrinciples.md) when changing response or feedback. Current project rules and user instructions take precedence.
3. Inspect available ingredients under `Assets/GabrielAguiarProductions` and their materials, shaders, meshes, maps and dependencies. Verify actual local availability and render-pipeline compatibility; old catalogs may be stale. Finished effects labeled `Reference` are for study only. Assemble from reusable `VFXSource` ingredients, never a finished vendor prefab, nested effect, graph or recolored variant. Source all new/revised visual ingredients from the local Gabriel library; report a missing ingredient rather than substituting another library or generated artwork. Existing unrelated effects are outside the revision.
4. Describe a compact visual breakdown: physical cause, dominant silhouette, supporting layers, motion, light and relevant phases. Choose timing in seconds for this action; anticipation, release, impact and aftermath are useful concepts, not mandatory layers or a fixed sequence.

## Author with native systems

- Start with Unity Particle System modules and compatible existing source materials. Record the concrete missing capability before adding focused ShaderLab/HLSL. A compatible source Shader Graph material is reusable; author a new graph only when simpler installed options do not meet the need. Use VFX Graph only for a demonstrated GPU simulation need.
- Use the official Unity CLI MCP and Pipeline workflow through [unity-cli](../unity-cli/SKILL.md) or [unity-pipeline](../unity-pipeline/SKILL.md). Discover live commands; modify scenes, prefabs, materials and import settings through Unity APIs. Do not hand-edit Unity serialization.
- Keep effect-owned assets, profiles and focused interaction scripts under `Assets/_Project/VFX/<Effect>/`. Store authored setup/tuning in ScriptableObjects; components hold references and runtime state. Never mutate shared configuration during gameplay. Preserve native labels and provenance under [content classification](../../../Docs/ProjectContentClassification.md).
- Let gameplay own collision, damage and network authority. Cosmetics consume source/contact positions, normals and travel direction without inventing hits. Keep emission source, travel and impact frames explicit, preserving attachment, native world depth and relevant surface interactions.
- Use native lights/Animator envelopes where illumination is needed. Keep material emission, bloom and receiver illumination independently inspectable. Camera changes, hit stop and global exposure/bloom changes require scope from the effect request; they are not automatic styling steps.

## Inspect and deliver

Check compilation and shader diagnostics when applicable, then inspect rendered output at gameplay scale and representative phases. In Play Mode verify that frame/time values advance before claiming runtime evidence. Inspect applicable facing, movement, contact, interruption, release and tail cleanup; sustained effects must stop correctly. Restore the prior editor/Play state and remove temporary preview objects.

Use proportional local inspection. Do not create or run automated tests, extra players, builds or network sessions for ordinary visual authoring. Report sources, changes, observations and unresolved limits; distinguish technical validation from user visual acceptance. A compiled prefab or screenshot alone does not establish approval.

Keep effect tuning local. Do not automatically expand this skill after every iteration or turn one effect's correction into a universal rule.
