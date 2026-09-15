# Phase 4 direct content unassignment: implementation and qualification

Branch: `codex/phase-4-direct-content-unassignment`.
Starting HEAD/main: `eb7f918779267cb6b9a52e33374e814915ddd2e2` (merged PR #203).
GitHub main was checked with `git ls-remote origin refs/heads/main` and matched the clean local checkout before implementation.

## Final qualification status (supersedes provisional status below)

- Static checks: **7/7 passed**. Standalone Roslyn core/test, player and Editor compilation had zero errors; core/test compilation retained five existing CS0649 JSON-deserialization warnings in `BuildReadinessTests`.
- EditMode: **844 passed, 0 failed, 0 ignored/skipped, 0 inconclusive**; `PhaseFourUnassignment`: **31/31 passed**; wrapper exit code **0**. Results: `C:\\Users\\gdg34\\AppData\\Local\\Temp\\phase4_unassignment_editmode.xml` (SHA-256 `3311B5506264BAAD4F0B400F3063DA8C368BBA1BEC2196C21631F7C12B151FD5`).
- PlayMode: **2,344 passed, 0 failed, 10 expected ignored/skipped, 0 inconclusive**; direct-unassignment fixture: **31/31 passed**; wrapper exit code **0**. Results: `C:\\Users\\gdg34\\AppData\\Local\\Temp\\phase4_unassignment_playmode.xml` (SHA-256 `E4EFCDBC5491BF2572E8C01D68B1C1A234C35AD26BE8878F8A3158FC870F7CCD`). The expected ignores are eight synchronous EditMode-only GameRoot fixtures, one inverse non-Windows fixture and one Windows Player-only fixture.
- Windows Development Build: wrapper exit code **0**; Unity **6000.3.2f1**; target `StandaloneWindows64`; Development Build **true**; `BuildWindowsDevelopment` succeeded with Bootstrap scene, **0 errors** and **1 warning**. Output: `Builds/Development/Windows/Dungeon Lord.exe`; report: `Builds/Development/Windows/build-report.json`; provenance: `Builds/Development/Windows/build-provenance.json`; build log: `C:\\Users\\gdg34\\AppData\\Local\\Temp\\phase4_unassignment_windows_development_build.log`. The only warning was Unity Cloud native-symbol upload skipped because no access token was available.
- The user directly validated Editor UAT steps 1–13 and standalone UAT steps 14–15. Duplicate individual selection, monster/trap/loot-node unassignment, zero mana change, unchanged geometry/investment, exactly-once custody, repeat safety, free redeployment, destination-capacity retention, round trip, dungeon run, persistence, 1920x1080/1280x720 UI, and standalone editing/persistence all passed. Screenshots were not captured; this is honest user-validated manual evidence.
- Latest standalone log reviewed: `C:\\Users\\gdg34\\AppData\\LocalLow\\gdg3417\\Dungeon Lord\\Player.log` (2026-09-14 17:59:53 local). It records successful canonical saves for Boot, StateChange and AppQuit; no implementation-related error, exception, assertion, canonical-validation failure, save/load/write failure, ownership/custody failure or localization failure. `GarbageCollector disposing of ComputeBuffer` and PlayerConnection `MemoryLeaks` are accepted nonblocking shutdown-only diagnostics.
- Final review: schema remains **9**; no migration was added; the English string table has only seven intended localized additions and no whole-file line-ending churn; Basic Loot Node, Hidden Cache and Glittering Hoard use `ReturnToPlayerCustody`; unassignment preserves identity and sequence and changes neither mana, structural investment nor geometry.
- `ProjectSettings/UnityConnectSettings.asset` is a pre-existing local-only change (`m_Enabled: 0` to `m_Enabled: 1`), intentionally excluded from the feature commit and PR.

## Implemented contract

- `DetachedCanonicalMutationRequest.Unassign(AssignmentId)` is a distinct `UnassignContent` mutation. The caller supplies no category, option, source room or sequence authority.
- The pure preparation path clones and validates the canonical source, resolves exactly one active assignment across floors, consults the exact authored category/option removal record and requires `ReturnToPlayerCustody`. It removes only that active assignment and adds one returned record preserving AssignmentId, CategoryId, OptionId and Sequence, with the existing return disposition. It canonicalizes and validates the entire candidate.
- The complete writer checks exact current session bytes against disk before preparing ownership movement, including candidate-identical stale retries. The existing complete-save transaction, validation, recovery and runtime publication boundary remain authoritative. No runtime publication occurs on rejected preparation or persistence.
- Unassignment neither charges nor refunds mana, changes historical structural investment/construction accounting, nor advances NextSequence. Floor/room/edge identities, geometry, route, anchors, orientations, unrelated assignments and recognized runtime state are preserved.
- The production removal-policy records for `placement.option.loot_node.basic`, `placement.option.loot_node.hidden_cache`, and `placement.option.loot_node.glittering_hoard` change from `Unresolved` (0) to `ReturnToPlayerCustody` (1). The mutation consults policy for every category. No generated/resolved reward state travels with a returned loot node.
- Schema remains 9. No persisted fields, migration, second inventory, ownership authority or write path were added. Acquisition prices, StartingMana, structural tuning and workload limits are unchanged.
- The existing production raw-save `MaximumArrayElements` limit remains the custody boundary. Tests read the authoritative configuration, accept the last valid returned record and require one-over rejection before persistence with the active assignment retained.
- Existing redeployment remains free and authoritative, including destination capacity validation and its established sequence-collision handling. Unassignment does not resequence content.
- Bootstrap retains only the selected AssignmentId, enumerates active content in validated canonical order for the selected room, and shows localized content name plus position/count. It exposes cycle and return actions alongside existing returned-content controls. Unassignment follows redeployment's placement-lock restriction and remains independent of Purchase gating, offline status and pending verification.
- SaveService's general successful-mutation rule invalidates pending renovation undo. Failed unassignment preserves the current undo capability.

## Coverage added and reconciled

`DirectContentUnassignmentTests` adds 31 parameterized/individual cases: all eight production options; exact source/identity/recognized-state preservation; zero-balance success; deterministic serialization/order; same-option Goblin targeting; invalid/missing/repeated/already-returned targets; missing record/policy, unresolved and destructive fixtures; corrupt identity/ownership/sequence/option/geometry; write failure; stale replay before and after movement; exact production custody boundary; free other-room redeployment and full destination rejection; Bootstrap duplicate cycling, room selection, localized feedback and Purchase/placement-lock behavior. The Editor bridge registers `PhaseFourUnassignment` for discovery.

`StructuralEconomyTests` adds one case covering undo retention on write failure and invalidation on successful unassignment. Existing deletion and schema-8 policy tests now expect the approved production loot return. Explicit unresolved fixtures preserve rejection coverage and prove redeployment does not consult current removal policy. The historical monster/trap workload fixture remains unchanged apart from its obsolete explanatory comment.

## Validation actually executed

- Standalone Roslyn core/test compilation: PASS, zero errors; five existing CS0649 warnings for JSON-deserialized `BuildReadinessTests.QualificationAssemblyDefinition` fields.
- Standalone Roslyn player compilation: PASS, zero errors/warnings.
- Standalone Roslyn Editor compilation including the new discovery bridge: PASS, zero errors/warnings.
- Seven non-Unity static checks: 7 passed, 0 failed (exact three policy changes; localization uniqueness; seven new messages; selection placeholders; unchanged prices/structural tuning/limits/save contracts; unique Unity metadata GUID; `git diff --check`).
- An initial compiler response-file assembly attempt failed because two source paths were concatenated without a newline. Correcting the temporary response file resolved the invocation; it was not a source compilation failure.

These are static C# compiles against installed references, not Unity import, executed NUnit tests or a player build. Response files derive from the prior acquisition static-compile inputs, adding the new test source and redirecting the temporary core reference. No Unity-generated project files were edited.

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\NetCoreRuntime\dotnet.exe" "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\DotNetSdkRoslyn\csc.dll" "@$env:TEMP\unassignment-static-core.rsp"
& "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\NetCoreRuntime\dotnet.exe" "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\DotNetSdkRoslyn\csc.dll" "@$env:TEMP\unassignment-static-player.rsp"
& "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\NetCoreRuntime\dotnet.exe" "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\DotNetSdkRoslyn\csc.dll" "@$env:TEMP\unassignment-static-editor.rsp"
```

## Historical provisional qualification status

Complete EditMode passed on 2026-09-14: **844 passed, 0 failed, 0 ignored/skipped, 0 inconclusive**, duration **115.6336255 seconds**. The registered `PhaseFourUnassignment` fixture passed **31/31** and the new renovation-undo integration case passed. CLI exit status: **0**. The owner explicitly approved the exact command once, outside the sandbox with normal user permissions; branch, HEAD, working tree and absence of Unity processes were rechecked immediately before launch. The first Unity invocation for this change was:

```powershell
& "C:\Users\gdg34\AppData\Local\Unity\bin\unity.exe" test "C:\Dev\Dungeon-Lord" --mode EditMode --output "$env:TEMP\phase4_unassignment_editmode.xml"
```

XML: `C:\Users\gdg34\AppData\Local\Temp\phase4_unassignment_editmode.xml`.
SHA-256: `3311B5506264BAAD4F0B400F3063DA8C368BBA1BEC2196C21631F7C12B151FD5`.
Editor log preserved at `C:\Users\gdg34\AppData\Local\Temp\phase4_unassignment_editmode_editor.log`.
Log SHA-256: `257738BA81CD58AA058E92BB1FB100D0DD99DEBD528FBB8679DE142D1896F216`.
The CLI returned no console text. Editor.log confirms test exit code 0 and the qualified 6000.3.2f1 Editor. It contains a startup licensing access-token error followed by successful entitlement/license resolution, an existing empty `Tests 1.asmdef` warning, expected save-delete warnings and the deliberate export-error/exception test, plus shutdown `abort_threads`, debugger-port and MemoryLeaks diagnostics. No failing test or unhandled gameplay exception was reported; no ComputeBuffer diagnostic appeared in this run. The XML contains no error/exception output. These diagnostics are recorded rather than represented as a clean log.

After Unity exited, no Unity PIDs remained and the tracked/untracked working-tree file set matched the pre-run set exactly: no incidental Unity-generated file changes. `git diff --check` passed. Only this evidence record was subsequently updated by the agent; no implementation changes were needed.

Complete PlayMode ran once on 2026-09-14 with separate explicit owner approval for this exact invocation outside the sandbox with normal user permissions:

```powershell
& "C:\Users\gdg34\AppData\Local\Unity\bin\unity.exe" test "C:\Dev\Dungeon-Lord" --mode PlayMode --output "$env:TEMP\phase4_unassignment_playmode.xml"
```

Branch/HEAD/worktree/process checks preceded launch; no Unity processes were running. CLI exit: **0**. XML reports **2,344 passed, 0 failed, 10 ignored/skipped, 0 inconclusive**, total 2,354, duration **110.7968077 seconds**. Aggregate result is `Skipped:Ignored` due to those explicit skips. `DirectContentUnassignmentTests` passed **31/31** in PlayMode discovery. The ten ignored cases are eight synchronous EditMode-only GameRoot fixtures (`gd66.test.synchronous_edit_mode_fixture`), one inverse non-Windows fixture (`gd66.test.windows_only_inverse`), and one Windows Player-only fixture (`gd66.test.windows_player_only`). They do not claim standalone qualification.

XML: `C:\Users\gdg34\AppData\Local\Temp\phase4_unassignment_playmode.xml`.
XML SHA-256: `E4EFCDBC5491BF2572E8C01D68B1C1A234C35AD26BE8878F8A3158FC870F7CCD`.
Preserved Editor log: `C:\Users\gdg34\AppData\Local\Temp\phase4_unassignment_playmode_editor.log`.
Log SHA-256: `886BA29E680196FD4C5868CEB5123C65A3C45551907B10B4D819C660595B6228`.
The CLI emitted no console text. Editor.log records the startup licensing-token error, the existing empty `Tests 1.asmdef` warning, expected save-delete and large-time-delta test warnings, and shutdown thread-abort/debugger-port/MemoryLeaks diagnostics. No unhandled exception, failed assertion, C# compilation error or ComputeBuffer diagnostic was found. Test completion explicitly reports exit code 0.

Unity produced one incidental tracked diff in `ProjectSettings/ProjectSettings.asset`: it reordered `applicationIdentifier` entries so Android precedes Standalone, preserving both `com.gdg3417.dungeonlord` values. The exact diff was inspected and reported; restoration is awaiting explicit owner approval. No automatic restore was performed. Post-run status: 19 modified tracked files and three untracked entries (the test source, metadata, and evidence directory); `git diff --check` passes and no Unity processes remain. No implementation changes were needed; the agent updated only this evidence record after the run.

Manual Editor UAT at 1920x1080 and 1280x720, Windows Development Build, standalone UAT, close/reopen persistence and Player.log review remain pending. Each further Unity CLI command requires a fresh branch/HEAD/worktree/process check and separate narrow one-time approval. No Unity processes have been killed, and no incidental Unity files have been restored.

Unit and SIT apply to ownership, atomic save, lifecycle and redeployment integration. UAT applies to the Bootstrap individual-selection flow. The implementation is available for static review but is not qualified for promotion or external review readiness under the build-promotion policy until required checks pass.

## Historical provisional manual qualification checklist

At both requested resolutions, establish two rooms; acquire duplicate monsters, a trap and each practical loot node. Cycle individual selections and return one duplicate, proving the other remains and mana/geometry/investment stay unchanged. Return the trap and loot node without rewards/refunds. Redeploy the same identities to the other room; reject full/incompatible destinations while retaining custody. Run the dungeon, close/reopen and inspect committed ownership. Repeat the core flow in the Windows Development Build, reopen it, and review Player.log for errors, exceptions, assertions, canonical/save/ownership/localization failures. Historical shutdown-only ComputeBuffer warnings remain a separate known concern unless new evidence ties them to this change.

## Historical draft PR

Title: **Phase 4: Add identity-preserving direct content unassignment**

Players can select an individual monster, trap or reusable loot node in the Bootstrap room controls and return its existing assignment identity to custody without deleting the room. The distinct canonical mutation preserves identity and sequence, costs zero mana, uses authored removal policy and the existing freshness/atomic-save path, and integrates with free redeployment. The three current loot nodes now authorize return to custody; schema remains 9 with no migration.

Adds identity, policy, corruption, custody-boundary, stale-session, persistence, redeployment, localization/UI and undo coverage. Static compilation, seven static checks, EditMode (844 passed) and PlayMode (2,344 passed, ten explicitly ignored, zero failures) are complete; manual Editor/standalone and build qualification remain pending. No online/offline mana earning, generated-loot inventory/tables/equipment/rewards, new wallet/ownership system, schema change, placement multiplicity redesign, final editor, structural tuning, Floor 2/branches, backend features or renderer cleanup is included. Do not merge.
