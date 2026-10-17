# Game audio — 2026-10-17

The subsequent [controller/audio/portal follow-up](ControllerAudioAndPortalRefinements.md) supersedes the initial 65-clip/41-cue snapshot and direct-to-audio Esc flow below. Current content uses 71 clips, 44 cues, main options with Audio/Video/Lobby, persistent controller highlights, stronger final skill impacts and a separate energetic gameplay playlist. The original implementation record is retained here.

Audio uses the supplied `18e2dab` libraries. The selected 65 clips are organized under `Assets/_Project/Audio/`; `AudioClipOrigins.csv` records each original package path and current path. Native AssetDatabase moves preserve GUIDs and upstream references. Original creation method and licensing are unverified; imported sound artwork remains provisional Placeholder. Unselected package files remain available in their original libraries.

| Library | Current use |
| --- | --- |
| UI - Mechanical | Navigation, confirm, cancel, open/close, error and notifications |
| AlpineAudio Sci-Fi ForceField | Magnetic pull, MegaSlash, CriticalX, repeated skill impacts, player hit and portal effects |
| Gamemaster Audio - Pro Sound Collection | Melee/combo/finishers, SwordFire, SMG/impacts, lance throws, footsteps, jump/landing/dash, crate impacts/break, healing, death/revive and portal travel |
| 8 Sci fi Tracks | Three looping music selections |
| Sci Fi Ambiances | Two looping environment beds |

## Configuration and mixer

`Settings/Audio/GameAudio.asset` owns all 41 cue definitions, clip alternatives, gain, pitch range, spatial blend, priorities, per-cue concurrency, distance, footstep cadence, music/ambient playlists and voice/crossfade tuning. Runtime components never mutate the profile. The native mixer, its groups and exposed volume controls were created in Unity by the user. The setup tool matches the already exposed volume parameters to their native groups and names them consistently using SerializedObject; it does not create mixer structure with reflection or handwritten YAML.

`MechaAudio.mixer` routes Master → UI, SFX, Ambient, Music. Footsteps, combat and destruction belong to SFX. The native exposed controls are `MasterVolume`, `UIVolume`, `SFXVolume`, `AmbientVolume`, `MusicVolume`. Slider linear gain converts to dB; 0% uses -80 dB. An authoring fallback applies source gains if a mixer or exposed control is missing, with one startup warning. Once connected, the mixer owns all five volumes.

Esc / 3 / controller Menu opens the Canvas audio configuration overlay. Keyboard/controller navigation uses native Selectable/Slider controls; left/right adjusts, up/down changes row, and controller Back closes. The menu has defaults, lobby/squad and close buttons. Mixer gain updates immediately, values persist through PlayerPrefs, and closing the menu flushes them. A short existing resume-input grace prevents button leakage. The overlay blocks local input and keeps the multiplayer clock running. Lobby selection and chat entry respect the audio modal. Existing typing/keyboard cancellation retains priority.

## Admission and overlap

Unity AudioSources own playback, rolloff and pitch; AudioMixer owns routing and gain. Unity priority alone does not express per-cue limits, newest-first replacement or non-stealable groups, so GameAudio adds only that game-specific admission policy.

- Forty-eight active transient slots plus eight short release tails. Per-cue caps prevent one sound type filling the mix. Footsteps have the lowest priority, then minor bullet impacts/multi-hit feedback, ordinary combat, player feedback and UI. Lower numerical values mean greater importance. Equally important new events replace the oldest eligible voice. An outgoing voice normally fades over 35 ms using another source. Saturated same-frame bursts can reuse a lower-priority already outgoing tail so new important events are retained. Lower-priority sounds cannot evict a more important live transient.
- Music and ambient beds use independent source pairs at priority 0 with 1.5-second equal-power crossfades on room changes. A second rapid room change waits for the outgoing bed rather than cutting it.
- Main skills, magnetic pull, player death/revive, portal opening/travel and portal hum use separate protected sources. These are never selected as transient victims; the pool can grow to retain concurrent rare events. Only intentional portal deactivation/destruction fades its loop out. Skill pulses remain ordinary bounded transient sounds beneath the complete protected skill layer.
- Frequent cues randomize pitch within authored limits and avoid immediate variant repeats. Repeated UI events are rate-limited; actor-specific intervals reject duplicate same-actor events. Distant transient events are culled before spending voices. Portal opening/travel are non-positional, so room travel does not move the listener out of their audible range; portal hum is positional and follows the portal.
- Native project capacity is 128 real voices and 512 virtual voices. The transient pool, four music/ambient sources and normal protected workload fit below the real-voice limit. Unbounded protected events can still exceed hardware/Unity limits and virtualize; the system preserves their playback position rather than promising unlimited simultaneous audibility. Unity's [AudioSource priority documentation](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/audiosource/priority) describes that native limit.

## Event integration

Existing player attack presentation drives slash/combo/finisher/air/dash/rising/dive/critical/magnetic/skill sounds at action or impact timing. Main skill audio plays once per accepted presentation, with independent low-gain repeated pulse layers. Movement dash presentation includes its observer path. Character hit presentation supplies player/enemy impacts; crates bypass that generic cue and use their own impact/break layer.

SMG fire, physical bullet impacts, enemy melee release, lance release, healing and existing death/damage/respawn presentation use authoritative observer events. They do not add predicted duplicates on the owning client. New cosmetic RPCs are unbuffered so joining clients do not replay past shots. Existing authoritative spawning, damage and travel rules remain unchanged. Lance scenery impacts use its existing observer event. Each client observes replicated actor displacement/ground state for footsteps and landing; teleports, dead actors and attack/dash movement do not produce walking sounds. Portal presentation observes room-clear state and cleans up its owned hum. Chat receipt uses UI notification; chat send throttling uses UI error feedback. Native UI button/toggle/slider callbacks provide confirmation and navigation sounds, independent of visual animation settings.

## Import settings and maintenance

Short selected effects are preloaded ADPCM / Decompress On Load for immediate repeated playback. Positional effects import as mono; UI retains its channel layout. The 14.75-second mono portal hum uses preloaded Vorbis / Compressed In Memory. Five long stereo music/ambient beds stream Vorbis at 0.8 quality with background loading and no PCM preload. Original sample rates are preserved. `Logs/audio-import-inspection.csv` reports actual post-import channels, durations and load settings. Dedicated-server builds do not initialize the audio sources.

`Tools → Mecha → Audio → Implement Audio` authors the profile once, reuses existing assets thereafter, attaches footsteps to actor prefabs, adds UI audio feedback, builds/saves the native menu prefab and MainScene, and connects the existing mixer. `Connect Mixer` only reconnects the current profile and names the user-exposed volume parameters. Settings category mapping includes Audio. `AgentScripts/InspectAudio.cs` provides read-only asset/runtime inspection through the official Pipeline `run_script` command, without the Test Runner.

## Validation

Compilation and read-only asset inspections are followed by manual Editor Play Mode inspection, including the local FishNet host, real gameplay presentation, native mixer parameter/readout changes and dense transient playback. No automated tests are created or run, and no extra players or Relay sessions are built. Device-build performance and remote-client delivery are not measured in this pass. See the final recorded observations below.

Final observations:

- Compilation completed without errors. The final fresh startup, local host and disconnect inspection produced zero runtime errors/warnings. Startup tolerates the interval before FishNet initializes the scene NetworkObject.
- Asset inspection found 65 clips, all 41 cues populated, five native mixer groups, all five exposed volume parameters and footsteps on MechaPlayer, EvaEnemy and LanceEnemy. Import readback confirmed 59 Decompress On Load effects, one Compressed In Memory portal hum and five stereo Streaming beds.
- Esc opened the audio modal both during play and over the lobby. Selection stayed on the Master slider; gameplay was blocked locally. Setting SFX to 0% produced -80 dB and a 0% readout. Music set to 37% persisted across a complete stop/start of Play Mode; the original 50% value was restored afterward.
- Local authoritative presentation produced one MegaSlash startup with four pulse layers, SwordFire startup/pulses, gunfire and physical bullet impacts, enemy throws/melee, character hits/death/revival, landings, crate destruction/healing and portal opening/hum. Clearing/travelling changed level 1 to 2 and emitted portal travel once. Disconnect stopped the portal hum and returned to the initial music/ambient beds.
- A manual same-frame burst of 240 transient requests admitted all 240 newer events while replacing 218 older eligible voices; the 56-source transient bank remained bounded. Both protected skill/portal clips and music remained playing with their original clips. This establishes admission/retention in that inspected workload; it does not imply that 240 clips were simultaneously audible.
- Active sources showed different authored pitch values, correct mixer routes and no virtualization in the observed workload. With the existing Editor master mute temporarily disabled, listener output was nonzero (sampled peak 0.304, RMS 0.094 for a music/ambient window). This is a signal check, not a complete loudness or clipping certification.
- The classification pass completed its large import after the client request timed out. Filesystem readback found zero missing managed labels against the 7,376-file inventory. No test or package files changed. Source/document whitespace checks passed.
- MainScene was left saved, clean and outside Play Mode. The original Editor master mute was restored; unmute the Game view to audition the implementation. A local menu preview is retained in ignored `Logs/audio-configuration-menu.png`.
