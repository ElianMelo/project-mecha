# Water direction from user feedback

Use these references for the rising-water family. New references and feedback still take priority. These are appearance references; their still images do not establish motion timing.

- [Eruption](../assets/water-references/01-eruption.png): separated foreground arcs, recessed body, bright foam caps, detached droplets, clear gaps and depth.
- [Rising column](../assets/water-references/02-rising-column.png): legible upward force, irregular pale foam around a colored body, foot contact and a distinct pool.
- [Foam openings](../assets/water-references/03-foam-openings.png): large transparent hollows interrupt broad foam regions; fine noise does not replace those openings.
- [Water shapes](../assets/water-references/04-water-shapes.png): sculptural curled lobes, thin white rims, thick shadow folds, hue changes through blue, cyan and turquoise/mint, and disconnected fragments.

Added 2026-10-28 (third pass):

- [Cel column with crack ring](../assets/water-references/05-cel-column-crack-ring.png): luminous cyan column with a streaked interior, bright contact flare and a broken ring of white crack-foam spikes on the floor.
- [Jellyfish canopy](../assets/water-references/06-jellyfish-canopy.png): dome canopy with a torn lower edge over long tapering tendril streaks; layered cyan/blue values on dark.
- [Cel surface flow](../assets/water-references/07-cel-surface-flow.png): flat-shaded blue regions, cyan highlights and thin white caustic lines; the target for interior flow textures.
- [Splash eruption](../assets/water-references/08-splash-eruption.png): crown of pointed tongues, detached round droplets and a foam pool; pointed rather than rounded tops.
- [Twisting vortex](../assets/water-references/09-twisting-vortex.png): twisted rising column with flung spray and expanding ripple rings.
- [Flat cel column and pool](../assets/water-references/10-flat-cel-column-pool.webp): flat-color column with dark elliptical blobs, a churning pale foam collar and a white foam network across the pool.
- [Pale column and ripples](../assets/water-references/11-pale-column-ripples.webp): translucent pale column with a spiky foam collar, concentric white ripple bands and cyan crack foam on a saturated blue pool.

Shared qualities: hard cel value regions (no gradients inside a region), white foam limited to rims, collars, cracks and ripple bands, a pool that belongs to the effect, and pointed or torn edges.

## User assessment of the first set

No candidate was selected or approved. The user liked motion in 01 and 03, spacing in 02, and the proposition of 08. Those positive observations do not approve their other qualities.

| Original candidate | Problem to address in the next attempt |
| --- | --- |
| 01 | Body and top merge; insufficient depth, holes and separation |
| 02 | Odd timing, position and spacing despite a better spacing proposition |
| 03 | Too little negative space and misplaced splashes |
| 04 | Too simple; no positive quality identified |
| 05 | Does not read as water |
| 06 | Square silhouette; missing pale splashes and hollows |
| 07 | Poor texture manipulation, timing and layering |
| 08 | Needs multiple streams and stronger water identity |
| 09 | Reconsider the entire texture, motion, timing and sequence |
| 10 | Too simple and insufficiently developed |

Across the set the user requested true transparent/low-opacity spaces, richer color composition with deliberate hue shifts, and more than one authoring pass. Do not reproduce the rejected dense two-color noise mask, uniform flat tops, solid cone blades or upright rotating rings as a default water recipe.

## Apply the feedback

Compose recessed folds, main water masses, curled crests and detached foam with separate spatial and motion roles. Preserve visible gaps at the actual viewing scale; overlapping additional layers must not fill every hole. White foam belongs on selected thin rims, breaks and crests rather than blanketing the entire body. Keep detached splashes near the force that creates them, then let native velocity/gravity carry them outward and down.

Choose a related hue/value progression rather than merely changing blue's brightness: for this family, indigo/ultramarine depths, ocean-blue body, turquoise/mint thin surfaces and restrained pale foam. Do not make that exact palette universal. Avoid using one repeated noise mask across all layers.

Review onset, peak, pulse response, breakup and disappearance as distinct phases. After the first native render, name a concrete mismatch, revise it, and capture again. Compilation and a main-phase image are not evidence that the motion or art is good. Preserve user feedback separately from agent observations and await the next assessment.

## Second-set rejection: construction, not another illustration pass

The user rejected the second attempt as bouncing PNGs rather than the requested VFX. All ten must be rebuilt with actual VFX techniques and real effect textures. Do not keep the painted whole-splash sprite atlas as their construction, and do not treat its good still appearance as progress toward this request.

For this water family, use native particle/mesh simulation, scrolling and displaced water materials, animated dissolve masks, simulated stream trails, independent foam and ballistic droplets. No candidate should depend on SpriteRenderer water lobes or the rejected illustration atlas. Keep the existing action, source position, catalog identities and live-S selection boundary.

Prove the motion at a steady main-action scale: water texture/contour flow, changing holes, separating crest foam, and individual droplets/streams traveling through space. An expanding mesh with a stationary texture can fail for the same reason as an expanding picture. Review the actual moving result.

## Current assessment — 2026-10-26

The user said the native result follows a good direction. Continue refining these actual mesh/material/particle effects from future feedback. This is approval of the direction, not a winner selection or final artistic acceptance. Transient review captures/previews were removed at the user's request; retain the playable family and permanent visual references.

## Third pass — 2026-10-28

The user kept the native direction and anime style but asked for better shape, motion, warmup, action, recovery, opacity and shape change, and layering, noting misplaced pieces and timing problems. They supplied seven more references (above) and *The Unity Shaders Bible* for technique.

All ten were rebuilt as new approaches to "water rising around the player". Construction changes:

- A project cel-water shader (`RisingWaterVariants/Shaders/AnimeWater.shader`) replaces the PixPlays two-color lerp: three tone bands, foam on erosion edges, torn tops, back-face shading, a Fresnel rim, flowing caustic/foam textures and vertex wobble. The gap is documented in the shader header.
- One shared timeline drives every piece: warmup (gathering pool, inward rings and streaks, pre-bulge), action (overshoot snap onto impact, beats on later pulses, pulse-masked crests, spray and ripples), recovery (erosion from the top, sinking and widening, drips, splash-down, final ripple) and interruption through the Animator.
- Layer roles: pool, deep shell, body, foam/spray, ordered with sort fudges.
- Spray originates at the crest or rim that creates it.

Agent observations, not user approval: the ten read as distinct constructions across phases; torn tops read as pointed tongues after vertically stretched tear sampling; recovery opens large foam-edged holes. The pool's deep patches look dark against the grey test floor during recovery. No candidate is selected; awaiting user evaluation.
