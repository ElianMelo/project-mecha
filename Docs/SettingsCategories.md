# Settings categories

Game ScriptableObjects live under `Assets/_Project/Settings/`. In Unity use **Tools → Mecha → Settings → Open Categories** to reach the folders. The project Input Action asset remains under Inputs, and native animation controllers/clips remain under Animations.

| Folder | What to tune |
| --- | --- |
| Guns | SecondarySMG: magazine, cadence, damage, recovery, targeting and aim feedback |
| Audio | GameAudio: clips, buses, volume defaults, pitch, overlap priorities, playlists and footstep tuning |
| Skills | MegaSlash, SwordFire, CriticalX and their shared skill presentation |
| Attacks | Basic/air/dash/rising attacks, magnetic recall, critical strike and PlayerCombat catalogue/global tuning |
| Combos | Ground/air sequences, selected PlayerCombos loadout and ComboConfigurator |
| Player | Movement, health, input, body collision and awareness |
| Enemies | Behavior, crowd spacing, lance throws, prediction, spawning, states, tactics and traversal |
| Interface | Chat, controller navigation, HUD layout, squad indicators, interface feel and VideoConfiguration (frame limits/defaults and display confirmation duration) |
| Network | Lobby and network simulation profiles |
| World | Arena layout, procedural levels, scenario, platforms and destructible props |
| Camera | Room framing and zoom |
| Animation | Player/enemy animation profiles |
| Effects | Particle bursts, combat effects and skill-fire effects |
| Backgrounds | Parallax layers and scene composition |
| Rendering | Render pipeline presentation and toon look |

Settings are references, not runtime copies to edit globally: components retain their asset references and keep runtime state separately. The folder migration used Unity's AssetDatabase, preserving asset GUIDs and serialized references. Names and authored values were retained except for the requested gun/tracking tuning.

`MechaSettingsSetup` owns the category mapping. `MechaPresentationSetup.Profile<T>` resolves the categorized asset, with an exact-name/type fallback for older layouts; `Create<T>` reuses it or creates it in the canonical category. Attack/skill authoring uses the same policy, so reapplying setup does not recreate the former flat structure or duplicate definitions. **Organize Categories** can migrate existing settings again. Add future profile types to the category policy when introducing them.

The user explicitly approved updating the asset-path literal in each of `LanceProjectileTests.cs` and `ProceduralLevelTests.cs` after the move. Only those two strings changed; no tests were run.
