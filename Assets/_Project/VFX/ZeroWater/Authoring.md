# Zero hydraulic water skills

Current rendering policy: the later user clarification supersedes the foreground overlay note below. Water uses normal scene depth. Optional VfxSurfaceDepth moves cosmetic emitter branches just ahead of a nearby platform, within an authored bound; the effect root and any interaction/light branches keep their world positions. The 2026-10-19 lighting revision below adds real lights; particle modules, materials and source art remain unchanged. See [depth/interaction verification](../Rendering/Authoring.md).

## Real light emission — 2026-10-19

The user requested light emission from Zero's water effects. All five prefabs previously contained zero lights. Native blue point lights now illuminate nearby armor and normal-mapped platforms; native Animator clips own their intensity envelopes. These are receiver lights, separate from the existing particle shader/bloom. No new shader, package, source art, gameplay timing, damage, modifier or networking change.

| Effect | Native light setup | Envelope |
| --- | --- | --- |
| BubblePreparation | 1 point, range 2.3 | Builds to 1.6, zero at .32 s |
| BubbleDetonation | 1 point, range 3.8 | Peak 6 at .025 s, cooling to zero at 1.1 s |
| FountainPreparation | 4 nozzle points, range 1.45 | Peak .65 at .08 s, zero at .3 s |
| RisingFountain | 4 nozzle points, range 2.3 | Four peaks of 2.4 aligned to .22 s particle bursts, zero at 1.35 s |
| WaterModeAura | 1 point, range 2.5 | 1.1–1.3 sustained loop; release fades to zero over .35 s |

Color is (.075,.48,1). Lights have no shadows or indirect bounce and remain at their source positions, Z=-.45; fountains align with their four existing nozzle X positions. They are siblings of cosmetic particle branches, so platform-depth bias cannot move them. Aura light follows the existing WaterEffect attachment; its lifetime component signals only the native Animator Release trigger when mode/lifetime ends. The release clip then holds zero through particle drain/cleanup. AlwaysAnimate keeps the envelope advancing when offscreen. Native prefab/clip assets own tuning; runtime never edits shared configuration.

`Editor/WaterLightingAuthoring.cs` authors/reapplies native assets explicitly through Unity APIs. `BuildCharacterRoster` calls the same helper when rebuilding effects. New assets are provisional Replace with native category labels; existing source provenance is preserved. The project already uses native Animator-driven fire lights; this revision reuses that built-in approach and adds no custom lighting controller.

Manual inspection invoked the existing Bubble/Fountain skill Begin/PresentPulse and Water Mode modifier presentation on a local unspawned Zero in the actual generated room. Frames advanced 605→965 and time 11.128→18.016. Same-frame owned-light on/off floor comparisons (86,940 pixels) changed 3,032 pixels for bubble preparation; 26,241 detonation; 11,365 fountain preparation; 60,744 fountain; 10,683 aura. Maximum channel differences were 29/111/37/111/36. Nearby armor response and source-local platform light were visually inspected. Aura light followed a one-unit actor move exactly; clearing mode gave intensity .776 during fade and zero afterward. All water roots/lights drained. Compilation passed and final console ground truth had zero errors/warnings. A first authoring call exposed Unity's missing-component null sentinel; explicit Unity null checks fixed it before prefab saves. No automated tests, extra players/builds or network sessions. MainScene clean and Play Mode stopped. Appearance remains pending user review. Captures: ignored Logs/WaterLighting; operator helper AgentScripts/PreviewWaterLighting.cs.

Original native Particle System assemblies for Zero. Interpret water as pressurized coolant/recirculation hardware, using deep navy liquid, cyan midtones and restrained pale foam. Preserve dark technical-anime/mecha silhouettes and negative space.

| Effect/layer | Onset / life | Source / native behavior | Purpose |
| --- | --- | --- | --- |
| A pressure shell | cast / .32 s | native sphere mesh particle, Fresnel material | Growing confined water bubble in front |
| A interior | .02 / .28 s | Water2, 7×7 sheet, small rolling lobes | Dense moving water inside shell |
| A detonation | impact / .52 s | Water2, distinct expanding particles | Single broad pressurized release |
| A spray | impact + .03 / .65 s | hollow splash source, 2×2 sheet, spherical emission/gravity | Broken radial droplets, no straight bars |
| A foam | impact + .12 / .85 s | supplied water foam density source | Lighter residual water |
| S preparation | cast / .3 s | foam at four nozzle positions | Ground-source pressure lead-in |
| S rising columns | impact / .62 s per particle | Water2, upward cones, four staggered bursts at .22 s | Four flowing pillars around player |
| S crowns / bases | .42 / .65 s; .1 / .9 s | splash/foam, gravity and noise | Upper breakup and base turbulence |
| D envelope | mode start / looping .9 s particles | native mesh shell, low-opacity Fresnel | Persistent readable surrounding aura |
| D recirculation | mode start / looping .65–.7 s | Water2, orbital flow and side vents | Upward coolant motion while following actor |

Maximum authored layer count is 12 for the fountain. Native particle counts are bounded per system. No performance benchmark is claimed. WaterEffect owns attachment and bounded cleanup only; ParticleSystem modules own simulation. World-space one-shot tails remain after emission stops. Mode layers use local simulation and follow the actor, then drain for 1.5 seconds. Cosmetic particles never apply damage.

The supplied Hovl Projectile 14 water prefab was read-only study: it separates water, drops and a circle, and uses a 7×7 sheet for Water2. No finished prefab, vendor controller, material or Shader Graph was cloned or referenced. Three unchanged source textures are reused directly; original native prefabs/materials are authored under this folder. Their lineage is recorded in Docs/VfxEffectOrigins.csv.

Concrete rendering gap: native URP Unlit particles do not provide this combined luminance-density treatment, broad two-band water shading and view-dependent mesh-shell Fresnel. The focused ZeroWater ShaderLab/HLSL shader supplies those features. No global rendering/post-processing setting is changed. The shader preserves the supplied source density in alpha while separating dark liquid from pale highlights.

Initial manual local-host preview advanced 98–110 render frames per phase sequence. Bubble hit the front receiver once for 48 configured damage and the rear receiver zero times. Fountain hit front/rear four times each for 64 damage. Mode dealt no damage and activated 1.3× movement / 1.4× attack modifiers. The first mode preview exposed a weakly readable aura; the technical correction adds a restrained surrounding water shell and side recirculation. User visual acceptance remains pending.

Raw phase captures/observations are in ignored Logs/Characters. No automated tests were created or run.

2026-10-18 visibility follow-up: effect renderer children now use TransparentFX and the native URP foreground camera stack in every current scene. WaterEffect applies that default when spawned, and the authoring helper preserves it on rebuild. This supersedes the earlier statement that no global rendering configuration changes: materials/shaders/source maps remain intact, while world occlusion now follows the user's foreground policy. A fountain placed behind a player and solid platform was observed rendering over both. See [foreground rendering](../Rendering/Authoring.md) and ignored Logs/ZeroRevision/Foreground.png. No new art or vendor dependency was introduced.
