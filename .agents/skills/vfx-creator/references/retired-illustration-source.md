# Retired illustration source record

The user rejected this whole-illustration construction. Its unused project image/sprite assets and transient review captures were removed on 2026-10-26. This document preserves provenance and prompts only; it is not current VFX authoring guidance.

# Second water attempt: source and image prompt record

The generated texture is Assets/_Project/VFX/RisingWaterVariants/Textures/water-lobes-v2.png. It was generated with the built-in image tool, then refined with the same tool. It retains genuine transparency. Native Unity APIs import it and create eight sprite assets. The final sprite sources carry AI Generated provenance labels. No vendor artwork or screenshot was overwritten.

Prompt 1: Create one transparent 4-column by 2-row atlas with eight isolated rising-water components. Side-view cel/anime curved water sheets, large transparent hollows, indigo/ultramarine folded depths, blue bodies, turquoise/mint thin surfaces, selective pale white foam rims, detached droplets. Components: inward curl, branching jet, broad hollow sheet, capped sinuous stream, hollow crown, diagonal surge, pierced twisting curtain, detached foam shards. No labels, grid, scenery, ground, opaque backdrop or baked glow. Clear per-cell margins; readable broad regions rather than noise, rectangular columns, spikes, fire or energy bolts.

Refinement prompt: Preserve the 4x2 arrangement and silhouettes. Reduce fine detail/noise by 90 percent; use broad clean anime cel regions, deep ultramarine shadows, blue bodies, turquoise/mint light, selective white foam, a few curving caustic lines, and large transparent holes. Remove grain, sparkles, scattered tiny bubbles/strokes and realistic foam. Keep every part inside its cell with transparent margins and a bottom-centered root.

Installed source: Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Liquids/CFXR Water Splash.prefab. Native Big Ripples and Foam systems are copied into project-owned candidate prefabs; vendor assets remain unchanged. Ripple contact, zero-rate pulse spray, short ballistic lifetimes, size/fade modules and materials come from those compatible sources. The project action and testing stage remain unchanged.

## Runtime review

The ten browser entries were individually replayed with advancing frames. Four observed pulse times per candidate are in review-observations.txt. Across this review the callbacks occurred at 0.280-0.287, 0.500-0.503, 0.720-0.723 and 0.941-0.943 seconds, respectively; authored targets remain 0.28/0.50/0.72/0.94.

Native row navigation reached 01 through 10 in order, scrolling the final row into view. Two Space presses increased playback by exactly two and left one current effect. An early End removed the water presentation after its short interruption tail. Stop left zero water objects and zero wrappers, with browser focus retained. Compilation completed without errors and all ten profiles resolved compatible shaders and the same original action. The original live-S/Zero asset hashes remained unchanged.

The draft review corrected a creation-frame animation/pulse offset, animated transform bindings that lost other axes, and pulse systems that needed their simulation running before Emit. The previous dense-noise mask construction was replaced. These observations document the second attempt; they do not claim artistic acceptance.

## Final image-tool refinement prompt, verbatim

Refine this game VFX sprite atlas into clean anime CEL water. Preserve exact 4 columns x2 rows and eight different silhouettes, but re-render with clean broad illustrated color regions: deep ultramarine shadow shapes, blue water body, broad turquoise/mint lit areas and thin pale-white foam. Reduce fine noise/detail by 90 percent. Remove sparkles, grain, tiny strokes, bubbles scattered everywhere and realistic foam. Each sprite needs only about 8-12 large elegant interior regions, 2-4 curved bright caustic lines and 3-6 foam breaks at tips. Preserve 2 or3 LARGE TRANSPARENT HOLES in each water body and empty space under curls. Simple rich anime water as a high quality action-game VFX texture, silhouette readable at small scale, NOT ornate concept illustration. Every sprite fully contained with clear transparent margin on all four sides WITHIN its own cell; separate the cells with clear space, no adjacent sprite may cross boundary. Smooth crisp antialiased shapes on truly transparent background, no color haze, no dark rectangle. No text. Keep sprite roots bottom-centered, vertical upward energy, curved tops. This is a reusable texture atlas and not a scene render.

