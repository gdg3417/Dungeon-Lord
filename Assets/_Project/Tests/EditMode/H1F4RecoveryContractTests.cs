#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using Fixture = DungeonBuilder.M0.Tests.EditMode.DetachedCanonicalWriteAuthorityTests.Fixture;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public class H1F4RecoveryContractTests
    {
        internal static readonly string[] Paths = {
            "Assets/_Project/Data/Bootstrap/run_simulation_config.json", "Assets/_Project/Resources/passive_online_mana.json",
            "Assets/_Project/Resources/structural_economy.json", "Assets/_Project/Resources/monster_recovery.json",
            "Assets/_Project/Data/Production/DungeonSpatial/content_manifest.json", "Assets/_Project/Data/Production/DungeonSpatial/dungeon_spatial_content.json",
            "Assets/_Project/Resources/living_dungeon_foreground.json", "Assets/_Project/Resources/living_dungeon_foreground_v2.json",
            "Assets/_Project/Data/Production/Save/room_content_spatial_occupancy.json",
            "Assets/_Project/Data/Production/DungeonSpatial/spatial_layout_compatibility_profiles.json",
            "Assets/_Project/Data/Bootstrap/content_bootstrap.json", "Assets/_Project/Data/Bootstrap/loot_config.json" };
        internal static byte[][] Inputs() => Paths.Select(File.ReadAllBytes).ToArray();
        static readonly string Hash = new string('a', 64);
        static SpatialSerializedInputLimits Limits => new SpatialSerializedInputLimits(4096, 128, 64, 2048, 8);
        static RecoveryEpisode Open(string mode = RecoveryEpisode.Precise, long residual = 0, string phase = RecoveryEpisode.OriginalPending) =>
            new RecoveryEpisode(Hash, new UnixTimePoint(80, 0), new UnixTimePoint(100, 2500000), RecoveryEpisode.UnknownPostCrash,
                mode, phase, mode == RecoveryEpisode.Legacy ? 200000000 : 202500000, residual);
        static string Encode(TimeProvenance value)
        { var w = new ContractJsonWriter(Limits); value.Write(w); return Encoding.UTF8.GetString(w.Finish()); }
        static bool Read(string owner, int schema, long seconds, out TimeProvenance time)
        {
            time = null; var issues = new SpatialIssueCollector(8);
            return ContractJson.TryParse(Encoding.UTF8.GetBytes("{\"lastSavedUtcUnix\":" + seconds + ",\"timeProvenance\":" + owner + "}"),
                Limits, issues, out var primary) && TimeProvenance.TryRead(primary, schema, out time);
        }
        static string Closed => Encode(new TimeProvenance(new UnixTimePoint(100, 0), Hash, null));

        [Test] public void FrozenSixteenRemainsReadOnlyAndExact()
        {
            var f = Fixture.Create(null); var source = f.FrozenSixteen;
            var historical = DetachedCompleteSaveContract.ParseValidateFrozenSchemaSixteenAndRoundTrip(source, f.Context);
            Assert.That(historical.IsValid, Is.True); Assert.That(historical.CurrentTargetValidated, Is.False);
            CollectionAssert.AreEqual(source, historical.GetBytes());
            Assert.That(historical.TimeProvenance.Mode, Is.EqualTo(TimeProvenance.LegacyMode));
            Assert.That(historical.TimeProvenance.AccountedBoundary.Fraction100ns, Is.Zero);
            var frozen = SaveSpatialMigrationLimitsLoader.Load(File.ReadAllBytes("Assets/_Project/Resources/save_spatial_migration_limits_schema16.json"));
            Assert.That(frozen.IsSuccess, Is.True);
            Assert.That(frozen.Profile.Canonical.Serialized.MaximumInputBytes, Is.EqualTo(1376348));
            Assert.That(frozen.Profile.Canonical.ForSchema(16).Serialized.MaximumParsedNodes, Is.EqualTo(10244));
        }
        [Test] public void TwelveExactInputsAreStableAndPrivatelyOwned()
        {
            var inputs = Inputs(); var identity = new RecoveryContextIdentity(inputs);
            Assert.That(Encoding.ASCII.GetByteCount(RecoveryContextIdentity.Domain), Is.EqualTo(19));
            Assert.That(new RecoveryContextIdentity(Inputs()).Digest, Is.EqualTo(identity.Digest));
            inputs[0][0] ^= 1;
            var returned = identity.GetInput(0); returned[0] ^= 1;
            CollectionAssert.AreEqual(File.ReadAllBytes(Paths[0]), identity.GetInput(0));
            Assert.That(identity.Matches(new TimeProvenance(new UnixTimePoint(100, 0), identity.Digest, null)), Is.True);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)] [TestCase(11)]
        public void EveryComponentChangesIdentity(int index)
        {
            var inputs = Inputs(); var original = new RecoveryContextIdentity(inputs);
            inputs[index] = inputs[index].Concat(new byte[] { 32 }).ToArray();
            Assert.That(new RecoveryContextIdentity(inputs).Digest, Is.Not.EqualTo(original.Digest));
        }
        [Test] public void GoblinHealthChangeBindsFullBootstrapAndChangesReadinessWithoutChangingLegacyBytes()
        {
            var inputs = Inputs(); string before = Encoding.UTF8.GetString(inputs[0]);
            string after = before.Replace("\"MaxHealth\": 8,", "\"MaxHealth\": 10,");
            Assert.That(after, Is.Not.EqualTo(before));
            Assert.That(BootstrapConfigValidationService.TryParseRunSimulationConfig(before, out var a), Is.True);
            Assert.That(BootstrapConfigValidationService.TryParseRunSimulationConfig(after, out var b), Is.True);
            CollectionAssert.AreEqual(LegacyGameplayConfigurationContract.SerializeCanonical(a), LegacyGameplayConfigurationContract.SerializeCanonical(b));
            var old = new RecoveryContextIdentity(inputs); inputs[0] = Encoding.UTF8.GetBytes(after);
            Assert.That(new RecoveryContextIdentity(inputs).Digest, Is.Not.EqualTo(old.Digest));
            var ra = MonsterRecoveryRules.CaptureBound(a, inputs[3]); var rb = MonsterRecoveryRules.CaptureBound(b, inputs[3]);
            Assert.That(ra.Readiness("placement.option.monster.goblin"), Is.EqualTo(6));
            Assert.That(rb.Readiness("placement.option.monster.goblin"), Is.EqualTo(7));
            Assert.That(ra.SameRules(rb), Is.False);
        }
        [Test] public void InvalidComponentCountAndMissingInputCannotEstablishIdentity()
        {
            Assert.Throws<ArgumentException>(() => new RecoveryContextIdentity(Inputs().Take(11).ToArray()));
            var inputs = Inputs(); inputs[4] = null;
            Assert.Throws<ArgumentException>(() => new RecoveryContextIdentity(inputs));
        }
        [Test] public void MixedResidualDurationsRemainOneExactUnpaidSumAndEntryIsStable()
        {
            var first = Open(residual: 42500000);
            var next = first.Advance(15000000).Advance(15000000);
            Assert.That(next.ResidualEligible100ns, Is.EqualTo(72500000));
            Assert.That(first.ResidualEligible100ns, Is.EqualTo(42500000));
            Assert.That(next.Entry, Is.SameAs(first.Entry)); Assert.That(next.Source, Is.SameAs(first.Source));
            Assert.That(next.IsValidThrough(new UnixTimePoint(112, 0)), Is.True);
            var settled = next.Advance(0, true);
            Assert.That(settled.Phase, Is.EqualTo(RecoveryEpisode.ResidualPending));
            Assert.Throws<ArgumentException>(() => settled.Advance(0, true));
        }
        [Test] public void NegativeAndOverflowCountersCannotBeAccepted()
        {
            Assert.Throws<ArgumentException>(() => Open().Advance(-1));
            Assert.Throws<OverflowException>(() => Open(residual: long.MaxValue).Advance(1));
            Assert.That(Open(residual: long.MaxValue).IsValidThrough(new UnixTimePoint(112, 0)), Is.False);
            Assert.Throws<ArgumentException>(() => new TimeProvenance(new UnixTimePoint(99, 0), Hash, Open()));
        }
        [Test] public void LegacyOriginalEndpointAndFractionalCarryAreDisjoint()
        {
            var episode = Open(RecoveryEpisode.Legacy, 2500000);
            var time = new TimeProvenance(episode.Entry, Hash, episode);
            Assert.That(episode.OriginalEndpoint, Is.EqualTo(new UnixTimePoint(100, 0)));
            Assert.That(episode.OriginalEligible100ns, Is.EqualTo(200000000));
            Assert.That(episode.ResidualEligible100ns, Is.EqualTo(2500000));
            Assert.That(Read(Encode(time), 17, 100, out var read), Is.True);
            Assert.That(read.Episode.Entry, Is.EqualTo(episode.Entry));
        }
        [Test] public void ClosedOwnerRetainsTheOnlyContextIdentityAcrossEncoding()
        {
            Assert.That(Read(Closed, 17, 100, out var read), Is.True);
            Assert.That(read.Episode, Is.Null); Assert.That(read.RecoveryContextSha256, Is.EqualTo(Hash));
            Assert.That(Encode(read), Is.EqualTo(Closed));
            Assert.That(Read(Closed, 16, 100, out _), Is.False);
            Assert.That(Read(TimeProvenance.LegacyJson, 17, 100, out _), Is.False);
            Assert.That(Read(Closed, 18, 100, out _), Is.False);
        }
        [TestCase("\"FormatVersion\":2", "\"FormatVersion\":1")]
        [TestCase("preciseRecovery", "unsupported")]
        [TestCase("\"AccountedFraction100ns\":0", "\"AccountedFraction100ns\":10000000")]
        [TestCase("\"AccountedFraction100ns\":0", "\"AccountedFraction100ns\":-1")]
        [TestCase("\"AccountedFraction100ns\":0", "\"AccountedFraction100ns\":0.0")]
        [TestCase("\"AccountedFraction100ns\":0", "\"AccountedFraction100ns\":0e0")]
        [TestCase("\"Episode\":null", "\"episode\":null")]
        [TestCase("\"Episode\":null", "\"Episode\":{}")]
        [TestCase("\"Episode\":null", "\"Episode\":null,\"Extra\":0")]
        public void StrictOwnerRejectsMalformedVariants(string from, string to) => Assert.That(Read(Closed.Replace(from, to), 17, 100, out _), Is.False);

        [Test] public void MaximumGrammarOwnerIsExactly590BytesAndSixteenValueNodes()
        {
            const long maximum = 2534023007989999999;
            string episode = "{\"EntryPredecessorSha256\":\"" + Hash + "\",\"SourceSeconds\":253402300799,\"SourceFraction100ns\":9999999,\"EntrySeconds\":253402300799,\"EntryFraction100ns\":9999999,\"EntryContext\":\"unknownPostCrash\",\"OriginalMode\":\"legacyWholeSeconds\",\"Phase\":\"originalPending\",\"OriginalEligible100ns\":" + maximum + ",\"ResidualEligible100ns\":" + maximum + "}";
            string owner = "{\"FormatVersion\":2,\"Mode\":\"preciseRecovery\",\"AccountedFraction100ns\":9999999,\"RecoveryContextSha256\":\"" + Hash + "\",\"Episode\":" + episode + "}";
            Assert.That(Encoding.UTF8.GetByteCount(",\"timeProvenance\":" + owner), Is.EqualTo(590));
            var issues = new SpatialIssueCollector(8);
            Assert.That(ContractJson.TryParse(Encoding.UTF8.GetBytes(owner), new SpatialSerializedInputLimits(590, 16, 16, 428, 8), issues, out _), Is.True);
            Assert.That(ContractJson.TryParse(Encoding.UTF8.GetBytes(owner), new SpatialSerializedInputLimits(590, 15, 16, 428, 8), new SpatialIssueCollector(8), out _), Is.False);
            // This is the maximum syntax envelope, not a semantically valid elapsed interval.
            Assert.That(Read(owner, 17, UnixTimePoint.MaximumSeconds, out _), Is.False);
        }
    }
}
#endif
