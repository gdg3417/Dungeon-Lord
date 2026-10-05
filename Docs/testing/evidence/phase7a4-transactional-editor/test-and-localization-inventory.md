# Phase 7A4 exact test and localization inventory

EditMode adds 67 cases: 37 domain/presenter/storage probes, 23 durability cases and 7 actual-scene cases. Genuine PlayMode adds eight shell scenarios; its full suite also discovers the 60 domain/durability cases. Existing production-content consumer coverage was extended without removing its authority restrictions.

## EditMode cases

- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_ContradictoryOrIncompleteCommitChainFailsClosed("missing-predecessor-commit") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_ContradictoryOrIncompleteCommitChainFailsClosed("missing-predecessor-candidate") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_ContradictoryOrIncompleteCommitChainFailsClosed("wrong-predecessor") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_ContradictoryOrIncompleteCommitChainFailsClosed("wrong-hash") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_ContradictoryOrIncompleteCommitChainFailsClosed("wrong-identity") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_ContradictoryOrIncompleteCommitChainFailsClosed("malformed-commit") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_ContradictoryOrIncompleteCommitChainFailsClosed("duplicate-field") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_ContradictoryOrIncompleteCommitChainFailsClosed("conflicting-session") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_FailedDiscardBlocksLiveSaveAndRecoveryUsesRemainingCommitEvidence("delete-before",1) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_FailedDiscardBlocksLiveSaveAndRecoveryUsesRemainingCommitEvidence("delete-after",0) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_FailedDiscardBlocksLiveSaveAndRecoveryUsesRemainingCommitEvidence("delete-barrier",-1) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_InitialUnknownOutcomeRecoversOnlyWithCommitOrOffersExplicitDiscard(False) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_InitialUnknownOutcomeRecoversOnlyWithCommitOrOffersExplicitDiscard(True) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_StaleBaselineRejectsCommittedEvidenceAndMaximumJournalRecovers — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_SuccessHasIndependentCommitChainAndCanonicalCommitAfterRecovery — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_UnknownOutcomeResolvedOnlyByCommitEvidence("candidate-before",0) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_UnknownOutcomeResolvedOnlyByCommitEvidence("candidate-partial",0) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_UnknownOutcomeResolvedOnlyByCommitEvidence("candidate-after",0) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_UnknownOutcomeResolvedOnlyByCommitEvidence("candidate-barrier",0) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_UnknownOutcomeResolvedOnlyByCommitEvidence("commit-before",0) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_UnknownOutcomeResolvedOnlyByCommitEvidence("commit-after",1) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_UnknownOutcomeResolvedOnlyByCommitEvidence("commit-barrier",1) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4DraftDurability.DurabilityMatrix_UnknownOutcomeResolvedOnlyByCommitEvidence("commit-readback",1) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.ActualSceneNormalEditMoveInvalidSaveDiscardAndTextModes — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.LongerLocalizationWrapsWithoutLosingCriticalActions — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.RepresentativeLayoutsKeepChromeInsideSafeRootAndCaptureEvidence — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.ActiveSnapshotIsolatedWhileLaterRunUsesCommittedPosition — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.BaselineDeterminismRecoveryRejectsStaleCanonicalWithoutRebase — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.CanonicalPersistenceFailureAppliesNothingAndRetainsDurableDraft(Write) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.CanonicalPersistenceFailureAppliesNothingAndRetainsDurableDraft(Replace) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.CanonicalPersistenceFailureAppliesNothingAndRetainsDurableDraft(Flush) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.CommitCleanupFailureLeavesStaleDraftThatCannotReapply — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.DiscardFailureNeverReportsDurableDiscardAndNeverChangesCanonical — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.DraftStorageEvidenceProbe_SuccessAndExceptionCanLeaveIdenticalFiles(Write) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.DraftStorageEvidenceProbe_SuccessAndExceptionCanLeaveIdenticalFiles(Replace) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.DraftStorageEvidenceProbe_SuccessAndExceptionCanLeaveIdenticalFiles(Move) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.DraftStorageEvidenceProbe_SuccessAndExceptionCanLeaveIdenticalFiles(Delete) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.DraftStorageEvidenceProbe_SuccessAndExceptionCanLeaveIdenticalFiles(Flush) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.FailedPredecessorBlocksSaveAndLaterCommandsUntilRetry — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.FinalValidationAndStaleSessionFailuresApplyNothing — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.HitTargetsConvertPhysicalUnitsAndActualPanelScale(Android,320,96) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.HitTargetsConvertPhysicalUnitsAndActualPanelScale(IOS,326,88) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.InvalidTargetDoesNotMutateDraftOrCanonical(-1,0) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.InvalidTargetDoesNotMutateDraftOrCanonical(4,0) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.InvalidTargetDoesNotMutateDraftOrCanonical(0,4) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.InvalidTargetDoesNotMutateDraftOrCanonical(int.MaxValue,int.MinValue) — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.LocalizedHudLongValuesAndProductionAssetsResolveWithoutMutatingState — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.MalformedAndUnsupportedDraftsFailClosed("version") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.MalformedAndUnsupportedDraftsFailClosed("duplicate") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.MalformedAndUnsupportedDraftsFailClosed("malformed") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.MalformedAndUnsupportedDraftsFailClosed("oversized") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.MalformedAndUnsupportedDraftsFailClosed("sequence") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.MalformedAndUnsupportedDraftsFailClosed("target") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.MaximumCommandEnvelopeIsBoundedAndRecoverable — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.NetUnchangedDraftHasNoChangedFloorOrEconomicCommit — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.OccupiedReservedAndConfiguredMultiTilePositionsFailClosed — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.OrderedAcknowledgedPrefixRecoversWithoutPendingSuffixOrAutomaticCommit — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.OriginalReproduction_CandidateBarrierFailureNeverRecoversCandidateOrNeedsRollback — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.RepositionPreservesIdentityWalletCanonicalBytesAndReopensExactPosition("placement.category.monster","placement.option.monster.skeleton") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.RepositionPreservesIdentityWalletCanonicalBytesAndReopensExactPosition("placement.category.trap","placement.option.trap.spike") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.RepositionPreservesIdentityWalletCanonicalBytesAndReopensExactPosition("placement.category.loot_node","placement.option.loot_node.basic") — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.SafeAreaMappingZoomPanAndBlockedPinchArePureAndClamped — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4TransactionalEditor.WrongRoomAndAssignmentIdsNeverReassignOrSubstitute — Passed

- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.SelectionCloseEmptyTapFloorDiscardAndCommitClearPreview — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.UnresolvedRecoveryRetainsResolutionAndFailedDeleteCanRetry — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.ValidRecoveryDiscardFailureRemovesResumeAndAllowsRetry — Passed
- DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene.ValidRecoveryResumeKeepsCanonicalAndManaUnchanged — Passed

## Genuine PlayMode shell cases

- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.ProductionShellLongerLocalization — Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.ProductionShellNormalEditMoveSaveDiscardAndTextModes — Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.ProductionShellPortraitLandscapeCutoutAndScreenshotEvidence — Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.ProductionShellRealInputSystemChromeAndPinch — Passed

- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.ProductionShellSelectionPreviewLifetime — Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.ProductionShellUnresolvedRecoveryAndDeleteRetry — Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.ProductionShellValidRecoveryDiscardAndDeleteRetry — Passed
- DungeonBuilder.M0.Tests.PlayMode.PhaseSevenA4ProductionShellPlayModeTests.ProductionShellValidRecoveryResume — Passed

## Established skips

Exact fullname sets match TestResults/phase7a3-full-editmode.xml and phase7a3-full-playmode.xml. No new skip, ignore or reclassification was added.

EditMode:

- DungeonBuilder.M0.Editor.Tests.PhaseSevenA2WindowsSpatialMigration.CurrentNonWindowsRuntimeFailsClosed

PlayMode:

- DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.BootstrapDeletionPresentationLocalizesReturnedRemovedAndAllBlockingContentWithoutRawIds
- DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.BootstrapRenovationPresentationDisclosesLocalizedMovementReplacementAndCapacityConsequences
- DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.StructuralConstructionThroughRealRootPersistsPublishesAndClearsPreview
- DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.StructuralDeletionMissingRuntimePolicyFailsClosedThroughRealRoot
- DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.StructuralDeletionThroughRealRootPersistsPublishesAndPresents(6,"north",DirectDoorway,"Direct Doorway","")
- DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.StructuralDeletionThroughRealRootPersistsPublishesAndPresents(7,"east",PhysicalCorridor,"Straight Stone Corridor","(1,6)")
- DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.StructuralReplacementThroughRealRootPersistsPublishesAndReopens(False)
- DungeonBuilder.M0.Tests.EditMode.Gd66GameRootBootIntegrationTests.StructuralReplacementThroughRealRootPersistsPublishesAndReopens(True)
- DungeonBuilder.M0.Tests.EditMode.Gd66WindowsSpatialMigrationFileSystemTests.CurrentNonWindowsRuntimeFailsClosed
- DungeonBuilder.M0.Tests.PlayMode.Gd66WindowsStandaloneQualificationTests.WindowsStandalonePreflightAndNativeFilesystemQualification

## Localization additions

Exactly 41 new Bootstrap English entries. No existing entry was changed; runtime C#, UXML and USS introduce no player-facing English. Production presentation consumes ContentService localization and current label/heat/mana format authorities. Test-only longer values exercise wrapping. No Unity Localization or Japanese content was added.

| Key | English fallback |
| --- | --- |
| ui.dungeon.capacity | Floor space: {0} used · {1} remaining |
| ui.dungeon.close | Close |
| ui.dungeon.commit_review | Save all repositioned content across the dungeon? Spending: 0 mana. Refunds: 0 mana. Net change: 0 mana. Current runs keep their original dungeon. |
| ui.dungeon.committed | Changes saved. |
| ui.dungeon.continue | Continue editing |
| ui.dungeon.discard | Discard draft |
| ui.dungeon.discard_review | Discard all draft changes? Your committed dungeon will stay unchanged. |
| ui.dungeon.discarded | Draft discarded. |
| ui.dungeon.draft.acknowledged | Draft protected locally. Save Changes to publish it. |
| ui.dungeon.draft.clean | Draft has no changes. |
| ui.dungeon.draft.delete_failed | The local draft could not be removed. It may still appear on reopen. Retry discarding it. |
| ui.dungeon.draft.failed | Draft protection failed. Save Changes is blocked. Retry to protect your edits. |
| ui.dungeon.draft.invalid | This position is unavailable. Choose a tile in the same room, inside its bounds, clear of reserved tiles and incompatible content. |
| ui.dungeon.draft.limit | This draft has reached its command limit. Save or discard it before making more changes. |
| ui.dungeon.draft.pending | Protecting draft changes… Save Changes is waiting. |
| ui.dungeon.draft.recovery_failed | Draft recovery evidence is incomplete or conflicting. Your saved dungeon is unchanged. The draft has been preserved. |
| ui.dungeon.draft.stale | The committed dungeon has changed. This draft cannot be resumed or merged. Discard the old draft to edit the current dungeon. |
| ui.dungeon.draft.unknown | Draft protection could not be confirmed. Save Changes is blocked. Reopen the dungeon to recover the last committed edits. |
| ui.dungeon.edit | Edit dungeon |
| ui.dungeon.edit_mode | Edit Mode · Local draft |
| ui.dungeon.floor | Floor {0} |
| ui.dungeon.floor_changed |  · Edited |
| ui.dungeon.hud.heat | Heat<br>{0} |
| ui.dungeon.hud.rate | Mana/hour<br>{0} |
| ui.dungeon.hud.total | Total Mana<br>{0} |
| ui.dungeon.hud.usable | Usable Mana<br>{0} |
| ui.dungeon.legacy | Development tools |
| ui.dungeon.move | Move |
| ui.dungeon.move_hint | Tap a target tile in this room. |
| ui.dungeon.normal | Dungeon |
| ui.dungeon.recovery | A protected local draft was recovered. Resume Editing or discard it. Your committed dungeon has not changed. |
| ui.dungeon.reset | Focus floor |
| ui.dungeon.resume | Resume Editing |
| ui.dungeon.retry | Retry protection |
| ui.dungeon.return | Return to Dungeon |
| ui.dungeon.save | Save Changes |
| ui.dungeon.text_default | Default |
| ui.dungeon.text_large | Large |
| ui.dungeon.text_size | Text size |
| ui.dungeon.text_small | Small |
| ui.dungeon.unavailable | Unavailable |
