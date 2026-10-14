# project-mecha

Grand Chase / Elesis-inspired movement and combat prototype.

Open `Assets/Scenes/MainScene.unity`, enter Play Mode and press **1** to start the local host. Use **Left/Right** to move, **Up/Space** to jump, **Down** to descend or fast-fall, **Z** to attack, and **double-tap a direction or Shift** to dash. Skills: **A Mega Slash**, **S Sword Fire**, **D Critical X**.

Player inputs, movement, attacks, health and effects remain separate components on `MechaPlayer`. The arena has one-way platforms; enemies have server-controlled damage and hit reactions. The existing Relay flow remains available.

- [Gameplay research, controls and tuning](Docs/GrandChase/GameplayResearch.md)
- [Implementation process and runtime validation](Docs/GrandChase/ProcessAndValidation.md)

The setup can be reapplied from **Tools → Mecha → Apply Mecha Gameplay** while MainScene is open in Edit Mode. The camera follows the local player at a closer distance. Hold **Down** to descend through platforms; press **Up during a Z combo** for a rising slash. Unity Toon Shader is installed and available for material styling.

- [Further research and gameplay refinements](Docs/GrandChase/Enhancements.md)

Prototype tuning is intentionally simplified; the research documents record differences from the reference game.
