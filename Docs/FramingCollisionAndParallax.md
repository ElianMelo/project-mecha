# Camera edges, height framing, enemy bodies and three-layer parallax

Implemented 2026-10-14 in MainScene. This pass follows the user's latest directions and supersedes earlier camera height steps and the single-city-image/pillar background. No models were created or changed.

## Camera

The floor collider and twelve visible floor faces cover X = −36 to +36. The previous confiner extended half a unit beyond each end. MechaArenaSetup now authors the exact floor extent into RoomCameraSettings.viewBounds, and MechaCameraSetup applies it to native Cinemachine Confiner 2D. The collider walls remain just outside the playable floor, with their inner faces on the floor ends. All wall renderers, including any children, are disabled. The camera lens cache continues to update during action zoom.

The low deck is treated as the second standing level, counting the floor as the first. It shares the floor's final camera height. RoomCameraSettings.groundedRiseByElevation is an editable native AnimationCurve rather than another set of discrete height thresholds. The current keys are (support elevation, camera rise): (0, 0), (1.9, 0), (3.66, 1.569), (5.51, 2.8614). Keys have flat tangents to ease between tiers.

| Support | Surface Y | Final camera Y | Feet in normal viewport |
|---|---:|---:|---:|
| Floor | −3.50 | −0.7120 | 10% |
| Low deck | −1.69 | −0.7120 | 35.97% |
| Upper deck | 0.16 | 0.8570 | 40% |
| Summit deck | 2.01 | 2.1494 | 48% |

The existing gentle airborne follow remains: 18% weight, 0.55-unit cap, 0.35 s smoothing. Contact starts the existing 0.96 s landing ease immediately. Jumping to a low deck can therefore make a small airborne camera movement, followed by a return to the same floor camera Y. The player rises within the frame as they climb, while the camera shifts only for the higher tiers. Room confinement remains authoritative at the edges.

## Enemy body collision

Enemy prefabs retain their existing enabled, non-trigger CapsuleCollider. Player layer 8 now collides with enemy layer 9. Enemy layer 9 still ignores itself so the existing 20–40% crowd spacing remains soft. Destructible crates still ignore player/enemy movement.

EnemyBodyCollider registers active enemy capsules. PlayerEnemyCollision is a separate component on MechaPlayer. It runs after movement's fixed update and uses actual IsDashing state, including the existing server-accepted protection window. It applies Physics.IgnoreCollision per player/enemy pair, rather than temporarily disabling the entire layer pair. New enemy bodies are picked up during a dash. Disabled/dead bodies are skipped.

PlayerBodyCollision.asset authors the maximum 0.15 s reentry grace if the dash ends inside a capsule and the native solver separation cap of 3 units/s. Clear pairs restore immediately. Overlapping pairs restore no later than the grace deadline, and the native solver separates any remaining penetration. OnDisable restores all held pairs. Damage immunity remains in the existing authoritative health/movement code. Ordinary dynamic enemies can be pushed by contacts; collisions prevent walking straight through their bodies.

## Background art and configuration

Three distinct 1536 × 1024 PNGs were generated using the built-in image generation tool and copied into Assets/_Project/Art/Environment. Their complete prompts and filenames are in [the art manifest](ThreeLayerParallaxPrompts.md). The two near layers retain their generated transparent alpha.

| Layer | Image | Relative scrolling X / Y | World image size |
|---|---|---|---|
| Far Sky | CyberpunkFarSky.png | 0.08 / 0.04 | 36 × 24 |
| Middle Skyline | CyberpunkMiddleSkyline.png | 0.22 / 0.10 | 30 × 20 |
| Near Rooftops | CyberpunkNearRooftops.png | 0.42 / 0.18 | 30 × 20 |

ParallaxScene.asset owns sprites, placement, size, tint, copy count and sorting order. Each layer has a separate ParallaxSettings asset. Three native SpriteRenderer copies per layer cover the room without revealing an image edge during traversal; the authored images are repeated, not claimed to be seamless textures. ParallaxLayer updates after Cinemachine, moving the artwork in world space by camera displacement × (1 − relative scrolling factor). It therefore scrolls more slowly than gameplay geometry. Backgrounds have no colliders and cast no shadows.

MechaBackgroundSetup imports the images with bilinear filtering, uncompressed texture data, original alpha and full-rectangle native Sprite assets. It replaces Distant City and Industrial Silhouettes, including the old cube pillars. Reapplying the setup reuses existing objects and assets, and removes stale layer/tile children if the profile changes. Platform and crate art/configuration remain separate. The old city image and sprite assets remain available on disk as historical art but are no longer in the scene.

## Process and validation

Used the official Unity CLI 1.0.0-beta.13 against the connected Unity 6000.3.11f1 Editor and Pipeline 0.8.0-exp.1. Edited scripts normally; applied prefab, scene, import, ScriptableObject and collision-matrix changes using Unity APIs through CLI. Saved MainScene and native assets before Play Mode. No automated tests were created or run.

Compilation completed without errors. During the final removal of unused old art fields, one explicit recompile request returned HTTP 400 while Unity's automatic refresh was processing changes; the subsequent recompile-status query reported completed, compilationFailed=false and no errors. The Editor remained connected. The console has existing FishNet obsolete-API warnings; the inspected pass had no runtime errors.

Entered Play Mode, enabled background ticking temporarily, verified frameCount advanced, and started a local FishNet host. Observed all eighteen encounter enemies with the new body component and all eleven scene crates spawned. Used passive CSV recording and manually issued position/input commands. Temporary health protection allowed camera inspection without repeated knockback. For the isolated collision observation, enemy AI was temporarily disabled and one enemy was held kinematic; these runtime changes were discarded on exiting Play Mode.

Observed:

- Left viewport edge −36.000008 and right edge +36.000008 at normal zoom, within floating-point precision of the floor ends.
- Skill zoom lens 3.813515 while the right viewport edge remained exactly +36. Dash zoom also held the left edge at approximately −36.000015.
- Holding left stopped the player root at X −35.73, one capsule radius inside the floor end. Dashing into that wall kept the same position.
- Landing and settling on the low deck returned camera Y to −0.7120001, identical to floor framing. Upper/summit settled at 0.8569999 / 2.1493998 and viewport feet 0.40 / 0.48.
- A real Space press from the floor jumped onto the low deck. The airborne camera rose gently from about −0.712 to −0.47, then eased back to −0.712 after contact.
- Walking into the isolated enemy at X −27 stopped the player at X −27.47. A Shift dash crossed to X −24.78. Passive records show ignored collision pairs during passage and restored pairs afterward.
- The three backgrounds moved at different rates during cross-room movement and covered both room ends. Transparent sky regions preserved alpha. Preview captures were visually inspected.
- Final scene: three parallax components, nine image renderers, no wall renderers enabled, one PlayerEnemyCollision and one EnemyBodyCollider on their respective prefabs, zero missing scene scripts. Player/enemy collision enabled; enemy/enemy collision ignored; crates still non-blocking.

Evidence is saved locally under Logs/FramingCollisionTrace.csv and Logs/FramingCollisionPass, including ScenePreview.png, LeftEdge.png, RightEdge.png, LowDeck.png and HighDeck.png. These are ignored inspection artifacts. The screenshot helper initially writes beneath Assets/Logs; captures were copied to the ignored Logs folder and removed from Assets through AssetDatabase.

Restored Edit Mode, unpaused, Time.timeScale=1, runInBackground=false and original quality index 1. MainScene is saved and clean. No remote-client or multiplayer latency validation was performed; the existing authority model was preserved and local host behavior was inspected. Different display aspect ratios and extreme crowd overlap still merit a later playability review.
