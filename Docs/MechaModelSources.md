# Mecha model sources and Wing Zero placeholder

Updated 2026-10-18. Both mecha remain provisional placeholder artwork.

## Original player source

The previous player is **OZ-00MS2B Tallgeese III**, from **Gundam Memories: Tatakai no Kioku (JPN), PlayStation Portable**. Its [Models Resource entry](https://models.spriters-resource.com/psp/gundammemoriestatakainokioku/asset/292777/) is credited to uploader ZRen (Ren in the filenames).

Local evidence is the mesh/object name `Gundam Memories - OZ-00MS2B Tallgeese III - Ren` inside `Assets/_Project/Models/Player/Mecha.fbx`, plus the original texture filename retained in `MechaCharacterSetup.cs`. The local FBX includes the previously supplied custom Bip001 rig and animation; it is not an untouched game export. The original model, texture, clips, GUIDs and importer settings are preserved.

`Assets/_Project/Prefabs/Characters/TallgeeseVisual.prefab` preserves the previous configured visual and gun assembly. Its original animation profile remains `Assets/_Project/Settings/Animation/PlayerAnimation.asset`.

## Second model and assets

**XXXG-00W0 Wing Gundam Zero (EW)** comes from [the same game's Models Resource listing](https://models.spriters-resource.com/psp/gundammemoriestatakainokioku/asset/292776/), uploaded by ZRen on June 14, 2015. The archive was downloaded through the public listing on 2026-10-18.

| Asset | Location |
| --- | --- |
| Original four-file archive | SourceArchive/WingZeroEW-ModelsResource-292776.zip |
| OBJ, MTL, albedo, derived skin and toon material | Assets/_Project/Models/Player/WingZeroEW/ |
| Reusable rigged visual | Assets/_Project/Prefabs/Characters/WingZeroVisual.prefab |
| Ten baked clips and native controller | Assets/_Project/Animations/WingZeroEW/ |
| Animation ScriptableObject | Assets/_Project/Settings/Animation/WingZeroAnimation.asset |
| Active player | Assets/_Project/Prefabs/MechaPlayer.prefab |

Archive SHA-256: `5CA7096471FBCE639837CA1D268B0B63B21F6FF08717901CB59756C6B9501ED3`.

The source is static OBJ geometry with textures, without bones or animation. Mesh01 supplies the character, wings and twin rifles. Mesh02 is an effect plane excluded from the derived character skin; its texture remains in the untouched source archive. Model/texture origin is imported game artwork, with upstream AI provenance unverified. No image or model was AI-generated during this work.

## Rigging and playback

Unity's Animator remains the playback system. Generic transform clips cannot animate an unrigged OBJ, and automatic Humanoid retargeting cannot consume these existing Generic clips. The focused editor-only helper builds a compatible 31-joint Bip001 skeleton, binds 162 disconnected armor pieces rigidly and bakes ten clips at 60 samples per second. It transfers anatomical orientations, retains the target's segment lengths, solves the legs against the sampled source foot trajectories and reduces curves with bounded component error. Wings stay rigid on the back; rifles follow the hands. Original UVs and surfaces remain intact.

The set covers Idle_V2, Running, Dash, Jump, JumpLoop, Attack_A/B/C, Death and the existing skill-cast clip. The copied controller preserves state names and PlaybackSpeed; the animation profile preserves state mappings and timing. No runtime retargeting component or new package was added. Rigidbody movement still owns travel.

The existing player's network prefab identity and gameplay components remain. Movement visuals, body Animator, aiming forearm, existing secondary gun assembly and combat hand references were remapped. The body visual stays first in the hierarchy so CharacterAnimator finds it before the reticle Animator. Presentation reapplication preserves the selected animation profile and the native replacement mesh's material/outline units.

`AgentScripts/WingZeroPlaceholder.cs` provides operator-invoked BuildRig, Retarget, Integrate and Preview methods through the official Pipeline run_script tool. Use Edit Mode. BuildRig/Retarget can be reapplied; Integrate preserves the Tallgeese backup and skips an already integrated player. To restore Tallgeese, use TallgeeseVisual at scale 0.65, assign PlayerAnimation and remap the same movement/Animator/arm/weapon/hand fields. Keep the body visual first and retain the root network object identity.

## Validation and limits

Unity compiled the editor setup changes without errors. Manual render previews inspected bind, idle, run, jump and attack poses. An isolated instance of the actual player prefab was inspected in Play Mode: WingZeroAnimation and its Running clip resolved, the new skin rendered, and Time.frameCount advanced from 1680 to 1687 during observation. The aiming reference resolved to the replacement left forearm. The player has zero missing scripts.

Gameplay behaviours were disabled in that isolated preview to inspect native Animator playback. Automatic approval review rejected starting a local Quick Host because this is a visual change. No network host, Relay session, extra player or build was started. Full interactive combat and multiplayer playback were not exercised. Large rigid wings and rifles can overlap in melee poses; individually animated feathers and weapon swaps are outside this placeholder pass.

Inspection captures and part-binding receipts remain under ignored Logs/MechaRetarget. Temporary runtime objects and captures under Assets were removed. MainScene was restored to Edit Mode, unpaused, time scale 1, with prior run-in-background and auto-tick settings retained. No automated tests were created, modified or run.

## 2026-10-18 user adjustment

The initial retarget above was superseded after the user reported broken feet and unusual motion. Native inspection identified a 100x double transform of default BakeMesh vertices, missing source-to-target bind rotation offsets, foot goals carrying the static OBJ's airborne stance, and knee hints derived from that unsuitable stance. The revised bake uses BakeMesh with scale enabled before TransformPoint, transfers anatomical rotation offsets, derives foot/knee goals from the animated source relative to its pelvis, and compensates target pelvis reach relative to idle. Both soles remain above the physics root foot plane, including jump poses. Ten native clips were regenerated with existing GUIDs. Zero's running reference speed now matches its 8.5-unit/s movement instead of the inherited 4.2 reference.

Zero is 0.50 visual scale instead of 0.65, with wing geometry reduced to 60% around its attachments in the derived mesh. The tuning asset is Assets/_Project/Settings/Characters/ZeroRig.asset. Source OBJ/texture, original player rig/clips and physics remain unchanged. Actual selection portraits were refreshed. Native source/target bind/idle/run/jump comparisons, sampled sole positions and an advancing Play Mode run preview are recorded under ignored Logs/ZeroRevision. The final idle holds a standing stance without the prior folded knees. The skin remains a rigid placeholder and wings/rifles can overlap in extreme poses; this does not claim newly authored animation or individually articulated feathers.
