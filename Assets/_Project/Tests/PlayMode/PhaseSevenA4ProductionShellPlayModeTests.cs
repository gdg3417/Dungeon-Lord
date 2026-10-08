#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonBuilder.M0.Tests.PlayMode
{
    /// <summary>
    /// The predefined runtime assembly cannot be a compile-time dependency of this isolated
    /// qualification assembly. Follow its existing reflection boundary and run the same actual
    /// scene scenarios in the PlayMode runner, where Application.isPlaying is required.
    /// </summary>
    public sealed class PhaseSevenA4ProductionShellPlayModeTests
    {
        private object fixture;
        private Type fixtureType;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.That(Application.isPlaying, Is.True);
            fixtureType = Type.GetType("DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests.PhaseSevenA4ProductionScene, Assembly-CSharp-Editor", true);
            fixture = Activator.CreateInstance(fixtureType);
            yield return Invoke("SetUp");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (fixture != null) yield return Invoke("TearDown");
            fixture = null;
        }

        [UnityTest]
        public IEnumerator TransactionalRoomConstructionProductionParity()
        { yield return Invoke("TransactionalRoomConstructionProductionPreviewConfirmSaveRecovery"); }
        [UnityTest]
        public IEnumerator CompositionNormalInspectionFocusRailCollapse()
        { yield return Invoke("CompositionNormalInspectionFocusRailAndCollapseAreReadOnly"); }
        [UnityTest]
        public IEnumerator CompositionCorridorInspection()
        { yield return Invoke("CompositionAuthoredCorridorInspectionAndOutlineAreReadOnly"); }
        [UnityTest]
        public IEnumerator CompositionLayoutsAndBoundedPresentation()
        { yield return Invoke("CompositionLayoutsAndPresentationWorkRemainBounded"); }
        [UnityTest]
        public IEnumerator CompositionFloorLayoutSummarySemantics()
        { yield return Invoke("CompositionFloorLayoutSummaryDoesNotMisreportRequiredRoute"); }
        [UnityTest]
        public IEnumerator CompositionGenuineInputInspectionFocusAndChrome()
        { yield return Invoke("CompositionInputSystemInspectionFocusFitAndChrome"); }
        [UnityTest]
        public IEnumerator CompositionGenuineInputFloorsMousePanZoom()
        { yield return Invoke("CompositionInputSystemFloorNavigationMousePanAndZoom"); }
        [UnityTest]
        public IEnumerator TransactionalRoomConstructionInvalidCorrection()
        { yield return Invoke("TransactionalRoomConstructionInvalidCorrectionAndCollapseKeepActionsVisible"); }
        [UnityTest]
        public IEnumerator TransactionalRoomConstructionOutOfBoundsIntentRecoveryBoundsCameraInputAndSave()
        { yield return Invoke("TransactionalRoomConstructionOutOfBoundsIntentBoundsRecoverCameraAndSave"); }
        [UnityTest]
        public IEnumerator TransactionalRoomConstructionLocalizationLayoutsAndResume()
        { yield return Invoke("TransactionalRoomConstructionLayoutsLocalizationAndResume"); }
        [UnityTest]
        public IEnumerator TransactionalRoomConstructionActualInput()
        { yield return Invoke("InputSystemConstructionTapPreviewsWithoutAcknowledgingUntilConfirm"); }

        [UnityTest]
        public IEnumerator ProductionShellRuntimeThemeAndVisibleLocalizedText()
        { yield return Invoke("RuntimeThemeAndLocalizedTextRenderInActualScene"); }

        [UnityTest]
        public IEnumerator PhaseSevenA5RoomMovementProductionParity()
        { yield return Invoke("PhaseSevenA5GraphicalRoomInvalidCorrectionEconomyAndAtomicSave"); }
        [UnityTest]
        public IEnumerator PhaseSevenA5InvalidRecovery()
        { yield return Invoke("PhaseSevenA5InvalidRecoveryRestoresFootprintReasonAndDiscard"); }
        [UnityTest]
        public IEnumerator PhaseSevenA5ExperimentationAndInsufficientMana()
        { yield return Invoke("PhaseSevenA5ExperimentationOriginRecoveryAndInsufficientManaKeepDraft"); }
        [UnityTest]
        public IEnumerator PhaseSevenA5LongLocalization()
        { yield return Invoke("PhaseSevenA5LongLocalizedRoomSheetInvalidReasonAndEconomicsRemainReadable"); }
        [UnityTest]
        public IEnumerator PhaseSevenA5MoveGuidancePortraitLandscape()
        { yield return Invoke("PhaseSevenA5MoveGuidancePortraitLandscapeAndCurrentDraft"); }
        [UnityTest]
        public IEnumerator PhaseSevenA5MoveGuidanceNoAlternative()
        { yield return Invoke("PhaseSevenA5MoveGuidanceNoAlternativeIsLocalized"); }
        [UnityTest]
        public IEnumerator PhaseSevenA5MoveGuidanceLongTextRecovery()
        { yield return Invoke("PhaseSevenA5MoveGuidanceLongTextAndInvalidRecovery"); }

        [UnityTest]
        public IEnumerator ProductionShellNormalEditMoveSaveDiscardAndTextModes()
        {
            yield return Invoke("ActualSceneNormalEditMoveInvalidSaveDiscardAndTextModes");
        }

        [UnityTest]
        public IEnumerator ProductionShellPortraitLandscapeCutoutAndScreenshotEvidence()
        {
            yield return Invoke("RepresentativeLayoutsKeepChromeInsideSafeRootAndCaptureEvidence");
        }

        [UnityTest]
        public IEnumerator ProductionShellRealInputSystemChromeAndPinch()
        {
            yield return Invoke("InputSystemChromeOriginDoesNotLeakAndTwoTouchesZoomWithoutDraftWrites");
        }

        [UnityTest]
        public IEnumerator ProductionShellLongerLocalization()
        {
            yield return Invoke("LongerLocalizationWrapsWithoutLosingCriticalActions");
        }

        [UnityTest]
        public IEnumerator ProductionShellUnresolvedRecoveryAndDeleteRetry()
        { yield return Invoke("UnresolvedRecoveryRetainsResolutionAndFailedDeleteCanRetry"); }

        [UnityTest]
        public IEnumerator ProductionShellValidRecoveryResume()
        { yield return Invoke("ValidRecoveryResumeKeepsCanonicalAndManaUnchanged"); }

        [UnityTest]
        public IEnumerator ProductionShellValidRecoveryDiscardAndDeleteRetry()
        { yield return Invoke("ValidRecoveryDiscardFailureRemovesResumeAndAllowsRetry"); }

        [UnityTest]
        public IEnumerator ProductionShellSelectionPreviewLifetime()
        { yield return Invoke("SelectionCloseEmptyTapFloorDiscardAndCommitClearPreview"); }

        [UnityTest]
        public IEnumerator ProductionShellExplicitDeleteQuiescenceAndFreshBoot()
        { yield return Invoke("ExplicitDeleteQuiescesProductionShellAndFreshBootHasNoDraft"); }

        [UnityTest]
        public IEnumerator ProductionShellFailedExplicitDeleteQuiescence()
        { yield return Invoke("FailedExplicitDeleteStillQuiescesProductionShellWithoutFurtherWrites"); }

        private IEnumerator Invoke(string method) => (IEnumerator)fixtureType.GetMethod(method,
            BindingFlags.Public | BindingFlags.Instance).Invoke(fixture, null);
    }
}
#endif
