# Phase 7A5 test and localization inventory

The original inventory below is retained. The owner-UAT correction adds seven domain cases and three actual-scene cases, each of the latter also running through genuine PlayMode wrappers. Exact new domain methods in PhaseSevenA5TransactionalRoomMovement are:

- MoveGuidanceEditGridUsesExactlySelectedAuthoredFloorBounds(0)
- MoveGuidanceEditGridUsesExactlySelectedAuthoredFloorBounds(1)
- MoveGuidanceCustomGridBoundsAndWorkloadAreConfigurationOwned
- MoveGuidanceIsExactlyAuthoritativeOrderedAlternativesWithoutMutation
- MoveGuidanceUsesLastValidDraftProjectionAfterChangesAndInvalidIntent
- MoveGuidanceNoAlternativeHasEmptyResultAndBoundedWork
- MoveGuidanceRefusesOverBudgetEnvelopeBeforeCandidateEnumeration

New actual-scene methods are PhaseSevenA5MoveGuidancePortraitLandscapeAndCurrentDraft, PhaseSevenA5MoveGuidanceNoAlternativeIsLocalized and PhaseSevenA5MoveGuidanceLongTextAndInvalidRecovery. The existing explicit-delete/fresh-boot test now waits explicitly for async scene completion and asserts its disposable filename; its original deletion/recovery assertions remain. [Correction evidence](owner-uat-floor-guidance-correction.md) records all results, localization copy, bounded work, the test-isolation incident and remaining comprehension UAT. No original A5 domain/economy/durability assertion changed.

Final XML reports: `TestResults/phase7a5-final-editmode.xml` and `TestResults/phase7a5-final-playmode.xml`. All names below come from NUnit XML, not source-name guesses.

## Exact unchanged skips

### editmode (1)

- `DungeonBuilder.M0.Editor.Tests.PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed`

### playmode (10)

- `DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.BootstrapDeletionPresentationLocalizesReturnedRemovedAndAllBlockingContentWithoutRawIds`
- `DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.BootstrapRenovationPresentationDisclosesLocalizedMovementReplacementAndCapacityConsequences`
- `DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.StructuralConstructionThroughRealRootPersistsPublishesAndClearsPreview`
- `DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.StructuralDeletionMissingRuntimePolicyFailsClosedThroughRealRoot`
- `DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.StructuralDeletionThroughRealRootPersistsPublishesAndPresents(6,"north",DirectDoorway,"Direct Doorway","")`
- `DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.StructuralDeletionThroughRealRootPersistsPublishesAndPresents(7,"east",PhysicalCorridor,"Straight Stone Corridor","(1,6)")`
- `DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.StructuralReplacementThroughRealRootPersistsPublishesAndReopens(False)`
- `DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.StructuralReplacementThroughRealRootPersistsPublishesAndReopens(True)`
- `DungeonBuilder.M0.Tests.EditMode.Gd66WindowsSpatialMigrationFileSystemTests.CurrentNonWindowsRuntimeFailsClosed`
- `DungeonBuilder.M0.Tests.PlayMode.Gd66WindowsStandaloneQualificationTests.WindowsStandalonePreflightAndNativeFilesystemQualification`

## A5 domain cases (45)

- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.ActualA4V2RecordsRecoverAndUpgradeOnlyThroughExplicitNewRecord` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.BootstrapMovementIsDevelopmentOnlyWhileReplacementAndConstructionRemain` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.CompleteSaveFailurePublishesNoGeometryWalletOrInvestment(Flush)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.CompleteSaveFailurePublishesNoGeometryWalletOrInvestment(Read)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.CompleteSaveFailurePublishesNoGeometryWalletOrInvestment(Replace)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.CompleteSaveFailurePublishesNoGeometryWalletOrInvestment(Write)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.CurrentEconomicConfigurationAndCurrentWalletAreRecalculatedAfterDrafting` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.CurrentWalletExactBalanceAndInsufficientCommitAreAtomic(0)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.CurrentWalletExactBalanceAndInsufficientCommitAreAtomic(10)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.CurrentWalletExactBalanceAndInsufficientCommitAreAtomic(9)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.DiscriminatedPayloadRoundTripsWithoutHiddenFieldMeanings` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.DraftAndDiscardPreserveKnowledgeLifecycleWalletAndActiveRunWhileCommitAffectsOnlyLaterRun` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.EveryMaterialValidationInputIsBoundToRecoveryAndCommit("bounds")` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.EveryMaterialValidationInputIsBoundToRecoveryAndCommit("capacity")` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.EveryMaterialValidationInputIsBoundToRecoveryAndCommit("limits")` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.EveryMaterialValidationInputIsBoundToRecoveryAndCommit("occupancy")` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.EveryMaterialValidationInputIsBoundToRecoveryAndCommit("options")` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.EveryMaterialValidationInputIsBoundToRecoveryAndCommit("orientation")` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.EveryMaterialValidationInputIsBoundToRecoveryAndCommit("sockets")` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.ExpandedPrefixBudgetRefusesBeforeMutationAndKeepsAcknowledgedEvidenceRecoverable` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.ExpandedRecordBindingsRejectContradictionWithoutCanonicalPublication("context")` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.ExpandedRecordBindingsRejectContradictionWithoutCanonicalPublication("downgrade")` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.ExpandedRecordBindingsRejectContradictionWithoutCanonicalPublication("duplicate")` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.ExperimentationPricesOnlyFinalTargetAndOriginRestoresZeroInvestment(False)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.ExperimentationPricesOnlyFinalTargetAndOriginRestoresZeroInvestment(True)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.FailedInvalidPersistenceAllowsCorrectionButOnlyAcknowledgedPrefixCanSave` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.InvalidIntentAcknowledgesRecoversAndCorrectsWithExactStableReason(0,0,"structural.edit.fixed_structure_overlap")` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.InvalidIntentAcknowledgesRecoversAndCorrectsWithExactStableReason(20,20,"structural.edit.out_of_bounds")` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.InvalidIntentFaultMatrixNeverMasqueradesAsLiveAcknowledgement("candidate-after",0)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.InvalidIntentFaultMatrixNeverMasqueradesAsLiveAcknowledgement("candidate-barrier",0)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.InvalidIntentFaultMatrixNeverMasqueradesAsLiveAcknowledgement("candidate-before",0)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.InvalidIntentFaultMatrixNeverMasqueradesAsLiveAcknowledgement("candidate-partial",0)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.InvalidIntentFaultMatrixNeverMasqueradesAsLiveAcknowledgement("commit-after",1)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.InvalidIntentFaultMatrixNeverMasqueradesAsLiveAcknowledgement("commit-barrier",1)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.InvalidIntentFaultMatrixNeverMasqueradesAsLiveAcknowledgement("commit-before",0)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.InvalidIntentFaultMatrixNeverMasqueradesAsLiveAcknowledgement("commit-readback",1)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.InvalidOneFloorBlocksWholeDungeonAndDiscardPreservesRecognizedState` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.MaterialContextDriftFailsClosedAndCanonicalStalenessRemainsDistinct` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.MixedContentAndStructuralHistoryPreservesExactIdentityAndRoomLocalArrangement` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.MovingRoomPreservesRepresentativeMonsterTrapAndLootExactlyThroughCompleteSave` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.SuffixConsequencesAndIndependentTargetsUseUniqueDeterministicFinalBasis(False)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.SuffixConsequencesAndIndependentTargetsUseUniqueDeterministicFinalBasis(True)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.ValidDraftUsesInjectedRoomPriceAndMovementFactorWithoutCanonicalMutation("spatial.room.basic",300,0.25d,75)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.ValidDraftUsesInjectedRoomPriceAndMovementFactorWithoutCanonicalMutation("spatial.room.large_chamber",400,0.25d,100)` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA5TransactionalRoomMovement.ValidDraftUsesInjectedRoomPriceAndMovementFactorWithoutCanonicalMutation("spatial.room.rectangle",320,0.25d,80)` — passed

## Actual-scene cases added for A5 (four, each in EditMode and PlayMode)

- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.PhaseSevenA5ExperimentationOriginRecoveryAndInsufficientManaKeepDraft` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.PhaseSevenA5GraphicalRoomInvalidCorrectionEconomyAndAtomicSave` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.PhaseSevenA5InvalidRecoveryRestoresFootprintReasonAndDiscard` — passed
- `DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.PhaseSevenA5LongLocalizedRoomSheetInvalidReasonAndEconomicsRemainReadable` — passed
- `DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.PhaseSevenA5ExperimentationAndInsufficientMana` — passed
- `DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.PhaseSevenA5InvalidRecovery` — passed
- `DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.PhaseSevenA5LongLocalization` — passed
- `DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.PhaseSevenA5RoomMovementProductionParity` — passed

## Localization ownership

The existing Bootstrap English table owns five new keys. Stable structural/economic reasons continue through existing mappings; no raw IDs or localized strings enter journal authority. The existing `ui.dungeon.commit_review` copy now explains free content positioning and final structural pricing. No package or Japanese translation was added.

```text
{"key": "ui.dungeon.commit_review", "text": "Save all dungeon changes? Structural costs are recalculated using your current mana. Content repositioning is free. Current runs keep their original dungeon."},
{"key": "ui.dungeon.room_move_hint", "text": "Tap the requested anchor for this room. Its required-route descendants move with it."},
{"key": "ui.dungeon.room_movement_consequences", "text": "Final structural changes: {0} rooms moved; {1} connections updated; {2} Completion Terminals moved. Contents keep their room-local arrangement."},
{"key": "ui.dungeon.room_selected", "text": "{0} · Room {1}"},
{"key": "ui.dungeon.room_move_retired", "text": "Move rooms in the Dungeon's graphical Edit Mode."},
{"key": "ui.dungeon.draft.context_stale", "text": "The room movement rules have changed. Discard this draft and start editing again."},
```

The final long-text actual-scene test covers the room sheet, reason, economic review and consequences. Screenshots: `TestResults/phase7a5-screenshots/phase7a5-invalid-long-text.png` and `phase7a5-valid-long-text.png`. They were visually inspected, not treated as strict goldens or manual UAT.
