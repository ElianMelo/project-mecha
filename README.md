# project-mecha

Grand Chase / Elesis-inspired movement and combat prototype.

Open `Assets/Scenes/MainScene.unity`, enter Play Mode and press **1** to start the local host. Use **Left/Right** to move, **Up/Space** to jump, **Down** to descend or fast-fall, **Z** to attack, and **double-tap a direction or Shift** to dash. Skills: **A Mega Slash**, **S Sword Fire**, **D Critical X**.

Player inputs, movement, attacks, health and effects remain separate components on `MechaPlayer`. The arena has one-way platforms; enemies have server-controlled damage and hit reactions. Each encounter spawns eighteen enemies and finishes. Player-centered ranges select calm wandering, suspicious investigation or alert pursuit. Prediction enemies can choose a different reachable platform and wait to intercept the player. The existing Relay flow remains available.

- [Gameplay research, controls and tuning](Docs/GrandChase/GameplayResearch.md)
- [Implementation process and runtime validation](Docs/GrandChase/ProcessAndValidation.md)

The setup can be reapplied from **Tools → Mecha → Apply Mecha Gameplay** while MainScene is open in Edit Mode. Hold **Down** to descend through platforms; press **Up during a Z combo** for a rising slash.

The supplied mecha is now the player; the former player rig is reused for reactive enemies. Toon materials use hard shade bands and dark outlines. Combat effects use Unity Particle Systems. Cinemachine strengthens horizontal follow near screen edges; camera Y follows airborne movement slightly and eases into its landing adjustment over 0.96 seconds. Skills zoom out slightly; dashes use a smaller, gentler zoom, and both return smoothly. The PC renderer uses 4× MSAA plus high-quality SMAA. Invisible walls and camera bounds are separate. All touched system tuning uses ScriptableObjects under `Assets/_Project/Settings`.

The camera stops with its screen edge at the floor tile boundary. Floor and low platforms share the same camera Y; higher platforms progressively frame the feet higher, at about 40% and 48%. Physical support contact starts the easing immediately. Enemy capsules block ordinary movement, while dashes pass through them. Compact skills sit below HP/MP, with smaller controls text at the top right. The arena has three generated cyberpunk image layers moving at separate parallax speeds, separate floor/floating-platform artwork and eleven non-blocking destructible supply crates.

- [Character, toon, camera, effects and validation](Docs/PresentationAndCamera.md)
- [Architecture decisions for future features](Docs/Architecture.md)
- [Official Unity MCP setup and migration](Docs/UnityMcpSetup.md)
- [Closer camera, dash escape, enemy traversal and larger arena](Docs/GameplayRefinements.md)
- [Finite encounters, enemy states and landing camera](Docs/EncounterStatesAndCamera.md)
- [Faster landing camera, reference research and validation](Docs/LandingCameraRefinement.md)
- [Current smooth landing, action zoom and encounter density](Docs/CameraMotionAndEncounterDensity.md)
- [Latest airborne follow, longer landing ease and anti-aliasing](Docs/SoftCameraAndRendering.md)
- [Current height framing, HUD, cyberpunk scenario and Prediction enemies](Docs/ScenarioAndPrediction.md)
- [Generated art assets and complete prompts](Docs/ScenarioArtPrompts.md)
- [Current camera edges, height curve, enemy collisions and parallax](Docs/FramingCollisionAndParallax.md)
- [Three new background images and full generation prompts](Docs/ThreeLayerParallaxPrompts.md)

- [Further research and gameplay refinements](Docs/GrandChase/Enhancements.md)

Prototype tuning is intentionally simplified; the research documents record differences from the reference game.
