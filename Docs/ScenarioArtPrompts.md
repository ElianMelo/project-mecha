# Generated scenario art — 2026-10-14

Generated with the **built-in image-generation tool**, not the API/CLI fallback. Both selected outputs were copied into the Unity project and imported through Unity APIs. The current request explicitly authorized new scenario/background/platform art; earlier reference-only restrictions are preserved for the existing character models.

## Asset manifest

- `D:/PersonalProjects/project-mecha/Assets/_Project/Art/Environment/CyberpunkCity.png` — 1536×1024 city panorama.
- `D:/PersonalProjects/project-mecha/Assets/_Project/Art/Environment/IndustrialPlatforms.png` — 1536×1024 RGBA platform atlas, preserving generated transparency.
- `Assets/_Project/Art/Environment/CityBackdrop.asset` — full-image native Sprite.
- `Assets/_Project/Art/Environment/FloorPlatform.asset` — native Sprite rectangle (0,532,1536,394), top-center pivot.
- `Assets/_Project/Art/Environment/FloatingPlatform.asset` — native Sprite rectangle (0,80,1536,358), top-center pivot.

ScenarioSettings references these assets. The Sprite rectangles are Unity's native atlas selections; no image-editing script modified the source PNGs. Parallax and all gameplay/configuration are separate components/assets.

## City generation prompt

Use case: stylized-concept. Asset type: wide seamless-looking 2D side-scrolling game background, landscape panorama 1536x1024. Primary request: cyberpunk industrial city at night for an 80s hand-painted mecha anime platform action game. Camera perfectly side-on, distant skyline, stacked industrial towers, antennae, elevated pipes, a few cyan and amber windows, deep petrol blue and muted violet, atmospheric haze, dramatic hard cel-shaded architecture. Keep central/lower gameplay region low contrast and quiet, skyline details concentrated upper two thirds, no near ground plane or playable platforms, no characters, no readable text, no logos, no user interface, no border. Background must fill the whole image. Fine painterly anime detail, smooth pixels, not pixel art. Horizontal ends should have compatible dark city silhouettes for repeating scenery.

Transparent background: false.

## Platform atlas generation prompt

Use case: stylized-concept. Asset type: 2D side-view platform texture sprite atlas for a cyberpunk industrial mecha action game. Exactly TWO horizontal rows, each occupying exactly half of a wide landscape 1536x1024 image. Transparent background outside the silhouettes. TOP HALF: a continuous heavy ground/floor cross-section strip, straight level walkable top edge near top of its row, dark blue steel and concrete foundation filling the rest of the top half down to the center line, repeatable paneling, small amber hazard edge markings and cyan conduits, strong readable top lip. BOTTOM HALF: a separate suspended floating platform cross-section strip, a straight level steel walkable top lip near top of the bottom row, chunky navy industrial metal underneath, support ribs, cyan light accents and a jagged technical underside ending just above bottom edge, alpha outside. Both strips extend all the way to left and right image edges, their ends horizontally compatible for tiling. Orthographic elevation, no perspective, no top-down view, no isometric view. 1980s painted cel anime, crisp readable industrial shapes, smooth detailed texture not pixel art. No buildings, no characters, no text, no logos, no labels, no border, no shadows outside silhouettes. Keep floor and floating platform visibly distinct. This is a production texture atlas, not a screenshot or scene.

Transparent background: true. The actual generated strip boundaries were inspected and selected with the Sprite rectangles listed above.
