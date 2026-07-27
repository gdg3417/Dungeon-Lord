using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonBuilder.M0;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum ProductionSpatialGeneratedSetDiagnostic
    {
        None = 0, MissingInput = 1, BlankPath = 2, InvalidPath = 3, DuplicatePath = 4,
        MissingOutput = 5, ExtraOutput = 6, EmptyFile = 7, InvalidUtf8 = 8, BomPresent = 9,
        InvalidLineEnding = 10, InvalidTrailingNewline = 11, MalformedJson = 12,
        InvalidJsonRoot = 13, DuplicateField = 14, CaseAmbiguousField = 15, UnknownField = 16,
        MissingRequiredField = 17, WrongFieldType = 18, UnsupportedNumber = 19,
        IntegerOverflow = 20, UnknownEnum = 21, CatalogIdentityMismatch = 22,
        StringTableIdentityMismatch = 23, ManifestIdentityMismatch = 24,
        ContentVersionMismatch = 25, LanguageMismatch = 26, ManifestRegistrationMismatch = 27,
        LocalizationInvalid = 28, WorkloadExceeded = 29, CatalogInvalid = 30,
        NoncanonicalOutput = 31, DiagnosticLimitExceeded = 32
    }

    [Serializable]
    public sealed class ProductionSpatialContentManifest
    {
        public string schema;
        public int schemaVersion;
        public string contentVersion;
        public ProductionSpatialRequiredSchema[] requiredSchemas = Array.Empty<ProductionSpatialRequiredSchema>();
    }

    [Serializable]
    public sealed class ProductionSpatialRequiredSchema
    {
        public string schemaId;
        public int schemaVersion;
    }

    public sealed class ProductionSpatialGeneratedFile
    {
        private readonly byte[] bytes;
        public ProductionSpatialGeneratedFile(string path, byte[] bytes)
        {
            Path = path;
            this.bytes = bytes == null ? null : (byte[])bytes.Clone();
        }
        public string Path { get; }
        public byte[] Bytes => bytes == null ? null : (byte[])bytes.Clone();
    }

    public sealed class ProductionSpatialGeneratedSet
    {
        private readonly ProductionSpatialGeneratedFile[] files;
        public ProductionSpatialGeneratedSet(IEnumerable<ProductionSpatialGeneratedFile> files)
        {
            this.files = files == null ? null : files.Select(file => file == null
                ? null : new ProductionSpatialGeneratedFile(file.Path, file.Bytes)).ToArray();
        }
        public ProductionSpatialGeneratedFile[] Files => files == null ? null : files.Select(file => file == null
            ? null : new ProductionSpatialGeneratedFile(file.Path, file.Bytes)).ToArray();
    }

    public sealed class ProductionSpatialParsedSet
    {
        internal ProductionSpatialParsedSet(SpatialContentCatalog catalog, StringTable english,
            ProductionSpatialContentManifest manifest)
        { Catalog = catalog; English = english; Manifest = manifest; }
        public SpatialContentCatalog Catalog { get; }
        public StringTable English { get; }
        public ProductionSpatialContentManifest Manifest { get; }
    }

    public sealed class ProductionSpatialGeneratedSetResult
    {
        internal ProductionSpatialGeneratedSetResult(ProductionSpatialParsedSet value,
            IEnumerable<ProductionSpatialGeneratedSetDiagnostic> diagnostics)
        { Value = value; Diagnostics = diagnostics.Distinct().OrderBy(item => (int)item).ToArray(); }
        public bool Success => Value != null && Diagnostics.Length == 0;
        public ProductionSpatialParsedSet Value { get; }
        public ProductionSpatialGeneratedSetDiagnostic[] Diagnostics { get; }
    }

    public static class ProductionSpatialGeneratedSetParser
    {
        public const string ManifestPath = "Assets/_Project/Data/Production/DungeonSpatial/content_manifest.json";
        public const string CatalogPath = "Assets/_Project/Data/Production/DungeonSpatial/dungeon_spatial_content.json";
        public const string EnglishPath = "Assets/_Project/Data/Production/DungeonSpatial/string_table_en.json";
        private static readonly string[] CanonicalPaths = { ManifestPath, CatalogPath, EnglishPath };
        private static readonly IReadOnlyList<string> PublicPaths = Array.AsReadOnly(CanonicalPaths);
        public static IReadOnlyList<string> RequiredPaths => PublicPaths;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        public static ProductionSpatialGeneratedSetResult ParseAndValidate(ProductionSpatialGeneratedSet supplied,
            SpatialContentValidationWorkloadLimits limits)
        {
            if (!limits.IsValid) return Failure(ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded);
            var diagnostics = new DiagnosticCollector(limits.MaximumIssues);
            var files = supplied?.Files;
            if (files == null) return Failure(ProductionSpatialGeneratedSetDiagnostic.MissingInput);
            var map = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (ProductionSpatialGeneratedFile file in files)
            {
                string path = file?.Path;
                if (string.IsNullOrWhiteSpace(path)) { diagnostics.Add(ProductionSpatialGeneratedSetDiagnostic.BlankPath); continue; }
                if (!IsNormalizedPath(path) || (Array.IndexOf(CanonicalPaths, path) < 0 &&
                    CanonicalPaths.Any(required => string.Equals(required, path, StringComparison.OrdinalIgnoreCase))))
                { diagnostics.Add(ProductionSpatialGeneratedSetDiagnostic.InvalidPath); continue; }
                if (map.ContainsKey(path)) { diagnostics.Add(ProductionSpatialGeneratedSetDiagnostic.DuplicatePath); continue; }
                map.Add(path, file.Bytes);
            }
            foreach (string path in CanonicalPaths)
                if (!map.ContainsKey(path)) diagnostics.Add(ProductionSpatialGeneratedSetDiagnostic.MissingOutput);
            if (map.Keys.Any(path => Array.IndexOf(CanonicalPaths, path) < 0)) diagnostics.Add(ProductionSpatialGeneratedSetDiagnostic.ExtraOutput);
            if (diagnostics.HasAny) return diagnostics.Failure();

            var parseBudget = new StrictJsonWorkloadBudget(limits);
            if (!TryObject<ProductionSpatialContentManifest>(map[ManifestPath], out var manifest, diagnostics, parseBudget) |
                !TryObject<SpatialContentCatalog>(map[CatalogPath], out var catalog, diagnostics, parseBudget) |
                !TryObject<StringTable>(map[EnglishPath], out var english, diagnostics, parseBudget))
                return diagnostics.Failure();

            ValidateIdentities(manifest, catalog, english, diagnostics);
            var keys = ValidateEnglish(english, limits, diagnostics);
            var referencedKeys = new HashSet<string>((catalog?.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                .Select(value => value?.LocalizationKey)
                .Concat((catalog?.Corridors ?? Array.Empty<CorridorSpatialDefinition>()).Select(value => value?.LocalizationKey))
                .Concat((catalog?.FixedStructures ?? Array.Empty<FixedSpatialStructureDefinition>()).Select(value => value?.LocalizationKey)),
                StringComparer.Ordinal);
            if (!keys.SetEquals(referencedKeys)) diagnostics.Add(ProductionSpatialGeneratedSetDiagnostic.LocalizationInvalid);
            long englishCharacters = 0L;
            bool englishCharacterOverflow = !TryAddCharacters(ref englishCharacters, english?.schema) ||
                !TryAddCharacters(ref englishCharacters, english?.language);
            foreach (StringEntry entry in english?.entries ?? Array.Empty<StringEntry>())
                if (!TryAddCharacters(ref englishCharacters, entry?.text)) englishCharacterOverflow = true;
            if (englishCharacterOverflow) diagnostics.Add(ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded);
            SpatialContentValidationResult validation = SpatialContentValidator.Validate(catalog, limits, keys, englishCharacters);
            SpatialContentCatalog canonical = null;
            if (!validation.IsValid)
                diagnostics.Add(IsWorkloadFailure(validation)
                    ? ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded
                    : ProductionSpatialGeneratedSetDiagnostic.CatalogInvalid);
            else if (!SpatialContentCanonicalizer.TryCanonicalize(catalog, limits, out canonical))
                diagnostics.Add(ProductionSpatialGeneratedSetDiagnostic.CatalogInvalid);
            if (diagnostics.HasAny) return diagnostics.Failure();

            english.entries = english.entries.OrderBy(entry => entry.key, StringComparer.Ordinal).ToArray();
            manifest.requiredSchemas = manifest.requiredSchemas.OrderBy(entry => entry.schemaId, StringComparer.Ordinal).ToArray();
            if (!BytesEqual(map[CatalogPath], SerializeCanonical(canonical)) || !BytesEqual(map[EnglishPath], SerializeCanonical(english)) ||
                !BytesEqual(map[ManifestPath], SerializeCanonical(manifest)))
                return Failure(ProductionSpatialGeneratedSetDiagnostic.NoncanonicalOutput);
            return new ProductionSpatialGeneratedSetResult(new ProductionSpatialParsedSet(canonical, english, manifest),
                Array.Empty<ProductionSpatialGeneratedSetDiagnostic>());
        }

        public static byte[] SerializeCanonical(object value) => Utf8.GetBytes(JsonUtility.ToJson(value, true) + "\n");

        private static bool TryObject<T>(byte[] bytes, out T value,
            DiagnosticCollector diagnostics, StrictJsonWorkloadBudget budget) where T : class
        {
            value = null;
            int jsonLength;
            if (!TryNormalize(bytes, out jsonLength, diagnostics)) return false;
            if (!StrictJson.TryParse(bytes, jsonLength, typeof(T), diagnostics, budget,
                out JsonNode root, out var diagnostic))
            { diagnostics.Add(diagnostic); return false; }
            if (root.Kind != JsonKind.Object) { diagnostics.Add(ProductionSpatialGeneratedSetDiagnostic.InvalidJsonRoot); return false; }
            StrictJson.Validate(typeof(T), root, diagnostics);
            if (diagnostics.HasAny) return false;
            try { value = JsonUtility.FromJson<T>(StrictJson.ToCompactJson(root)); }
            catch { diagnostics.Add(ProductionSpatialGeneratedSetDiagnostic.MalformedJson); }
            return value != null;
        }

        private static bool TryNormalize(byte[] bytes, out int jsonLength, DiagnosticCollector issues)
        {
            jsonLength = 0;
            if (bytes == null || bytes.Length == 0) { issues.Add(ProductionSpatialGeneratedSetDiagnostic.EmptyFile); return false; }
            if (bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
            { issues.Add(ProductionSpatialGeneratedSetDiagnostic.BomPresent); return false; }
            if (bytes.Contains((byte)'\r')) { issues.Add(ProductionSpatialGeneratedSetDiagnostic.InvalidLineEnding); return false; }
            if (bytes[bytes.Length - 1] != (byte)'\n' || (bytes.Length > 1 && bytes[bytes.Length - 2] == (byte)'\n'))
            { issues.Add(ProductionSpatialGeneratedSetDiagnostic.InvalidTrailingNewline); return false; }
            try
            {
                jsonLength = bytes.Length - 1;
                Utf8.GetCharCount(bytes, 0, jsonLength);
                return true;
            }
            catch (DecoderFallbackException) { issues.Add(ProductionSpatialGeneratedSetDiagnostic.InvalidUtf8); return false; }
        }

        private static void ValidateIdentities(ProductionSpatialContentManifest manifest, SpatialContentCatalog catalog,
            StringTable english, DiagnosticCollector issues)
        {
            if (catalog?.Metadata == null || catalog.Metadata.SchemaId != "dungeon_spatial_content" || catalog.Metadata.SchemaVersion != 1)
                issues.Add(ProductionSpatialGeneratedSetDiagnostic.CatalogIdentityMismatch);
            if (english == null || english.schema != "string_table" || english.schemaVersion != 1)
                issues.Add(ProductionSpatialGeneratedSetDiagnostic.StringTableIdentityMismatch);
            if (manifest == null || manifest.schema != "content_manifest" || manifest.schemaVersion != 1)
                issues.Add(ProductionSpatialGeneratedSetDiagnostic.ManifestIdentityMismatch);
            if (string.IsNullOrWhiteSpace(manifest?.contentVersion) ||
                string.IsNullOrWhiteSpace(catalog?.Metadata?.ContentVersion) ||
                !string.Equals(manifest.contentVersion, catalog.Metadata.ContentVersion, StringComparison.Ordinal))
                issues.Add(ProductionSpatialGeneratedSetDiagnostic.ContentVersionMismatch);
            if (english?.language != "en") issues.Add(ProductionSpatialGeneratedSetDiagnostic.LanguageMismatch);
            var registrations = manifest?.requiredSchemas;
            if (registrations == null || registrations.Length != 2 || registrations[0] == null || registrations[1] == null ||
                registrations[0].schemaId != "dungeon_spatial_content" || registrations[0].schemaVersion != 1 ||
                registrations[1].schemaId != "string_table" || registrations[1].schemaVersion != 1)
                issues.Add(ProductionSpatialGeneratedSetDiagnostic.ManifestRegistrationMismatch);
        }

        private static ISet<string> ValidateEnglish(StringTable table, SpatialContentValidationWorkloadLimits limits,
            DiagnosticCollector issues)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            long count = 0L, characters = 0L;
            bool overflow = !TryAddCharacters(ref characters, table?.schema) ||
                !TryAddCharacters(ref characters, table?.language);
            if (table?.entries == null) { issues.Add(ProductionSpatialGeneratedSetDiagnostic.LocalizationInvalid); return keys; }
            foreach (StringEntry entry in table.entries)
            {
                if (count == long.MaxValue) overflow = true; else count++;
                if (entry == null || string.IsNullOrWhiteSpace(entry.key) || string.IsNullOrWhiteSpace(entry.text) || !keys.Add(entry.key))
                    issues.Add(ProductionSpatialGeneratedSetDiagnostic.LocalizationInvalid);
                else if (!TryAddCharacters(ref characters, entry.key) ||
                    !TryAddCharacters(ref characters, entry.text)) overflow = true;
            }
            if (overflow || !limits.IsValid || count > limits.MaximumNestedRecords || characters > limits.MaximumStringCharacters)
                issues.Add(ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded);
            return keys;
        }

        private static bool IsNormalizedPath(string path) => !path.StartsWith("/", StringComparison.Ordinal) &&
            path.IndexOf('\\') < 0 && path.IndexOf("..", StringComparison.Ordinal) < 0 &&
            path.IndexOf("//", StringComparison.Ordinal) < 0 && !path.Contains(":");
        private static bool BytesEqual(byte[] a, byte[] b) => a != null && b != null && a.SequenceEqual(b);
        private static bool TryAddCharacters(ref long total, string value)
        {
            long additional = value?.Length ?? 0L;
            if (additional < 0L || total < 0L || total > long.MaxValue - additional) return false;
            total += additional;
            return true;
        }
        private static bool IsWorkloadFailure(SpatialContentValidationResult validation) => validation.Issues.Any(issue =>
            issue.Reason == SpatialContentValidationReason.WorkloadLimitsInvalid ||
            issue.Reason == SpatialContentValidationReason.WorkloadExceeded);
        private static ProductionSpatialGeneratedSetResult Failure(ProductionSpatialGeneratedSetDiagnostic diagnostic) =>
            new ProductionSpatialGeneratedSetResult(null, new[] { diagnostic });
    }

    internal sealed class DiagnosticCollector
    {
        private readonly int maximum;
        private readonly List<ProductionSpatialGeneratedSetDiagnostic> diagnostics =
            new List<ProductionSpatialGeneratedSetDiagnostic>();
        private int ordinaryCount;
        private bool overflowed;

        internal DiagnosticCollector(int maximum) { this.maximum = maximum; }
        internal bool HasAny => diagnostics.Count != 0 || overflowed;
        internal bool LimitExceeded => overflowed;
        internal void Add(ProductionSpatialGeneratedSetDiagnostic diagnostic)
        {
            if (overflowed) return;
            if (ordinaryCount >= maximum)
            {
                overflowed = true;
                return;
            }
            ordinaryCount++;
            diagnostics.Add(diagnostic);
        }
        internal ProductionSpatialGeneratedSetResult Failure()
        {
            IEnumerable<ProductionSpatialGeneratedSetDiagnostic> result = overflowed
                ? diagnostics.Concat(new[] { ProductionSpatialGeneratedSetDiagnostic.DiagnosticLimitExceeded })
                : diagnostics;
            return new ProductionSpatialGeneratedSetResult(null, result);
        }
    }

    internal enum JsonKind { Object, Array, String, Number, Boolean, Null }
    internal enum JsonNumberStatus { Success, Unsupported, Overflow }
    internal sealed class JsonNode
    {
        internal JsonKind Kind; internal string Text; internal List<KeyValuePair<string, JsonNode>> Fields;
        internal List<JsonNode> Items; internal JsonNumberStatus NumberStatus;
    }

    internal sealed class StrictJsonWorkloadBudget
    {
        private readonly SpatialContentValidationWorkloadLimits limits;
        private long topLevelRecords;
        private long nestedRecords;
        private long stringCharacters;
        private long structuralRecords;
        private long structuralStringCharacters;

        internal StrictJsonWorkloadBudget(SpatialContentValidationWorkloadLimits limits) { this.limits = limits; }
        internal int MaximumPropertyNameCharacters => limits.MaximumStringCharacters;

        internal bool TryAddArrayItem(Type elementType, Type rootType, bool unknown)
        {
            if (rootType == typeof(ProductionSpatialContentManifest) || unknown)
                return TryAdd(ref structuralRecords, 1L, limits.MaximumNestedRecords);
            bool topLevel = !unknown && (elementType == typeof(FloorSpatialConfiguration) ||
                elementType == typeof(RoomSpatialDefinition) || elementType == typeof(CorridorSpatialDefinition) ||
                elementType == typeof(FixedSpatialStructureDefinition) ||
                elementType == typeof(SpatialSocketTypeDefinition));
            return topLevel
                ? TryAdd(ref topLevelRecords, 1L, limits.MaximumTopLevelRecords)
                : TryAdd(ref nestedRecords, 1L, limits.MaximumNestedRecords);
        }

        internal bool TryAddStringCharacter(Type rootType, bool unknown)
        {
            if (rootType == typeof(ProductionSpatialContentManifest) || unknown)
                return TryAdd(ref structuralStringCharacters, 1L, limits.MaximumStringCharacters);
            return TryAdd(ref stringCharacters, 1L, limits.MaximumStringCharacters);
        }

        private static bool TryAdd(ref long current, long amount, long maximum)
        {
            if (amount < 0L || current < 0L || maximum < 0L || current > maximum - amount) return false;
            current += amount;
            return true;
        }
    }

    internal static class StrictJson
    {
        internal static bool TryParse(byte[] bytes, int length, Type rootType, DiagnosticCollector issues,
            StrictJsonWorkloadBudget budget, out JsonNode node,
            out ProductionSpatialGeneratedSetDiagnostic diagnostic)
        {
            var reader = new Reader(bytes, length, rootType, issues, budget);
            node = null; diagnostic = ProductionSpatialGeneratedSetDiagnostic.None;
            try { node = reader.Value(0, rootType, false); reader.Space(); if (!reader.End) throw new FormatException(); return true; }
            catch (JsonFailure failure) { diagnostic = failure.Diagnostic; return false; }
            catch { diagnostic = ProductionSpatialGeneratedSetDiagnostic.MalformedJson; return false; }
        }

        internal static string ToCompactJson(JsonNode node)
        {
            var result = new StringBuilder();
            AppendNode(result, node);
            return result.ToString();
        }

        private static void AppendNode(StringBuilder result, JsonNode node)
        {
            if (node.Kind == JsonKind.Object)
            {
                result.Append('{');
                for (int index = 0; index < node.Fields.Count; index++)
                {
                    if (index != 0) result.Append(',');
                    AppendString(result, node.Fields[index].Key); result.Append(':');
                    AppendNode(result, node.Fields[index].Value);
                }
                result.Append('}'); return;
            }
            if (node.Kind == JsonKind.Array)
            {
                result.Append('[');
                for (int index = 0; index < node.Items.Count; index++)
                { if (index != 0) result.Append(','); AppendNode(result, node.Items[index]); }
                result.Append(']'); return;
            }
            if (node.Kind == JsonKind.String) { AppendString(result, node.Text); return; }
            if (node.Kind == JsonKind.Number) { result.Append(node.Text); return; }
            if (node.Kind == JsonKind.Boolean) { result.Append(node.Text); return; }
            result.Append("null");
        }

        private static void AppendString(StringBuilder result, string value)
        {
            result.Append('"');
            foreach (char character in value)
            {
                switch (character)
                {
                    case '"': result.Append("\\\""); break;
                    case '\\': result.Append("\\\\"); break;
                    case '\b': result.Append("\\b"); break;
                    case '\f': result.Append("\\f"); break;
                    case '\n': result.Append("\\n"); break;
                    case '\r': result.Append("\\r"); break;
                    case '\t': result.Append("\\t"); break;
                    default:
                        if (character < 0x20) result.Append("\\u").Append(
                            ((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        else result.Append(character);
                        break;
                }
            }
            result.Append('"');
        }

        internal static void Validate(Type type, JsonNode node, DiagnosticCollector issues)
        {
            if (type.IsArray)
            {
                if (node.Kind != JsonKind.Array) { issues.Add(ProductionSpatialGeneratedSetDiagnostic.WrongFieldType); return; }
                foreach (JsonNode item in node.Items) Validate(type.GetElementType(), item, issues);
                return;
            }
            if (type == typeof(string)) { if (node.Kind != JsonKind.String) issues.Add(ProductionSpatialGeneratedSetDiagnostic.WrongFieldType); return; }
            if (type == typeof(int)) { ValidateInt(node, issues); return; }
            if (type.IsEnum) { if (!ValidateInt(node, issues) || !Enum.IsDefined(type, int.Parse(node.Text))) issues.Add(ProductionSpatialGeneratedSetDiagnostic.UnknownEnum); return; }
            if (type == typeof(bool)) { if (node.Kind != JsonKind.Boolean) issues.Add(ProductionSpatialGeneratedSetDiagnostic.WrongFieldType); return; }
            if (node.Kind == JsonKind.Null) { if (type.IsValueType) issues.Add(ProductionSpatialGeneratedSetDiagnostic.WrongFieldType); return; }
            if (node.Kind != JsonKind.Object) { issues.Add(ProductionSpatialGeneratedSetDiagnostic.WrongFieldType); return; }
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var pair in node.Fields)
            {
                FieldInfo exact = fields.FirstOrDefault(field => field.Name == pair.Key);
                if (exact == null)
                {
                    issues.Add(fields.Any(field => string.Equals(field.Name, pair.Key, StringComparison.OrdinalIgnoreCase))
                        ? ProductionSpatialGeneratedSetDiagnostic.CaseAmbiguousField : ProductionSpatialGeneratedSetDiagnostic.UnknownField);
                }
                else Validate(exact.FieldType, pair.Value, issues);
            }
            foreach (FieldInfo field in fields)
                if (!node.Fields.Any(pair => pair.Key == field.Name)) issues.Add(ProductionSpatialGeneratedSetDiagnostic.MissingRequiredField);
        }

        private static bool ValidateInt(JsonNode node, DiagnosticCollector issues)
        {
            if (node.Kind != JsonKind.Number) { issues.Add(ProductionSpatialGeneratedSetDiagnostic.WrongFieldType); return false; }
            if (node.NumberStatus == JsonNumberStatus.Unsupported)
            { issues.Add(ProductionSpatialGeneratedSetDiagnostic.UnsupportedNumber); return false; }
            if (node.NumberStatus == JsonNumberStatus.Overflow)
            { issues.Add(ProductionSpatialGeneratedSetDiagnostic.IntegerOverflow); return false; }
            return true;
        }

        private sealed class JsonFailure : Exception { internal JsonFailure(ProductionSpatialGeneratedSetDiagnostic d) { Diagnostic = d; } internal ProductionSpatialGeneratedSetDiagnostic Diagnostic; }
        private sealed class Reader
        {
            private readonly byte[] bytes;
            private readonly int length;
            private readonly Type rootType;
            private readonly DiagnosticCollector issues;
            private readonly StrictJsonWorkloadBudget budget;
            private int i;
            internal Reader(byte[] bytes, int length, Type rootType, DiagnosticCollector issues,
                StrictJsonWorkloadBudget budget)
            { this.bytes = bytes; this.length = length; this.rootType = rootType; this.issues = issues; this.budget = budget; }
            internal bool End => i == length;
            internal void Space() { while (i < length && (bytes[i] == (byte)' ' || bytes[i] == (byte)'\t' || bytes[i] == (byte)'\n')) i++; }
            internal JsonNode Value(int depth, Type expectedType, bool unknown)
            {
                if (depth > 64) throw new FormatException(); Space(); if (i >= length) throw new FormatException();
                char c = (char)bytes[i]; if (c == '{') return Object(depth, expectedType, unknown); if (c == '[') return Array(depth, expectedType, unknown);
                if (c == '"') return new JsonNode { Kind = JsonKind.String, Text = String(true, unknown) };
                if (c == 't' && Literal("true")) return new JsonNode { Kind = JsonKind.Boolean, Text = "true" };
                if (c == 'f' && Literal("false")) return new JsonNode { Kind = JsonKind.Boolean, Text = "false" };
                if (c == 'n' && Literal("null")) return new JsonNode { Kind = JsonKind.Null };
                if (c == '-' || c == '+' || (c >= '0' && c <= '9')) return Number(); throw new FormatException();
            }
            private JsonNode Object(int depth, Type expectedType, bool unknown)
            {
                i++; var fields = new List<KeyValuePair<string, JsonNode>>(); var names = new HashSet<string>(StringComparer.Ordinal); Space();
                if (Take('}')) return new JsonNode { Kind = JsonKind.Object, Fields = fields };
                FieldInfo[] expectedFields = unknown || expectedType == null
                    ? System.Array.Empty<FieldInfo>()
                    : expectedType.GetFields(BindingFlags.Instance | BindingFlags.Public);
                while (true)
                {
                    Space(); string name = String(false, false);
                    if (!names.Add(name)) throw new JsonFailure(ProductionSpatialGeneratedSetDiagnostic.DuplicateField);
                    Space(); Need(':');
                    FieldInfo exact = expectedFields.FirstOrDefault(field => field.Name == name);
                    if (exact == null)
                    {
                        FieldInfo ambiguous = expectedFields.FirstOrDefault(field =>
                            string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase));
                        issues.Add(ambiguous != null ? ProductionSpatialGeneratedSetDiagnostic.CaseAmbiguousField
                            : ProductionSpatialGeneratedSetDiagnostic.UnknownField);
                        if (issues.LimitExceeded) throw new JsonFailure(ProductionSpatialGeneratedSetDiagnostic.DiagnosticLimitExceeded);
                        JsonNode invalidValue = Value(depth + 1, ambiguous?.FieldType, ambiguous == null);
                        if (ambiguous != null)
                            fields.Add(new KeyValuePair<string, JsonNode>(ambiguous.Name, invalidValue));
                    }
                    else fields.Add(new KeyValuePair<string, JsonNode>(name, Value(depth + 1, exact.FieldType, false)));
                    Space(); if (Take('}')) break; Need(',');
                }
                return new JsonNode { Kind = JsonKind.Object, Fields = fields };
            }
            private JsonNode Array(int depth, Type expectedType, bool unknown)
            {
                i++; var items = new List<JsonNode>(); Space(); if (Take(']')) return new JsonNode { Kind = JsonKind.Array, Items = items };
                Type elementType = !unknown && expectedType != null && expectedType.IsArray
                    ? expectedType.GetElementType() : null;
                while (true)
                {
                    if (!budget.TryAddArrayItem(elementType, rootType, unknown))
                        throw new JsonFailure(ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded);
                    JsonNode item = Value(depth + 1, elementType, unknown);
                    if (!unknown) items.Add(item);
                    Space(); if (Take(']')) break; Need(',');
                }
                return new JsonNode { Kind = JsonKind.Array, Items = items };
            }
            private JsonNode Number()
            {
                bool negative = false, unsupported = false, overflow = false;
                if (Take('+')) unsupported = true;
                else negative = Take('-');
                if (i >= length || bytes[i] < (byte)'0' || bytes[i] > (byte)'9') throw new FormatException();
                bool leadingZero = bytes[i] == (byte)'0';
                long magnitude = 0L;
                long maximum = negative ? 2147483648L : int.MaxValue;
                long digits = 0L;
                while (i < length && bytes[i] >= (byte)'0' && bytes[i] <= (byte)'9')
                {
                    int digit = bytes[i++] - (byte)'0';
                    if (digits == long.MaxValue) overflow = true; else digits++;
                    if (magnitude > (maximum - digit) / 10L) overflow = true;
                    else if (!overflow) magnitude = magnitude * 10L + digit;
                }
                if (leadingZero && digits > 1) unsupported = true;
                if (negative && digits == 1 && magnitude == 0L) unsupported = true;
                if (Take('.'))
                {
                    unsupported = true;
                    if (i >= length || bytes[i] < (byte)'0' || bytes[i] > (byte)'9') throw new FormatException();
                    while (i < length && bytes[i] >= (byte)'0' && bytes[i] <= (byte)'9') i++;
                }
                if (i < length && (bytes[i] == (byte)'e' || bytes[i] == (byte)'E'))
                {
                    unsupported = true; i++;
                    if (i < length && (bytes[i] == (byte)'+' || bytes[i] == (byte)'-')) i++;
                    if (i >= length || bytes[i] < (byte)'0' || bytes[i] > (byte)'9') throw new FormatException();
                    while (i < length && bytes[i] >= (byte)'0' && bytes[i] <= (byte)'9') i++;
                }
                JsonNumberStatus status = unsupported ? JsonNumberStatus.Unsupported :
                    overflow ? JsonNumberStatus.Overflow : JsonNumberStatus.Success;
                int value = negative
                    ? (magnitude == 2147483648L ? int.MinValue : -(int)magnitude)
                    : (int)magnitude;
                return new JsonNode
                {
                    Kind = JsonKind.Number, NumberStatus = status,
                    Text = status == JsonNumberStatus.Success ? value.ToString(CultureInfo.InvariantCulture) : "0"
                };
            }
            private string String(bool contentValue, bool unknown)
            {
                Need('"'); var b = new StringBuilder();
                while (i < length)
                {
                    byte current = bytes[i++]; if (current == (byte)'"') return b.ToString();
                    if (current < 0x20) throw new FormatException();
                    if (current >= 0x80)
                    {
                        int codePoint = DecodeUtf8CodePoint(current);
                        if (codePoint <= 0xffff) AppendDecoded(b, (char)codePoint, contentValue, unknown);
                        else
                        {
                            codePoint -= 0x10000;
                            AppendDecoded(b, (char)(0xd800 + (codePoint >> 10)), contentValue, unknown);
                            AppendDecoded(b, (char)(0xdc00 + (codePoint & 0x3ff)), contentValue, unknown);
                        }
                        continue;
                    }
                    char decoded;
                    char c = (char)current;
                    if (c != '\\') decoded = c;
                    else
                    {
                        if (i >= length) throw new FormatException(); char e = (char)bytes[i++];
                        if ("\"\\/".IndexOf(e) >= 0) decoded = e;
                        else if (e == 'b') decoded = '\b'; else if (e == 'f') decoded = '\f';
                        else if (e == 'n') decoded = '\n'; else if (e == 'r') decoded = '\r';
                        else if (e == 't') decoded = '\t';
                        else if (e == 'u' && i + 4 <= length)
                        { decoded = (char)ReadHex4(); }
                        else throw new FormatException();
                    }
                    AppendDecoded(b, decoded, contentValue, unknown);
                }
                throw new FormatException();
            }
            private void AppendDecoded(StringBuilder result, char value, bool contentValue, bool unknown)
            {
                if ((contentValue || unknown) && !budget.TryAddStringCharacter(rootType, unknown))
                    throw new JsonFailure(ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded);
                if (!contentValue && result.Length >= budget.MaximumPropertyNameCharacters)
                    throw new JsonFailure(ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded);
                result.Append(value);
            }
            private int DecodeUtf8CodePoint(byte first)
            {
                int count, value, minimum;
                if (first >= 0xc2 && first <= 0xdf) { count = 1; value = first & 0x1f; minimum = 0x80; }
                else if (first >= 0xe0 && first <= 0xef) { count = 2; value = first & 0x0f; minimum = 0x800; }
                else if (first >= 0xf0 && first <= 0xf4) { count = 3; value = first & 0x07; minimum = 0x10000; }
                else throw new FormatException();
                for (int index = 0; index < count; index++)
                {
                    if (i >= length || (bytes[i] & 0xc0) != 0x80) throw new FormatException();
                    value = (value << 6) | (bytes[i++] & 0x3f);
                }
                if (value < minimum || value > 0x10ffff || (value >= 0xd800 && value <= 0xdfff))
                    throw new FormatException();
                return value;
            }
            private int ReadHex4()
            {
                int value = 0;
                for (int index = 0; index < 4; index++)
                {
                    byte current = bytes[i++]; int digit;
                    if (current >= (byte)'0' && current <= (byte)'9') digit = current - (byte)'0';
                    else if (current >= (byte)'a' && current <= (byte)'f') digit = current - (byte)'a' + 10;
                    else if (current >= (byte)'A' && current <= (byte)'F') digit = current - (byte)'A' + 10;
                    else throw new FormatException();
                    value = value * 16 + digit;
                }
                return value;
            }
            private bool Literal(string value)
            {
                if (i + value.Length > length) return false;
                for (int index = 0; index < value.Length; index++)
                    if (bytes[i + index] != (byte)value[index]) return false;
                i += value.Length; return true;
            }
            private bool Take(char c) { if (i < length && bytes[i] == (byte)c) { i++; return true; } return false; }
            private void Need(char c) { if (!Take(c)) throw new FormatException(); }
        }
    }
}
