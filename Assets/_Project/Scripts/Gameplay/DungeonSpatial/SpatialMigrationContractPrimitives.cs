using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public static class SpatialMigrationContractIdentity
    {
        public const string CanonicalSerializerId = "gd66.serializer.canonical_spatial_save";
        public const int CanonicalSerializerVersion = 1;
        public const int AuthorityMarkerContractVersion = 1;
        public const int MigrationContractVersion = 1;
        public const int JournalSchemaVersion = 1;
    }

    public readonly struct SpatialSerializedInputLimits
    {
        public SpatialSerializedInputLimits(int maximumInputBytes, int maximumParsedNodes,
            int maximumCollectionRecords, int maximumStringCharacters, int maximumDiagnostics)
        {
            MaximumInputBytes = maximumInputBytes;
            MaximumParsedNodes = maximumParsedNodes;
            MaximumCollectionRecords = maximumCollectionRecords;
            MaximumStringCharacters = maximumStringCharacters;
            MaximumDiagnostics = maximumDiagnostics;
        }

        public int MaximumInputBytes { get; }
        public int MaximumParsedNodes { get; }
        public int MaximumCollectionRecords { get; }
        // Includes every decoded property name and string value, measured in UTF-16 code units.
        public int MaximumStringCharacters { get; }
        public int MaximumDiagnostics { get; }
        public bool IsValid => MaximumInputBytes > 0 && MaximumParsedNodes > 0 &&
            MaximumCollectionRecords >= 0 && MaximumStringCharacters >= 0 && MaximumDiagnostics > 0;
    }

    public enum SpatialContractIssue
    {
        InvalidLimits = 1, InputByteLimitExceeded = 2, InvalidUtf8 = 3, BomPresent = 4,
        LeadingOrTrailingWhitespace = 5, MalformedJson = 6, UnknownField = 7, DuplicateField = 8,
        CaseAmbiguousField = 9, WrongFieldOrder = 10, WrongFieldType = 11, UnsupportedNumber = 12,
        IntegerOverflow = 13, UndefinedEnum = 14, WorkloadExceeded = 15, InvalidField = 16,
        InvalidStableId = 17, InvalidHash = 18, InvalidIdentity = 19, InvalidPath = 20,
        InvalidStage = 21, InvalidStageData = 22, NonCanonicalBytes = 23,
        StructuralValidationFailed = 24
    }

    public sealed class SpatialContractResult<T>
    {
        internal SpatialContractResult(T value, IEnumerable<SpatialContractIssue> issues)
        {
            Value = value;
            Issues = new List<SpatialContractIssue>(issues).ToArray();
        }

        public T Value { get; }
        public SpatialContractIssue[] Issues { get; }
        public bool IsValid => Issues.Length == 0;
    }

    internal sealed class SpatialIssueCollector
    {
        private readonly int maximum;
        private readonly List<SpatialContractIssue> issues = new List<SpatialContractIssue>();

        internal SpatialIssueCollector(int maximumDiagnostics)
        {
            maximum = Math.Max(1, maximumDiagnostics);
        }

        internal bool IsExhausted { get; private set; }
        internal int Count => issues.Count;
        internal SpatialContractIssue[] ToArray() => issues.ToArray();

        internal void Add(SpatialContractIssue issue)
        {
            if (IsExhausted) return;
            if (issues.Count < maximum)
            {
                issues.Add(issue);
                return;
            }

            // The first issue beyond the caller budget replaces the last ordinary issue with the stable
            // exhaustion issue. Thus even a limit of one returns exactly [WorkloadExceeded].
            issues[maximum - 1] = SpatialContractIssue.WorkloadExceeded;
            IsExhausted = true;
        }
    }

    public static class SpatialContractSha256
    {
        public static string Compute(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            using (SHA256 hash = SHA256.Create())
            {
                byte[] digest = hash.ComputeHash(bytes);
                var text = new StringBuilder(64);
                foreach (byte value in digest)
                    text.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return text.ToString();
            }
        }

        public static bool IsCanonical(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (char character in value)
                if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f')))
                    return false;
            return true;
        }

        public static bool IsStableId(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            bool separator = true;
            foreach (char character in value)
            {
                bool alpha = character >= 'a' && character <= 'z';
                bool digit = character >= '0' && character <= '9';
                if (alpha || digit) separator = false;
                else if ((character == '.' || character == '_' || character == '-') && !separator) separator = true;
                else return false;
            }
            return !separator;
        }
    }

    internal sealed class ContractJsonWorkloadBudget
    {
        private readonly SpatialSerializedInputLimits limits;
        private int nodes;
        private int records;
        private int stringCharacters;
        private int bytes;

        internal ContractJsonWorkloadBudget(SpatialSerializedInputLimits limits)
        {
            this.limits = limits;
        }

        internal bool TryNode() => TryAdd(ref nodes, 1, limits.MaximumParsedNodes);
        internal bool TryRecord() => TryAdd(ref records, 1, limits.MaximumCollectionRecords);
        internal bool TryString(int utf16Units) => TryAdd(ref stringCharacters, utf16Units,
            limits.MaximumStringCharacters);
        internal bool TryBytes(int count) => TryAdd(ref bytes, count, limits.MaximumInputBytes);

        private static bool TryAdd(ref int current, int amount, int maximum)
        {
            if (amount < 0 || current < 0 || maximum < 0 || current > maximum - amount) return false;
            current += amount;
            return true;
        }
    }

    internal sealed class ContractJsonWriter
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private readonly StringBuilder builder = new StringBuilder();
        private readonly ContractJsonWorkloadBudget budget;

        internal ContractJsonWriter(SpatialSerializedInputLimits limits)
            : this(new ContractJsonWorkloadBudget(limits)) { }

        internal ContractJsonWriter(ContractJsonWorkloadBudget budget)
        {
            this.budget = budget;
        }

        internal ContractJsonWorkloadBudget Budget => budget;

        internal void Node()
        {
            if (!budget.TryNode()) throw new ContractJsonBudgetException(SpatialContractIssue.WorkloadExceeded);
        }

        internal void Record()
        {
            if (!budget.TryRecord()) throw new ContractJsonBudgetException(SpatialContractIssue.WorkloadExceeded);
        }

        internal void Token(string value)
        {
            if (!budget.TryBytes(value.Length))
                throw new ContractJsonBudgetException(SpatialContractIssue.InputByteLimitExceeded);
            builder.Append(value);
        }

        internal void String(string value)
        {
            if (value == null) value = string.Empty;
            if (!budget.TryString(value.Length)) throw new ContractJsonBudgetException(SpatialContractIssue.WorkloadExceeded);
            var encoded = new StringBuilder();
            ContractJson.AppendEscapedString(encoded, value);
            string text = encoded.ToString();
            int byteCount = Utf8.GetByteCount(text);
            if (!budget.TryBytes(byteCount))
                throw new ContractJsonBudgetException(SpatialContractIssue.InputByteLimitExceeded);
            builder.Append(text);
        }

        internal void AppendPrecounted(string value)
        {
            builder.Append(value);
        }

        internal string FinishText() => builder.ToString();
        internal byte[] Finish() => Utf8.GetBytes(builder.ToString());
    }

    internal sealed class ContractJsonBudgetException : Exception
    {
        internal ContractJsonBudgetException(SpatialContractIssue issue) { Issue = issue; }
        internal SpatialContractIssue Issue { get; }
    }

    internal enum ContractJsonKind { Object, Array, String, Number, Boolean, Null }

    internal sealed class ContractJsonNode
    {
        internal ContractJsonKind Kind;
        internal string Text;
        internal List<KeyValuePair<string, ContractJsonNode>> Fields;
        internal List<ContractJsonNode> Items;
    }

    internal static class ContractJson
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        internal static byte[] Bytes(string text) => Utf8.GetBytes(text);

        internal static void AppendEscapedString(StringBuilder builder, string value)
        {
            if (value == null) value = string.Empty;
            builder.Append('"');
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (char.IsHighSurrogate(character))
                {
                    if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                        throw new FormatException();
                    builder.Append(character).Append(value[++index]);
                    continue;
                }
                if (char.IsLowSurrogate(character)) throw new FormatException();
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 0x20)
                            builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        else builder.Append(character);
                        break;
                }
            }
            builder.Append('"');
        }

        internal static bool TryParse(byte[] bytes, SpatialSerializedInputLimits limits,
            SpatialIssueCollector issues, out ContractJsonNode node)
        {
            node = null;
            if (!limits.IsValid) { issues.Add(SpatialContractIssue.InvalidLimits); return false; }
            if (bytes == null || bytes.Length > limits.MaximumInputBytes)
            { issues.Add(SpatialContractIssue.InputByteLimitExceeded); return false; }
            if (bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
            { issues.Add(SpatialContractIssue.BomPresent); return false; }
            string text;
            try { text = Utf8.GetString(bytes); }
            catch (DecoderFallbackException) { issues.Add(SpatialContractIssue.InvalidUtf8); return false; }
            if (text.Length == 0 || char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[text.Length - 1]))
            { issues.Add(SpatialContractIssue.LeadingOrTrailingWhitespace); return false; }
            try
            {
                node = new Reader(text, new ContractJsonWorkloadBudget(limits)).Read();
                return true;
            }
            catch (JsonFailure failure) { issues.Add(failure.Issue); }
            catch { issues.Add(SpatialContractIssue.MalformedJson); }
            return false;
        }

        internal static bool ValidateShape(ContractJsonNode node, string[] fields, SpatialIssueCollector issues)
        {
            if (node == null || node.Kind != ContractJsonKind.Object)
            { issues.Add(SpatialContractIssue.WrongFieldType); return false; }
            bool valid = true;
            if (node.Fields.Count != fields.Length)
            { issues.Add(SpatialContractIssue.InvalidField); valid = false; }
            for (int index = 0; index < node.Fields.Count && !issues.IsExhausted; index++)
            {
                string name = node.Fields[index].Key;
                int exact = Array.IndexOf(fields, name);
                if (exact < 0)
                {
                    bool ambiguous = false;
                    foreach (string field in fields)
                        if (string.Equals(field, name, StringComparison.OrdinalIgnoreCase)) ambiguous = true;
                    issues.Add(ambiguous ? SpatialContractIssue.CaseAmbiguousField : SpatialContractIssue.UnknownField);
                    valid = false;
                }
                else if (exact != index) { issues.Add(SpatialContractIssue.WrongFieldOrder); valid = false; }
            }
            return valid && !issues.IsExhausted;
        }

        internal static ContractJsonNode Field(ContractJsonNode node, int index) => node.Fields[index].Value;
        internal static bool Int(ContractJsonNode node, out int value)
        {
            value = 0;
            return node.Kind == ContractJsonKind.Number && int.TryParse(node.Text,
                NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }
        internal static bool Long(ContractJsonNode node, out long value)
        {
            value = 0L;
            return node.Kind == ContractJsonKind.Number && long.TryParse(node.Text,
                NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }
        internal static bool String(ContractJsonNode node, out string value)
        { value = node.Kind == ContractJsonKind.String ? node.Text : null; return value != null; }

        private sealed class JsonFailure : Exception
        {
            internal JsonFailure(SpatialContractIssue issue) { Issue = issue; }
            internal SpatialContractIssue Issue { get; }
        }

        private sealed class Frame
        {
            internal ContractJsonNode Node;
            internal int State;
            internal bool AfterComma;
            internal string PendingName;
            internal HashSet<string> Names;
        }

        private sealed class Reader
        {
            private readonly string source;
            private readonly ContractJsonWorkloadBudget budget;
            private int position;

            internal Reader(string source, ContractJsonWorkloadBudget budget)
            { this.source = source; this.budget = budget; }

            internal ContractJsonNode Read()
            {
                ContractJsonNode root = ReadValue();
                var stack = new Stack<Frame>();
                if (IsContainer(root)) stack.Push(NewFrame(root));
                while (stack.Count != 0)
                {
                    Frame frame = stack.Peek();
                    if (frame.Node.Kind == ContractJsonKind.Object)
                        StepObject(stack, frame);
                    else StepArray(stack, frame);
                }
                if (position != source.Length) Fail(SpatialContractIssue.MalformedJson);
                return root;
            }

            private void StepObject(Stack<Frame> stack, Frame frame)
            {
                if (frame.State == 0)
                {
                    if (Take('}'))
                    {
                        if (frame.AfterComma) Fail(SpatialContractIssue.MalformedJson);
                        stack.Pop(); return;
                    }
                    string name = ReadString();
                    if (!frame.Names.Add(name)) Fail(SpatialContractIssue.DuplicateField);
                    Need(':'); frame.PendingName = name; frame.State = 1;
                }
                else if (frame.State == 1)
                {
                    ContractJsonNode child = ReadValue();
                    frame.Node.Fields.Add(new KeyValuePair<string, ContractJsonNode>(frame.PendingName, child));
                    frame.State = 2;
                    if (IsContainer(child)) stack.Push(NewFrame(child));
                }
                else
                {
                    if (Take('}')) { stack.Pop(); return; }
                    Need(','); frame.State = 0; frame.AfterComma = true;
                }
            }

            private void StepArray(Stack<Frame> stack, Frame frame)
            {
                if (frame.State == 0)
                {
                    if (Take(']'))
                    {
                        if (frame.AfterComma) Fail(SpatialContractIssue.MalformedJson);
                        stack.Pop(); return;
                    }
                    if (!budget.TryRecord()) Fail(SpatialContractIssue.WorkloadExceeded);
                    ContractJsonNode child = ReadValue();
                    frame.Node.Items.Add(child); frame.State = 1;
                    if (IsContainer(child)) stack.Push(NewFrame(child));
                }
                else
                {
                    if (Take(']')) { stack.Pop(); return; }
                    Need(','); frame.State = 0; frame.AfterComma = true;
                }
            }

            private ContractJsonNode ReadValue()
            {
                if (!budget.TryNode()) Fail(SpatialContractIssue.WorkloadExceeded);
                if (position >= source.Length) Fail(SpatialContractIssue.MalformedJson);
                char current = source[position];
                if (current == '{')
                { position++; return new ContractJsonNode { Kind = ContractJsonKind.Object,
                    Fields = new List<KeyValuePair<string, ContractJsonNode>>() }; }
                if (current == '[')
                { position++; return new ContractJsonNode { Kind = ContractJsonKind.Array,
                    Items = new List<ContractJsonNode>() }; }
                if (current == '"') return new ContractJsonNode { Kind = ContractJsonKind.String, Text = ReadString() };
                if (current == 't' && position + 4 <= source.Length && source.Substring(position, 4) == "true")
                { position += 4; return new ContractJsonNode { Kind = ContractJsonKind.Boolean, Text = "true" }; }
                if (current == 'f' && position + 5 <= source.Length && source.Substring(position, 5) == "false")
                { position += 5; return new ContractJsonNode { Kind = ContractJsonKind.Boolean, Text = "false" }; }
                if (current == 'n' && position + 4 <= source.Length && source.Substring(position, 4) == "null")
                { position += 4; return new ContractJsonNode { Kind = ContractJsonKind.Null }; }
                return ReadNumber();
            }

            private ContractJsonNode ReadNumber()
            {
                int start = position; Take('-');
                if (position >= source.Length || !char.IsDigit(source[position])) Fail(SpatialContractIssue.MalformedJson);
                if (source[position] == '0' && position + 1 < source.Length && char.IsDigit(source[position + 1]))
                    Fail(SpatialContractIssue.UnsupportedNumber);
                while (position < source.Length && char.IsDigit(source[position])) position++;
                if (position < source.Length && source[position] == '.')
                {
                    position++;
                    if (position >= source.Length || !char.IsDigit(source[position])) Fail(SpatialContractIssue.MalformedJson);
                    while (position < source.Length && char.IsDigit(source[position])) position++;
                }
                if (position < source.Length && (source[position] == 'e' || source[position] == 'E'))
                {
                    position++; if (position < source.Length && (source[position] == '+' || source[position] == '-')) position++;
                    if (position >= source.Length || !char.IsDigit(source[position])) Fail(SpatialContractIssue.MalformedJson);
                    while (position < source.Length && char.IsDigit(source[position])) position++;
                }
                return new ContractJsonNode { Kind = ContractJsonKind.Number,
                    Text = source.Substring(start, position - start) };
            }

            private string ReadString()
            {
                Need('"'); var builder = new StringBuilder();
                while (position < source.Length)
                {
                    char character = source[position++];
                    if (character == '"') return builder.ToString();
                    if (character < 0x20) Fail(SpatialContractIssue.MalformedJson);
                    if (character == '\\') character = Escape();
                    AppendUnicode(builder, character);
                }
                Fail(SpatialContractIssue.MalformedJson); return null;
            }

            private char Escape()
            {
                if (position >= source.Length) Fail(SpatialContractIssue.MalformedJson);
                char escape = source[position++];
                if (escape == '"' || escape == '\\' || escape == '/') return escape;
                if (escape == 'b') return '\b'; if (escape == 'f') return '\f';
                if (escape == 'n') return '\n'; if (escape == 'r') return '\r'; if (escape == 't') return '\t';
                if (escape != 'u') Fail(SpatialContractIssue.MalformedJson);
                return ReadHexCodeUnit();
            }

            private void AppendUnicode(StringBuilder builder, char character)
            {
                if (char.IsLowSurrogate(character)) Fail(SpatialContractIssue.MalformedJson);
                if (char.IsHighSurrogate(character))
                {
                    char low;
                    if (position < source.Length && source[position] == '\\' && position + 6 <= source.Length &&
                        source[position + 1] == 'u') { position += 2; low = ReadHexCodeUnit(); }
                    else if (position < source.Length) low = source[position++];
                    else { Fail(SpatialContractIssue.MalformedJson); return; }
                    if (!char.IsLowSurrogate(low)) Fail(SpatialContractIssue.MalformedJson);
                    AddStringUnits(2); builder.Append(character).Append(low); return;
                }
                AddStringUnits(1); builder.Append(character);
            }

            private char ReadHexCodeUnit()
            {
                if (position + 4 > source.Length) { Fail(SpatialContractIssue.MalformedJson); return '\0'; }
                int value;
                if (!int.TryParse(source.Substring(position, 4), NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out value)) { Fail(SpatialContractIssue.MalformedJson); return '\0'; }
                position += 4; return (char)value;
            }

            private void AddStringUnits(int amount)
            { if (!budget.TryString(amount)) Fail(SpatialContractIssue.WorkloadExceeded); }
            private static bool IsContainer(ContractJsonNode node) =>
                node.Kind == ContractJsonKind.Object || node.Kind == ContractJsonKind.Array;
            private static Frame NewFrame(ContractJsonNode node) => new Frame
            { Node = node, Names = node.Kind == ContractJsonKind.Object
                ? new HashSet<string>(StringComparer.Ordinal) : null };
            private bool Take(char expected)
            { if (position < source.Length && source[position] == expected) { position++; return true; } return false; }
            private void Need(char expected) { if (!Take(expected)) Fail(SpatialContractIssue.MalformedJson); }
            private static void Fail(SpatialContractIssue issue) { throw new JsonFailure(issue); }
        }
    }
}
