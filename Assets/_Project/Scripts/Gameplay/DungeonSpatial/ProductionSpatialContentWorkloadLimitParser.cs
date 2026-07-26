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
            var canonicalSeen = new bool[FieldNames.Length];
            SemanticFailure selectedFailure = default;
            bool hasSemanticFailure = false;
            reader.SkipWhitespace();
            if (!reader.TryConsume('}'))
            {
                while (true)
                {
                    if (!reader.TryReadPropertyName(out string propertyName))
                        return Failure(ProductionSpatialWorkloadLimitDiagnostic.MalformedJson);

                    int fieldIndex = CanonicalIndex(propertyName);
                    int ambiguousIndex = fieldIndex < 0 ? AmbiguousIndex(propertyName) : -1;
                    ProductionSpatialWorkloadLimitField field = fieldIndex >= 0
                        ? Field(fieldIndex)
                        : ambiguousIndex >= 0 ? Field(ambiguousIndex) : ProductionSpatialWorkloadLimitField.None;

                    reader.SkipWhitespace();
                    if (!reader.TryConsume(':'))
                        return Failure(ProductionSpatialWorkloadLimitDiagnostic.MalformedJson, field);
                    reader.SkipWhitespace();

                    NumberResult numberResult = reader.TryReadIntegerValue(out int value);
                    if (numberResult == NumberResult.Malformed)
                        return Failure(ProductionSpatialWorkloadLimitDiagnostic.MalformedJson, field);

                    if (fieldIndex < 0)
                    {
                        SelectFailure(ref selectedFailure, ref hasSemanticFailure, new SemanticFailure(
                            ambiguousIndex >= 0
                                ? ProductionSpatialWorkloadLimitDiagnostic.AmbiguousField
                                : ProductionSpatialWorkloadLimitDiagnostic.UnknownField,
                            field));
                        // A case variant identifies the intended required slot for deterministic diagnostics,
                        // but it never supplies a value or permits successful conversion.
                        if (ambiguousIndex >= 0)
                            present[ambiguousIndex] = true;
                    }
                    else
                    {
                        if (canonicalSeen[fieldIndex])
                            SelectFailure(ref selectedFailure, ref hasSemanticFailure, new SemanticFailure(
                                ProductionSpatialWorkloadLimitDiagnostic.DuplicateField, field));
                        canonicalSeen[fieldIndex] = true;
                        present[fieldIndex] = true;

                        ProductionSpatialWorkloadLimitDiagnostic numericFailure = NumericFailure(numberResult, value);
                        if (numericFailure != ProductionSpatialWorkloadLimitDiagnostic.None)
                            SelectFailure(ref selectedFailure, ref hasSemanticFailure,
                                new SemanticFailure(numericFailure, field));
                        else
                            values[fieldIndex] = value;
                    }

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
                    SelectFailure(ref selectedFailure, ref hasSemanticFailure, new SemanticFailure(
                        ProductionSpatialWorkloadLimitDiagnostic.MissingRequiredField, Field(index)));

            if (hasSemanticFailure)
                return Failure(selectedFailure.Diagnostic, selectedFailure.Field);

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

        private static void SelectFailure(
            ref SemanticFailure selected,
            ref bool hasSelection,
            SemanticFailure candidate)
        {
            // Semantic precedence is explicit diagnostic numeric order, then canonical field order.
            // Therefore serialized property order can never select the reported failure.
            if (!hasSelection || candidate.CompareTo(selected) < 0)
            {
                selected = candidate;
                hasSelection = true;
            }
        }

        private static ProductionSpatialWorkloadLimitDiagnostic NumericFailure(NumberResult result, int value)
        {
            if (result == NumberResult.InvalidToken)
                return ProductionSpatialWorkloadLimitDiagnostic.InvalidNumericToken;
            if (result == NumberResult.UnsupportedOrOverflow)
                return ProductionSpatialWorkloadLimitDiagnostic.IntegerOverflowOrUnsupportedRepresentation;
            return value <= 0
                ? ProductionSpatialWorkloadLimitDiagnostic.NonpositiveValue
                : ProductionSpatialWorkloadLimitDiagnostic.None;
        }

        private readonly struct SemanticFailure
        {
            public SemanticFailure(
                ProductionSpatialWorkloadLimitDiagnostic diagnostic,
                ProductionSpatialWorkloadLimitField field)
            {
                Diagnostic = diagnostic;
                Field = field;
            }

            public ProductionSpatialWorkloadLimitDiagnostic Diagnostic { get; }
            public ProductionSpatialWorkloadLimitField Field { get; }

            public int CompareTo(SemanticFailure other)
            {
                int diagnostic = ((int)Diagnostic).CompareTo((int)other.Diagnostic);
                return diagnostic != 0 ? diagnostic : ((int)Field).CompareTo((int)other.Field);
            }
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

        private enum NumberResult { Success, InvalidToken, UnsupportedOrOverflow, Malformed }

        private sealed class Reader
        {
            private const int MaximumValueDepth = 16;
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

            public bool TryReadPropertyName(out string value)
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

            public NumberResult TryReadIntegerValue(out int value)
            {
                value = 0;
                if (IsAtEnd)
                    return NumberResult.Malformed;
                if (Peek != '-' && (Peek < '0' || Peek > '9'))
                    return TrySkipValue(0) ? NumberResult.InvalidToken : NumberResult.Malformed;

                bool negative = TryConsume('-');
                if (IsAtEnd || Peek < '0' || Peek > '9')
                    return NumberResult.Malformed;

                bool unsupported = false;
                long magnitude = 0;
                if (Peek == '0')
                {
                    index++;
                    if (!IsAtEnd && Peek >= '0' && Peek <= '9')
                    {
                        unsupported = true;
                        while (!IsAtEnd && Peek >= '0' && Peek <= '9') index++;
                    }
                }
                else
                {
                    while (!IsAtEnd && Peek >= '0' && Peek <= '9')
                    {
                        int digit = Peek - '0';
                        if (magnitude > (int.MaxValue - digit) / 10L)
                            unsupported = true;
                        else if (!unsupported)
                            magnitude = magnitude * 10 + digit;
                        index++;
                    }
                }

                if (!IsAtEnd && Peek == '.')
                {
                    unsupported = true;
                    index++;
                    if (IsAtEnd || Peek < '0' || Peek > '9')
                        return NumberResult.Malformed;
                    while (!IsAtEnd && Peek >= '0' && Peek <= '9') index++;
                }
                if (!IsAtEnd && (Peek == 'e' || Peek == 'E'))
                {
                    unsupported = true;
                    index++;
                    if (!IsAtEnd && (Peek == '+' || Peek == '-')) index++;
                    if (IsAtEnd || Peek < '0' || Peek > '9')
                        return NumberResult.Malformed;
                    while (!IsAtEnd && Peek >= '0' && Peek <= '9') index++;
                }

                if (unsupported)
                    return NumberResult.UnsupportedOrOverflow;
                value = negative ? -(int)magnitude : (int)magnitude;
                return NumberResult.Success;
            }

            private bool TrySkipValue(int depth)
            {
                if (depth > MaximumValueDepth || IsAtEnd)
                    return false;
                if (Peek == '"')
                    return TrySkipString();
                if (TryConsumeLiteral("true") || TryConsumeLiteral("false") || TryConsumeLiteral("null"))
                    return true;
                if (TryConsume('['))
                    return TrySkipCollection(']', depth);
                if (TryConsume('{'))
                    return TrySkipObject(depth);
                return false;
            }

            private bool TrySkipCollection(char end, int depth)
            {
                SkipWhitespace();
                if (TryConsume(end)) return true;
                while (TrySkipValue(depth + 1))
                {
                    SkipWhitespace();
                    if (TryConsume(end)) return true;
                    if (!TryConsume(',')) return false;
                    SkipWhitespace();
                }
                return false;
            }

            private bool TrySkipObject(int depth)
            {
                SkipWhitespace();
                if (TryConsume('}')) return true;
                while (TrySkipString())
                {
                    SkipWhitespace();
                    if (!TryConsume(':')) return false;
                    SkipWhitespace();
                    if (!TrySkipValue(depth + 1)) return false;
                    SkipWhitespace();
                    if (TryConsume('}')) return true;
                    if (!TryConsume(',')) return false;
                    SkipWhitespace();
                }
                return false;
            }

            private bool TrySkipString()
            {
                if (!TryConsume('"')) return false;
                while (!IsAtEnd)
                {
                    char current = source[index++];
                    if (current == '"') return true;
                    if (current < 0x20) return false;
                    if (current != '\\') continue;
                    if (IsAtEnd) return false;
                    char escaped = source[index++];
                    if (escaped == 'u')
                    {
                        for (int count = 0; count < 4; count++)
                            if (IsAtEnd || !IsHex(source[index++])) return false;
                    }
                    else if ("\"\\/bfnrt".IndexOf(escaped) < 0)
                        return false;
                }
                return false;
            }

            private bool TryConsumeLiteral(string literal)
            {
                if (index + literal.Length > source.Length ||
                    !string.Equals(source.Substring(index, literal.Length), literal, StringComparison.Ordinal))
                    return false;
                index += literal.Length;
                return true;
            }

            private static bool IsHex(char value) =>
                (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f') ||
                (value >= 'A' && value <= 'F');
        }
    }
}
