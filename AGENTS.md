# Project architecture and validation

- Prefer Unity's built-in systems and official packages before custom replacements: Particle System for combat effects, Cinemachine for camera framing, Animator for animation, and Unity physics for room boundaries.
- Store system tuning, setup profiles, item definitions, and other authored data in ScriptableObject assets. Components hold references and runtime state; shared configuration assets must not be mutated during gameplay.
- Keep input, movement, combat, animation presentation, effects, and camera responsibilities separate. Follow SOLID where useful: small focused files, explicit dependencies, reusable services, and interfaces at meaningful boundaries; avoid speculative abstraction.
- Document user architecture decisions and reuse them for future systems. See Docs/Architecture.md.
- Prefer CLI and MCP to inspect, modify, and validate work. Modify Unity scenes, prefabs, materials and importer settings through Unity APIs.
- Use the official Unity CLI MCP server (`unity mcp`) with `com.unity.pipeline` for this project. Discover live commands before invoking them; see Docs/UnityMcpSetup.md. Do not reintroduce the removed third-party MCP integration.
- Enter Play Mode when useful, then restore the previous editor state.
- Do not create, add, or run automated tests unless the user explicitly requests automated tests. Use source review, compilation, static checks, and manual runtime inspection.
