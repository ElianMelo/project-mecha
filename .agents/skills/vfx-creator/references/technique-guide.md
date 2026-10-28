# Technique guide: phases, layers and cel shading

How to build anime VFX that move with intent. Distilled from *The Unity Shaders Bible* (Jettelly) and from the third rising-water pass. The book teaches shader fundamentals, not VFX art; the sections cited are the ones that map onto effect work.

## 1. Plan the phases before any artwork

Every effect gets an explicit timeline in seconds, locked to the gameplay clock (impact, pulses, action end, tail). Write the keys as data, then derive every curve from them.

| Phase | Purpose | Typical tools |
| --- | --- | --- |
| Warmup / anticipation | Tell the player where and when the action lands. Gather inward, darken or thin out, swell slightly. Ends exactly on the impact frame. | Shrinking rings, inward streaks (negative radial velocity), sparse-to-solid erosion, a small pre-bulge |
| Action | Snap to the main silhouette on impact with overshoot (fast in, ~0.08-0.1 s, peak 110-120 %, settle by +0.12 s). Hold with living interior motion. React to every pulse. | Size curves with overshoot, pulse "beats" (+5-8 % then settle), pulse-only emitters (crests, spray, ripples) |
| Recovery | Break the form apart with intent: erosion opens holes from the top with foam edges, bodies sink and widen, droplets fall, a last ripple spreads. Never just fade alpha. | Custom-data erosion curve, height falls / width grows, gravity drips, splash-down burst |
| Interruption | A shorter recovery triggered by an early End. | Animator state driving a material recovery property |

- Timing comes from the action data, not guesses. The rising-water family uses impact 0.28 s, pulses every 0.22 s, action 1.25 s, tail 0.35 s.
- Shape change carries the motion; opacity is secondary. Change silhouette (height, width, tilt, spin, tear depth, erosion) per phase.
- Anime timing is uneven: fast attacks, holds, then a sudden break. Avoid linear ramps.
- Pulse-locked pieces belong to a mask (bit n = pulse n) so effects can sequence: one pillar per pulse, one tier per pulse, a rising band per pulse.

## 2. Layer front to back with distinct roles

Stack 3-4 roles, each with its own material settings and sort priority:

1. **Ground / pool**: flat disc and ripple rings under everything (draws first).
2. **Deep layer**: a slightly larger, darker, lower-opacity shell behind the body; it gives depth and makes transparency read.
3. **Body**: the main mass with caustic blobs and flowing foam lines.
4. **Foam / crest / spray**: white, mostly opaque, drawn last.

Control draw order with Particle System `sortingFudge` (lower draws on top) or the material render queue (Bible 3.1.5: Transparent is 2700-3599). Transparent pieces use `ZWrite Off` (Bible 3.2.2) to avoid fighting each other. Open sheets need `Cull Off` (3.2.1).

## 3. Cel shading recipe (Bible chapter 4)

- **Hard tone bands with `step`/`smoothstep` (4.1.5).** Build a tone value (vertical gradient + caustic texture + Fresnel), then threshold it into deep / body / light. Use a tiny smoothstep width for anti-aliased cel edges. This replaces the rejected two-color noise lerp.
- **Foam on the erosion edge.** Two thresholds on the same field give a band: `alive = smoothstep(cut)`, `edge = alive - smoothstep(cut + width)`. Animate `cut` from particle custom data, so holes open with white rims.
- **Torn tops instead of flat rims.** Subtract a low-frequency, vertically stretched noise from `1 - uv.y`; tall cells make pointed tongues, round cells make scallops. Put a foam band along the same threshold.
- **Back faces (3.2.1 `SV_IsFrontFace`).** Shade inner faces toward the deep color so open sheets read as volume.
- **Fresnel rim (7.0.6)** `pow(1 - saturate(dot(n, v)), p)` lifts silhouettes into the light band. Keep its weight low on sheets or they turn white.
- **UV scroll over `_Time` (4.2.0)** with distortion from a second texture: water must flow inside the shape while the shape holds still.
- **Vertex wobble (`sin`, 4.1.1)** along the normal gives a living surface without new geometry.
- **Blending (3.1.7).** Water bodies use alpha blend (`SrcAlpha OneMinusSrcAlpha`). Reserve additive for glints; additive water turns into glow and loses the cel look.

## 4. Texture frequency is a design choice

- Check how many features a mask holds per tile before choosing tiling. `Voronoi01` holds about 15 cells per tile; at tiling ~1 it erodes into speckle. Tiling ~0.3 gives a few large holes.
- Match frequency to the role: large erosion cells, medium caustic blobs, foam lines only in the lit band, the lowest frequency for tears.
- Too much white is the most common failure. Foam belongs on rims, crests, edges and a few lines, not across the body.

## 5. Particle contract for this approach

- One authored mesh surface per mesh particle (`renderMode = Mesh`, `alignment = Local`, `startSize3D`), shaped by separate-axis size curves and rotation-over-lifetime (radians/s, all axes in Curve mode).
- Custom vertex streams: Position, Normal, Color, UV, Custom1.xy (TEXCOORD0.zw). Custom1.x = erosion over lifetime, Custom1.y = random seed for texture offsets.
- Author meshes at edit time as project assets with clean UVs (u around, v up, bottom at y = 0) so shaders can rely on `uv.y` for height. No runtime mesh generation.
- Billboard droplets use the same shader with a radial shape field; erosion shrinks them instead of fading them.
