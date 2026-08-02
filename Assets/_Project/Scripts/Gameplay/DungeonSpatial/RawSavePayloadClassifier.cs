using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum RawJsonValueKind { None, Object, Array, String, Number, Boolean, Null }
    public enum RawSaveEnvelopeKind { Invalid, WrappedSaveRoot, UnwrappedSaveData }
    public enum RawSaveMemberState { Absent, Null, NonNull }
    public enum RawLegacyRoutePresence { Absent, Present }

    public readonly struct RawSavePayloadClassificationLimits
    {
        // Input and scan work are totals. Nesting is the maximum number of simultaneously
        // open containers. Member/element limits apply independently to each container.
        // String bytes are raw UTF-8 bytes between quotes, including escape bytes.
        // Scan work charges consumed lexical bytes and every delimiter inspection (including
        // failed delimiter alternatives), making it distinct from the input byte limit.
        public RawSavePayloadClassificationLimits(int maximumInputBytes, int maximumNestingDepth,
            int maximumObjectMembers, int maximumArrayElements, int maximumStringBytes,
            int maximumScanWork)
        { MaximumInputBytes = maximumInputBytes; MaximumNestingDepth = maximumNestingDepth;
          MaximumObjectMembers = maximumObjectMembers; MaximumArrayElements = maximumArrayElements;
          MaximumStringBytes = maximumStringBytes;
          MaximumScanWork = maximumScanWork; }
        public int MaximumInputBytes { get; }
        public int MaximumNestingDepth { get; }
        public int MaximumObjectMembers { get; }
        public int MaximumArrayElements { get; }
        public int MaximumStringBytes { get; }
        public int MaximumScanWork { get; }
        public bool IsValid => MaximumInputBytes > 0 && MaximumNestingDepth > 0 &&
            MaximumObjectMembers > 0 && MaximumArrayElements > 0 && MaximumStringBytes > 0 &&
            MaximumScanWork > 0;
    }

    public readonly struct RawSaveEnvelopeVersionContract
    {
        public RawSaveEnvelopeVersionContract(int minimumVersion, int maximumVersion)
        { MinimumVersion = minimumVersion; MaximumVersion = maximumVersion; }
        public int MinimumVersion { get; }
        public int MaximumVersion { get; }
        public bool IsValid => MinimumVersion >= 0 && MaximumVersion >= MinimumVersion;
        public bool Accepts(int value) => IsValid && value >= MinimumVersion && value <= MaximumVersion;
    }

    public sealed class RawSaveMemberEvidence
    {
        private readonly byte[] _bytes;
        internal RawSaveMemberEvidence(string name, RawSaveMemberState state, RawJsonValueKind kind,
            int offset, byte[] bytes)
        { Name = name; State = state; Kind = kind; ByteOffset = offset; _bytes = bytes ?? Array.Empty<byte>(); }
        public string Name { get; }
        public RawSaveMemberState State { get; }
        public RawJsonValueKind Kind { get; }
        public int ByteOffset { get; }
        public int ByteLength => _bytes.Length;
        public byte[] GetRawValueBytes() => (byte[])_bytes.Clone();
    }

    public sealed class RawUnknownMemberEvidence
    {
        private readonly byte[] _bytes;
        internal RawUnknownMemberEvidence(string name, RawJsonValueKind kind, int offset, byte[] bytes)
        { Name = name; Kind = kind; ByteOffset = offset; _bytes = bytes; }
        public string Name { get; }
        public RawJsonValueKind Kind { get; }
        public int ByteOffset { get; }
        public int ByteLength => _bytes.Length;
        public byte[] GetRawValueBytes() => (byte[])_bytes.Clone();
    }

    public sealed class RawSavePayloadClassification
    {
        internal RawSavePayloadClassification(RawSaveEnvelopeKind envelope, string reason, int reasonOffset,
            int? schemaVersion, RawSaveMemberEvidence rootSchema, RawSaveMemberEvidence rootSchemaVersion,
            RawSaveMemberEvidence rootPrimary, IList<RawSaveMemberEvidence> members,
            IList<RawUnknownMemberEvidence> rootUnknown, IList<RawUnknownMemberEvidence> primaryUnknown,
            RawLegacyRoutePresence assignments, RawLegacyRoutePresence floor, RawLegacyRoutePresence placements)
        { Envelope = envelope; FailureReason = reason; FailureByteOffset = reasonOffset; SchemaVersion = schemaVersion;
          RootSchemaEvidence = rootSchema; RootSchemaVersionEvidence = rootSchemaVersion; RootPrimaryEvidence = rootPrimary;
          Members = new ReadOnlyCollection<RawSaveMemberEvidence>(new List<RawSaveMemberEvidence>(members));
          UnknownRootMembers = new ReadOnlyCollection<RawUnknownMemberEvidence>(new List<RawUnknownMemberEvidence>(rootUnknown));
          UnknownPrimaryMembers = new ReadOnlyCollection<RawUnknownMemberEvidence>(new List<RawUnknownMemberEvidence>(primaryUnknown));
          RoomSlotAssignmentsPresence = assignments; FloorLayoutPresence = floor; DungeonPlacementsPresence = placements; }
        public RawSaveEnvelopeKind Envelope { get; }
        public bool IsSuccess => Envelope != RawSaveEnvelopeKind.Invalid;
        public string FailureReason { get; }
        public int FailureByteOffset { get; }
        public int? SchemaVersion { get; }
        public RawSaveMemberEvidence RootSchemaEvidence { get; }
        public RawSaveMemberEvidence RootSchemaVersionEvidence { get; }
        public RawSaveMemberEvidence RootPrimaryEvidence { get; }
        public IReadOnlyList<RawSaveMemberEvidence> Members { get; }
        public IReadOnlyList<RawUnknownMemberEvidence> UnknownRootMembers { get; }
        public IReadOnlyList<RawUnknownMemberEvidence> UnknownPrimaryMembers { get; }
        public RawLegacyRoutePresence RoomSlotAssignmentsPresence { get; }
        public RawLegacyRoutePresence FloorLayoutPresence { get; }
        public RawLegacyRoutePresence DungeonPlacementsPresence { get; }
    }

    public static class RawSavePayloadClassifier
    {
        public const string InvalidPrimaryReason = "gd66.payload.invalid_primary";
        public const string WorkloadExceededReason = "gd66.payload.workload_exceeded";
        public const string UnreadableReason = "gd66.payload.unreadable";
        public const string AmbiguousEnvelopeReason = "gd66.payload.ambiguous_envelope";

        private static readonly string[] SaveDataMembers = {
            "saveVersion", "contentVersion", "createdUtcUnix", "lastSavedUtcUnix", "lastPausedUtcUnix",
            "lastResumedUtcUnix", "totalTicks", "lastKnownAppState", "dungeonLayout", "mvpDungeonPlacements",
            "mvpDungeonFloorLayout", "mvpRoomSlotAssignments", "mvpSelectedRoomSlotIndex", "structureRuntime",
            "runHistory", "researchPending", "researchProgress", "completedResearch", "completedObjectives",
            "lastOfflineSummary", "integrityFlags" };

        public static IReadOnlyList<string> RecognizedSaveDataMemberNames =>
            new ReadOnlyCollection<string>((string[])SaveDataMembers.Clone());

        public static RawSavePayloadClassification Classify(byte[] sourceBytes,
            RawSavePayloadClassificationLimits limits, RawSaveEnvelopeVersionContract versionContract)
        {
            if (!limits.IsValid || !versionContract.IsValid)
                return Failed(WorkloadExceededReason, 0);
            if (sourceBytes == null) return Failed(UnreadableReason, 0);
            byte[] owned = (byte[])sourceBytes.Clone();
            if (owned.Length > limits.MaximumInputBytes) return Failed(WorkloadExceededReason, limits.MaximumInputBytes);
            Node root; string reason; int offset;
            if (!Scanner.TryParse(owned, limits, out root, out reason, out offset)) return Failed(reason, offset);
            if (root.Kind != RawJsonValueKind.Object) return Failed(AmbiguousEnvelopeReason, root.Start);

            Node schema = Find(root, "schema"), schemaVersion = Find(root, "schemaVersion"), primary = Find(root, "primary");
            bool envelopeMember = schema != null || schemaVersion != null || primary != null;
            Node payload; RawSaveEnvelopeKind envelope; int? acceptedVersion = null;
            if (envelopeMember)
            {
                if (schema == null) return Failed("gd66.payload.missing_schema", root.Start);
                if (schema.Kind != RawJsonValueKind.String || !string.Equals(schema.Text, "save_root", StringComparison.Ordinal))
                    return Failed("gd66.payload.invalid_schema", schema.Start);
                if (schemaVersion == null) return Failed("gd66.payload.missing_schema_version", root.Start);
                int parsedVersion; int versionRelation;
                if (schemaVersion.Kind != RawJsonValueKind.Number ||
                    !TryClassifyIntegralVersion(schemaVersion.Text, versionContract, out parsedVersion, out versionRelation))
                    return Failed("gd66.payload.nonintegral_schema_version", schemaVersion.Start);
                if (versionRelation > 0)
                    return Failed("gd66.payload.newer_than_application", schemaVersion.Start);
                if (versionRelation < 0)
                    return Failed("gd66.payload.unsupported_legacy_version", schemaVersion.Start);
                if (primary == null) return Failed("gd66.payload.missing_primary", root.Start);
                if (primary.Kind == RawJsonValueKind.Null) return Failed("gd66.payload.null_primary", primary.Start);
                if (primary.Kind != RawJsonValueKind.Object) return Failed(InvalidPrimaryReason, primary.Start);
                envelope = RawSaveEnvelopeKind.WrappedSaveRoot; payload = primary; acceptedVersion = parsedVersion;
            }
            else
            {
                bool recognized = false;
                for (int i = 0; i < SaveDataMembers.Length; i++) if (Find(root, SaveDataMembers[i]) != null) { recognized = true; break; }
                if (!recognized) return Failed(AmbiguousEnvelopeReason, root.Start);
                envelope = RawSaveEnvelopeKind.UnwrappedSaveData; payload = root;
            }

            var evidence = new List<RawSaveMemberEvidence>(SaveDataMembers.Length);
            for (int i = 0; i < SaveDataMembers.Length; i++)
            {
                Node value = Find(payload, SaveDataMembers[i]);
                evidence.Add(value == null
                    ? new RawSaveMemberEvidence(SaveDataMembers[i], RawSaveMemberState.Absent, RawJsonValueKind.None, -1, null)
                    : new RawSaveMemberEvidence(SaveDataMembers[i], value.Kind == RawJsonValueKind.Null ? RawSaveMemberState.Null : RawSaveMemberState.NonNull,
                        value.Kind, value.Start, Slice(owned, value.Start, value.End)));
            }
            var unknownRoot = envelope == RawSaveEnvelopeKind.WrappedSaveRoot
                ? Unknown(root, owned, true, false)
                : new List<RawUnknownMemberEvidence>();
            var unknownPrimary = Unknown(payload, owned, false, true);
            return new RawSavePayloadClassification(envelope, null, -1, acceptedVersion,
                Evidence("schema", schema, owned), Evidence("schemaVersion", schemaVersion, owned),
                Evidence("primary", primary, owned), evidence, unknownRoot, unknownPrimary,
                ArrayAuthority(payload, "mvpRoomSlotAssignments", "Rooms"), FloorAuthority(payload),
                ArrayAuthority(payload, "mvpDungeonPlacements", "Entries"));
        }

        private static RawSavePayloadClassification Failed(string reason, int offset) =>
            new RawSavePayloadClassification(RawSaveEnvelopeKind.Invalid, reason, offset, null,
                Absent("schema"), Absent("schemaVersion"), Absent("primary"), Array.Empty<RawSaveMemberEvidence>(), Array.Empty<RawUnknownMemberEvidence>(),
                Array.Empty<RawUnknownMemberEvidence>(), RawLegacyRoutePresence.Absent,
                RawLegacyRoutePresence.Absent, RawLegacyRoutePresence.Absent);

        // Analyzes the JSON token as decimal digits plus a base-10 shift. No fixed-width
        // numeric type is used until the normalized value is proven to be in contract range.
        private static bool TryClassifyIntegralVersion(string token, RawSaveEnvelopeVersionContract contract,
            out int value, out int relation)
        {
            value = 0; relation = 0; int p = 0; bool negative = token[p] == '-'; if (negative) p++;
            int exponentMarker = token.IndexOfAny(new[] { 'e', 'E' }, p);
            int mantissaEnd = exponentMarker < 0 ? token.Length : exponentMarker;
            int dot = token.IndexOf('.', p, mantissaEnd - p);
            int fractionDigits = dot < 0 ? 0 : mantissaEnd - dot - 1;
            var digits = new StringBuilder(mantissaEnd - p);
            for (int i = p; i < mantissaEnd; i++) if (token[i] != '.') digits.Append(token[i]);
            long exponent = 0;
            if (exponentMarker >= 0)
            {
                int e = exponentMarker + 1; bool exponentNegative = token[e] == '-'; if (token[e] == '+' || token[e] == '-') e++;
                for (; e < token.Length; e++) exponent = exponent > 1000000 ? 1000001 : exponent * 10 + token[e] - '0';
                if (exponentNegative) exponent = -exponent;
            }
            long shift = exponent - fractionDigits;
            string all = digits.ToString(); int first = 0; while (first < all.Length && all[first] == '0') first++;
            if (first == all.Length) { value = 0; relation = contract.Accepts(0) ? 0 : 0 < contract.MinimumVersion ? -1 : 1; return true; }
            if (shift < 0)
            {
                long removed = -shift; if (removed > all.Length) return false;
                for (int i = all.Length - (int)removed; i < all.Length; i++) if (all[i] != '0') return false;
                all = all.Substring(0, all.Length - (int)removed); shift = 0;
                first = 0; while (first < all.Length && all[first] == '0') first++;
                if (first == all.Length) { value = 0; relation = contract.Accepts(0) ? 0 : 0 < contract.MinimumVersion ? -1 : 1; return true; }
            }
            long normalizedLength = all.Length - first + shift;
            string bound = (negative ? Math.Abs((long)contract.MinimumVersion) : (long)contract.MaximumVersion).ToString(CultureInfo.InvariantCulture);
            if (normalizedLength != bound.Length) relation = normalizedLength > bound.Length ? (negative ? -1 : 1) : 0;
            else
            {
                int compare = 0;
                for (int i = 0; i < normalizedLength; i++)
                {
                    char digit = i < all.Length - first ? all[first + i] : '0';
                    if (digit != bound[i]) { compare = digit > bound[i] ? 1 : -1; break; }
                }
                if (compare > 0) relation = negative ? -1 : 1;
            }
            if (relation != 0) return true;
            long parsed = 0; for (int i = first; i < all.Length; i++) parsed = parsed * 10 + all[i] - '0';
            for (long i = 0; i < shift; i++) parsed *= 10;
            if (negative) parsed = -parsed;
            if (parsed < contract.MinimumVersion) { relation = -1; return true; }
            if (parsed > contract.MaximumVersion) { relation = 1; return true; }
            value = (int)parsed; return true;
        }

        private static RawSaveMemberEvidence Absent(string name) =>
            new RawSaveMemberEvidence(name, RawSaveMemberState.Absent, RawJsonValueKind.None, -1, null);
        private static RawSaveMemberEvidence Evidence(string name, Node value, byte[] bytes) => value == null
            ? Absent(name)
            : new RawSaveMemberEvidence(name, value.Kind == RawJsonValueKind.Null ? RawSaveMemberState.Null : RawSaveMemberState.NonNull,
                value.Kind, value.Start, Slice(bytes, value.Start, value.End));
        private static Node Find(Node node, string name)
        { for (int i = 0; i < node.Members.Count; i++) if (string.Equals(node.Members[i].Name, name, StringComparison.Ordinal)) return node.Members[i].Value; return null; }
        private static byte[] Slice(byte[] bytes, int start, int end)
        { var result = new byte[end - start]; Buffer.BlockCopy(bytes, start, result, 0, result.Length); return result; }
        private static List<RawUnknownMemberEvidence> Unknown(Node node, byte[] bytes, bool root, bool saveData)
        {
            var result = new List<RawUnknownMemberEvidence>();
            for (int i = 0; i < node.Members.Count; i++)
            {
                Member member = node.Members[i]; bool known = root
                    ? member.Name == "schema" || member.Name == "schemaVersion" || member.Name == "primary"
                    : saveData && Array.IndexOf(SaveDataMembers, member.Name) >= 0;
                if (!known) result.Add(new RawUnknownMemberEvidence(member.Name, member.Value.Kind,
                    member.Value.Start, Slice(bytes, member.Value.Start, member.Value.End)));
            }
            return result;
        }
        private static RawLegacyRoutePresence ArrayAuthority(Node payload, string outer, string inner)
        {
            Node container = Find(payload, outer);
            if (container == null || container.Kind == RawJsonValueKind.Null) return RawLegacyRoutePresence.Absent;
            if (container.Kind != RawJsonValueKind.Object) return RawLegacyRoutePresence.Present;
            Node array = Find(container, inner);
            if (array == null || array.Kind == RawJsonValueKind.Null) return RawLegacyRoutePresence.Absent;
            if (array.Kind != RawJsonValueKind.Array) return RawLegacyRoutePresence.Present;
            for (int i = 0; i < array.Elements.Count; i++) if (array.Elements[i].Kind != RawJsonValueKind.Null) return RawLegacyRoutePresence.Present;
            return RawLegacyRoutePresence.Absent;
        }
        private static RawLegacyRoutePresence FloorAuthority(Node payload)
        {
            Node layout = Find(payload, "mvpDungeonFloorLayout");
            if (layout == null || layout.Kind == RawJsonValueKind.Null) return RawLegacyRoutePresence.Absent;
            if (layout.Kind != RawJsonValueKind.Object) return RawLegacyRoutePresence.Present;
            if (layout.Members.Count != 2) return RawLegacyRoutePresence.Present;
            Node nodes = Find(layout, "Nodes");
            if (nodes == null || nodes.Kind != RawJsonValueKind.Array || nodes.Elements.Count != 4) return RawLegacyRoutePresence.Present;
            Node next = Find(layout, "NextRevision");
            if (next == null || next.Kind != RawJsonValueKind.Number || next.Text != "1") return RawLegacyRoutePresence.Present;
            for (int i = 0; i < 4; i++) if (!BlankNode(nodes.Elements[i], i)) return RawLegacyRoutePresence.Present;
            return RawLegacyRoutePresence.Absent;
        }
        private static bool BlankNode(Node node, int index)
        {
            if (node == null || node.Kind != RawJsonValueKind.Object || node.Members.Count != 6) return false;
            return Integer(Find(node, "FloorIndex"), 0) && Integer(Find(node, "NodeIndex"), index) &&
                StringValue(Find(node, "SlotId"), "mvp.floor.00.node." + index.ToString("D2", CultureInfo.InvariantCulture)) &&
                StringValue(Find(node, "CategoryId"), "") && StringValue(Find(node, "OptionId"), "") && Integer(Find(node, "Revision"), 0);
        }
        private static bool Integer(Node node, int expected) => node != null && node.Kind == RawJsonValueKind.Number && node.Text == expected.ToString(CultureInfo.InvariantCulture);
        private static bool StringValue(Node node, string expected) => node != null && node.Kind == RawJsonValueKind.String && node.Text == expected;

        private sealed class Member { public string Name; public Node Value; }
        private sealed class Node
        {
            public RawJsonValueKind Kind; public int Start; public int End; public string Text;
            public readonly List<Member> Members = new List<Member>(); public readonly List<Node> Elements = new List<Node>();
        }

        private static class Scanner
        {
            private sealed class Frame
            {
                public Node Node; public int State; public string Name; public bool CanClose = true; public readonly HashSet<string> Names = new HashSet<string>(StringComparer.Ordinal);
            }
            public static bool TryParse(byte[] bytes, RawSavePayloadClassificationLimits limits, out Node root, out string reason, out int errorOffset)
            {
                root = null; reason = UnreadableReason; errorOffset = 0;
                if (bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf) return false;
                int p = 0, work = 0; var stack = new Stack<Frame>(); Skip(bytes, ref p, ref work);
                Node first; if (!Value(bytes, ref p, limits, ref work, out first, out reason, out errorOffset)) return false;
                root = first; if (first.Kind == RawJsonValueKind.Object || first.Kind == RawJsonValueKind.Array) stack.Push(new Frame { Node = first });
                while (stack.Count != 0)
                {
                    if (work > limits.MaximumScanWork || p > limits.MaximumScanWork) { reason = WorkloadExceededReason; errorOffset = p; return false; }
                    Frame f = stack.Peek(); Skip(bytes, ref p, ref work);
                    if (f.Node.Kind == RawJsonValueKind.Object)
                    {
                        if (f.State == 0)
                        {
                            if (f.CanClose && Take(bytes, ref p, (byte)'}', ref work)) { f.Node.End = p; stack.Pop(); continue; }
                            string name; if (!String(bytes, ref p, limits, ref work, out name, out reason, out errorOffset)) return false;
                            if (!f.Names.Add(name)) { errorOffset = p; return false; }
                            if (f.Names.Count > limits.MaximumObjectMembers) { reason = WorkloadExceededReason; errorOffset = p; return false; }
                            f.Name = name; Skip(bytes, ref p, ref work); if (!Take(bytes, ref p, (byte)':', ref work)) { errorOffset = p; return false; }
                            Skip(bytes, ref p, ref work); Node child; if (!Value(bytes, ref p, limits, ref work, out child, out reason, out errorOffset)) return false;
                            f.Node.Members.Add(new Member { Name = f.Name, Value = child }); f.State = 1; f.CanClose = true;
                            if ((child.Kind == RawJsonValueKind.Object || child.Kind == RawJsonValueKind.Array)) { if (stack.Count >= limits.MaximumNestingDepth) { reason = WorkloadExceededReason; errorOffset = child.Start; return false; } stack.Push(new Frame { Node = child }); }
                        }
                        else { if (Take(bytes, ref p, (byte)',', ref work)) { f.State = 0; f.CanClose = false; } else if (Take(bytes, ref p, (byte)'}', ref work)) { f.Node.End = p; stack.Pop(); } else { errorOffset = p; return false; } }
                    }
                    else
                    {
                        if (f.State == 0 && f.CanClose && Take(bytes, ref p, (byte)']', ref work)) { f.Node.End = p; stack.Pop(); continue; }
                        if (f.State == 1) { if (Take(bytes, ref p, (byte)',', ref work)) { f.State = 0; f.CanClose = false; } else if (Take(bytes, ref p, (byte)']', ref work)) { f.Node.End = p; stack.Pop(); } else { errorOffset = p; return false; } continue; }
                        Node child; if (!Value(bytes, ref p, limits, ref work, out child, out reason, out errorOffset)) return false;
                        f.Node.Elements.Add(child); f.CanClose = true; if (f.Node.Elements.Count > limits.MaximumArrayElements) { reason = WorkloadExceededReason; errorOffset = child.Start; return false; }
                        f.State = 1; if (child.Kind == RawJsonValueKind.Object || child.Kind == RawJsonValueKind.Array) { if (stack.Count >= limits.MaximumNestingDepth) { reason = WorkloadExceededReason; errorOffset = child.Start; return false; } stack.Push(new Frame { Node = child }); }
                    }
                }
                Skip(bytes, ref p, ref work); if (p != bytes.Length) { errorOffset = p; return false; }
                if (work > limits.MaximumScanWork || p > limits.MaximumScanWork) { reason = WorkloadExceededReason; errorOffset = p; return false; }
                return true;
            }
            private static bool Value(byte[] b, ref int p, RawSavePayloadClassificationLimits l, ref int work, out Node n, out string reason, out int error)
            {
                n = null; reason = UnreadableReason; error = p; if (p >= b.Length) return false; int start = p; byte c = b[p];
                if (c == (byte)'{' || c == (byte)'[') { p++; work++; n = new Node { Start = start, Kind = c == '{' ? RawJsonValueKind.Object : RawJsonValueKind.Array }; return true; }
                if (c == (byte)'"') { string s; if (!String(b, ref p, l, ref work, out s, out reason, out error)) return false; n = new Node { Start = start, End = p, Kind = RawJsonValueKind.String, Text = s }; return true; }
                if ((c == (byte)'t' && Literal(b, ref p, "true", ref work)) || (c == (byte)'f' && Literal(b, ref p, "false", ref work))) { n = new Node { Start = start, End = p, Kind = RawJsonValueKind.Boolean }; return true; }
                if (c == (byte)'n' && Literal(b, ref p, "null", ref work)) { n = new Node { Start = start, End = p, Kind = RawJsonValueKind.Null }; return true; }
                string number; if (Number(b, ref p, ref work, out number)) { n = new Node { Start = start, End = p, Kind = RawJsonValueKind.Number, Text = number }; return true; }
                return false;
            }
            private static bool Number(byte[] b, ref int p, ref int work, out string text)
            {
                int s = p; text = null; if (p < b.Length && b[p] == '-') p++; if (p >= b.Length) { p = s; return false; }
                if (b[p] == '0') p++; else { if (b[p] < '1' || b[p] > '9') { p = s; return false; } while (p < b.Length && b[p] >= '0' && b[p] <= '9') p++; }
                if (p < b.Length && b[p] == '.') { p++; int d = p; while (p < b.Length && b[p] >= '0' && b[p] <= '9') p++; if (p == d) { p = s; return false; } }
                if (p < b.Length && (b[p] == 'e' || b[p] == 'E')) { p++; if (p < b.Length && (b[p] == '+' || b[p] == '-')) p++; int d = p; while (p < b.Length && b[p] >= '0' && b[p] <= '9') p++; if (p == d) { p = s; return false; } }
                work += p - s; text = Encoding.ASCII.GetString(b, s, p - s); return true;
            }
            private static bool String(byte[] b, ref int p, RawSavePayloadClassificationLimits l, ref int work, out string value, out string reason, out int error)
            {
                value = null; reason = UnreadableReason; error = p;
                if (!Take(b, ref p, (byte)'"', ref work)) return false;
                int rawStart = p; var chars = new StringBuilder();
                while (p < b.Length)
                {
                    byte c = b[p++]; work++;
                    if (c == '"') { value = chars.ToString(); return true; }
                    if (p - rawStart > l.MaximumStringBytes) { reason = WorkloadExceededReason; error = p - 1; return false; }
                    if (c < 0x20) { error = p - 1; return false; }
                    if (c == '\\')
                    {
                        if (p >= b.Length) return false; byte e = b[p++]; work++;
                        if (p - rawStart > l.MaximumStringBytes) { reason = WorkloadExceededReason; error = p - 1; return false; }
                        if (e == '"' || e == '\\' || e == '/') chars.Append((char)e);
                        else if (e == 'b') chars.Append('\b'); else if (e == 'f') chars.Append('\f');
                        else if (e == 'n') chars.Append('\n'); else if (e == 'r') chars.Append('\r'); else if (e == 't') chars.Append('\t');
                        else if (e == 'u')
                        {
                            int code; if (!Hex(b, ref p, ref work, out code)) return false;
                            if (p - rawStart > l.MaximumStringBytes) { reason = WorkloadExceededReason; error = p - 1; return false; }
                            if (code >= 0xd800 && code <= 0xdbff)
                            {
                                if (p + 2 > b.Length || b[p++] != '\\' || b[p++] != 'u') return false; work += 2;
                                if (p - rawStart > l.MaximumStringBytes) { reason = WorkloadExceededReason; error = p - 1; return false; }
                                int low; if (!Hex(b, ref p, ref work, out low) || low < 0xdc00 || low > 0xdfff) return false;
                                if (p - rawStart > l.MaximumStringBytes) { reason = WorkloadExceededReason; error = p - 1; return false; }
                                chars.Append(char.ConvertFromUtf32(0x10000 + ((code - 0xd800) << 10) + low - 0xdc00));
                            }
                            else if (code >= 0xdc00 && code <= 0xdfff) return false; else chars.Append((char)code);
                        }
                        else return false; continue;
                    }
                    if (c < 0x80) { chars.Append((char)c); continue; }
                    int count = c < 0xe0 ? 2 : c < 0xf0 ? 3 : c < 0xf5 ? 4 : 0;
                    if (count == 0 || p + count - 1 > b.Length) return false;
                    int cp = c & (0x7f >> count);
                    for (int i = 1; i < count; i++) { byte x = b[p++]; work++; if ((x & 0xc0) != 0x80) return false; cp = (cp << 6) | (x & 0x3f); }
                    if (p - rawStart > l.MaximumStringBytes) { reason = WorkloadExceededReason; error = p - 1; return false; }
                    if ((count == 2 && cp < 0x80) || (count == 3 && cp < 0x800) || (count == 4 && cp < 0x10000) || cp > 0x10ffff || (cp >= 0xd800 && cp <= 0xdfff)) return false;
                    chars.Append(char.ConvertFromUtf32(cp));
                }
                return false;
            }
            private static bool Hex(byte[] b, ref int p, ref int work, out int value)
            { value = 0; if (p + 4 > b.Length) return false; for (int i = 0; i < 4; i++) { int h = b[p++]; work++; int d = h >= '0' && h <= '9' ? h - '0' : h >= 'a' && h <= 'f' ? h - 'a' + 10 : h >= 'A' && h <= 'F' ? h - 'A' + 10 : -1; if (d < 0) return false; value = value * 16 + d; } return true; }
            private static bool Literal(byte[] b, ref int p, string value, ref int work)
            { if (p + value.Length > b.Length) return false; for (int i = 0; i < value.Length; i++) if (b[p + i] != value[i]) return false; p += value.Length; work += value.Length; return true; }
            private static void Skip(byte[] b, ref int p, ref int work) { while (p < b.Length && (b[p] == 0x20 || b[p] == 0x09 || b[p] == 0x0a || b[p] == 0x0d)) { p++; work++; } }
            private static bool Take(byte[] b, ref int p, byte expected, ref int work) { work++; if (p >= b.Length || b[p] != expected) return false; p++; return true; }
        }
    }
}
