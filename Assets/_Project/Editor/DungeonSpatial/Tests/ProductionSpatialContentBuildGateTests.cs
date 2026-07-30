#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Editor.Build;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DungeonBuilder.M0.Editor.DungeonSpatial.Tests
{
    public sealed class ProductionSpatialContentBuildGateTests
    {
        [Test]
        public void ProductionGate_ValidInstalledSetAndExactBootstrapAssignmentsPass()
        {
            ProductionSpatialBuildGateResult result = new ProductionSpatialContentBuildGate().Validate();
            Assert.That(result.Success, Is.True, result.Reason + ":" + result.Detail);
        }

        [Test]
        public void ProductionGate_SuccessPreservesRequiredFilesByteForByte()
        {
            string[] paths = ProductionSpatialGeneratedSetParser.RequiredPaths
                .Concat(new[] { ProductionSpatialContentPublicationService.LimitsPath }).ToArray();
            byte[][] before = paths.Select(File.ReadAllBytes).ToArray();
            Assert.That(new ProductionSpatialContentBuildGate().Validate().Success, Is.True);
            for (int index = 0; index < paths.Length; index++)
                CollectionAssert.AreEqual(before[index], File.ReadAllBytes(paths[index]), paths[index]);
        }

        [Test]
        public void StagesExecuteInRecoveryInstalledCompositionOrder()
        {
            var calls = new List<string>();
            var gate = Gate(
                () => RecordSuccess(calls, "recovery"),
                () => RecordSuccess(calls, "installed"),
                () => RecordSuccess(calls, "composition"));
            Assert.That(gate.Validate().Success, Is.True);
            CollectionAssert.AreEqual(new[] { "recovery", "installed", "composition" }, calls);
        }

        [TestCase(ProductionSpatialBuildGateReason.RecoveryFailure, 1)]
        [TestCase(ProductionSpatialBuildGateReason.InstalledGeneratedSetValidationFailure, 2)]
        public void FailureShortCircuitsLaterStages(ProductionSpatialBuildGateReason reason, int expectedCalls)
        {
            int calls = 0;
            var gate = Gate(
                () => ++calls == expectedCalls ? ProductionSpatialContentBuildGate.Failure(reason, "test") : ProductionSpatialContentBuildGate.Success(),
                () => ++calls == expectedCalls ? ProductionSpatialContentBuildGate.Failure(reason, "test") : ProductionSpatialContentBuildGate.Success(),
                () => { calls++; return ProductionSpatialContentBuildGate.Success(); });
            Assert.That(gate.Validate().Reason, Is.EqualTo(reason));
            Assert.That(calls, Is.EqualTo(expectedCalls));
        }

        [Test]
        public void UnexpectedExceptionBecomesStableInternalFailure()
        {
            var gate = Gate(() => throw new IOException(), ProductionSpatialContentBuildGate.Success,
                ProductionSpatialContentBuildGate.Success);
            ProductionSpatialBuildGateResult result = gate.Validate();
            Assert.That(result.Reason, Is.EqualTo(ProductionSpatialBuildGateReason.UnexpectedInternalValidationFailure));
            Assert.That(result.Detail, Is.EqualTo(typeof(IOException).FullName));
        }

        [Test]
        public void PreprocessorImplementsSharedUnityBuildCallback()
        {
            var callback = new ProductionSpatialContentBuildPreprocessor();
            Assert.That(callback, Is.InstanceOf<IPreprocessBuildWithReport>());
            Assert.That(callback.callbackOrder, Is.EqualTo(-1000));
        }

        [Test]
        public void CallbackAllowsSuccessAndConvertsFailureToBuildFailedExceptionWithReasonCode()
        {
            Assert.DoesNotThrow(() => ProductionSpatialContentBuildPreprocessor.ValidateOrThrow(Gate()));
            var failing = Gate(() => ProductionSpatialContentBuildGate.Failure(
                ProductionSpatialBuildGateReason.RecoveryFailure, "InvalidJournal"));
            BuildFailedException exception = Assert.Throws<BuildFailedException>(() =>
                ProductionSpatialContentBuildPreprocessor.ValidateOrThrow(failing));
            StringAssert.Contains("[ProductionSpatialBuildGate:RecoveryFailure]", exception.Message);
        }

        [Test]
        public void DevelopmentUtilityUsesBuildPipelineWithoutDuplicateGateInvocation()
        {
            string source = File.ReadAllText("Assets/_Project/Scripts/BuildTools/DevelopmentBuildUtility.cs");
            StringAssert.Contains("BuildPipeline.BuildPlayer(options)", source);
            Assert.That(source.Contains("ProductionSpatialContentBuildGate"), Is.False);
            Assert.That(source.Contains("OnPreprocessBuild"), Is.False);
        }

        [Test]
        public void SharedCallbackIsIndependentOfPlatformAndUtilityEntryPoint()
        {
            string source = File.ReadAllText("Assets/_Project/Editor/DungeonSpatial/ProductionSpatialContentBuildPreprocessor.cs");
            StringAssert.Contains("IPreprocessBuildWithReport", source);
            Assert.That(source.Contains("BuildTarget.StandaloneWindows64"), Is.False);
            Assert.That(source.Contains("BuildTarget.Android"), Is.False);
            Assert.That(source.Contains("DevelopmentBuildUtility"), Is.False);
        }

        private static ProductionSpatialContentBuildGate Gate(
            Func<ProductionSpatialBuildGateResult> recovery = null,
            Func<ProductionSpatialBuildGateResult> installed = null,
            Func<ProductionSpatialBuildGateResult> composition = null) =>
            new ProductionSpatialContentBuildGate(recovery ?? ProductionSpatialContentBuildGate.Success,
                installed ?? ProductionSpatialContentBuildGate.Success,
                composition ?? ProductionSpatialContentBuildGate.Success);

        private static ProductionSpatialBuildGateResult RecordSuccess(ICollection<string> calls, string value)
        {
            calls.Add(value);
            return ProductionSpatialContentBuildGate.Success();
        }
    }
}
#endif
