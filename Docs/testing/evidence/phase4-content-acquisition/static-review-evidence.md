# Phase 4 content acquisition: static review evidence

Baseline: `c1c7b2ceb8dcc628d6e0030b9996ffc28498c3fb` (merged and qualified PR #202).
Branch: `phase-4-content-acquisition-economy`.

## Scope and authority

`Assets/_Project/Resources/content_acquisition_economy.json` owns StartingMana 40 and the eight approved prices: Skeleton 25, Goblin 20, Spike 20, Snare 15, Chilling Sigil 20, Basic Loot 15, Hidden Cache 10, Glittering Hoard 25. Runtime consumes a validated immutable snapshot. Structural capacity remains owned by `structural_economy.json`; structural tuning, investment rules, save fields, schema 9, migration paths and production workload limits are unchanged.

Only GameRoot's player NEW `Place` call reaches acquisition charging. Pure spatial preparation and migration reconstruction do not spend mana. Purchases and zero-cost returned-content redeployment use the same detached complete-save replacement/validation/atomic-persistence/readback boundary. A current-session check rejects old purchase/redeployment sessions, including candidate-identical retries. Native creation initializes starting mana on a detached input copy; existing-save load does not invoke initialization.

## Non-Unity validation actually run

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

## Automated coverage authored; execution pending

`ContentAcquisitionEconomyTests` adds 55 NUnit cases, registered through `PhaseFourAcquisition` for EditMode discovery and the existing core test route. Coverage includes all production prices/starting mana, malformed/missing/duplicate/unknown/category/nonfinite configuration, bounded parsing, immutable ordinal snapshots, injected prices and starting mana, affordability/exact balance, commit-time balance recheck, no-op/full/invalid/unavailable/stale/snapshot/persistence rejection, fresh ownership/custody separation, durable purchase reopen, free identity-preserving redeployment/rejection/retry, investment separation, fresh native schema-9 saves, existing 0/37 balances without grants, migration balance preservation and localization resolution/presentation.

Existing fixtures inject the acquisition snapshot. QA-wallet fixtures retain explicit fake starting balances. The custody stress fixture uses explicitly test-owned zero acquisition prices to isolate its existing exact workload boundaries; no assertion or production limit was weakened. PR #202 redeployment assertions are retained.

Unit/EditMode, SIT/PlayMode, manual Bootstrap/UAT and any required build qualification are pending external code review and later authorized Unity qualification. No tests are claimed as executed or passed. No Unity executable, Unity CLI command, EditMode run, PlayMode run or Unity build was launched.

## Planning reconciled

The Phase 4A economy contract, post-GD60 execution plan and cross-spec invariant glossary now identify PRs #201/#202 as merged/qualified and this acquisition/starting-mana packet as pending qualification. Online mana earning remains later; offline mana follows meaningful canonical online production authority. Non-goals remain earning/offline mana, Mana Farm/storage/research progression, structural tuning/refunds, floors/branches/geometry, shop/inventory/editor frameworks, loot-removal policy, broad runtime refactors, backend services and schema 10.
