# Phase 4 content acquisition: static review evidence

Baseline: `c1c7b2ceb8dcc628d6e0030b9996ffc28498c3fb` (merged and qualified PR #202).
Branch: `phase-4-content-acquisition-economy`.

## Scope and authority

`Assets/_Project/Resources/content_acquisition_economy.json` owns StartingMana 40 and the eight approved prices: Skeleton 25, Goblin 20, Spike 20, Snare 15, Chilling Sigil 20, Basic Loot 15, Hidden Cache 10, Glittering Hoard 25. Runtime consumes a validated immutable snapshot. Structural capacity remains owned by `structural_economy.json`; structural tuning, investment rules, save fields, schema 9, migration paths and production workload limits are unchanged.

Only GameRoot's player NEW `Place` call reaches acquisition charging. Pure spatial preparation and migration reconstruction do not spend mana. Purchases and zero-cost returned-content redeployment use the same detached complete-save replacement/validation/atomic-persistence/readback boundary. A current-session check rejects old purchase/redeployment sessions, including candidate-identical retries. Native creation initializes starting mana on a detached input copy; existing-save load does not invoke initialization.

## Historical initial non-Unity static review

- Python JSON/static checks passed: exact approved values/coverage/categories, no duplicate keys/IDs, ordinal authoring order, StartingMana within existing capacity, localization coverage/placeholders, owned/free reuse label, new asset metadata, and no approved tuning literals in new runtime acquisition classes.
- Source/diff inspection passed: the deduction reads current `StructureRuntimeState.ManaReserve`, remains detached until durable persistence succeeds, and never adds content spending to investment. Existing implicit starter geometry still receives its required zero structural basis. No-op/rejection returns before publication. Schema and migration files, structural tuning and production workload configuration have no changes.
- Standalone Roslyn compilation passed for core with Editor/test defines (269 source files), core with Editor/test defines removed (269 inputs; guarded tests excluded), and Editor assembly including the NUnit fixture bridge (10 source files). This is C# compilation with installed reference assemblies, not Unity compilation/import or a player build. Core emitted five pre-existing CS0649 warnings for JSON-deserialized `BuildReadinessTests.QualificationAssemblyDefinition` fields. Player and Editor compiles emitted no warnings/errors.
- `git diff --check` passed with the repository's normal line-ending configuration. Complete runtime, configuration, localization, fixture and planning diffs were inspected before commit.

Compiler invocation pattern (response files were generated in TEMP from existing ignored project references/source lists, with the three new core sources appended and the Editor reference redirected to the temporary compiled core):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\NetCoreRuntime\dotnet.exe" "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\DotNetSdkRoslyn\csc.dll" "@$env:TEMP\acquisition-static-core.rsp"
& "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\NetCoreRuntime\dotnet.exe" "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\DotNetSdkRoslyn\csc.dll" "@$env:TEMP\acquisition-static-player.rsp"
& "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\NetCoreRuntime\dotnet.exe" "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Data\DotNetSdkRoslyn\csc.dll" "@$env:TEMP\acquisition-static-editor.rsp"
```

## Historical initial coverage before Unity execution

The initial `ContentAcquisitionEconomyTests` packet added 55 NUnit cases, registered through `PhaseFourAcquisition` for EditMode discovery and the existing core test route. Coverage includes all production prices/starting mana, malformed/missing/duplicate/unknown/category/nonfinite configuration, bounded parsing, immutable ordinal snapshots, injected prices and starting mana, affordability/exact balance, commit-time balance recheck, no-op/full/invalid/unavailable/stale/snapshot/persistence rejection, fresh ownership/custody separation, durable purchase reopen, free identity-preserving redeployment/rejection/retry, investment separation, fresh native schema-9 saves, existing 0/37 balances without grants, migration balance preservation and localization resolution/presentation.

Existing fixtures inject the acquisition snapshot. QA-wallet fixtures retain explicit fake starting balances. The custody stress fixture uses explicitly test-owned zero acquisition prices to isolate its existing exact workload boundaries; no assertion or production limit was weakened. Owned identity, custody, atomicity and workload assertions remain; later monster multiplicity coverage supersedes the old same-option no-op assumption.

At the time of initial static review, Unit/EditMode, SIT/PlayMode, manual Bootstrap/UAT and build qualification were pending external review and later authorized Unity execution. No tests or Unity invocation were claimed at that stage. This historical status and the initial 55-case count are superseded by final qualification below.

## Historical initial planning reconciliation

The Phase 4A economy contract, post-GD60 execution plan and cross-spec invariant glossary initially identified PRs #201/#202 as merged/qualified and this acquisition/starting-mana packet as pending qualification; final reconciliation below supersedes that historical status. Online mana earning remains later; offline mana follows meaningful canonical online production authority. Non-goals remain earning/offline mana, Mana Farm/storage/research progression, structural tuning/refunds, floors/branches/geometry, shop/inventory/editor frameworks, loot-removal policy, broad runtime refactors, backend services and schema 10.

## Final qualification closeout

PR #203 is qualified but not yet merged. PR #201 structural economy and PR #202 returned-content redeployment are merged/qualified. Baseline: `c1c7b2ceb8dcc628d6e0030b9996ffc28498c3fb`. Final qualified production implementation: `e599857477bbf09fbf54c25f4359519d6c3a508f`. Any later closeout commit is documentation/evidence only. Results below are the accepted final qualification authority; no Unity execution occurred during documentation closeout.

- Static core/player/editor compilation passed with no compilation errors. Historical static-review CS0649 fixture warnings above are retained honestly.
- Unity 6000.3.2f1 full EditMode: 812 total, 812 passed, 0 failed. Full PlayMode: 2322 total, 2312 passed, 0 failed, 10 existing ignored. Final ContentAcquisitionEconomyTests discovery contains 58 cases, plus GameRoot/Bootstrap Purchase-gate and multiplicity integration coverage.
- Windows Development Build: Succeeded, StandaloneWindows64, Development Build true, `Assets/_Project/Scenes/Bootstrap.unity` included, output `Builds/Development/Windows/Dungeon Lord.exe`. Errors 0; warnings 1 (unavailable Unity Cloud credentials for native-symbol upload); total size 170,402,997 bytes.
- Manual Editor UAT passed: fresh StartingMana 40; all eight approved prices; offline and verification-pending rejection without mutation; online and exact-balance acquisition; insufficient funds with no change; reopen at 0 without starting-mana regrant; free offline owned-content redeployment; 1280x720 presentation and ordinary gameplay smoke. Two separately charged Skeletons occupy Basic Room at Monsters 2/2, the third is capacity-rejected, duplicates survive full Unity close/reopen, and returned same-option monsters redeploy beside active monsters while capacity remains. No new Console errors.
- Standalone UAT passed: fresh 40 mana, Skeleton price 25, offline purchase blocked without spending, online Skeleton 40 -> 15, Basic Loot Node 15 -> 0. Close/reopen preserved mana 0 and both assignments. No raw localization/content IDs were visible; run/observe dungeon completed; all requested standalone steps passed.
- Final standalone Player.log: `C:\Users\gdg34\AppData\LocalLow\gdg3417\Dungeon Lord\Player.log`, modified 2026-09-13 20:29:21 CDT / 2026-09-14 01:29:21 UTC, corresponding to the final Development executable session. Review found no Error/Exception/Assert entries, save/load/write/reopen, canonical validation, migration, acquisition configuration, localization, Purchase-gate or persistence/recovery failures.

### Final behavior and invariants

NEW paid acquisition evaluates the existing RestrictedActionType.Purchase gate using current GameRoot.IsOnline and VerificationPending before canonical mutation; offline/pending attempts are blocked without state changes. Returned owned-content redeployment is not a new purchase and remains outside that gate. Offline returns localized `gate.error.offline_required`; pending verification returns `gate.error.verification_pending`, retaining offline precedence when both apply. Blocked attempts leave mana, ownership, NextSequence, returned custody and persisted bytes unchanged without a canonical write attempt.

Repeated MONSTER acquisition is allowed while MonsterCapacity remains: every successful acquisition charges again and creates distinct AssignmentId/sequence; capacity rejection charges zero. Stale-session protection is separate from intentional current-session repeated acquisition. Trap/loot same-option live placement remains unchanged pending explicit approval. Each successful monster purchase advances NextSequence once with deterministic canonical assignment ordering. Same-option monster redeployment uses remaining capacity, preserves owned identity, consumes custody once and leaves mana unchanged. Stale pre-purchase sessions cannot charge, duplicate ownership or advance counters. Save/reopen, canonical/run-input projection and localized room composition preserve duplicate monster multiplicity; migration preserves generic option multiplicity/order.

StartingMana 40 and all eight prices remain tunable authored data in the sole acquisition configuration above. The existing complete-save validation/reopen/atomic persistence/readback boundary commits mana and fresh ownership together before runtime/session publication. Rejected/no-op operations charge nothing; failed persistence does not publish changes. NEW acquisition does not consume returned custody or alter structural investment. Schema 9 -> 9, existing-save balances (including 0 and 37), migration, structural tuning and workload limits remain unchanged. Online mana earning is deferred; offline mana follows meaningful canonical online production-rate authority.

### Accepted non-blocking limitation and follow-up

Shutdown reports `GarbageCollector disposing of ComputeBuffer. Please use ComputeBuffer.Release() or .Dispose() to manually release the buffer.` and MemoryLeaks telemetry of 74,343 bytes. The same warning appears in the previous standalone session. Repository search found no direct ComputeBuffer or GraphicsBuffer use; PR #203 does not modify rendering/resource-management code. Ownership remains unattributed. Acceptance is not proof that no leak exists; the warning is not fixed. This is a performance/resource-cleanup follow-up outside this packet. `d3d12: failed to query info queue interface (0x80004002)` was followed by successful graphics initialization/gameplay and is also accepted as non-blocking.

No rendering/URP/TMP correction or direct content unassignment is implemented. Direct unassignment/reconfiguration remains a separate follow-up: reusable monsters/traps should preserve AssignmentId through returned custody; loot disposition requires explicit gameplay/policy approval. Original non-goals remain unchanged. Purchase-gate review thread `PRRT_kwDOQxUKE86h7x5C` is ready for external resolution based on production, automated integration, manual Editor and standalone evidence; this closeout does not resolve it.
