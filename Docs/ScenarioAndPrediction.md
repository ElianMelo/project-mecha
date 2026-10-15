# Scenario, height framing and Prediction enemies — 2026-10-14

This pass follows the user's clarified height rule and expanded scenario request. It retains the 18-enemy finite encounter, gentle airborne follow, 0.96-second landing ease, action zoom and PC anti-aliasing.

## Camera and HUD

RoomCameraSettings now selects the normal-view feet position from supporting surface elevation above the floor (-3.5): below 0.5 units uses 10%, from 0.5 to below 3.4 uses 30%, and 3.4 or higher uses 40%. Thus Lower Deck is mid framing and Upper/Summit Decks use high framing. Higher tiers show more of the descent space below; the low floor framing leaves room to see upward traversal. Cinemachine's room limits remain authoritative.

PlayerMovement reports upward-facing physical collision contact immediately, alongside its ground probe. RoomCameraRig starts easing from the currently rendered height as soon as support changes; it does not wait for the body to stop settling. The target comes from the collider top, avoiding repeated target changes from small foot-position errors.

The existing uGUI HUD retains its health/mana and live cooldown references. GameplayHudLayoutSettings places three 144×52 skill cards below HP/MP at (24,-100), spacing 8 and font size 13. A native HorizontalLayoutGroup positions the cards. Tutorial text is three compact lines at top-right, font size 12. Its text, dimensions, offsets and card text padding are profile data. Passive panels do not intercept pointer events. Canvas scaling remains the existing 1280×720 reference.

## Scenery and destructibles

Generated two original textures using the built-in image-generation tool: a cyberpunk city panorama and an industrial platform atlas. See [complete prompts and asset manifest](ScenarioArtPrompts.md). Native Sprite assets select separate floor and floating-platform regions; source PNGs were not cropped or repainted by scripts. Texture imports use bilinear filtering, uncompressed alpha and no mipmaps. Floor faces repeat in six-unit sections; floating faces fit existing collider widths. Colliders and jump-through behavior remain independent from the art, and platform visuals cast no shadows.

The far city is a 36×24 world-unit backdrop. CityParallax uses movement factors (0.12,0.06), while IndustrialParallax uses (0.42,0.18). ParallaxLayer updates after Cinemachine; nearer industrial silhouettes move at a different rate. ScenarioSettings owns art references, dimensions, colors, placements and scenery geometry parameters. Native primitives are used for simple industrial silhouettes and supply-crate construction.

Eleven supply crates are placed at authored floor and platform positions, with their centers behind the actor plane. DestructibleBoxes configures 28 HP and existing Particle System debris/smoke feedback. DestructibleProp implements ICombatTarget with server-owned network health. Damage changes visibility and disables the hitbox on destruction; the scene NetworkObject remains registered for synchronized state. Player attack queries include the Destructible layer, whose movement collisions with player/enemy layers are disabled and saved to native Physics settings. Boxes cannot obstruct platform navigation or actor movement.

The first Play Mode pass exposed FishNet's throttled automatic scene-ID generation after creating many objects in one editor operation: ten crates were uninitialized. The repeatable setup now runs FishNet's native scene-ID and editor serialization methods after building the objects. The subsequent run spawned all eleven correctly. No FishNet source was modified.

## Prediction state

EnemyPredictionSettings owns chance, timing and interception costs. Alert enemies outside immediate melee range periodically consider Prediction (45% chance, decisions every 1.5–3.5 seconds). The planner leads player X by 0.6 seconds, capped at three units, then searches other reachable platforms within eight horizontal units and a 0.7–4 unit vertical difference. Existing platform routing determines reachability; the current player support is excluded as the interception goal.

PredictionState routes to the selected position, then waits for 1.8–3.2 seconds after arrival. The route has a seven-second timeout. Close combat proximity or the player reaching the waiting platform returns to Alert, which uses the existing attack/windup behavior. Damage forces Alert. Loss of awareness retains the existing state timing rather than creating instant distant pursuit. While vertically separated, an interceptor may remain in Prediction inside the hard radius; this allows waiting on another platform instead of immediately abandoning the plan. Per-enemy runtime state is separate from shared configuration.

## Manual validation

Compiled through the official Unity CLI/local Pipeline connection; CLI 1.0.0-beta.13 was current. Used Unity APIs to import art, create configuration/Sprite assets, edit the existing HUD, save the enemy prefab, serialize scene objects and save MainScene. No automated tests were created or run.

The corrected local-host session confirmed its frame counter advanced and reported zero console errors and warnings. Temporary invulnerability, slow motion, repositioning and manual keyboard input were used for observation. Passive telemetry supplied no inputs or assertions.

- Settled floor feet viewport: 0.100000, camera Y=-0.712000. Lower Deck 01: 0.300000, Y=-0.296000. Upper Deck 01: 0.400000, Y=0.857000.
- On the first observed Lower Deck support sample, camera Y had already changed from -0.712000 to -0.705590 while the player root was still settling at -1.688350. On Upper Deck contact it changed from -0.296000 to -0.295966 and continued easing in the next sample. No settle timer is involved.
- All eleven crates reported network-spawned, with health 28. A normal Mega Slash on Supply Crate 01 reduced health to zero, set IsDead=true and disabled its collider/renderers. No errors occurred.
- Manual rightward movement crossed background crate locations. After release the player stopped at x≈19.970, velocity X=0 and RightArrow released. The player/prop collision pair remained ignored.
- During traversal, the camera reached x≈1.825 while the city layer reached x≈-1.634, consistent with its slower relative motion. Background coverage and the HUD were inspected in captured Game Views.
- Passive observations recorded up to two simultaneous Prediction enemies, including two waiting on their selected surfaces. A manually chosen waiting interceptor was in Prediction at time 183.66069. Moving the player near it on Upper Deck 01 changed it to Alert; by 183.915619 IsAttacking was true.

Stopped Play Mode and restored unpaused Edit Mode, time scale 1, background runtime false and Editor quality level 1. Temporary gameplay state was discarded. Captures and CSV traces are in ignored `Logs/ScenarioPass` and `Logs/ScenarioPassTrace.csv`, outside shipped assets. Final scene inspection checks missing scripts and clean scene state. Validation covers the local host and current flat arena; separate remote-client/late-join behavior and performance were not benchmarked.
