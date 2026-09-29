# Phase 6A1 schema 11 floor activation implementation report

## Delivery and verified baseline

- Branch: `codex/phase-6a1-schema-11-floor-activation`.
- Final commit SHA: recorded in the final delivery report after committing this evidence file.
- Exact starting baseline: `7b7afa1eabbde2f03255fc0de8fdfe4f0e81d869`, merged PR #214 on `main`; PR #213 and #214 were inspected as merged.
- The branch was created from that exact baseline. No new baseline commits or unrelated initial working-tree changes were incorporated.
- Recommended configuration: GPT-6 Astra, Low. Task classification: Complex, because canonical schema, frozen compatibility, migration, and publication authority carry persistent-state risk. The recommendation follows `Docs/process/AI_Model_Selection_Policy.md`.
- Proposed PR title: **Phase 6A1: Persist schema 11 floor activation authority**.
- Scope: one migration and activation-authority PR. External review follows publication; no owner manual qualification has been performed for this packet.

## Implementation and canonical schema

Schema 11 is the writable target in both schema authorities and native complete-save creation. `SavedSpatialFloor.ActivationState` is the sole persisted writable activation authority. There is no parallel active-floor collection or saved active-floor count.

The finite domain is integer **Active = 1**, **Inactive = 2**. Default zero is invalid. Native schema-11 floors must explicitly contain a valid value. Strict shape/type/domain and exact canonical-round-trip checks reject missing, zero, negative, unknown, null, string, boolean, fractional, overflowing, duplicated, mis-cased, or reordered activation fields.

Schema-11 floor field order:

```text
FloorInstanceId
FloorDefinitionId
FloorIndex
ActivationState
Layout
FixedStructures
RoomContents
```

The frozen schema 7–10 order remains exactly the prior six members, without ActivationState. The version-aware declared-field lookup is used recursively by shape validation, materialization, and serialization. Reflection-contract verification still requires all current serializable fields to match the declared contract and verifies that the frozen order omits exactly the activation member. Frozen in-memory records may carry only the zero sentinel; supplying activation fails frozen validation. That sentinel is never accepted as current activation authority.

A dedicated frozen schema-10 complete-save parser retains schema-10 Phase 5 owners and exact bytes. The 9 → 10 upgrade now validates against that frozen target. Historical 7/8/9 schema tests retain their historical semantics rather than being mechanically renumbered.

## Migration, semantic validation, and publication

The explicit 10 → 11 upgrader:

1. Requires valid frozen schema-10 complete-save bytes, and rejects schema 11 as its source.
2. Traverses the existing lossless contract tree, retaining root and primary extension members and number tokens.
3. Changes the root schemaVersion to 11 and inserts ActivationState:1 immediately after each existing FloorIndex.
4. Preserves floor count, identity, definition, index, order, authority marker/contract, topology, fixed structures, room assignments, lifecycle/custody, investment, corridor assignments, branch knowledge, runtime, research, objective state, and run history. It creates no floor.
5. Produces the same bytes for the same input and does not mutate source bytes.
6. Validates the current complete contract. The load coordinator additionally requires current-target contextual semantics and session opening before persistence.

Migration chains remain 7 → 8 → 9 → 10 → 11, 8 → 9 → 10 → 11, 9 → 10 → 11, and 10 → 11. Recovery deliberately recognizes frozen schema 10.

The existing detached flow remains candidate validation → atomic persistence → exact durable readback → reopen validation → publication. The coordinator's final reread must also equal the candidate before publication. Existing stale-session checks and Windows filesystem authority remain in force; no alternate writer, backend, or concurrency system is added.

A frozen schema-10 record with a missing shallower floor is not repaired: adding Active makes the target violate the prefix invariant, so upgrade preparation fails with no candidate.

## Activation and passive mana

Validation evaluates a sorted view of floors without mutating the supplied state. Every current floor must have a valid finite-domain activation value. FloorIndex 0, when present, must be Active. Active indices must equal the next expected index beginning at zero and cannot occur after an inactive shallower persisted record. Thus missing shallower active records and inactive gaps both fail.

Zero-floor native canonical state remains valid. Inactive records remain representable when their ordinary spatial/lifecycle state is valid. Six Active synthetic records are accepted by the existing serializer workload configuration, demonstrating that no unrelated five-floor lifetime ceiling was invented.

`CanonicalActiveFloorResolver` first validates canonical current state, then counts only Active records. Online and offline rate formulas, efficiency, tick/timestamp behavior, and production tuning are unchanged. One Active plus one Inactive synthetic floor produces exactly one floor contribution on both mana paths. An ordinary migrated one-floor save retains its existing rate. The single-floor route projection refuses inactive Floor 1; full multi-floor snapshots remain deferred.

## Production and configuration audit

The only production/configuration data edit is:

`Assets/_Project/Data/Production/DungeonSpatial/spatial_layout_compatibility_profiles.json`

It adds the schema-11 starter and contract selection using existing geometry and layout contract 1. Its starter hash is `ad0ccc7efcaa7a4cc39a0bd888c0413ea337d91dcb9b3d6ddd01d70541489c80`. All schema 7–10 entries and geometry/migration records compare unchanged to baseline. Release authorization checks retained starter identity, version, contract, and shared geometry, including frozen schema 10.

Runtime source changes are the schema constants, spatial contracts/serializer, complete-save frozen parser, 9 → 10 final validation, new 10 → 11 upgrader, recovery/load coordinator, native creation/initial-floor allocation, compatibility authorization, mana resolver, and single-floor run projection. The complete path inventory below lists every source file.

No content-authoring table, Floor 2 content, economy/research/gameplay tuning, localization, UI action, or ProjectSettings edit is included. Unity reordered two applicationIdentifier keys during PlayMode; that generated ordering change was restored and the final ProjectSettings diff is empty. Current-status documentation was reconciled without changing Phase 6A0 gameplay/tuning decisions.

## Automated coverage added and materially updated

- `PhaseSixAActivationTests.cs`: 38 cases for constants/native empty creation, exact current floor shape, invalid serialization/domain values, inactive Floor 1/run rejection, active-prefix gaps, valid Active/Inactive round trips and both mana paths, serializer workload independence, frozen-10 smuggling rejection, populated lossless migration, determinism/source preservation, write/replace/readback failures, stale sessions, zero/one/three-floor migration, one-floor rate continuity, all frozen chains, and invalid target refusal. `PhaseSixAEditModeFixtures.cs` follows the existing Editor bridge.
- `DetachedSpatialSaveLoadCoordinatorTests.cs`: four schema-10 load/reopen/success/failure cases; schema-8 downgrade fixtures now use frozen floor shape before returning to 11.
- `ProductionSpatialContentBuildGateTests.cs`: geometry-hash authorization rejection independently covers starter schemas 7, 8, 9, 10, and 11. Existing authorization mutation matrix protects retained starter IDs/versions.
- `SpatialLayoutCompatibilityProfileTests.cs`: verifies all five starters/contracts, current schema 11, retained frozen targets, and missing selection via future schema 12.
- `PhaseFourTestSupport.cs`: explicit 10 → 11 continuation and a test-only FrozenTen projection helper.
- `PhaseFiveADurableBranchTests.cs`: intentionally frozen 9 → 10 cases still assert schema 10 and use frozen parsing; schema 7–9 optional-branch rejection is unchanged.
- `DetachedCanonicalSpatialSaveContractTests.cs`, `Gd66CanonicalRuntimeProjectionTests.cs`, `SchemaEightLifecycleOwnershipTests.cs`, and `Gd66SpatialMigrationContractCorrectionTests.cs`: current synthetic floors explicitly provide Active.
- `Gd66DetachedCompleteSaveContractTests.cs`: frozen-7 comparison uses frozen members; current opening follows the full chain.
- `StructuralEconomyTests.cs`: historical downgrade and structural-investment fixtures retain frozen shapes; current sessions continue to 11.
- `Gd66SaveWorkloadMeasurementTests.cs`: current-target workload measurements select the current schema authority; historical schema-6 defaults remain.
- `DetachedCanonicalWriteAuthorityTests.cs`: current writes expect 11; a historical run projection fixture upgrades before entering the current reader.
- Current-target output expectations are updated in `ContentAcquisitionEconomyTests.cs`, `DirectContentUnassignmentTests.cs`, `Gd66WindowsSpatialMigrationFileSystemTests.cs`, `OfflinePassiveManaTests.cs`, `PassiveOnlineManaTests.cs`, `PhaseFiveBBranchIntegrationTests.cs`, `ProductionSpatialContentLoadingTests.cs`, and `ReturnedContentRedeploymentTests.cs`.

The populated migration fixture includes room and corridor content, paid branch investment, returned custody, completed research, a completed Phase 5B run, branch knowledge, and unknown root/primary extensions. Removing only the target schema change and added activation members reconstructs the exact original complete bytes, proving that other owners are retained without projection loss.

## Final automated results and evidence

Unity CLI: 1.0.0-beta.10; Editor: 6000.3.2f1. Tests ran sequentially on the local Windows project.

| Run | Total | Passed | Failed | Skipped | Inconclusive | Test duration |
|---|---:|---:|---:|---:|---:|---:|
| Final focused EditMode | 160 | 160 | 0 | 0 | 0 | 7.6581356 s |
| Complete EditMode | 1117 | 1117 | 0 | 0 | 0 | 136.7687564 s |
| Complete PlayMode | 2615 | 2605 | 0 | 10 | 0 | 134.6008481 s |

The affected-system subset extracted from the final complete EditMode XML passed 783/783. Final focused coverage includes 38 activation cases, four schema-10 load cases, 59 compatibility-profile cases, and 59 build-gate cases.

Local final XML paths:

- `C:\Dev\Dungeon-Lord\TestResults\phase6a1-focused.xml`
- `C:\Dev\Dungeon-Lord\TestResults\phase6a1-full-editmode.xml`
- `C:\Dev\Dungeon-Lord\TestResults\phase6a1-full-playmode.xml`

These generated XML files remain local and are not committed. `phase6a1-affected.xml` records an intermediate exploratory run (777 passed / 1 failed) before its outdated schema-selection expectation was corrected; the final complete suite is the authoritative affected-system verdict.

Commands, using `C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe`:

```powershell
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode EditMode --filter 'PhaseSixAActivation|SchemaTenLoad|PhaseFourCompatibility|ProductionSpatialContentBuildGateTests' --output 'TestResults/phase6a1-focused.xml' --timeout 600 --no-color
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode EditMode --output 'TestResults/phase6a1-full-editmode.xml' --timeout 1200 --no-color
& 'C:/Users/gdg34/AppData/Local/Unity/bin/unity.exe' test 'C:/Dev/Dungeon-Lord' --mode PlayMode --output 'TestResults/phase6a1-full-playmode.xml' --timeout 1200 --no-color
```

All 10 PlayMode skips are expected: eight synchronous EditMode-only GameRoot fixtures, the non-Windows inverse check on Windows, and the Windows Player-only standalone qualification. No unexpected skips or inconclusive results.

Expected skipped cases:

- `Gd66GameRootBootIntegrationTests.BootstrapDeletionPresentationLocalizesReturnedRemovedAndAllBlockingContentWithoutRawIds`
- `Gd66GameRootBootIntegrationTests.BootstrapRenovationPresentationDisclosesLocalizedMovementReplacementAndCapacityConsequences`
- `Gd66GameRootBootIntegrationTests.StructuralConstructionThroughRealRootPersistsPublishesAndClearsPreview`
- `Gd66GameRootBootIntegrationTests.StructuralDeletionMissingRuntimePolicyFailsClosedThroughRealRoot`
- `Gd66GameRootBootIntegrationTests.StructuralDeletionThroughRealRootPersistsPublishesAndPresents` (two cases)
- `Gd66GameRootBootIntegrationTests.StructuralReplacementThroughRealRootPersistsPublishesAndReopens` (two cases)
- `Gd66WindowsSpatialMigrationFileSystemTests.CurrentNonWindowsRuntimeFailsClosed`
- `Gd66WindowsStandaloneQualificationTests.WindowsStandalonePreflightAndNativeFilesystemQualification`

The eight synchronous fixtures use `gd66.test.synchronous_edit_mode_fixture`; the inverse-platform skip uses `gd66.test.windows_only_inverse`; standalone uses `gd66.test.windows_player_only`.

Additional verification:

- Compatibility JSON parses; all five starter SHA-256 hashes verify using the existing empty-CanonicalHash input convention.
- Frozen schema 7–10 starters/contracts and all geometry/migration records compare unchanged to the exact baseline.
- English localization parses with 914 entries and zero duplicate keys. Localization is unchanged.
- `git diff --check` passes.
- Final content-authoring, production economy/bootstrap, and ProjectSettings diffs are empty.
- Windows Editor filesystem/recovery tests are included in the complete suites. No new standalone build or owner manual validation is claimed.

Early compile/fixture failures were resolved before final validation. The first complete EditMode run found two retained-schema-10 authorization regressions; authority was extended to protect all five retained starters, then focused and complete tests were rerun successfully. A sandboxed initial Unity attempt encountered licensing access restrictions and produced no test verdict; subsequent test commands ran with required approval.

## Risks, deviations, and deferred qualification

No gameplay-design contradiction or scope deviation was identified. The compatibility authorization correction is necessary to preserve frozen authority when advancing the current target. Ordinary invalid/default activation and gaps fail deterministically without repair.

Save-risk evidence includes exact full-byte preservation, canonical field ordering, repeated deterministic migration, source immutability, exact durable readback/reopen, refusal of schema-11 migration sources, no new floor creation, rollback on injected write/replace/readback failures, and stale-session rejection with no runtime/activation publication. Existing Windows persistence/recovery restrictions remain unchanged.

No unresolved automated blocker is known. Future Floor 2 production content, construction profile/cost/research permission, activation commands/cascades, floor targeting, multi-floor run transitions/snapshots/settlement, knowledge/depth objectives, new encounters, run-event mana, and speculative supporting architecture are deferred.

External review must determine whether blocking findings remain. If stable, narrow owner qualification remains for existing-save automatic upgrade and normal Floor 1 build/content/run/passive-mana/save/close/reopen/continued play. Windows standalone parity is conditional on external review requiring full persistence qualification. Deterministic schema edge cases and unimplemented Floor 2 workflows are not manual owner requirements.

## Final changed-file inventory

- `Assets/_Project/Data/Production/DungeonSpatial/spatial_layout_compatibility_profiles.json`
- `Assets/_Project/Editor/DungeonSpatial/Tests/PhaseSixAEditModeFixtures.cs`
- `Assets/_Project/Editor/DungeonSpatial/Tests/PhaseSixAEditModeFixtures.cs.meta`
- `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionSpatialContentBuildGateTests.cs`
- `Assets/_Project/Scripts/Economy/CanonicalPassiveManaService.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/CanonicalSaveSchemaVersions.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/CanonicalSpatialSaveContracts.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/CanonicalSpatialSaveSerializer.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalSpatialMutation.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCompleteSaveContract.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedSpatialMigrationTransaction.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedSpatialSaveLoadCoordinator.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/NativeCanonicalSaveCreator.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SchemaNineToTenUpgrade.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SchemaTenToElevenUpgrade.cs`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SchemaTenToElevenUpgrade.cs.meta`
- `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialLayoutCompatibilityProfiles.cs`
- `Assets/_Project/Scripts/Gameplay/MvpDungeonPlacements/CanonicalMvpRouteProjection.cs`
- `Assets/_Project/Scripts/Save/SaveMigration.cs`
- `Assets/_Project/Tests/EditMode/ContentAcquisitionEconomyTests.cs`
- `Assets/_Project/Tests/EditMode/DetachedCanonicalSpatialSaveContractTests.cs`
- `Assets/_Project/Tests/EditMode/DetachedCanonicalWriteAuthorityTests.cs`
- `Assets/_Project/Tests/EditMode/DetachedSpatialSaveLoadCoordinatorTests.cs`
- `Assets/_Project/Tests/EditMode/DirectContentUnassignmentTests.cs`
- `Assets/_Project/Tests/EditMode/Gd66CanonicalRuntimeProjectionTests.cs`
- `Assets/_Project/Tests/EditMode/Gd66DetachedCompleteSaveContractTests.cs`
- `Assets/_Project/Tests/EditMode/Gd66SaveWorkloadMeasurementTests.cs`
- `Assets/_Project/Tests/EditMode/Gd66SpatialMigrationContractCorrectionTests.cs`
- `Assets/_Project/Tests/EditMode/Gd66WindowsSpatialMigrationFileSystemTests.cs`
- `Assets/_Project/Tests/EditMode/OfflinePassiveManaTests.cs`
- `Assets/_Project/Tests/EditMode/PassiveOnlineManaTests.cs`
- `Assets/_Project/Tests/EditMode/PhaseFiveADurableBranchTests.cs`
- `Assets/_Project/Tests/EditMode/PhaseFiveBBranchIntegrationTests.cs`
- `Assets/_Project/Tests/EditMode/PhaseFourTestSupport.cs`
- `Assets/_Project/Tests/EditMode/PhaseSixAActivationTests.cs`
- `Assets/_Project/Tests/EditMode/PhaseSixAActivationTests.cs.meta`
- `Assets/_Project/Tests/EditMode/ProductionSpatialContentLoadingTests.cs`
- `Assets/_Project/Tests/EditMode/ReturnedContentRedeploymentTests.cs`
- `Assets/_Project/Tests/EditMode/SchemaEightLifecycleOwnershipTests.cs`
- `Assets/_Project/Tests/EditMode/SpatialLayoutCompatibilityProfileTests.cs`
- `Assets/_Project/Tests/EditMode/StructuralEconomyTests.cs`
- `Docs/28 - Save_Data_Model_Versioning_and_Migration.md`
- `Docs/38 - Dungeon_Floor_Spatial_Capacity_and_Route_Graph.md`
- `Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md`
- `Docs/testing/evidence/phase6a1-schema11-floor-activation/implementation-evidence.md`
- `README.md`
- `docs/planning/gd63-spatial-and-progression-design-decisions.md`
- `docs/planning/phase-5b-production-tuning-and-run-condition-contract.md`
- `docs/planning/phase-6-multi-floor-foundation-design-and-tuning-lock.md`
- `docs/planning/post-gd60-mvp-execution-plan.md`

Generated local XML evidence is excluded from the 50-file commit.

## Complete proposed PR description

Schema 10 inferred activation from the number of persisted floors. Phase 6A1 makes schema 11 a real persistence boundary: every floor now has one explicit, strictly validated `ActivationState`, and passive mana derives its floor contribution from that authority.

## Baseline and scope

Verified `main` at `7b7afa1eabbde2f03255fc0de8fdfe4f0e81d869`, merged PR #214 after #213. This branch starts at that exact baseline. Recommendation: GPT-6 Astra, Low; classification: Complex because this changes save-schema, frozen compatibility, migration, and publication authority.

This packet implements activation persistence and migration while preserving current Floor 1 editing and Phase 5B runs.

## Save contract and migration

- Writable constants and native complete-save creation target schema 11.
- Floor field order is `FloorInstanceId, FloorDefinitionId, FloorIndex, ActivationState, Layout, FixedStructures, RoomContents`. Active is 1, Inactive is 2; missing, zero, unknown, malformed, duplicate, or noncanonical values fail validation.
- Schemas 7–10 retain the historical six-field floor contract. Version-aware shape validation, materialization, serialization, and exact round trips omit only the new field for frozen contracts. A dedicated frozen schema-10 complete-save path rejects activation smuggling; reflection-contract checks remain strict.
- The explicit 10 → 11 upgrader accepts only valid frozen schema 10, rejects schema 11 sources, changes the root schema target, and inserts Active after FloorIndex on every existing floor. It preserves floor identities, definitions, indices, order, topology, fixed structures, assignments, lifecycle/custody, investment, corridor content, branch knowledge, runtime, research, objectives, run history, authority marker/contract, and root/primary extensions. It creates no floors and uses the lossless contract tree rather than Unity save reconstruction.
- Existing transitions remain sequential: 7 → 8 → 9 → 10 → 11. Identical input produces identical bytes.
- Current-target contextual semantic validation precedes persistence. Existing atomic persistence, exact durable readback, reopened validation, and publication remain the only write path; the load coordinator also checks its final reread against the candidate before publication. Recovery recognizes frozen schema 10.

## Activation, mana, and configuration

Validation is side-effect free. Floor index 0 is Active when present. Active floors must form the contiguous zero-based prefix: no inactive or missing shallower floor may precede a deeper Active floor. Empty native saves remain valid, and structurally valid inactive records remain representable. No unrelated five-floor serializer ceiling is added.

`CanonicalActiveFloorResolver` counts only validated Active records. Online/offline formulas and tuning are unchanged, and migrated ordinary Floor 1 saves retain their contribution. The existing single-floor route projection rejects inactive Floor 1.

The only production configuration edit adds the schema-11 starter and contract selection in `spatial_layout_compatibility_profiles.json`. Geometry, migration records, and schema 7–10 entries are unchanged. Release authorization protects identity, version, contract, and shared geometry across all retained starters.

## Files and automated coverage

Runtime changes cover schema constants, spatial contracts/serializer, frozen complete-save parsing, upgrade/load/recovery/native creation, initial-floor allocation, compatibility selection, mana resolution, and the single-floor run guard. Current-status documentation is reconciled; Phase 6A0 gameplay/tuning decisions remain locked.

Added 38 activation/migration/mana/determinism/failure test cases with an EditMode bridge and four schema-10 load/reopen/failure cases. Updated current-target fixtures deliberately while retaining historical 7–10 expectations. Compatibility build-gate coverage now checks geometry mismatch independently for all five schema starters. Existing structural economy, custody, branches, Phase 5B runs, save lifecycle, and Windows filesystem regressions are included in the complete suites.

Final validation:

| Suite | Total | Passed | Failed | Skipped | Inconclusive |
|---|---:|---:|---:|---:|---:|
| Focused EditMode | 160 | 160 | 0 | 0 | 0 |
| Complete EditMode | 1117 | 1117 | 0 | 0 | 0 |
| Complete PlayMode | 2615 | 2605 | 0 | 10 | 0 |

The affected-system subset in the final complete EditMode run passed 783/783. All 10 PlayMode skips are expected: eight synchronous EditMode-only GameRoot fixtures, the non-Windows inverse check on Windows, and the Windows Player-only standalone qualification. No unexpected skips or inconclusive results.

Compatibility JSON parses and all five starter hashes verify; frozen production entries compare unchanged to baseline. English localization parses with 914 entries and zero duplicate keys; no localization edits. `git diff --check` passes. Detailed test names, local XML evidence paths, scope audit, changed-file inventory, and implementation report are in `Docs/testing/evidence/phase6a1-schema11-floor-activation/implementation-evidence.md`.

## Risks, limits, and owner qualification

Deterministic tests cover exact lossless migration, malformed activation, active-prefix gaps, stale sessions, failed writes/replacements/durable readback, source/disk preservation on tested rollback paths, no live publication on failure, and exact reopen/readback. Exploratory failures in outdated current-target fixtures and compatibility authorization were corrected; final suites above are authoritative.

No Floor 2 content/construction/profile/shell cost, lifecycle commands, floor targeting, multi-floor run snapshots/transitions/settlement, new encounters, run-event mana, backend/concurrency architecture, or speculative deferred infrastructure is introduced. No gameplay tuning, UI text, content authoring, or ProjectSettings change.

No unresolved automated blocker is known. This is an automated-validation result, not owner manual or new Windows standalone qualification. External review comes next. If review finds the code stable, narrow owner qualification remains: existing-save upgrade and ordinary Floor 1 build/place/run/mana/save/close/reopen play; Windows standalone parity only if external review requires full persistence qualification. No manual schema-edge-case reproduction or Floor 2 workflow qualification is requested.
