using System;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    [Serializable]
    public sealed class SpatialContentValidationWorkloadLimitsData
    {
        public int MaximumTopLevelRecords;
        public int MaximumNestedRecords;
        public int MaximumMaterializedTiles;
        public int MaximumIssues;
        public int MaximumStringCharacters;
    }

    public enum ProductionSpatialWorkloadLimitField
    {
        None = 0,
        MaximumTopLevelRecords = 1,
        MaximumNestedRecords = 2,
        MaximumMaterializedTiles = 3,
        MaximumIssues = 4,
        MaximumStringCharacters = 5
    }

    public enum ProductionSpatialWorkloadLimitDiagnostic
    {
        None = 0,
        MissingInput = 1,
        EmptyInput = 2,
        MalformedJson = 3,
        InvalidRoot = 4,
        MissingRequiredField = 5,
        DuplicateField = 6,
        AmbiguousField = 7,
        UnknownField = 8,
        InvalidNumericToken = 9,
        IntegerOverflowOrUnsupportedRepresentation = 10,
        NonpositiveValue = 11
    }

    public readonly struct ProductionSpatialContentWorkloadLimitParseResult
    {
        internal ProductionSpatialContentWorkloadLimitParseResult(
            SpatialContentValidationWorkloadLimits limits)
        {
            Success = true;
            Limits = limits;
            Diagnostic = ProductionSpatialWorkloadLimitDiagnostic.None;
            Field = ProductionSpatialWorkloadLimitField.None;
        }

        internal ProductionSpatialContentWorkloadLimitParseResult(
            ProductionSpatialWorkloadLimitDiagnostic diagnostic,
            ProductionSpatialWorkloadLimitField field = ProductionSpatialWorkloadLimitField.None)
        {
            Success = false;
            Limits = default;
            Diagnostic = diagnostic;
            Field = field;
        }

        public bool Success { get; }
        public SpatialContentValidationWorkloadLimits Limits { get; }
        public ProductionSpatialWorkloadLimitDiagnostic Diagnostic { get; }
        public ProductionSpatialWorkloadLimitField Field { get; }
    }

    public static class ProductionSpatialContentWorkloadLimitParser
    {
        private static readonly string[] FieldNames =
        {
            "MaximumTopLevelRecords",
            "MaximumNestedRecords",
            "MaximumMaterializedTiles",
            "MaximumIssues",
            "MaximumStringCharacters"
        };

        public static ProductionSpatialContentWorkloadLimitParseResult Parse(TextAsset asset) =>
            asset == null
                ? Failure(ProductionSpatialWorkloadLimitDiagnostic.MissingInput)
                : Parse(asset.text);

        public static ProductionSpatialContentWorkloadLimitParseResult Parse(string json)
        {
            if (json == null)
                return Failure(ProductionSpatialWorkloadLimitDiagnostic.MissingInput);
            if (json.Length == 0 || string.IsNullOrWhiteSpace(json))
                return Failure(ProductionSpatialWorkloadLimitDiagnostic.EmptyInput);

            var reader = new Reader(json);
            reader.SkipWhitespace();
            if (!reader.TryConsume('{'))
                return Failure(reader.IsAtEnd
                    ? ProductionSpatialWorkloadLimitDiagnostic.MalformedJson
                    : ProductionSpatialWorkloadLimitDiagnostic.InvalidRoot);

            var values = new int[FieldNames.Length];
            var present = new bool[FieldNames.Length];
            reader.SkipWhitespace();
            if (!reader.TryConsume('}'))
            {
                while (true)
                {
                    if (!reader.TryReadString(out string propertyName))
                        return Failure(ProductionSpatialWorkloadLimitDiagnostic.MalformedJson);

                    int fieldIndex = CanonicalIndex(propertyName);
                    if (fieldIndex < 0)
                    {
                        int ambiguousIndex = AmbiguousIndex(propertyName);
                        return ambiguousIndex >= 0
                            ? Failure(ProductionSpatialWorkloadLimitDiagnostic.AmbiguousField, Field(ambiguousIndex))
                            : Failure(ProductionSpatialWorkloadLimitDiagnostic.UnknownField);
                    }
                    if (present[fieldIndex])
                        return Failure(ProductionSpatialWorkloadLimitDiagnostic.DuplicateField, Field(fieldIndex));

                    reader.SkipWhitespace();
                    if (!reader.TryConsume(':'))
                        return Failure(ProductionSpatialWorkloadLimitDiagnostic.MalformedJson, Field(fieldIndex));
                    reader.SkipWhitespace();
                    NumberResult numberResult = reader.TryReadInteger(out int value);
                    if (numberResult != NumberResult.Success)
                        return Failure(numberResult == NumberResult.InvalidToken
                            ? ProductionSpatialWorkloadLimitDiagnostic.InvalidNumericToken
                            : ProductionSpatialWorkloadLimitDiagnostic.IntegerOverflowOrUnsupportedRepresentation,
                            Field(fieldIndex));
                    if (value <= 0)
                        return Failure(ProductionSpatialWorkloadLimitDiagnostic.NonpositiveValue, Field(fieldIndex));

                    present[fieldIndex] = true;
                    values[fieldIndex] = value;
                    reader.SkipWhitespace();
                    if (reader.TryConsume('}'))
                        break;
                    if (!reader.TryConsume(','))
                        return Failure(ProductionSpatialWorkloadLimitDiagnostic.MalformedJson);
                    reader.SkipWhitespace();
                    if (reader.Peek == '}')
                        return Failure(ProductionSpatialWorkloadLimitDiagnostic.MalformedJson);
                }
            }

            reader.SkipWhitespace();
            if (!reader.IsAtEnd)
                return Failure(ProductionSpatialWorkloadLimitDiagnostic.MalformedJson);
            for (int index = 0; index < present.Length; index++)
                if (!present[index])
                    return Failure(ProductionSpatialWorkloadLimitDiagnostic.MissingRequiredField, Field(index));

            var data = new SpatialContentValidationWorkloadLimitsData
            {
                MaximumTopLevelRecords = values[0],
                MaximumNestedRecords = values[1],
                MaximumMaterializedTiles = values[2],
                MaximumIssues = values[3],
                MaximumStringCharacters = values[4]
            };
            return new ProductionSpatialContentWorkloadLimitParseResult(
                new SpatialContentValidationWorkloadLimits(data.MaximumTopLevelRecords, data.MaximumNestedRecords,
                    data.MaximumMaterializedTiles, data.MaximumIssues, data.MaximumStringCharacters));
        }

        private static ProductionSpatialContentWorkloadLimitParseResult Failure(
            ProductionSpatialWorkloadLimitDiagnostic diagnostic,
            ProductionSpatialWorkloadLimitField field = ProductionSpatialWorkloadLimitField.None) =>
            new ProductionSpatialContentWorkloadLimitParseResult(diagnostic, field);

        private static ProductionSpatialWorkloadLimitField Field(int index) =>
            (ProductionSpatialWorkloadLimitField)(index + 1);

        private static int CanonicalIndex(string value)
        {
            for (int index = 0; index < FieldNames.Length; index++)
                if (string.Equals(value, FieldNames[index], StringComparison.Ordinal))
                    return index;
            return -1;
        }

        private static int AmbiguousIndex(string value)
        {
            for (int index = 0; index < FieldNames.Length; index++)
                if (string.Equals(value, FieldNames[index], StringComparison.OrdinalIgnoreCase))
                    return index;
            return -1;
        }

        private enum NumberResult { Success, InvalidToken, UnsupportedOrOverflow }

        private sealed class Reader
        {
            private readonly string source;
            private int index;

            public Reader(string source) { this.source = source; }
            public bool IsAtEnd => index >= source.Length;
            public char Peek => IsAtEnd ? '\0' : source[index];

            public void SkipWhitespace()
            {
                while (!IsAtEnd && (Peek == ' ' || Peek == '\t' || Peek == '\r' || Peek == '\n'))
                    index++;
            }

            public bool TryConsume(char value)
            {
                if (Peek != value)
                    return false;
                index++;
                return true;
            }

            public bool TryReadString(out string value)
            {
                value = null;
                SkipWhitespace();
                if (!TryConsume('"'))
                    return false;
                int start = index;
                while (!IsAtEnd)
                {
                    char current = source[index++];
                    if (current == '"')
                    {
                        value = source.Substring(start, index - start - 1);
                        return value.IndexOf('\\') < 0;
                    }
                    if (current == '\\' || current < 0x20)
                        return false;
                }
                return false;
            }

            public NumberResult TryReadInteger(out int value)
            {
                value = 0;
                if (IsAtEnd || (Peek != '-' && (Peek < '0' || Peek > '9')))
                    return NumberResult.InvalidToken;

                bool negative = TryConsume('-');
                if (IsAtEnd || Peek < '0' || Peek > '9')
                    return NumberResult.InvalidToken;
                if (Peek == '0' && index + 1 < source.Length && source[index + 1] >= '0' && source[index + 1] <= '9')
                    return NumberResult.UnsupportedOrOverflow;

                long magnitude = 0;
                while (!IsAtEnd && Peek >= '0' && Peek <= '9')
                {
                    int digit = Peek - '0';
                    if (magnitude > (int.MaxValue - digit) / 10L)
                    {
                        while (!IsAtEnd && Peek >= '0' && Peek <= '9') index++;
                        return NumberResult.UnsupportedOrOverflow;
                    }
                    magnitude = magnitude * 10 + digit;
                    index++;
                }
                if (!IsAtEnd && (Peek == '.' || Peek == 'e' || Peek == 'E'))
                    return NumberResult.UnsupportedOrOverflow;
                value = negative ? -(int)magnitude : (int)magnitude;
                return NumberResult.Success;
            }
        }
    }
}
