# Level routing — first iteration, 2026-10-17

## Current presentation — dedicated 3D scene

At the user's request, the routing GUI has been replaced by **Assets/Scenes/LevelRouting.unity**. RouteMapMenu now loads/unloads this scene additively for local presentation while MainScene and its authoritative FishNet objects remain resident. The scene contains a perspective camera, physical table, encounter miniature prefabs, SpriteRenderer paths/focus/vote markers, BoxCollider selection volumes, and a 3D briefing terminal using TextMeshPro meshes. There are **no routing Canvases, RawImages, RenderTextures or invisible UI Buttons**. Opening this scene in the Editor shows authored example miniatures for scene/art iteration; runtime replaces them with the host's current graph.

The native PhysicsRaycaster and existing EventSystem/Input System handle pointer and submit events on the actual world nodes. A small IMoveHandler selects screen-direction neighbours between these 3D objects because native collider interaction has no controller-navigation graph. Mouse hover and controller focus update the same world-space description, vote intent and destination feedback. White SpriteRenderer focus rings remain visible independently of motion. Existing Animator miniature reactions, countdown pulses, sounds and haptics are retained.

Options retain their existing modal UI; world selection is blocked while they are open and focus returns to the same 3D node. Gameplay cameras and HUD canvases are temporarily hidden and restored with their previous enabled states. Selection-layer collision pairs are temporarily ignored through Unity physics and restored after unloading, so the presentation colliders cannot push combat actors. This is a local presentation change: routing graph generation, encounter registration, voting authority, countdown rules and room construction are unchanged.

Validation of this revision used compilation and manual Editor Play Mode inspection: the dedicated scene loaded with 11 world nodes and zero Canvases/RawImages; controller confirmation and an actual collider-based mouse click both entered the fixed opening encounter and unloaded routing with the Main Camera restored. After clearance, the scene reopened for branching choices. Directional controller movement, collider-based hover, modal blocking and selection restoration were inspected. A motion-disabled runtime profile left one persistent focus ring visible with zero enabled scene Animators. The selection layer's combat-collision exclusion was observed during routing and its original collision state restored after travel. Original input settings and stopped, clean MainScene were restored. No automated tests, extra Player builds or repeated co-op/Relay sessions were run for this presentation-only revision. The earlier build/network notes below describe the initial iteration.

Solo Host and the existing ready-to-start co-op flow now enter a 3D expedition table before combat. The only available opening node is Insertion; confirming it launches the existing fixed opening arena. Clearing an encounter and using its exit returns the squad to the table. Only direct outgoing connections are selectable destinations; other visible nodes can be focused to scout their encounter previews.

## Research and foundation

Research was performed before implementation, followed by a second search after the initial feedback pass. The attached Slay the Spire/Inscryption pictures are visual references, not additional instructions. The table/miniature presentation is an interpretation of those references; no reference-game artwork is imported.

| Primary source | Finding and implementation decision |
| --- | --- |
| [Anthony Giovannetti, designer interview](https://thegamedesignroundtable.com/211-slay-the-spire-a-designers-deep-dive-with-anthony-giovannetti/) | The interview explicitly covers procedural look-ahead at 33:20. Keep future nodes and their connections visible so routes can be planned. |
| [Mega Crit, metrics-driven design and balance, GDC 2019](https://media.gdcvault.com/gdc2019/presentations/Giovannetti_Anthony_SlayTheSpire.pdf) | Iteration and balancing motivate keeping route widths, encounter eligibility and weights in authored data. This implementation is not a reconstruction of Mega Crit's generator. |
| [Bazzaz and Cooper, FDG 2025](https://arxiv.org/html/2504.03918v1) | Their map uncertainty research distinguishes route choice from encounter uncertainty. Here, enemy-only choices advertise concrete room formats instead of pretending to offer different reward systems. |
| [Daniel Mullins, Inscryption post-mortem, GDC 2022](https://www.gdcvault.com/play/1027609/Independent-Games-Summit-Sacrifices-Were) | The creator discusses the game's art direction and combining 3D assets. Together with the supplied screenshots, this motivates a physical mission table and reusable room miniatures. The session abstract is the accessible source; no claim of reproducing its map algorithm is made. |
| [FishNet RPC documentation](https://fish-networking.gitbook.io/docs/guides/features/network-communication/remote-procedure-calls) | Use injected sender connections, reliable server requests and buffered observer descriptions for late joiners. Never accept a client-supplied voter identity or winner. |
| [Unity Selectable navigation](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-SelectableNavigation.html) | Reuse native Buttons, EventSystem and the existing explicit-navigation scope. |
| [Unity Input System gamepads](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Gamepad.html) | Native gamepad rumble supplies brief optional confirmation feedback. |
| [Game Accessibility Guidelines: color](https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-fixed-colour-alone/) | Vote colors also have P-number labels and names; focus, availability, completion and confirmation have text states. |

The installed systems already supply FishNet replication, deterministic procedural rooms, native uGUI, Animator feedback, AudioMixer/AudioSource cues and Input System navigation. Their concrete missing capabilities were this game's branching encounter graph, eligibility catalogue, squad ballot rules and board-to-UI projection. Small dedicated scripts fill those gaps. No package, runtime drawing system, custom mesh generator or custom audio synthesizer was added. Static rings and path dots reuse image/Sprite assets; room miniatures use authored native primitive geometry.

## Route graph and data

`LevelRouteSettings` owns widths, opening encounter, timings, board references, palette and haptics. Default row widths are **1, 3, 2, 1, 3, 1**: one opening, a three-way branch, partial merges, a shared encounter, another branch and a terminal merge. Forward-only, ordered edges give every node an entrance and exit, except the opening and terminal respectively. Every path through a sector has six encounters. Set all widths to one for a strict route. Up to three lanes are supported in this iteration.

Only the server generates the graph and encounter seeds. It sends the complete node/edge description; peers do not independently roll routes. The first room seed remains fixed. Later seeds vary between runs. The final cleared encounter's portal starts another sector using the same catalogue; there is no newly invented boss, reward economy or victory screen.

The initial catalogue contains Insertion plus **Vertical assault (Rising)**, **Flank patrol (Long)** and **Open stronghold (Big)**. Each selected definition controls the actual room format, including its preview text and 3D miniature. Existing procedural room construction, enemy roles, clearance, player placement and camera framing remain in their original systems.

## Add another encounter

1. Create a **Mecha / Routing / Encounter** asset in `Assets/_Project/Settings/World/Resources/RouteEncounters`. Give it a unique stable `encounterId`, title, description, reward preview, format, room-settings reference and map miniature prefab. Enable `includeInRoutes` and set weight/depth eligibility. Resources discovery automatically adds it to new sectors/runs; there is no routing enum or hand-maintained registration list.
2. For ordinary enemy content, leave `encounterPrefab` empty. The existing EnemySpawner owns the encounter.
3. For a chest or other custom encounter, create a FishNet NetworkObject prefab with a concrete `RouteEncounter` component. Implement server-owned `BeginEncounter(layout, seed)`, `IsComplete` and optionally `Remaining`. Assign that prefab to the definition and include it in FishNet's normal spawnable-prefab collection. The room director spawns it and opens the exit when it completes; routing does not need to understand gold, shops or chest logic.

A new chest still needs its actual collection/reward implementation. Automatic routing registration does not invent that gameplay. Definitions are loaded at run startup; adding assets while a match is already running does not hot-load them. Builds must contain matching definitions and network prefab data. Shared ScriptableObjects are read-only during play.

## Voting behavior

| Situation | Behavior |
| --- | --- |
| Solo | Confirm the available destination; a short 0.3-second reaction precedes travel. |
| First co-op vote with no majority | Begin the soft **5-second** deadline. Players can change or withdraw votes; changing a vote does not restart the timer. |
| Strict majority of active spawned pilots | Lock the winning destination and count **3 → 2 → 1**, then travel. |
| Everyone voted, without a majority | Lock immediately. A unique plurality wins; tied leading destinations are eligible for a server roll. |
| Soft deadline expires | Lock using submitted votes, then run the same **3-second** countdown. Nonvoters abstain. |
| Two pilots choose different destinations | Count **3 → 2 → 1**, roll uniformly between the tied choices on the host, show the winner for **1 second**, then travel. |
| Roster changes before resolution | Remove departed voters and reevaluate the active roster. A committed tie result retains its one-second reveal. |

Requests carry the route round and destination. The server validates round, direct connectivity, sender eligibility, phase and repeat rate. Duplicate votes do not increase vote counts. Once locked, votes cannot change. A player leaving during gameplay follows the existing network transport's disconnect detection interval. Host departure retains the existing session shutdown behavior; host migration is not added.

The buffered graph and ballot snapshot support a joining observer's current route view. Frequent snapshots are sent only while choosing/counting down. Countdown display interpolates locally; the server clock alone advances phases and rooms.

## Controller, mouse and feedback

All nodes are world GameObjects with selection colliders and EventSystem handlers. First focus lands on an available destination. Directional navigation is confined to visible route objects; future/previous nodes remain focusable for inspection. Confirm on an unavailable node produces an error cue. Hover moves focus and updates the 3D briefing terminal. B withdraws an unlocked vote; Esc/Start opens the existing options screens. Routing is mandatory between encounters, so Back does not dismiss the scene and resume combat. Options suspend world interaction; closing them restores route focus. Chat is hidden while routing so Enter cannot also start chat. Gameplay actions remain blocked while the routing scene is active.

Pass one added miniature lift/press/release animation, native button reactions, focused path highlighting, vote rings, text previews, countdown pulses, existing UI sounds and brief low-strength gamepad haptics.

The second research pass revisited [Xbox UI focus handling](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/113), the [Juice It or Lose It talk](https://www.youtube.com/watch?v=Fy0aCDmgnxg), and [Folmer Kelly's contextual-polish counterpoint](https://gdcvault.com/play/1021398/Don-t-Juice-It-or). The applied improvements were:

- Persistent white focus borders independent of motion, brighter future-path dots and explicit state labels.
- Numbered votes, colored vote pips and named squad intentions; destination confirmation is distinct from local focus.
- Immediate sent-vote feedback followed by authoritative acknowledgement; duplicate clicks do not replay route submission.
- Distinct invalid-choice audio, vote-arrival cues, locked countdown beats and a winner reveal.
- Frame the 3D board independently of Canvas scaling; pad first/last nodes away from screen edges and allow all four squad entries in the preview card.
- Respect the existing motion setting for miniature/countdown motion and haptics; stop rumble on close/disable.

## Validation and limits

No automated tests were created, modified or run. Validation used compilation, source review, an ordinary Windows development build, live frame advancement, native UI inspection and manually driven local sessions.

Observed: fixed opening waits at level 0; confirming through the ordinary UI listener/server RPC enters level 1 with seven enemies. A real second local client joined and appeared in the ballot. With only the host voting, the ballot entered SoftWait and subsequently deployed. Clearing through the ordinary authoritative damage method and entering the valid exit returned the squad to the three available branches. A manually staged guest ballot created a two-way split; the host entered Locked, then TieReveal, selected one of those branches and loaded its Rising room. A subsequent forced continuation loaded level 3. Runtime checks recorded no new managed gameplay errors.

Native gamepad navigation and South-button confirmation were manually injected through Input System; confirmation launched the opening room. Start opened options with Resume focused; East returned to the previously selected route. A runtime-only copy of InputSettings permitted those gestures while the Editor was unfocused; the original settings were restored. Motion-disabled runtime copies left zero enabled route UI Animators while the persistent selection border remained visible. Pointer hover was also inspected through Input System. Physical standalone controller input was not exercised.

The guest split vote was staged directly on the live server for resolution/presentation inspection. It does **not** prove a guest's button input or its RPC transmission. Three/four-player majorities were source-reviewed rather than exercised with extra processes. Relay, Internet latency/loss and late-join permutations were not retested. Existing Sessions/Relay startup code is unchanged.

The Windows build succeeded. One recorded build error was a Pipeline inspection timeout while the build occupied the Editor, not a compiler/build failure; its warning confirms Pipeline is disabled in Player builds. Automatic approval review rejected enabling the Player remote-control server as a persistent security-boundary change. It was left disabled. An attempted additional-Editor setup failed inside Unity's execution-graph initialization and was not retained. Validation used the existing Editor and one ordinary local Player instead.

Editor Play Mode is restored to stopped MainScene after inspection. Temporary input devices, client process and review captures are cleaned up; captures and player logs are retained outside game content under ignored `Logs/LevelRouting`. The provisional room miniatures/materials/animation are Placeholder art; no AI-image provenance is asserted.

Repeatable authoring is available at **Tools / Mecha / Apply Level Routing**. It preserves existing encounter definitions and tuning while wiring native assets and the scene.
