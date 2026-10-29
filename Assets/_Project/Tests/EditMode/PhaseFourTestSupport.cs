#if UNITY_EDITOR
using System.IO;
using System.Linq;
using DungeonBuilder.M0.Economy;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    internal static class PhaseFourTestSupport
    {
        internal static SaveSpatialMigrationLimitsProfile ProductionProfile() =>
            SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes(SaveSpatialMigrationLimitsLoader.ProductionPath)).Profile;
        // Explicit producer for a fresh synthetic fixture. Its complete validated
        // bytes, including the internal CombatC0 DTO, exist before a context or
        // save owner is constructed; this never rebinds an existing save.
        internal static byte[][] AuthoredInputs(System.Action<RunSimulationConfig, ContentBootstrap> author)
        {
            var bytes = H1F4RecoveryContractTests.Inputs();
            if (author == null) return bytes;
            Assert.That(BootstrapConfigValidationService.TryParseRunSimulationConfig(
                System.Text.Encoding.UTF8.GetString(bytes[0]), out var config), Is.True);
            var bootstrap = UnityEngine.JsonUtility.FromJson<ContentBootstrap>(System.Text.Encoding.UTF8.GetString(bytes[10]));
            author(config, bootstrap);
            string publicFields = UnityEngine.JsonUtility.ToJson(config);
            Assert.That(publicFields, Does.Not.Contain("\"CombatC0\""));
            Assert.That(config.CombatC0, Is.Not.Null);
            bytes[0] = System.Text.Encoding.UTF8.GetBytes(publicFields.Substring(0, publicFields.Length - 1) +
                ",\"CombatC0\":" + UnityEngine.JsonUtility.ToJson(config.CombatC0) + "}");
            bytes[10] = System.Text.Encoding.UTF8.GetBytes(UnityEngine.JsonUtility.ToJson(bootstrap));
            return bytes;
        }

        internal static byte[][] OneBoundaryInputs(byte[][] inputs)
        {
            foreach (int index in new[] { 6, 7 }) {
                string original = System.Text.Encoding.UTF8.GetString(inputs[index]);
                string bounded = original.Replace("\"MaximumBoundariesPerPump\": 64", "\"MaximumBoundariesPerPump\": 1");
                Assert.That(bounded, Is.Not.EqualTo(original));
                inputs[index] = System.Text.Encoding.UTF8.GetBytes(bounded);
            }
            return inputs;
        }

        // Explicit synthetic observation, bound to the exact source and horizons.
        // This uses the same transition capability as SaveService, never a raw W
        // assignment or a production bypass. An old A>C drain retains its W.
        internal static DetachedCanonicalWriteResult PumpForeground(DetachedCanonicalWriteAuthorityTests.Fixture f,
            LivingDungeonForegroundConfig policy, long through, long? wallSeconds = null,
            string activePath = null, ISpatialMigrationFileSystem fileSystem = null,
            System.Action<DetachedCanonicalSaveSession, TimeProvenance> candidatePrepared = null)
        {
            var prior = f.Runtime.timeProvenance;
            long wall = wallSeconds ?? System.Math.Max(1000, prior.AccountedBoundary.Seconds);
            var endpoint = new UnixTimePoint(wall, wall == prior.AccountedBoundary.Seconds ? prior.AccountedBoundary.Fraction100ns : 0);
            var observation = ForegroundObservation.ForDeterministicServiceTest(f.Session.GetCurrentBytes(), through,
                f.Runtime.livingDungeon.Scheduler?.AcceptedThrough ?? -1,
                f.Runtime.livingDungeon.Scheduler?.ClosedThrough ?? -1, endpoint, ForegroundObservationContext.EligibleForeground);
            var transition = f.Runtime.livingDungeon.IsClosed
                ? CanonicalTimeTransition.Foreground(f.Session.GetCurrentBytes(), prior, observation, f.Runtime.livingDungeon) : null;
            return PhaseSixA4Tests.Writer(f).AdvanceForeground(activePath ?? f.ActivePath, fileSystem ?? f.FileSystem, f.Session, f.Runtime,
                policy, through, f.Context.RecoveryInputs.Bootstrap.tickSeconds, (_, __) => { },
                transition?.Successor.AccountedBoundary.Seconds ?? prior.AccountedBoundary.Seconds, transition, candidatePrepared);
        }

        internal static bool Upgrade(byte[] source, CanonicalSpatialSerializationLimits limits, out byte[] current)
        {
            current = null;
            if (!UpgradeFrozenSixteen(source, limits, out var historical)) return false;
            current = SyntheticCurrent(historical, limits); return true;
        }

        internal static bool UpgradeFrozenSixteen(byte[] source, CanonicalSpatialSerializationLimits limits, out byte[] current)
        {
            source = WithTestTimestamp(source, limits);
            current = null;
            if (!SchemaSevenToEightUpgrade.TryPrepare(source, limits, out byte[] eight))
            { return false; }
            if (!SchemaEightToNineUpgrade.TryPrepare(eight, limits, out byte[] nine))
            { return false; }
            if (!SchemaNineToTenUpgrade.TryPrepare(nine, limits, out byte[] ten))
            { return false; }
            if (!SchemaTenToElevenUpgrade.TryPrepare(ten, limits, out byte[] eleven))
            { return false; }
            if (!SchemaElevenToTwelveUpgrade.TryPrepare(eleven, limits, out byte[] twelve))
            { return false; }
            if (!SchemaTwelveToThirteenUpgrade.TryPrepare(twelve, limits, PositionProfiles(limits),
                    out byte[] thirteen))
            { return false; }
            return SchemaThirteenToFourteenUpgrade.TryPrepare(thirteen, limits, out var fourteen) &&
                SchemaFourteenToFifteenUpgrade.TryPrepare(fourteen, limits, MonsterRecoveryRules.EditorFallback(), out var fifteen) &&
                SchemaFifteenToSixteenUpgrade.TryPrepareStructure(fifteen, limits, null, out current);
        }

        // Explicitly synthetic current-schema fixtures, not migration or producer
        // evidence. Frozen source bytes are retained separately. Absent Research
        // is made explicit for these newly authored fixtures only; malformed
        // historical shells are never repaired by production activation.
        internal static byte[] SyntheticCurrent(byte[] frozen, CanonicalSpatialSerializationLimits limits,
            RecoveryContextIdentity identity = null)
        {
            var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
            Assert.That(ContractJson.TryParse(frozen, limits.Serialized, issues, out var root), Is.True);
            var primary = root.Fields[2].Value;
            Assert.That(TimeProvenance.TryRead(primary, 16, out var old), Is.True);
            var time = new TimeProvenance(old.AccountedBoundary, (identity ?? new RecoveryContextIdentity(H1F4RecoveryContractTests.Inputs())).Digest, null);
            var w = new ContractJsonWriter(limits.Serialized); w.Node(); w.Token("{");
            for (int i = 0; i < root.Fields.Count; i++) {
                if (i != 0) w.Token(","); var f = root.Fields[i]; w.String(f.Key); w.Token(":");
                if (f.Key == "schemaVersion") w.Token("17");
                else if (f.Key != "primary") DetachedCompleteSaveContract.WriteCanonicalNode(w, f.Value);
                else {
                    w.Node(); w.Token("{"); bool first = true;
                    void Field(string name, ContractJsonNode node) {
                        if (!first) w.Token(","); first = false; w.String(name); w.Token(":");
                        if (name == TimeProvenance.Owner) time.Write(w);
                        else if (node == null) w.Token("null");
                        else DetachedCompleteSaveContract.WriteCanonicalNode(w, node);
                    }
                    foreach (string name in RawSavePayloadClassifier.RecognizedSaveDataMemberNames) {
                        if (name == PhaseFiveSaveContracts.CorridorOwnerName || name == PhaseFiveSaveContracts.KnowledgeOwnerName || name == PhaseSixFloorKnowledge.OwnerName) continue;
                        var found = primary.Fields.Where(p => p.Key == name).Select(p => p.Value).SingleOrDefault();
                        if (found != null || name == "researchPending" || name == "researchProgress") Field(name, found);
                    }
                    foreach (var p in primary.Fields.Take(primary.Fields.Count - 10).Where(p => !RawSavePayloadClassifier.RecognizedSaveDataMemberNames.Contains(p.Key))) Field(p.Key, p.Value);
                    foreach (var p in primary.Fields.Skip(primary.Fields.Count - 10)) Field(p.Key, p.Value);
                    w.Token("}");
                }
            }
            w.Token("}"); return w.Finish();
        }

        internal static DetachedCurrentTargetValidationContext CurrentContext(SpatialLayoutCompatibilitySnapshot compatibility,
            ProductionSpatialContentSnapshot production, byte[] legacyConfiguration, CanonicalSpatialSerializationLimits limits,
            RoomContentSpatialOccupancySnapshot occupancy = null, bool allowEditorTestOccupancyFallback = true,
            System.Collections.Generic.IReadOnlyList<LivingDungeonProfile> livingDungeonProfiles = null, MonsterRecoveryRules monsterRecovery = null)
        {
            occupancy ??= Occupancy(limits);
            var context = new DetachedCurrentTargetValidationContext(compatibility, production, legacyConfiguration, limits,
                occupancy, allowEditorTestOccupancyFallback, livingDungeonProfiles, monsterRecovery);
            var inputs = new RecoveryRuntimeInputs(H1F4RecoveryContractTests.Inputs(), production,
                ProductionSpatialContentWorkloadLimitParser.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(
                    "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json")).Limits, limits);
            context.BindRecoveryInputs(inputs); return context;
        }

        // Disposable older scaffolds omitted optional historical time. Supply an
        // explicit fake test boundary before testing the new strict writable owner.
        internal static byte[] WithTestTimestamp(byte[] source, CanonicalSpatialSerializationLimits limits)
        {
            var issues = new SpatialIssueCollector(limits.Serialized.MaximumDiagnostics);
            if (!ContractJson.TryParse(source, limits.Serialized, issues, out var root)) return source;
            var primary = root.Fields[2].Value;
            if (primary.Fields.Any(f => f.Key == "lastSavedUtcUnix")) return source;
            var w = new ContractJsonWriter(limits.Serialized); w.Node(); w.Token("{");
            for (int i = 0; i < root.Fields.Count; i++) {
                if (i != 0) w.Token(","); var f = root.Fields[i]; w.String(f.Key); w.Token(":");
                if (f.Key != "primary") DetachedCompleteSaveContract.WriteCanonicalNode(w, f.Value);
                else {
                    w.Node(); w.Token("{"); bool first = true, added = false;
                    foreach (var p in primary.Fields) {
                        if (!added && p.Key != "saveVersion" && p.Key != "contentVersion" && p.Key != "createdUtcUnix") {
                            if (!first) w.Token(","); w.String("lastSavedUtcUnix"); w.Token(":1"); first = false; added = true;
                        }
                        if (!first) w.Token(","); first = false; w.String(p.Key); w.Token(":"); DetachedCompleteSaveContract.WriteCanonicalNode(w, p.Value);
                    }
                    if (!added) { if (!first) w.Token(","); w.String("lastSavedUtcUnix"); w.Token(":1"); }
                    w.Token("}");
                }
            }
            w.Token("}"); return w.Finish();
        }

        internal static RoomContentPositionMigrationProfilesSnapshot PositionProfiles(
            CanonicalSpatialSerializationLimits limits)
        {
            ProductionSpatialContentWorkloadLimitParseResult structural =
                ProductionSpatialContentWorkloadLimitParser.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<
                    UnityEngine.TextAsset>(
                        "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json"));
            Assert.That(structural.Success, Is.True);
            RoomContentPositionMigrationProfilesResult parsed =
                RoomContentPositionMigrationProfiles.ParseAndValidateFrozen(
                    File.ReadAllBytes(RoomContentPositionMigrationProfiles.ProductionPath),
                    structural.Limits, limits.Serialized, true);
            Assert.That(parsed.Success, Is.True, string.Join(",", parsed.Diagnostics));
            return parsed.Value;
        }

        internal static RoomContentSpatialOccupancySnapshot Occupancy(
            CanonicalSpatialSerializationLimits limits)
        {
            ProductionSpatialContentWorkloadLimitParseResult structural =
                ProductionSpatialContentWorkloadLimitParser.Parse(UnityEditor.AssetDatabase.LoadAssetAtPath<
                    UnityEngine.TextAsset>(
                        "Assets/_Project/Data/Production/DungeonSpatial/validation_limits.json"));
            Assert.That(structural.Success, Is.True);
            RunSimulationConfig configuration = UnityEngine.JsonUtility.FromJson<RunSimulationConfig>(
                UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(
                    "Assets/_Project/Data/Bootstrap/run_simulation_config.json").text);
            Assert.That(RoomContentSpatialOccupancyAuthority.TryParse(File.ReadAllBytes(
                RoomContentSpatialOccupancyAuthority.ProductionPath), structural.Limits, configuration,
                out RoomContentSpatialOccupancySnapshot snapshot), Is.True);
            return snapshot;
        }

        // Test-only projection for fixtures that intentionally exercise frozen contracts.
        internal static string FrozenTen(byte[] current)
        {
            string value = System.Text.Encoding.UTF8.GetString(FrozenThirteen(current));
            int owner = value.LastIndexOf(",\"sharedFloorKnowledge\":", System.StringComparison.Ordinal);
            if (owner >= 0)
            {
                int start = value.IndexOf('{', owner), depth = 0, end = -1;
                bool quoted = false, escaped = false;
                for (int index = start; index < value.Length; index++)
                {
                    char character = value[index];
                    if (quoted)
                    {
                        if (escaped) escaped = false;
                        else if (character == '\\') escaped = true;
                        else if (character == '"') quoted = false;
                    }
                    else if (character == '"') quoted = true;
                    else if (character == '{') depth++;
                    else if (character == '}' && --depth == 0) { end = index + 1; break; }
                }
                if (start < owner || end < start) throw new System.ArgumentException("Invalid schema 12 test fixture");
                value = value.Remove(owner, end - owner);
            }
            value = System.Text.RegularExpressions.Regex.Replace(value,
                ",\"RoomLocalPosition\":\\{\"X\":-?[0-9]+,\"Y\":-?[0-9]+\\}", string.Empty);
            return value.Replace("\"schemaVersion\":13", "\"schemaVersion\":10")
                .Replace("\"schemaVersion\":12", "\"schemaVersion\":10")
                .Replace("\"schemaVersion\":11", "\"schemaVersion\":10")
                .Replace(",\"ActivationState\":1", "")
                .Replace(",\"ActivationState\":2", "");
        }

        internal static byte[] FrozenTwelve(byte[] current)
        {
            string value = System.Text.Encoding.UTF8.GetString(FrozenThirteen(current)).Replace(
                "\"schemaVersion\":13", "\"schemaVersion\":12");
            value = System.Text.RegularExpressions.Regex.Replace(value,
                ",\"RoomLocalPosition\":\\{\"X\":-?[0-9]+,\"Y\":-?[0-9]+\\}", string.Empty);
            return System.Text.Encoding.UTF8.GetBytes(value);
        }
        internal static byte[] FrozenThirteen(byte[] current) => System.Text.Encoding.UTF8.GetBytes(
            System.Text.Encoding.UTF8.GetString(FrozenFourteen(current)).Replace(
                ",\"livingDungeon\":{\"FormatVersion\":1,\"Revision\":0,\"Scheduler\":null,\"Parties\":[]}", "")
                .Replace("\"schemaVersion\":14", "\"schemaVersion\":13"));
        internal static byte[] FrozenFourteen(byte[] current)
        {
            var limits = new SpatialSerializedInputLimits(4000000, 50000, 30000, 4000000, 64);
            var issues = new SpatialIssueCollector(64);
            Assert.That(ContractJson.TryParse(current, limits, issues, out var root), Is.True);
            var writer = new ContractJsonWriter(limits); writer.Node(); writer.Token("{");
            for (int i = 0; i < root.Fields.Count; i++)
            {
                if (i != 0) writer.Token(","); var f = root.Fields[i]; writer.String(f.Key); writer.Token(":");
                if (f.Key == "schemaVersion") writer.Token("14");
                else if (f.Key == "primary")
                {
                    writer.Node(); writer.Token("{"); bool first = true;
                    foreach (var p in f.Value.Fields)
                    {
                        if (p.Key == MonsterContinuity.Owner || p.Key == TimeProvenance.Owner) continue;
                        if (!first) writer.Token(","); first = false; writer.String(p.Key); writer.Token(":");
                        DetachedCompleteSaveContract.WriteCanonicalNode(writer, p.Value);
                    }
                    writer.Token("}");
                }
                else DetachedCompleteSaveContract.WriteCanonicalNode(writer, f.Value);
            }
            writer.Token("}"); return writer.Finish();
        }
        internal static StructuralEconomySnapshot Economy(ProductionSpatialContentSnapshot production,
            CanonicalSpatialSerializationLimits limits)
        {
            Assert.That(StructuralEconomySnapshot.TryParse(File.ReadAllBytes(
                "Assets/_Project/Resources/structural_economy.json"), production.Catalog, limits, out var config), Is.True);
            return config;
        }
        internal static ContentAcquisitionEconomySnapshot Acquisition(StructuralEconomySnapshot economy,
            CanonicalSpatialSerializationLimits limits, double? testStartingMana = null)
        {
            byte[] bytes = File.ReadAllBytes("Assets/_Project/Resources/content_acquisition_economy.json");
            if (testStartingMana.HasValue)
            {
                // Explicit fake starting balance for existing QA-wallet fixtures; production is not overridden.
                var fake = UnityEngine.JsonUtility.FromJson<ContentAcquisitionEconomyConfiguration>(System.Text.Encoding.UTF8.GetString(bytes));
                fake.StartingMana = testStartingMana.Value;
                Assert.That(ContentAcquisitionEconomySnapshot.TryCreate(fake, economy, limits, out var injected), Is.True);
                return injected;
            }
            Assert.That(ContentAcquisitionEconomySnapshot.TryParse(bytes, economy, limits, out var config), Is.True);
            return config;
        }

        internal static PassiveOnlineManaConfigurationSnapshot PassiveMana(
            CanonicalSpatialSerializationLimits limits)
        {
            PassiveOnlineManaConfigurationLoadResult loaded =
                PassiveOnlineManaConfigurationSnapshot.Load(File.ReadAllBytes(
                    "Assets/_Project/Resources/passive_online_mana.json"), limits);
            Assert.That(loaded.IsSuccess, Is.True, loaded.Error.ToString());
            return loaded.Value;
        }
    }
}
#endif
