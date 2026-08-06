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

    public sealed class RawLegacyBlankFloorNodeContract
    {
        public RawLegacyBlankFloorNodeContract(int floorIndex, int nodeIndex, string slotId,
            string categoryId, string optionId, int revision)
        { FloorIndex = floorIndex; NodeIndex = nodeIndex; SlotId = slotId; CategoryId = categoryId;
          OptionId = optionId; Revision = revision; }
        public int FloorIndex { get; }
        public int NodeIndex { get; }
        public string SlotId { get; }
        public string CategoryId { get; }
        public string OptionId { get; }
        public int Revision { get; }
        internal bool IsValid => SlotId != null && CategoryId != null && OptionId != null;
    }

    public sealed class RawLegacyBlankFloorContract
    {
        private readonly ReadOnlyCollection<RawLegacyBlankFloorNodeContract> _nodes;
        private readonly ReadOnlyCollection<string> _layoutMembers;
        private readonly ReadOnlyCollection<string> _nodeMembers;

        public RawLegacyBlankFloorContract(int expectedNextRevision,
            IEnumerable<RawLegacyBlankFloorNodeContract> orderedNodes, bool fieldOrderingIsSignificant,
            bool nodeOrderingIsSignificant, IEnumerable<string> permittedLayoutMembers,
            IEnumerable<string> permittedNodeMembers)
        {
            ExpectedNextRevision = expectedNextRevision;
            FieldOrderingIsSignificant = fieldOrderingIsSignificant;
            NodeOrderingIsSignificant = nodeOrderingIsSignificant;
            _nodes = Copy(orderedNodes);
            _layoutMembers = Copy(permittedLayoutMembers);
            _nodeMembers = Copy(permittedNodeMembers);
        }
        public int ExpectedNextRevision { get; }
        public bool FieldOrderingIsSignificant { get; }
        public bool NodeOrderingIsSignificant { get; }
        public IReadOnlyList<RawLegacyBlankFloorNodeContract> OrderedNodes => _nodes;
        public IReadOnlyList<string> PermittedLayoutMembers => _layoutMembers;
        public IReadOnlyList<string> PermittedNodeMembers => _nodeMembers;
        public bool IsValid
        {
            get
            {
                if (_nodes == null || _layoutMembers == null || _nodeMembers == null ||
                    _nodes.Count == 0 || _layoutMembers.Count != 2 || _nodeMembers.Count != 6 ||
                    ExpectedNextRevision != 1) return false;
                var identities = new HashSet<string>(StringComparer.Ordinal);
                var slotIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < _nodes.Count; i++)
                {
                    RawLegacyBlankFloorNodeContract node = _nodes[i];
                    if (node == null || !node.IsValid || node.FloorIndex != 0 || node.NodeIndex < 0 ||
                        node.SlotId.Length == 0 || node.CategoryId.Length != 0 || node.OptionId.Length != 0 ||
                        node.Revision != 0 ||
                        !identities.Add(node.FloorIndex.ToString(CultureInfo.InvariantCulture) + ":" + node.NodeIndex.ToString(CultureInfo.InvariantCulture)) ||
                        !slotIds.Add(node.SlotId)) return false;
                }
                string[] layout = { "Nodes", "NextRevision" };
                string[] nodeMembers = { "FloorIndex", "NodeIndex", "SlotId", "CategoryId", "OptionId", "Revision" };
                return ExactMembers(_layoutMembers, layout, FieldOrderingIsSignificant) &&
                    ExactMembers(_nodeMembers, nodeMembers, FieldOrderingIsSignificant);
            }
        }
        private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> values) =>
            values == null ? null : new ReadOnlyCollection<T>(new List<T>(values));
        private static bool ExactMembers(IReadOnlyList<string> actual, IReadOnlyList<string> expected, bool ordered)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < actual.Count; i++)
                if (actual[i] == null || !set.Add(actual[i]) || (ordered && actual[i] != expected[i])) return false;
            for (int i = 0; i < expected.Count; i++) if (!set.Contains(expected[i])) return false;
            return true;
        }
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
            RawLegacyRoutePresence assignments, RawLegacyRoutePresence floor, RawLegacyRoutePresence placements,
            string sourceSha256)
        { Envelope = envelope; FailureReason = reason; FailureByteOffset = reasonOffset; SchemaVersion = schemaVersion;
          RootSchemaEvidence = rootSchema; RootSchemaVersionEvidence = rootSchemaVersion; RootPrimaryEvidence = rootPrimary;
          Members = new ReadOnlyCollection<RawSaveMemberEvidence>(new List<RawSaveMemberEvidence>(members));
          UnknownRootMembers = new ReadOnlyCollection<RawUnknownMemberEvidence>(new List<RawUnknownMemberEvidence>(rootUnknown));
          UnknownPrimaryMembers = new ReadOnlyCollection<RawUnknownMemberEvidence>(new List<RawUnknownMemberEvidence>(primaryUnknown));
          RoomSlotAssignmentsPresence = assignments; FloorLayoutPresence = floor; DungeonPlacementsPresence = placements;
          SourcePayloadSha256 = sourceSha256; }
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
        public string SourcePayloadSha256 { get; }
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
            RawSavePayloadClassificationLimits limits, RawSaveEnvelopeVersionContract versionContract,
            RawLegacyBlankFloorContract blankFloorContract)
        {
            if (!limits.IsValid) throw new ArgumentOutOfRangeException(nameof(limits));
            if (!versionContract.IsValid) throw new ArgumentOutOfRangeException(nameof(versionContract));
            if (blankFloorContract == null) throw new ArgumentNullException(nameof(blankFloorContract));
            if (!blankFloorContract.IsValid) throw new ArgumentException(null, nameof(blankFloorContract));
            if (sourceBytes == null) return Failed(UnreadableReason, 0);
            if (sourceBytes.Length > limits.MaximumInputBytes) return Failed(WorkloadExceededReason, limits.MaximumInputBytes);
            byte[] owned = (byte[])sourceBytes.Clone();
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
                ArrayAuthority(payload, "mvpRoomSlotAssignments", "Rooms"), FloorAuthority(payload, blankFloorContract),
                ArrayAuthority(payload, "mvpDungeonPlacements", "Entries"), SpatialContractSha256.Compute(owned));
        }

        private static RawSavePayloadClassification Failed(string reason, int offset) =>
            new RawSavePayloadClassification(RawSaveEnvelopeKind.Invalid, reason, offset, null,
                Absent("schema"), Absent("schemaVersion"), Absent("primary"), Array.Empty<RawSaveMemberEvidence>(), Array.Empty<RawUnknownMemberEvidence>(),
                Array.Empty<RawUnknownMemberEvidence>(), RawLegacyRoutePresence.Absent,
                RawLegacyRoutePresence.Absent, RawLegacyRoutePresence.Absent, null);

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
        private static RawLegacyRoutePresence FloorAuthority(Node payload, RawLegacyBlankFloorContract contract)
        {
            Node layout = Find(payload, "mvpDungeonFloorLayout");
            if (layout == null || layout.Kind == RawJsonValueKind.Null) return RawLegacyRoutePresence.Absent;
            if (layout.Kind != RawJsonValueKind.Object) return RawLegacyRoutePresence.Present;
            if (!MembersMatch(layout, contract.PermittedLayoutMembers, contract.FieldOrderingIsSignificant)) return RawLegacyRoutePresence.Present;
            Node nodes = Find(layout, "Nodes");
            if (nodes == null || nodes.Kind != RawJsonValueKind.Array || nodes.Elements.Count != contract.OrderedNodes.Count) return RawLegacyRoutePresence.Present;
            Node next = Find(layout, "NextRevision");
            if (next == null || next.Kind != RawJsonValueKind.Number || next.Text != contract.ExpectedNextRevision.ToString(CultureInfo.InvariantCulture)) return RawLegacyRoutePresence.Present;
            if (contract.NodeOrderingIsSignificant)
            { for (int i = 0; i < contract.OrderedNodes.Count; i++) if (!BlankNode(nodes.Elements[i], contract.OrderedNodes[i], contract)) return RawLegacyRoutePresence.Present; }
            else
            {
                var matched = new bool[contract.OrderedNodes.Count];
                for (int i = 0; i < nodes.Elements.Count; i++)
                { bool found = false; for (int j = 0; j < contract.OrderedNodes.Count; j++) if (!matched[j] && BlankNode(nodes.Elements[i], contract.OrderedNodes[j], contract)) { matched[j] = true; found = true; break; } if (!found) return RawLegacyRoutePresence.Present; }
            }
            return RawLegacyRoutePresence.Absent;
        }
        private static bool BlankNode(Node node, RawLegacyBlankFloorNodeContract expected, RawLegacyBlankFloorContract contract)
        {
            if (node == null || node.Kind != RawJsonValueKind.Object || !MembersMatch(node, contract.PermittedNodeMembers, contract.FieldOrderingIsSignificant)) return false;
            return Integer(Find(node, "FloorIndex"), expected.FloorIndex) && Integer(Find(node, "NodeIndex"), expected.NodeIndex) &&
                StringValue(Find(node, "SlotId"), expected.SlotId) && StringValue(Find(node, "CategoryId"), expected.CategoryId) &&
                StringValue(Find(node, "OptionId"), expected.OptionId) && Integer(Find(node, "Revision"), expected.Revision);
        }
        private static bool MembersMatch(Node node, IReadOnlyList<string> expected, bool ordered)
        {
            if (node.Members.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++)
            {
                if (ordered) { if (node.Members[i].Name != expected[i]) return false; }
                else if (Find(node, expected[i]) == null) return false;
            }
            return true;
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
            private sealed class WorkBudget
            {
                private readonly long _maximum; private long _used;
                public WorkBudget(int maximum) { _maximum = maximum; }
                public bool Exceeded { get; private set; }
                public int FailureOffset { get; private set; }
                public void ExceededByLimit(int offset) { Exceeded = true; FailureOffset = offset; }
                public bool Charge(int offset)
                {
                    if (Exceeded) return false;
                    if (_used == long.MaxValue || ++_used > _maximum)
                    { Exceeded = true; FailureOffset = offset; return false; }
                    return true;
                }
            }
            private sealed class Frame
            {
                public Node Node; public int State; public string Name; public bool CanClose = true;
                public readonly HashSet<string> Names = new HashSet<string>(StringComparer.Ordinal);
            }
            public static bool TryParse(byte[] bytes, RawSavePayloadClassificationLimits limits,
                out Node root, out string reason, out int errorOffset)
            {
                root = null; reason = UnreadableReason; errorOffset = 0;
                var budget = new WorkBudget(limits.MaximumScanWork);
                if (bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf) return false;
                int p = 0; var stack = new Stack<Frame>();
                if (!Skip(bytes, ref p, budget)) return BudgetFailure(budget, out reason, out errorOffset);
                Node first; if (!Value(bytes, ref p, limits, budget, out first)) return Failure(budget, p, out reason, out errorOffset);
                root = first; if (Container(first)) stack.Push(new Frame { Node = first });
                while (stack.Count != 0)
                {
                    Frame f = stack.Peek();
                    if (!Skip(bytes, ref p, budget)) return BudgetFailure(budget, out reason, out errorOffset);
                    if (f.Node.Kind == RawJsonValueKind.Object)
                    {
                        if (f.State == 0)
                        {
                            bool closing;
                            if (!Check(bytes, p, (byte)'}', budget, out closing)) return BudgetFailure(budget, out reason, out errorOffset);
                            if (f.CanClose && closing) { p++; f.Node.End = p; stack.Pop(); continue; }
                            string name; if (!String(bytes, ref p, limits, budget, out name)) return Failure(budget, p, out reason, out errorOffset);
                            if (!f.Names.Add(name)) { errorOffset = p; return false; }
                            if (f.Names.Count > limits.MaximumObjectMembers) { reason = WorkloadExceededReason; errorOffset = p; return false; }
                            f.Name = name;
                            if (!Skip(bytes, ref p, budget)) return BudgetFailure(budget, out reason, out errorOffset);
                            bool colon; if (!Check(bytes, p, (byte)':', budget, out colon)) return BudgetFailure(budget, out reason, out errorOffset);
                            if (!colon) { errorOffset = p; return false; } p++;
                            if (!Skip(bytes, ref p, budget)) return BudgetFailure(budget, out reason, out errorOffset);
                            Node child; if (!Value(bytes, ref p, limits, budget, out child)) return Failure(budget, p, out reason, out errorOffset);
                            f.Node.Members.Add(new Member { Name = f.Name, Value = child }); f.State = 1; f.CanClose = true;
                            if (Container(child)) { if (stack.Count >= limits.MaximumNestingDepth) { reason = WorkloadExceededReason; errorOffset = child.Start; return false; } stack.Push(new Frame { Node = child }); }
                        }
                        else
                        {
                            bool comma; if (!Check(bytes, p, (byte)',', budget, out comma)) return BudgetFailure(budget, out reason, out errorOffset);
                            if (comma) { p++; f.State = 0; f.CanClose = false; continue; }
                            bool close; if (!Check(bytes, p, (byte)'}', budget, out close)) return BudgetFailure(budget, out reason, out errorOffset);
                            if (!close) { errorOffset = p; return false; } p++; f.Node.End = p; stack.Pop();
                        }
                    }
                    else
                    {
                        if (f.State == 0 && f.CanClose)
                        {
                            bool emptyClose; if (!Check(bytes, p, (byte)']', budget, out emptyClose)) return BudgetFailure(budget, out reason, out errorOffset);
                            if (emptyClose) { p++; f.Node.End = p; stack.Pop(); continue; }
                        }
                        if (f.State == 1)
                        {
                            bool comma; if (!Check(bytes, p, (byte)',', budget, out comma)) return BudgetFailure(budget, out reason, out errorOffset);
                            if (comma) { p++; f.State = 0; f.CanClose = false; continue; }
                            bool close; if (!Check(bytes, p, (byte)']', budget, out close)) return BudgetFailure(budget, out reason, out errorOffset);
                            if (!close) { errorOffset = p; return false; } p++; f.Node.End = p; stack.Pop(); continue;
                        }
                        Node child; if (!Value(bytes, ref p, limits, budget, out child)) return Failure(budget, p, out reason, out errorOffset);
                        f.Node.Elements.Add(child); f.CanClose = true;
                        if (f.Node.Elements.Count > limits.MaximumArrayElements) { reason = WorkloadExceededReason; errorOffset = child.Start; return false; }
                        f.State = 1; if (Container(child)) { if (stack.Count >= limits.MaximumNestingDepth) { reason = WorkloadExceededReason; errorOffset = child.Start; return false; } stack.Push(new Frame { Node = child }); }
                    }
                }
                if (!Skip(bytes, ref p, budget)) return BudgetFailure(budget, out reason, out errorOffset);
                if (p != bytes.Length) { errorOffset = p; return false; }
                return true;
            }
            private static bool Value(byte[] b, ref int p, RawSavePayloadClassificationLimits limits,
                WorkBudget budget, out Node node)
            {
                node = null; if (p >= b.Length || !budget.Charge(p)) return false;
                int start = p; byte c = b[p++];
                if (c == '{' || c == '[') { node = new Node { Start = start, Kind = c == '{' ? RawJsonValueKind.Object : RawJsonValueKind.Array }; return true; }
                if (c == '"') { string text; if (!StringContents(b, ref p, limits, budget, out text)) return false; node = new Node { Start = start, End = p, Kind = RawJsonValueKind.String, Text = text }; return true; }
                if (c == 't' || c == 'f' || c == 'n')
                {
                    string literal = c == 't' ? "true" : c == 'f' ? "false" : "null";
                    if (!LiteralRemainder(b, ref p, literal, budget)) return false;
                    node = new Node { Start = start, End = p, Kind = c == 'n' ? RawJsonValueKind.Null : RawJsonValueKind.Boolean }; return true;
                }
                string number; if (Number(b, ref p, start, c, budget, out number)) { node = new Node { Start = start, End = p, Kind = RawJsonValueKind.Number, Text = number }; return true; }
                return false;
            }
            private static bool Number(byte[] b, ref int p, int start, byte first, WorkBudget budget, out string text)
            {
                text = null; byte c = first;
                if (c == '-') { if (!Consume(b, ref p, budget, out c)) return false; }
                if (c == '0') { }
                else if (c >= '1' && c <= '9') { while (p < b.Length && b[p] >= '0' && b[p] <= '9') if (!Consume(b, ref p, budget, out c)) return false; }
                else return false;
                if (p < b.Length && b[p] == '.')
                {
                    if (!Consume(b, ref p, budget, out c)) return false; int digits = p;
                    while (p < b.Length && b[p] >= '0' && b[p] <= '9') if (!Consume(b, ref p, budget, out c)) return false;
                    if (p == digits) return false;
                }
                if (p < b.Length && (b[p] == 'e' || b[p] == 'E'))
                {
                    if (!Consume(b, ref p, budget, out c)) return false;
                    if (p < b.Length && (b[p] == '+' || b[p] == '-')) if (!Consume(b, ref p, budget, out c)) return false;
                    int digits = p; while (p < b.Length && b[p] >= '0' && b[p] <= '9') if (!Consume(b, ref p, budget, out c)) return false;
                    if (p == digits) return false;
                }
                text = Encoding.ASCII.GetString(b, start, p - start); return true;
            }
            private static bool String(byte[] b, ref int p, RawSavePayloadClassificationLimits limits,
                WorkBudget budget, out string value)
            {
                value = null; bool quote; if (!Check(b, p, (byte)'"', budget, out quote) || !quote) return false; p++;
                return StringContents(b, ref p, limits, budget, out value);
            }
            private static bool StringContents(byte[] b, ref int p, RawSavePayloadClassificationLimits limits,
                WorkBudget budget, out string value)
            {
                value = null; int rawStart = p; var chars = new StringBuilder();
                while (p < b.Length)
                {
                    byte c; if (!Consume(b, ref p, budget, out c)) return false;
                    if (c == '"') { value = chars.ToString(); return true; }
                    if (p - rawStart > limits.MaximumStringBytes) { budget.ExceededByLimit(p - 1); return false; }
                    if (c < 0x20) return false;
                    if (c == '\\')
                    {
                        byte escape; if (!Consume(b, ref p, budget, out escape)) return false;
                        if (p - rawStart > limits.MaximumStringBytes) { budget.ExceededByLimit(p - 1); return false; }
                        if (escape == '"' || escape == '\\' || escape == '/') chars.Append((char)escape);
                        else if (escape == 'b') chars.Append('\b'); else if (escape == 'f') chars.Append('\f'); else if (escape == 'n') chars.Append('\n'); else if (escape == 'r') chars.Append('\r'); else if (escape == 't') chars.Append('\t');
                        else if (escape == 'u')
                        {
                            int high; if (!Hex(b, ref p, budget, out high)) return false;
                            if (p - rawStart > limits.MaximumStringBytes) { budget.ExceededByLimit(p - 1); return false; }
                            if (high >= 0xd800 && high <= 0xdbff)
                            {
                                byte slash, u; if (!Consume(b, ref p, budget, out slash) || !Consume(b, ref p, budget, out u) || slash != '\\' || u != 'u') return false;
                                int low; if (!Hex(b, ref p, budget, out low) || low < 0xdc00 || low > 0xdfff) return false;
                                if (p - rawStart > limits.MaximumStringBytes) { budget.ExceededByLimit(p - 1); return false; }
                                chars.Append(char.ConvertFromUtf32(0x10000 + ((high - 0xd800) << 10) + low - 0xdc00));
                            }
                            else if (high >= 0xdc00 && high <= 0xdfff) return false; else chars.Append((char)high);
                        }
                        else return false; continue;
                    }
                    if (c < 0x80) { chars.Append((char)c); continue; }
                    int count = c < 0xe0 ? 2 : c < 0xf0 ? 3 : c < 0xf5 ? 4 : 0; if (count == 0) return false;
                    int cp = c & (0x7f >> count);
                    for (int i = 1; i < count; i++) { byte continuation; if (!Consume(b, ref p, budget, out continuation) || (continuation & 0xc0) != 0x80) return false; cp = (cp << 6) | (continuation & 0x3f); }
                    if (p - rawStart > limits.MaximumStringBytes) { budget.ExceededByLimit(p - 1); return false; }
                    if ((count == 2 && cp < 0x80) || (count == 3 && cp < 0x800) || (count == 4 && cp < 0x10000) || cp > 0x10ffff || (cp >= 0xd800 && cp <= 0xdfff)) return false;
                    chars.Append(char.ConvertFromUtf32(cp));
                }
                return false;
            }
            private static bool Hex(byte[] b, ref int p, WorkBudget budget, out int value)
            {
                value = 0; for (int i = 0; i < 4; i++) { byte c; if (!Consume(b, ref p, budget, out c)) return false; int digit = c >= '0' && c <= '9' ? c - '0' : c >= 'a' && c <= 'f' ? c - 'a' + 10 : c >= 'A' && c <= 'F' ? c - 'A' + 10 : -1; if (digit < 0) return false; value = value * 16 + digit; } return true;
            }
            private static bool LiteralRemainder(byte[] b, ref int p, string literal, WorkBudget budget)
            { for (int i = 1; i < literal.Length; i++) { byte c; if (!Consume(b, ref p, budget, out c) || c != literal[i]) return false; } return true; }
            private static bool Skip(byte[] b, ref int p, WorkBudget budget)
            { while (p < b.Length && (b[p] == 0x20 || b[p] == 0x09 || b[p] == 0x0a || b[p] == 0x0d)) { byte ignored; if (!Consume(b, ref p, budget, out ignored)) return false; } return true; }
            private static bool Check(byte[] b, int p, byte expected, WorkBudget budget, out bool matches)
            { matches = false; if (!budget.Charge(p)) return false; matches = p < b.Length && b[p] == expected; return true; }
            private static bool Consume(byte[] b, ref int p, WorkBudget budget, out byte value)
            { value = 0; if (!budget.Charge(p)) return false; if (p >= b.Length) return false; value = b[p++]; return true; }
            private static bool Container(Node node) => node.Kind == RawJsonValueKind.Object || node.Kind == RawJsonValueKind.Array;
            private static bool Failure(WorkBudget budget, int offset, out string reason, out int errorOffset)
            { if (budget.Exceeded) return BudgetFailure(budget, out reason, out errorOffset); reason = UnreadableReason; errorOffset = offset; return false; }
            private static bool BudgetFailure(WorkBudget budget, out string reason, out int errorOffset)
            { reason = WorkloadExceededReason; errorOffset = budget.FailureOffset; return false; }
        }
    }
}
