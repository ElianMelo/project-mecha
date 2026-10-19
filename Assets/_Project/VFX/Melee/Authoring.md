# Equipped sword presentation

Original project presentation assembled with native Particle Systems, TrailRenderer, Light and Cinemachine. No finished vendor effect prefab/graph is a dependency. Sources, materials, profiles and focused scripts stay under this folder; equipped weapon prefabs stay under _Project/Prefabs/Weapons.

| Layer | Onset / life | Source / role |
| --- | --- | --- |
| Sword emission | Persistent | Original Blink Sword7 emission mask and red/blue HDR color, mapped to native toon emission properties/keyword |
| Blade illumination | Persistent plus 0.18s strike envelope | Native point light, range 2.6, idle 2.8 / light 6 / heavy 8 intensity |
| Blade path | Lead-in through active window | Native animated-tip TrailRenderer, supplied SpeedWind mask, 0.10/0.16s tails |
| Cut body | Impact lead / 0.17s | Copied isolated Gabriel Slash01 geometry, adapted into a half sweep through native Editor mesh APIs; supplied BrightSlash01 mask |
| Cutting rim | 0.012s delay / 0.105s | Narrower mask sampling and hotter material; separate value read |
| Displacement wake | 0 / 0.25s | Supplied DarkSlash01 mask, low-opacity charcoal, rear offset |
| Blade peel | Active window / 0.10-0.24s particles | Native world-space Particle System and supplied Point11 mask |
| Confirmed hit | Receiver damage event | Original project DamageSparks, brief light, existing audio and flash |
| Impact emphasis | Confirmed local hit | Native Cinemachine Bump and gamepad pulse, gated by optional motion |

SwordSwingSettings owns tuning; shared data is read-only during play. SwordSwingPresentation owns one reusable native cut instance per equipped sword, emission gates, light envelope and cleanup. The cut's local +X radius follows the sword's long +Y axis; its plane normal follows sword forward. It follows the actual blade during the active window and freezes for the remaining fade. The native world-space trail retains the real tip trajectory. Actor facing does not substitute for blade angle. Native trail/Particle System cover these jobs without a custom geometry runtime or shader.

Melee now uses native Humanoid Avatars and original source clips. Correcting a broken retarget by rotating weapons independently, baking wrist corrections or time-warping motion is retired. The old per-character melee bakes are removed. Both swords use the actual source attack FBX's weapon_r socket calibration under the right hand. Both guns use native left-hand sockets. Integrated extra weapon geometry is preserved until a split asset is available.

Native toon _Emissive_Tex, HDR _Emissive_Color and _EMISSIVE_SIMPLE retain the supplied glow. Main camera HDR/post-processing and the combat Volume Bloom are enabled (intensity 0.7, threshold 0.95); bloom and receiver illumination are separate contributions. Materials use ordinary world depth and the TransparentFX grouping layer, with no clear-depth/foreground overlay. A RenderTexture snapshot made from a copied camera without its URP additional data omits bloom; use the actual camera or final framebuffer for glow review.

Manual preview records advancing frames, standing Humanoid idle, equipped weapons, physical hits, blade presentation and light comparisons at the gameplay camera scale. Visual balance and the provisional rigid skin remain for user review. No automated tests were created or run. Source lineage is in Docs/VfxEffectOrigins.csv, and current behavior/limits in Docs/CombatFollowup.md.
