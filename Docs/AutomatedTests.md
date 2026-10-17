# Focused gameplay regression tests

Current preference (2026-10-16): do not create, add, modify or run automated tests without an explicit user request. Default validation uses compilation, source review, static checks and manual runtime inspection. The following coverage, commands and results document the existing suite; they do not authorize running it for future changes.

Avoid repeating standalone builds, Relay sessions, or multi-client sweeps for local gameplay, UI, VFX, and profile edits. Use a targeted network smoke check when changing transport, lobby membership/readiness, authority, replicated state, network spawning, or remote presentation. Broaden that check only when failures or source changes justify it. Local tests do not prove remote synchronization.

## Coverage

Explicitly requested in the 2026-10-16 game-adjustment task: `ChatRegressionTests.EnterSendsOnceExpiresMessagesAndRemainsUsableAfterHostStarts` passed against the saved MainScene. It covers real Enter events, one send per submit, literal text, bounded history, real-time expiry, cancellation, input suppression, a local host broadcast and continued use after disconnect. Run only this fixture for the chat scope; this request does not authorize unrelated suites.

The final expanded run on 2026-10-17 also passed chat over the controller-keyboard/menu overlay and mouse/native TMP activation without reopening after submit: 1 passed, 0 failed, 5.35 s. See [game-adjustment validation](GameAdjustments.md).

| Fixture | Cases | Regression coverage |
| --- | ---: | --- |
| `ComboSequenceTests` | 7 | Authored attack order and looping, input timeout, non-loop completion, ground/air switching, independent player cursors, reset, missing/empty sequences, unchanged sequence asset |
| `ProceduralLevelTests` | 13 | Deterministic seeds, unchanged authored profile, room shape progression, supported player/enemy/portal positions, enemy spacing, platform horizontal bounds, rising-room camera width and platform variety |
| `OneWayPlatformTests` | 6 | Real Rigidbody landing, retained capsule support, upward pass-through and descending collision, explicit drop priority, drop expiry, ignored-pair cleanup on disable |

Layout constraints exercise the opening room and all three generated shapes with four fixed seeds each. The tests load the actual procedural settings asset. Combo tests create disposable configuration objects; platform tests create disposable native colliders and a Rigidbody. They start no network manager or cloud session and use no production scenes or prefabs. Unity's Test Runner restores the editor scene after PlayMode execution.

The test-only `GameCode` bridge accesses gameplay in Unity's predefined `Assembly-CSharp` via reflection. Custom test assemblies cannot reference that predefined assembly directly. This keeps production assembly boundaries unchanged, exercises the actual compiled gameplay code, and excludes the test assemblies from ordinary player builds. If gameplay moves to a runtime asmdef later, replace this bridge with direct references. No gameplay implementation is duplicated in tests.

## Running

In Unity, open **Window → General → Test Runner** and select the project assembly under EditMode or PlayMode. Through the official Unity CLI, target this project and filter by assembly:

```powershell
unity command run_tests --project-path 'D:/PersonalProjects/project-mecha' --mode editor --filter Mecha.EditModeTests --filter_type assembly
unity command run_tests --project-path 'D:/PersonalProjects/project-mecha' --mode playmode --filter Mecha.PlayModeTests --filter_type assembly
```

For an individual regression, use `--filter_type testName --filter RoundedCapsuleKeepsEstablishedSupportAfterSmallPenetration` (with `--mode playmode`). For asynchronous execution add `--async_tests true` and read `unity command test_status`. PlayMode entry can reload the command connection; inspect `test_status` before repeating a run.

## Verified 2026-10-15

Unity 6000.3.11f1 compiled the new test assemblies with **0 errors and 0 warnings**. The official Unity Pipeline Test Runner reported **26 passed, 0 failed, 0 skipped**: 20 EditMode cases and 6 PlayMode cases. Reported test execution time was 0.19 s and 0.72 s respectively, excluding editor mode transitions and compilation. Unity returned to stopped Play Mode. No standalone player was built and no network session was opened for this pass.

These tests cover local rules and collision regressions. They do not replace visual review of VFX/camera feel or targeted network validation when network behavior changes.
