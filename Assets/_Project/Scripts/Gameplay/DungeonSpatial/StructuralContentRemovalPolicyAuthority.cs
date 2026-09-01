using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum StructuralContentRemovalPolicy
    {
        Unresolved = 0,
        ReturnToPlayerCustody = 1,
        DoesNotSurviveStructuralRemoval = 2
    }

    [Serializable]
    public sealed class StructuralContentRemovalPolicyRecord
    {
        public string CategoryId;
        public string OptionId;
        public StructuralContentRemovalPolicy Policy;
    }

    [Serializable]
    public sealed class StructuralContentRemovalPolicyConfiguration
    {
        public string Schema;
        public int SchemaVersion;
        public StructuralContentRemovalPolicyRecord[] Records = Array.Empty<StructuralContentRemovalPolicyRecord>();
    }

    public sealed class StructuralContentRemovalPolicySnapshot
    {
        internal StructuralContentRemovalPolicySnapshot(StructuralContentRemovalPolicyConfiguration value)
        { Value = value; }
        internal StructuralContentRemovalPolicyConfiguration Value { get; }
    }

    /// <summary>Schema-8-only policy input; never participates in frozen GD66 descriptors.</summary>
    public static class StructuralContentRemovalPolicyAuthority
    {
        public const string ProductionPath =
            "Assets/_Project/Data/Production/Save/structural_content_removal_policy.json";
        public const string MissingOrUnresolvedReason = "schema8.ownership.removal_policy_unresolved";

        public static bool TryParse(byte[] bytes, out StructuralContentRemovalPolicySnapshot snapshot)
        {
            snapshot = null;
            try
            {
                string text = new UTF8Encoding(false, true).GetString(bytes ?? Array.Empty<byte>());
                StructuralContentRemovalPolicyConfiguration value =
                    JsonUtility.FromJson<StructuralContentRemovalPolicyConfiguration>(text);
                if (value == null || value.Schema != "structural_content_removal_policy" ||
                    value.SchemaVersion != 1 || value.Records == null) return false;
                StructuralContentRemovalPolicyRecord[] canonical = value.Records.OrderBy(
                    item => item?.CategoryId, StringComparer.Ordinal).ThenBy(
                    item => item?.OptionId, StringComparer.Ordinal).ToArray();
                if (canonical.Any(item => item == null || string.IsNullOrWhiteSpace(item.CategoryId) ||
                    string.IsNullOrWhiteSpace(item.OptionId) ||
                    !Enum.IsDefined(typeof(StructuralContentRemovalPolicy), item.Policy)) ||
                    canonical.GroupBy(item => item.CategoryId + "\0" + item.OptionId,
                        StringComparer.Ordinal).Any(group => group.Count() != 1)) return false;
                value.Records = canonical;
                byte[] again = Encoding.UTF8.GetBytes(JsonUtility.ToJson(value, true) + "\n");
                if (!bytes.SequenceEqual(again)) return false;
                snapshot = new StructuralContentRemovalPolicySnapshot(value); return true;
            }
            catch { return false; }
        }

        public static bool TryResolve(StructuralContentRemovalPolicySnapshot snapshot,
            string categoryId, string optionId, out StructuralContentRemovalPolicy policy,
            out string reason)
        {
            policy = StructuralContentRemovalPolicy.Unresolved; reason = MissingOrUnresolvedReason;
            StructuralContentRemovalPolicyRecord[] matches = (snapshot?.Value.Records ??
                Array.Empty<StructuralContentRemovalPolicyRecord>()).Where(value =>
                string.Equals(value.CategoryId, categoryId, StringComparison.Ordinal) &&
                string.Equals(value.OptionId, optionId, StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1 || matches[0].Policy == StructuralContentRemovalPolicy.Unresolved)
                return false;
            policy = matches[0].Policy; reason = null; return true;
        }
    }
}
