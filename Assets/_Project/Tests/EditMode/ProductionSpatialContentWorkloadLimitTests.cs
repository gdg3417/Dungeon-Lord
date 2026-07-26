#if UNITY_EDITOR
using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class ProductionSpatialContentWorkloadLimitTests
    {
        private const string AssetPath = "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json";
        private static readonly string[] Names =
        {
            "MaximumTopLevelRecords", "MaximumNestedRecords", "MaximumMaterializedTiles",
            "MaximumIssues", "MaximumStringCharacters"
        };
        private static readonly int[] Approved = { 128, 512, 4096, 256, 32768 };

        private static string Valid(params int[] values)
        {
            int[] selected = values != null && values.Length == Names.Length ? values : new[] { 1, 2, 3, 4, 5 };
            return "{" + string.Join(",", Names.Select((name, index) => "\"" + name + "\":" + selected[index])) + "}";
        }

        private static string ReplaceValue(string json, int index, string value)
        {
            string marker = "\"" + Names[index] + "\":";
            int start = json.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
            int end = json.IndexOfAny(new[] { ',', '}' }, start);
            return json.Substring(0, start) + value + json.Substring(end);
        }

        [Test]
        public void CommittedProductionAsset_IsTheExactAuthorityAndConvertsAllValues()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetPath);
            Assert.That(asset, Is.Not.Null);
            string before = asset.text;
            Assert.That(Names.Sum(name => Count(before, "\"" + name + "\"")), Is.EqualTo(5));
            ProductionSpatialContentWorkloadLimitParseResult result =
                ProductionSpatialContentWorkloadLimitParser.Parse(asset);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Limits.IsValid, Is.True);
            CollectionAssert.AreEqual(Approved, Values(result.Limits));
            Assert.That(asset.text, Is.EqualTo(before));
        }

        [TestCase(null, ProductionSpatialWorkloadLimitDiagnostic.MissingInput)]
        [TestCase("", ProductionSpatialWorkloadLimitDiagnostic.EmptyInput)]
        [TestCase(" \r\n\t", ProductionSpatialWorkloadLimitDiagnostic.EmptyInput)]
        [TestCase("{", ProductionSpatialWorkloadLimitDiagnostic.MalformedJson)]
        [TestCase("[]", ProductionSpatialWorkloadLimitDiagnostic.InvalidRoot)]
        [TestCase("null", ProductionSpatialWorkloadLimitDiagnostic.InvalidRoot)]
        [TestCase("{} trailing", ProductionSpatialWorkloadLimitDiagnostic.MalformedJson)]
        [TestCase("{\"MaximumTopLevelRecords\":1,}", ProductionSpatialWorkloadLimitDiagnostic.MalformedJson)]
        [TestCase("{/*x*/\"MaximumTopLevelRecords\":1}", ProductionSpatialWorkloadLimitDiagnostic.MalformedJson)]
        public void InvalidDocuments_FailClosedWithoutThrowing(
            string json, ProductionSpatialWorkloadLimitDiagnostic diagnostic)
        {
            ProductionSpatialContentWorkloadLimitParseResult result = default;
            Assert.DoesNotThrow(() => result = ProductionSpatialContentWorkloadLimitParser.Parse(json));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Limits.IsValid, Is.False);
            Assert.That(result.Diagnostic, Is.EqualTo(diagnostic));
        }

        [Test]
        public void MissingTextAsset_FailsClosed()
        {
            var result = ProductionSpatialContentWorkloadLimitParser.Parse((TextAsset)null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic, Is.EqualTo(ProductionSpatialWorkloadLimitDiagnostic.MissingInput));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void EveryRequiredField_IsRequiredInCanonicalOrder(int omitted)
        {
            string json = "{" + string.Join(",", Names.Where((_, index) => index != omitted)
                .Select((name, index) => "\"" + name + "\":" + (index + 1))) + "}";
            var result = ProductionSpatialContentWorkloadLimitParser.Parse(json);
            Assert.That(result.Diagnostic, Is.EqualTo(ProductionSpatialWorkloadLimitDiagnostic.MissingRequiredField));
            Assert.That(result.Field, Is.EqualTo((ProductionSpatialWorkloadLimitField)(omitted + 1)));
        }

        [TestCase(0, "0", ProductionSpatialWorkloadLimitDiagnostic.NonpositiveValue)]
        [TestCase(1, "-1", ProductionSpatialWorkloadLimitDiagnostic.NonpositiveValue)]
        [TestCase(2, "2147483648", ProductionSpatialWorkloadLimitDiagnostic.IntegerOverflowOrUnsupportedRepresentation)]
        [TestCase(3, "1.0", ProductionSpatialWorkloadLimitDiagnostic.IntegerOverflowOrUnsupportedRepresentation)]
        [TestCase(4, "1e2", ProductionSpatialWorkloadLimitDiagnostic.IntegerOverflowOrUnsupportedRepresentation)]
        [TestCase(0, "\"1\"", ProductionSpatialWorkloadLimitDiagnostic.InvalidNumericToken)]
        [TestCase(1, "null", ProductionSpatialWorkloadLimitDiagnostic.InvalidNumericToken)]
        [TestCase(2, "true", ProductionSpatialWorkloadLimitDiagnostic.InvalidNumericToken)]
        [TestCase(3, "[]", ProductionSpatialWorkloadLimitDiagnostic.InvalidNumericToken)]
        [TestCase(4, "{}", ProductionSpatialWorkloadLimitDiagnostic.InvalidNumericToken)]
        public void UnsupportedFieldValues_FailWithStableField(
            int field, string value, ProductionSpatialWorkloadLimitDiagnostic diagnostic)
        {
            var result = ProductionSpatialContentWorkloadLimitParser.Parse(ReplaceValue(Valid(), field, value));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic, Is.EqualTo(diagnostic));
            Assert.That(result.Field, Is.EqualTo((ProductionSpatialWorkloadLimitField)(field + 1)));
        }

        [TestCase("[1]")]
        [TestCase("[1,2.5,-3e2]")]
        [TestCase("[{\"value\":1}]")]
        [TestCase("{\"value\":1}")]
        [TestCase("{\"value\":[1,{\"nested\":2}]}")]
        [TestCase("[\"text\",true,false,null,1,-2.5e+3,{\"nested\":[4]}]")]
        public void ValidWrongTypeCollections_AreInvalidNumericTokensRegardlessOfContents(string value)
        {
            foreach (int field in new[] { 0, 2, 4 })
            {
                ProductionSpatialContentWorkloadLimitParseResult result =
                    ProductionSpatialContentWorkloadLimitParser.Parse(WithFieldValue(field, value));
                Assert.That(result.Success, Is.False);
                Assert.That(result.Diagnostic,
                    Is.EqualTo(ProductionSpatialWorkloadLimitDiagnostic.InvalidNumericToken));
                Assert.That(result.Field, Is.EqualTo((ProductionSpatialWorkloadLimitField)(field + 1)));
                Assert.That(result.Limits.IsValid, Is.False);
                CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0 }, Values(result.Limits));
            }
        }

        [TestCase("[01]")]
        [TestCase("[1.]")]
        [TestCase("{\"value\":1e}")]
        [TestCase("[1,]")]
        [TestCase("{\"value\":1,}")]
        public void MalformedWrongTypeCollections_RemainMalformedJson(string value)
        {
            ProductionSpatialContentWorkloadLimitParseResult result =
                ProductionSpatialContentWorkloadLimitParser.Parse(WithFieldValue(1, value));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic, Is.EqualTo(ProductionSpatialWorkloadLimitDiagnostic.MalformedJson));
            Assert.That(result.Field, Is.EqualTo(ProductionSpatialWorkloadLimitField.MaximumNestedRecords));
            Assert.That(result.Limits.IsValid, Is.False);
        }

        [TestCase("2147483647", ProductionSpatialWorkloadLimitDiagnostic.None, true)]
        [TestCase("2147483648", ProductionSpatialWorkloadLimitDiagnostic.IntegerOverflowOrUnsupportedRepresentation, false)]
        [TestCase("-2147483647", ProductionSpatialWorkloadLimitDiagnostic.NonpositiveValue, false)]
        [TestCase("-2147483648", ProductionSpatialWorkloadLimitDiagnostic.NonpositiveValue, false)]
        [TestCase("-2147483649", ProductionSpatialWorkloadLimitDiagnostic.IntegerOverflowOrUnsupportedRepresentation, false)]
        public void SignedInt32Boundaries_ReturnExactDiagnostic(
            string value, ProductionSpatialWorkloadLimitDiagnostic diagnostic, bool success)
        {
            ProductionSpatialContentWorkloadLimitParseResult result =
                ProductionSpatialContentWorkloadLimitParser.Parse(WithFieldValue(2, value));
            Assert.That(result.Success, Is.EqualTo(success));
            Assert.That(result.Diagnostic, Is.EqualTo(diagnostic));
            Assert.That(result.Field, Is.EqualTo(success
                ? ProductionSpatialWorkloadLimitField.None
                : ProductionSpatialWorkloadLimitField.MaximumMaterializedTiles));
            if (success)
            {
                Assert.That(result.Limits.MaximumMaterializedTiles, Is.EqualTo(int.MaxValue));
                Assert.That(result.Limits.IsValid, Is.True);
            }
            else
            {
                Assert.That(result.Limits.IsValid, Is.False);
                CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0 }, Values(result.Limits));
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void EveryFieldRejectsZeroNegativeAndOverflow(int field)
        {
            foreach (string value in new[] { "0", "-1", "2147483648", new string('9', 10000) })
            {
                ProductionSpatialContentWorkloadLimitParseResult result = default;
                Assert.DoesNotThrow(() => result = ProductionSpatialContentWorkloadLimitParser.Parse(
                    ReplaceValue(Valid(), field, value)));
                Assert.That(result.Success, Is.False);
                Assert.That(result.Limits.IsValid, Is.False);
            }
        }

        [Test]
        public void DuplicateAmbiguousAndUnknownFields_FailClosed()
        {
            string valid = Valid();
            AssertDiagnostic(valid.Insert(valid.Length - 1, ",\"MaximumIssues\":7"),
                ProductionSpatialWorkloadLimitDiagnostic.DuplicateField);
            AssertDiagnostic(valid.Replace("MaximumIssues", "maximumissues"),
                ProductionSpatialWorkloadLimitDiagnostic.AmbiguousField);
            AssertDiagnostic(valid.Insert(valid.Length - 1, ",\"maximumissues\":7"),
                ProductionSpatialWorkloadLimitDiagnostic.AmbiguousField);
            AssertDiagnostic(valid.Insert(valid.Length - 1, ",\"Unexpected\":7"),
                ProductionSpatialWorkloadLimitDiagnostic.UnknownField);
        }

        [Test]
        public void OrderingWhitespaceSourceAndRepeatedResults_AreDeterministic()
        {
            string source = Valid();
            string reversed = "{\n  " + string.Join(",\n  ", Names.Reverse()
                .Select(name => "\"" + name + "\" : " + (Array.IndexOf(Names, name) + 1))) + "\n}";
            string before = source;
            var first = ProductionSpatialContentWorkloadLimitParser.Parse(source);
            var second = ProductionSpatialContentWorkloadLimitParser.Parse(source);
            var permutation = ProductionSpatialContentWorkloadLimitParser.Parse(reversed);
            Assert.That(source, Is.EqualTo(before));
            Assert.That(first.Success, Is.True);
            Assert.That(permutation.Success, Is.True);
            Assert.That(permutation.Diagnostic, Is.EqualTo(ProductionSpatialWorkloadLimitDiagnostic.None));
            Assert.That(permutation.Field, Is.EqualTo(ProductionSpatialWorkloadLimitField.None));
            CollectionAssert.AreEqual(Values(first.Limits), Values(second.Limits));
            CollectionAssert.AreEqual(Values(first.Limits), Values(permutation.Limits));

            var failure1 = ProductionSpatialContentWorkloadLimitParser.Parse("{}");
            var failure2 = ProductionSpatialContentWorkloadLimitParser.Parse("{}");
            Assert.That(failure2.Diagnostic, Is.EqualTo(failure1.Diagnostic));
            Assert.That(failure2.Field, Is.EqualTo(failure1.Field));
            CollectionAssert.AreNotEqual(Approved, Values(first.Limits));
        }

        [Test]
        public void MultipleNonpositiveFields_SelectCanonicalFieldIndependentOfPropertyOrder()
        {
            AssertSemanticPermutations(
                new[]
                {
                    Property(Names[3], "0"), Property(Names[0], "-1"), Property(Names[1], "2"),
                    Property(Names[2], "3"), Property(Names[4], "5")
                },
                ProductionSpatialWorkloadLimitDiagnostic.NonpositiveValue,
                ProductionSpatialWorkloadLimitField.MaximumTopLevelRecords);
        }

        [Test]
        public void MultipleUnsupportedFields_SelectCanonicalFieldIndependentOfPropertyOrder()
        {
            AssertSemanticPermutations(
                new[]
                {
                    Property(Names[3], "1e2"), Property(Names[1], "2147483648"), Property(Names[0], "1"),
                    Property(Names[2], "3"), Property(Names[4], "5")
                },
                ProductionSpatialWorkloadLimitDiagnostic.IntegerOverflowOrUnsupportedRepresentation,
                ProductionSpatialWorkloadLimitField.MaximumNestedRecords);
        }

        [Test]
        public void DuplicatePrecedesOtherSemanticFailuresIndependentOfPropertyOrder()
        {
            AssertSemanticPermutations(
                new[]
                {
                    Property(Names[0], "0"), Property(Names[1], "2"), Property(Names[2], "3"),
                    Property(Names[3], "4"), Property(Names[3], "6"), Property(Names[4], "5")
                },
                ProductionSpatialWorkloadLimitDiagnostic.DuplicateField,
                ProductionSpatialWorkloadLimitField.MaximumIssues);
        }

        [Test]
        public void AmbiguousFieldPrecedesOtherSemanticFailuresIndependentOfPropertyOrder()
        {
            AssertSemanticPermutations(
                new[]
                {
                    Property(Names[0], "0"), Property(Names[1], "2"), Property(Names[2], "3"),
                    Property("maximumissues", "4"), Property(Names[4], "5")
                },
                ProductionSpatialWorkloadLimitDiagnostic.AmbiguousField,
                ProductionSpatialWorkloadLimitField.MaximumIssues);
        }

        private static void AssertSemanticPermutations(
            string[] properties,
            ProductionSpatialWorkloadLimitDiagnostic diagnostic,
            ProductionSpatialWorkloadLimitField field)
        {
            foreach (string[] permutation in new[]
            {
                properties,
                properties.Reverse().ToArray(),
                properties.Skip(2).Concat(properties.Take(2)).ToArray()
            })
            {
                string json = "{" + string.Join(",", permutation) + "}";
                for (int repetition = 0; repetition < 2; repetition++)
                {
                    ProductionSpatialContentWorkloadLimitParseResult result =
                        ProductionSpatialContentWorkloadLimitParser.Parse(json);
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.Diagnostic, Is.EqualTo(diagnostic));
                    Assert.That(result.Field, Is.EqualTo(field));
                    Assert.That(result.Limits.IsValid, Is.False);
                    CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0 }, Values(result.Limits));
                }
            }
        }

        private static string Property(string name, string value) => "\"" + name + "\":" + value;

        private static string WithFieldValue(int field, string value) =>
            "{" + string.Join(",", Names.Select((name, index) =>
                Property(name, index == field ? value : (index + 1).ToString()))) + "}";

        private static void AssertDiagnostic(string json, ProductionSpatialWorkloadLimitDiagnostic expected) =>
            Assert.That(ProductionSpatialContentWorkloadLimitParser.Parse(json).Diagnostic, Is.EqualTo(expected));
        private static int Count(string value, string token) =>
            value.Split(new[] { token }, StringSplitOptions.None).Length - 1;
        private static int[] Values(SpatialContentValidationWorkloadLimits value) => new[]
        {
            value.MaximumTopLevelRecords, value.MaximumNestedRecords, value.MaximumMaterializedTiles,
            value.MaximumIssues, value.MaximumStringCharacters
        };
    }
}
#endif
