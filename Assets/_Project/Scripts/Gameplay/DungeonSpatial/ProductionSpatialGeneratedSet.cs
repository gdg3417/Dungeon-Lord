using System;
using System.Collections.Generic;
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

            if (!TryObject<ProductionSpatialContentManifest>(map[ManifestPath], out var manifest, diagnostics) |
                !TryObject<SpatialContentCatalog>(map[CatalogPath], out var catalog, diagnostics) |
                !TryObject<StringTable>(map[EnglishPath], out var english, diagnostics))
                return diagnostics.Failure();

            ValidateIdentities(manifest, catalog, english, diagnostics);
            var keys = ValidateEnglish(english, limits, diagnostics);
            var referencedKeys = new HashSet<string>((catalog?.Rooms ?? Array.Empty<RoomSpatialDefinition>())
                .Select(value => value?.LocalizationKey)
                .Concat((catalog?.Corridors ?? Array.Empty<CorridorSpatialDefinition>()).Select(value => value?.LocalizationKey))
                .Concat((catalog?.FixedStructures ?? Array.Empty<FixedSpatialStructureDefinition>()).Select(value => value?.LocalizationKey)),
                StringComparer.Ordinal);
            if (!keys.SetEquals(referencedKeys)) diagnostics.Add(ProductionSpatialGeneratedSetDiagnostic.LocalizationInvalid);
            long englishCharacters = (english?.schema?.Length ?? 0) + (english?.language?.Length ?? 0);
            foreach (StringEntry entry in english?.entries ?? Array.Empty<StringEntry>())
            {
                int length = entry?.text?.Length ?? 0;
                if (englishCharacters > long.MaxValue - length) { englishCharacters = long.MaxValue; break; }
                englishCharacters += length;
            }
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
            DiagnosticCollector diagnostics) where T : class
        {
            value = null;
            string json;
            if (!TryDecode(bytes, out json, diagnostics)) return false;
            if (!StrictJson.TryParse(json, out JsonNode root, out var diagnostic))
            { diagnostics.Add(diagnostic); return false; }
            if (root.Kind != JsonKind.Object) { diagnostics.Add(ProductionSpatialGeneratedSetDiagnostic.InvalidJsonRoot); return false; }
            StrictJson.Validate(typeof(T), root, diagnostics);
            if (diagnostics.HasAny) return false;
            try { value = JsonUtility.FromJson<T>(json); }
            catch { diagnostics.Add(ProductionSpatialGeneratedSetDiagnostic.MalformedJson); }
            return value != null;
        }

        private static bool TryDecode(byte[] bytes, out string value, DiagnosticCollector issues)
        {
            value = null;
            if (bytes == null || bytes.Length == 0) { issues.Add(ProductionSpatialGeneratedSetDiagnostic.EmptyFile); return false; }
            if (bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
            { issues.Add(ProductionSpatialGeneratedSetDiagnostic.BomPresent); return false; }
            if (bytes.Contains((byte)'\r')) { issues.Add(ProductionSpatialGeneratedSetDiagnostic.InvalidLineEnding); return false; }
            if (bytes[bytes.Length - 1] != (byte)'\n' || (bytes.Length > 1 && bytes[bytes.Length - 2] == (byte)'\n'))
            { issues.Add(ProductionSpatialGeneratedSetDiagnostic.InvalidTrailingNewline); return false; }
            try { value = Utf8.GetString(bytes, 0, bytes.Length - 1); return true; }
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
            if (manifest?.contentVersion != "0.1.0" || catalog?.Metadata?.ContentVersion != "0.1.0")
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
            long count = 0, characters = (table?.schema?.Length ?? 0) + (table?.language?.Length ?? 0);
            if (table?.entries == null) { issues.Add(ProductionSpatialGeneratedSetDiagnostic.LocalizationInvalid); return keys; }
            foreach (StringEntry entry in table.entries)
            {
                count++;
                if (entry == null || string.IsNullOrWhiteSpace(entry.key) || string.IsNullOrWhiteSpace(entry.text) || !keys.Add(entry.key))
                    issues.Add(ProductionSpatialGeneratedSetDiagnostic.LocalizationInvalid);
                else characters += entry.key.Length + entry.text.Length;
            }
            if (!limits.IsValid || count > limits.MaximumNestedRecords || characters > limits.MaximumStringCharacters)
                issues.Add(ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded);
            return keys;
        }

        private static bool IsNormalizedPath(string path) => !path.StartsWith("/", StringComparison.Ordinal) &&
            path.IndexOf('\\') < 0 && path.IndexOf("..", StringComparison.Ordinal) < 0 &&
            path.IndexOf("//", StringComparison.Ordinal) < 0 && !path.Contains(":");
        private static bool BytesEqual(byte[] a, byte[] b) => a != null && b != null && a.SequenceEqual(b);
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
    internal sealed class JsonNode
    {
        internal JsonKind Kind; internal string Text; internal List<KeyValuePair<string, JsonNode>> Fields;
        internal List<JsonNode> Items;
    }

    internal static class StrictJson
    {
        internal static bool TryParse(string text, out JsonNode node, out ProductionSpatialGeneratedSetDiagnostic diagnostic)
        {
            var reader = new Reader(text); node = null; diagnostic = ProductionSpatialGeneratedSetDiagnostic.None;
            try { node = reader.Value(0); reader.Space(); if (!reader.End) throw new FormatException(); return true; }
            catch (JsonFailure failure) { diagnostic = failure.Diagnostic; return false; }
            catch { diagnostic = ProductionSpatialGeneratedSetDiagnostic.MalformedJson; return false; }
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
            if (node.Text.IndexOfAny(new[] { '.', 'e', 'E', '+' }) >= 0 || (node.Text.Length > 1 && node.Text[0] == '0') || node.Text == "-0")
            { issues.Add(ProductionSpatialGeneratedSetDiagnostic.UnsupportedNumber); return false; }
            if (!int.TryParse(node.Text, out _)) { issues.Add(ProductionSpatialGeneratedSetDiagnostic.IntegerOverflow); return false; }
            return true;
        }

        private sealed class JsonFailure : Exception { internal JsonFailure(ProductionSpatialGeneratedSetDiagnostic d) { Diagnostic = d; } internal ProductionSpatialGeneratedSetDiagnostic Diagnostic; }
        private sealed class Reader
        {
            private readonly string s; private int i; internal Reader(string value) { s = value; }
            internal bool End => i == s.Length; internal void Space() { while (i < s.Length && " \t\n\r".IndexOf(s[i]) >= 0) i++; }
            internal JsonNode Value(int depth)
            {
                if (depth > 64) throw new FormatException(); Space(); if (i >= s.Length) throw new FormatException();
                char c = s[i]; if (c == '{') return Object(depth); if (c == '[') return Array(depth);
                if (c == '"') return new JsonNode { Kind = JsonKind.String, Text = String() };
                if (c == 't' && Literal("true") || c == 'f' && Literal("false")) return new JsonNode { Kind = JsonKind.Boolean };
                if (c == 'n' && Literal("null")) return new JsonNode { Kind = JsonKind.Null };
                if (c == '-' || char.IsDigit(c)) return Number(); throw new FormatException();
            }
            private JsonNode Object(int depth)
            {
                i++; var fields = new List<KeyValuePair<string, JsonNode>>(); var names = new HashSet<string>(StringComparer.Ordinal); Space();
                if (Take('}')) return new JsonNode { Kind = JsonKind.Object, Fields = fields };
                while (true) { Space(); string name = String(); if (!names.Add(name)) throw new JsonFailure(ProductionSpatialGeneratedSetDiagnostic.DuplicateField); Space(); Need(':'); fields.Add(new KeyValuePair<string, JsonNode>(name, Value(depth + 1))); Space(); if (Take('}')) break; Need(','); }
                return new JsonNode { Kind = JsonKind.Object, Fields = fields };
            }
            private JsonNode Array(int depth)
            {
                i++; var items = new List<JsonNode>(); Space(); if (Take(']')) return new JsonNode { Kind = JsonKind.Array, Items = items };
                while (true) { items.Add(Value(depth + 1)); Space(); if (Take(']')) break; Need(','); }
                return new JsonNode { Kind = JsonKind.Array, Items = items };
            }
            private JsonNode Number() { int start = i++; while (i < s.Length && "0123456789+-.eE".IndexOf(s[i]) >= 0) i++; return new JsonNode { Kind = JsonKind.Number, Text = s.Substring(start, i - start) }; }
            private string String()
            {
                Need('"'); var b = new StringBuilder(); while (i < s.Length) { char c = s[i++]; if (c == '"') return b.ToString(); if (c < 0x20) throw new FormatException(); if (c != '\\') { b.Append(c); continue; } if (i >= s.Length) throw new FormatException(); char e = s[i++]; if ("\"\\/".IndexOf(e) >= 0) b.Append(e); else if (e == 'b') b.Append('\b'); else if (e == 'f') b.Append('\f'); else if (e == 'n') b.Append('\n'); else if (e == 'r') b.Append('\r'); else if (e == 't') b.Append('\t'); else if (e == 'u' && i + 4 <= s.Length) { b.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; } else throw new FormatException(); } throw new FormatException();
            }
            private bool Literal(string value) { if (i + value.Length > s.Length || s.Substring(i, value.Length) != value) return false; i += value.Length; return true; }
            private bool Take(char c) { if (i < s.Length && s[i] == c) { i++; return true; } return false; }
            private void Need(char c) { if (!Take(c)) throw new FormatException(); }
        }
    }
}
