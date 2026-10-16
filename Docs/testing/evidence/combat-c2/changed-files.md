# Complete Combat C2 changed-file list

Relative to `0624bc0bbbd34a8ceff26d764d7f35c3b464d8bc`. No owner save, retained player or qualified historical checkpoint is changed.

| File | Purpose |
| --- | --- |
| `.gitattributes` | Preserve mandatory LF framing for the new frozen schema-14 limits on future checkouts. |
| `Assets/_Project/Data/Bootstrap/string_table_en.json` | Publish eight localized C2 status, reserve, shortage and recovery strings. |
| `Assets/_Project/Data/Production/DungeonSpatial/spatial_layout_compatibility_profiles.json` | Append sorted schema-15 starter/contract entries without changing historical entries. |
| `Assets/_Project/Data/Production/Save/save_spatial_migration_limits.json` | Publish explicit schema-15 whole/raw/parsed workload budgets. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/CombatC0ProductionEvidenceSceneTests.cs` | Expect the explicit new manual executable and recorded initial HP. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/CombatC1ReplaySceneTests.cs` | Use casualty test inputs compatible with unchanged persistent monster health. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/CombatC2EditModeFixtures.cs` | Expose focused C2 cases through the established EditMode bridge. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/CombatC2EditModeFixtures.cs.meta` | Unity GUID/import metadata for the adjacent new C2 source or configuration asset. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/CombatC2ProductionSceneTests.cs` | Two production manual runs consume committed individual starts; replay remains read-only. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/CombatC2ProductionSceneTests.cs.meta` | Unity GUID/import metadata for the adjacent new C2 source or configuration asset. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/PhaseSevenA4ProductionSceneTests.cs` | Fund test spending plus reserve and assert schema 15. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonReplaySceneTests.cs` | Account for post-run recovery debit without changing transient replay assertions. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/ProductionDungeonRoomContentSceneTests.cs` | Retain room identity assertions with appended localized HP information. |
| `Assets/_Project/Editor/DungeonSpatial/Tests/RoomContentPositionMigrationProfileTests.cs` | Preserve explicit position-migration behavior across the current schema chain. |
| `Assets/_Project/Editor/DungeonVisualBuildQualification.cs` | Write a unique C2 Windows player and retain every prior build. |
| `Assets/_Project/Resources/monster_recovery.json` | Own approved level 1, readiness 66/100 and one mana per whole HP. |
| `Assets/_Project/Resources/monster_recovery.json.meta` | Unity GUID/import metadata for the adjacent new C2 source or configuration asset. |
| `Assets/_Project/Resources/save_spatial_migration_limits_schema14.json` | Freeze the baseline production schema-14 budget bytes. |
| `Assets/_Project/Resources/save_spatial_migration_limits_schema14.json.meta` | Unity GUID/import metadata for the adjacent new C2 source or configuration asset. |
| `Assets/_Project/Scripts/Economy/CanonicalOfflinePassiveManaService.cs` | Expose recovery spending separately from the unchanged passive grant. |
| `Assets/_Project/Scripts/Economy/StructuralEconomyService.cs` | Carry derived reserve in previews without changing price/refund formulas. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/CanonicalSaveSchemaVersions.cs` | Advance only the writable target to schema 15. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/CanonicalSpatialSaveContracts.cs` | Retain explicit frozen schema-14 validation. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/CanonicalSpatialSaveSerializer.cs` | Select nested frozen/current serialization budgets. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalSaveSession.cs` | Append/reconcile continuity through the existing complete-save session. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCanonicalWriteAuthority.cs` | Settle injuries/recovery/debits atomically; enforce reserve and proven vacancy. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedCompleteSaveContract.cs` | Read/validate ninth owner and preserve B2 historical base/checkpoint quotas. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedSpatialMigrationTransaction.cs` | Recognize frozen schema 14 and validate current recovery with injected rules. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/DetachedSpatialSaveLoadCoordinator.cs` | Sequential migration to schema 15 and collision rejection. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/FloorLifecycleAuthority.cs` | Preview activation affordability against resulting global obligation. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/LivingDungeonScheduling.cs` | Allow recovery at existing controlled closed B2 boundaries; no activation. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/MonsterContinuity.cs` | Strict individual HP/level/revival owner, configured readiness, reserve and deterministic recovery. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/MonsterContinuity.cs.meta` | Unity GUID/import metadata for the adjacent new C2 source or configuration asset. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/NativeCanonicalSaveCreator.cs` | Initialize the ninth owner in native saves. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/RawSavePayloadClassifier.cs` | Recognize the new owner and scope expanded arrays to its Records only. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SaveSpatialMigrationLimits.cs` | Load current limits plus frozen schema-14/schema-13 profiles. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SchemaFourteenToFifteenUpgrade.cs` | Explicit detached healthy schema-14 to schema-15 migration. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SchemaFourteenToFifteenUpgrade.cs.meta` | Unity GUID/import metadata for the adjacent new C2 source or configuration asset. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SchemaThirteenToFourteenUpgrade.cs` | Keep this historical step explicitly pinned to frozen schema 14. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/SpatialLayoutCompatibilityProfiles.cs` | Publish schema-15 native starter and contract selection. |
| `Assets/_Project/Scripts/Gameplay/DungeonSpatial/TransactionalDungeonDraftCommit.cs` | Route discretionary draft spending through protected-reserve enforcement. |
| `Assets/_Project/Scripts/Gameplay/MvpDungeonPlacements/CanonicalMvpRouteProjection.cs` | Publish validated continuity with the canonical runtime projection. |
| `Assets/_Project/Scripts/Gameplay/RunSimulation/ActiveFloorRunSnapshot.cs` | Capture immutable individual starts without changing B1 checkpoints. |
| `Assets/_Project/Scripts/Gameplay/RunSimulation/CombatC0Evidence.cs` | Distinguish initial/final HP and revival and version C2 evidence. |
| `Assets/_Project/Scripts/Gameplay/RunSimulation/CombatC0ExecutionState.cs` | Supply optional C2 starts and recognize explicit C0/C2 evidence. |
| `Assets/_Project/Scripts/Gameplay/RunSimulation/CombatC0Kernel.cs` | Use individual HP/eligibility for C2 while retaining historical C0 execution. |
| `Assets/_Project/Scripts/Gameplay/RunSimulation/RunSimulationService.CombatC0.cs` | Exclude defeated/revival-pending monsters from engagements. |
| `Assets/_Project/Scripts/Gameplay/RunSimulation/RunSimulationService.Execution.cs` | Select persistent C2 manual inputs separately from B1 and historical C0. |
| `Assets/_Project/Scripts/Services/OfflinePassiveManaPresenter.cs` | Localize separate recovery debit and published wallet. |
| `Assets/_Project/Scripts/Services/ProductionDungeonPresenter.cs` | Present localized individual status and derived total/reserve/usable mana. |
| `Assets/_Project/Scripts/Services/SaveService.cs` | Inject rules, migrate/create schema 15, protect previews and prevent healing refunds. |
| `Assets/_Project/Scripts/Services/StructuralEconomyPresenter.cs` | Present the localized reserve-shortage reason. |
| `Assets/_Project/Scripts/UI/DungeonCombatReplay.cs` | Initialize recorded individual starting HP/revival and validate C2 events. |
| `Assets/_Project/Scripts/UI/DungeonFloorWorldReplay.cs` | Reuse existing defeat treatment for inactive partially revived actors. |
| `Assets/_Project/Scripts/UI/ProductionDungeonComposition.cs` | Add localized individual state to existing inspection. |
| `Assets/_Project/Scripts/UI/ProductionDungeonController.cs` | Use existing mana tooltip for derived reserve/shortage. |
| `Assets/_Project/Scripts/UI/ProductionDungeonReplay.cs` | Show recorded starting HP and revival through existing replay tooltips. |
| `Assets/_Project/Scripts/UI/ProductionDungeonRoomContent.cs` | Show retained individual state in returned-content choices. |
| `Assets/_Project/Scripts/Util/Models.cs` | Add only the nonserialized canonical continuity runtime projection. |
| `Assets/_Project/Tests/EditMode/CombatC0IntegrationTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/CombatC2Tests.cs` | Focused schema, identity, reserve, recovery, combat, replay and Windows atomic-failure cases. |
| `Assets/_Project/Tests/EditMode/CombatC2Tests.cs.meta` | Unity GUID/import metadata for the adjacent new C2 source or configuration asset. |
| `Assets/_Project/Tests/EditMode/ContentAcquisitionEconomyTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/DetachedCanonicalSpatialSaveContractTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/DetachedCanonicalWriteAuthorityTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/DetachedSpatialSaveLoadCoordinatorTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/DirectContentUnassignmentTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/Gd66DetachedCompleteSaveContractTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/Gd66DetachedSpatialMigrationTransactionTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/Gd66SaveWorkloadMeasurementTests.cs` | Measure new owner and recovery at the maximum valid production envelope. |
| `Assets/_Project/Tests/EditMode/Gd66WindowsSpatialMigrationFileSystemTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/LivingDungeonPhaseB2Tests.cs` | Prove occupied-floor funding waits for established terminal vacancy. |
| `Assets/_Project/Tests/EditMode/OfflinePassiveManaTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/PassiveOnlineManaTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/PhaseFiveBBranchIntegrationTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/PhaseFourTestSupport.cs` | Provide explicit frozen-14 fixtures and sequential current-schema upgrade helpers. |
| `Assets/_Project/Tests/EditMode/PhaseSevenA3IntraroomTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/PhaseSevenA4DraftDurabilityTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/PhaseSevenA4TransactionalEditorTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/PhaseSevenA5TransactionalRoomMovementTests.cs` | Retain exact spending/atomicity checks above required deployed reserve. |
| `Assets/_Project/Tests/EditMode/PhaseSixAActivationTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/ProductionSpatialContentLoadingTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/ReturnedContentRedeploymentTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/EditMode/SaveSpatialMigrationLimitsTests.cs` | Assert schema-15/frozen budget chains and prove malformed replacements change input. |
| `Assets/_Project/Tests/EditMode/SpatialLayoutCompatibilityProfileTests.cs` | Retain historical contracts, add 15 and use unsupported 16 only in missing-contract tests. |
| `Assets/_Project/Tests/EditMode/StructuralEconomyTests.cs` | Prove recovery debits cannot become refundable renovation investment. |
| `Assets/_Project/Tests/EditMode/TransactionalRoomConstructionTests.cs` | Retain structural price/atomicity checks while funding deployed reserve. |
| `Assets/_Project/Tests/EditMode/TransactionalRoomContentTests.cs` | Update current-schema/native-chain or reserve-funded fixture expectations; retain original regression assertions. |
| `Assets/_Project/Tests/PlayMode/CombatC2PlayModeTests.cs` | Verify frame delays create no healing and committed partial revival survives reopening. |
| `Assets/_Project/Tests/PlayMode/CombatC2PlayModeTests.cs.meta` | Unity GUID/import metadata for the adjacent new C2 source or configuration asset. |
| `Assets/_Project/Tests/PlayMode/PhaseSevenA4ProductionShellPlayModeTests.cs` | Expose the new C2 production scene check in PlayMode. |
| `Docs/Cross_Spec_Glossary_of_Invariants_UPDATED.md` | Record approved C2 authority/continuity status without rewriting historical invariants. |
| `Docs/testing/evidence/combat-c2/changed-files.md` | Complete per-file change inventory for review. |
| `Docs/testing/evidence/combat-c2/implementation-and-qualification.md` | Document contracts, choices, evidence, preservation and owner retest. |
| `Docs/testing/evidence/combat-c2/qualification-manifest.json` | Machine-readable actual verdicts, commands, hashes and final input applicability. |
| `Docs/testing/evidence/combat-c2/qualification-reports.zip` | Preserve actual XML and hash-audit JSON, including superseded failures. |
| `Tools/Presentation/collect_combat_c2_evidence.py` | Collect actual XML/audits/hashes without exporting raw licensing command lines. |
| `Tools/Presentation/qualify_combat_c2.py` | Reuse r2 with isolated namespace, exact input comparison and protected-file audits. |
| `Tools/Presentation/run_combat_c2_qualification.ps1` | Record exact staged CLI arguments, exits and verdicts; stop on failure. |
| `docs/planning/post-gd60-mvp-execution-plan.md` | Record C2 implementation/review status without rewriting historical evidence. |
