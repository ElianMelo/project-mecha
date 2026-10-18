# Host disconnect and session continuity

**Decision date:** 2026-10-18. **Status:** accepted behavior requirements; implementation deferred at the user's request until they return to this work after obtaining FishNet Pro.

## Required behavior

| Scenario when the host disconnects | Required outcome |
| --- | --- |
| 1v1 PVP, including a two-player Free-for-All or Team match | End the match and return the remaining player to the menu. Host migration is unnecessary. |
| Team PVP with more than two participants before the host disconnects | Preserve the lobby and match for the remaining players by transferring hosting to an eligible connected peer. Preserve teams, health, scores and the remaining match/respawn time. |
| Cooperative waiting lobby or an expedition in progress, with another player connected | Preserve the lobby and cooperative progress by transferring hosting to an eligible connected peer. The host's departure must not delete the shared session or restart the expedition. |

The Team PVP participant threshold is measured **before** the departure: a three-player match becoming a two-player match still belongs to the continuation scenario. Handle intentional host departure, application termination and connection loss. The old host's process cannot be kept alive after it exits; continuity means another process takes over the server responsibility.

Free-for-All with more than two participants has no approved disconnect policy yet. The outcomes when a team becomes empty, no eligible successor remains, or a transfer fails also need a product decision during implementation. This note does not authorize inventing a forfeit result, migrating to spectators, or retaining an empty lobby indefinitely.

## Verified FishNet support

Checked against official documentation on 2026-10-18:

- FishNet's [Networking Models / Host migration](https://fish-networking.gitbook.io/docs/guides/high-level-overview/networking-models#host-migration) says there is no built-in host migration and that implementing it yourself is possible but tricky. This is a general FishNet limitation, rather than a documented free-edition paywall.
- The published [Pro feature list](https://fish-networking.gitbook.io/docs/overview/readme/pro-projects-and-support) does not include host migration. Buying Pro must not be treated as automatically enabling the required behavior. Re-check the installed version, official support and compatible maintained integrations when the user returns to this work.
- FishNet supports separate dedicated servers as well as player-hosted servers. Running the server independently from player processes can provide continuity when a player leaves. The user has not selected a dedicated hosting service or authorized a topology migration here.

Do not build a custom migration implementation, add a migration package, buy an upgrade, change hosting topology or claim the feature works as part of this documentation request. The user explicitly asked to take note if migration is unavailable in the free edition.

## Current implementation

Host-disconnect continuity is **not implemented**. `MechaLobbyService.LeaveAsync` and `OnDestroy` delete the cloud session when the local player is its host. During gameplay, `OnHostChanged` invokes `OnRemoved`, which detaches the session and stops gameplay networking through `FishNetSessionNetworkHandler`. The present host-exit behavior therefore does not yet meet the continuation requirements above. The specific 1v1 return-to-menu outcome is also recorded as desired behavior, rather than claimed as newly implemented.

Changing the cloud session's host is separate from restoring a FishNet gameplay server and connecting the remaining clients to it. The current custom `INetworkHandler` starts and stops connections; it does not transfer authoritative game state. A session-host election alone must not be reported as gameplay host migration.

## Future implementation scope

Prefer supported installed/native systems, official packages or a maintained compatible integration before considering custom migration, following the project dependency policy. Keep the current host-trusted authority model unless the user separately requests dedicated server authority.

When implementation is authorized, review these concrete gaps:

- Transfer ownership before an intentional departure instead of deleting a continuing lobby. Provide equivalent recovery for an abrupt departure. Coordinate cloud membership, successor election, new connection/Relay configuration and client reconnection.
- Preserve Team PVP roster/team assignments, K/D/A, health, resources, effects/status durations, spawn state, the assist contribution ledger and match/respawn/cooldown time remaining. Rebase deadlines onto the successor's clock; one process's monotonic-clock timestamps cannot be copied directly to another.
- Preserve cooperative room/encounter state, seed, enemies, destructibles, player resources, cleared state, route progress, pending path plans and outstanding ballots. Remove the departed pilot from membership and applicable voting requirements without restarting progress.
- Gate combat and inputs during a bounded handoff, avoid duplicate spawning or awarding kills twice, then restore normal controller focus and presentation. Define and localize the user-visible recovery/failure paths once their behavior is approved.
- Perform targeted host-departure checks for 1v1 PVP, Team PVP with three or more starting participants, cooperative waiting lobbies and active expeditions. Cover both intentional departure and abrupt loss. Do not add or run automated tests without an explicit user request.

This note changes no runtime code or network settings. No builds, player launches or automated tests are needed for this documentation-only decision.
