#if UNITY_EDITOR
using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class SpatialLayoutCompatibilityProfileTests
    {
        private ProductionSpatialContentSnapshot spatial;
        private SpatialContentValidationWorkloadLimits limits;
        private TextAsset profiles;

        [SetUp]
        public void SetUp()
        {
            const string root="Assets/_Project/Data/Production/DungeonSpatial/";
            TextAsset limitAsset=Asset(root+"validation_limits.json");
            limits=ProductionSpatialContentWorkloadLimitParser.Parse(limitAsset).Limits;
            spatial=ProductionSpatialContentLoader.Load(Asset(root+"content_manifest.json"),
                Asset(root+"dungeon_spatial_content.json"),new[]{Asset(root+"string_table_en.json")},limitAsset).Value;
            profiles=Asset(SpatialLayoutCompatibilityProfiles.ProductionPath);
        }

        [Test] public void ProductionConfiguration_IsCanonicalAuthorizedReleaseAndRecomputesEveryHash()
        {
            SpatialLayoutCompatibilityResult result=SpatialLayoutCompatibilityProfiles.ParseAndValidate(profiles,spatial,limits,null,true);
            Assert.That(result.Success,Is.True,string.Join(",",result.Diagnostics.Select(x=>x.ToString()).ToArray()));
            SpatialLayoutCompatibilityProfilesData data=result.Value.Value;
            CompatibilityLayoutGeometryRecord geometry=data.GeometryRecords[0];
            Assert.That(data.GeometryRecords,Has.Length.EqualTo(1));
            Assert.That(data.GeometryRecords[0].Layouts.Select(x=>x.ExpectedOccupiedTileTotal).ToArray(),Is.EqualTo(new[]{26,42}));
            Assert.That(data.MigrationProfiles,Has.Length.EqualTo(1));
            Assert.That(data.StarterProfiles,Has.Length.EqualTo(2));
            Assert.That(data.ContractSelections,Has.Length.EqualTo(2));
            SpatialMigrationCompatibilityProfile migration=data.MigrationProfiles[0];
            CanonicalStarterLayoutProfile starter=data.StarterProfiles[0];
            CanonicalLayoutContractSelection contract=data.ContractSelections[0];
            CanonicalStarterLayoutProfile currentStarter=data.StarterProfiles.Single(x=>x.TargetSchemaVersion==8);
            CanonicalLayoutContractSelection currentContract=data.ContractSelections.Single(x=>x.TargetSchemaVersion==8);
            Assert.That(geometry.GeometryId,Is.EqualTo("compat.geometry.r1-r2"));
            Assert.That(geometry.GeometryVersion,Is.EqualTo(1));
            Assert.That(geometry.CanonicalHash,Is.EqualTo("7de8d5f88e8517655f0d6595dc37da7382c5ee84d1e41776ccaac6be7beba6db"));
            Assert.That(SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(geometry),Is.EqualTo(geometry.CanonicalHash));
            Assert.That(migration.ProfileId,Is.EqualTo("compat.profile.migration.schema_1_6_to_7.contract_1"));
            Assert.That(migration.ProfileVersion,Is.EqualTo(1));
            Assert.That(migration.Lifecycle,Is.EqualTo(CompatibilityProfileLifecycle.Active));
            Assert.That(migration.MinimumSourceSchemaVersion,Is.EqualTo(1));
            Assert.That(migration.MaximumSourceSchemaVersion,Is.EqualTo(6));
            Assert.That(migration.TargetSchemaVersion,Is.EqualTo(7));
            Assert.That(migration.TargetCanonicalLayoutContractVersion,Is.EqualTo(1));
            Assert.That(migration.GeometryId,Is.EqualTo("compat.geometry.r1-r2"));
            Assert.That(migration.GeometryVersion,Is.EqualTo(1));
            Assert.That(migration.GeometryCanonicalHash,Is.EqualTo("7de8d5f88e8517655f0d6595dc37da7382c5ee84d1e41776ccaac6be7beba6db"));
            Assert.That(SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(migration),Is.EqualTo(migration.CanonicalHash));
            Assert.That(migration.CanonicalHash,Is.EqualTo("88d1548225b55533c023f9bc2216a3362b8cb2b9935d9d3d601710bf1077bbbf"));
            Assert.That(starter.ProfileId,Is.EqualTo("compat.profile.starter.schema_7.contract_1"));
            Assert.That(starter.ProfileVersion,Is.EqualTo(1));
            Assert.That(starter.Lifecycle,Is.EqualTo(CompatibilityProfileLifecycle.Active));
            Assert.That(starter.TargetSchemaVersion,Is.EqualTo(7));
            Assert.That(starter.CanonicalLayoutContractVersion,Is.EqualTo(1));
            Assert.That(starter.GeometryId,Is.EqualTo("compat.geometry.r1-r2"));
            Assert.That(starter.GeometryVersion,Is.EqualTo(1));
            Assert.That(starter.GeometryCanonicalHash,Is.EqualTo("7de8d5f88e8517655f0d6595dc37da7382c5ee84d1e41776ccaac6be7beba6db"));
            Assert.That(SpatialLayoutCompatibilityProfiles.ComputeStarterProfileHash(starter),Is.EqualTo(starter.CanonicalHash));
            Assert.That(starter.CanonicalHash,Is.EqualTo("8ed993e71714e1466fff45445462baa1dc5f5eff9289f2f183a08742ed033007"));
            Assert.That(contract.TargetSchemaVersion,Is.EqualTo(7));
            Assert.That(contract.CanonicalLayoutContractVersion,Is.EqualTo(1));
            Assert.That(contract.Lifecycle,Is.EqualTo(CompatibilityProfileLifecycle.Active));
            Assert.That(result.Value.SelectContract(7).Value.CanonicalLayoutContractVersion,Is.EqualTo(1));
            Assert.That(currentStarter.ProfileId,Is.EqualTo("compat.profile.starter.schema_8.contract_1"));
            Assert.That(currentStarter.CanonicalHash,Is.EqualTo("3193aab078d3727047a8c17264b8309167db03128da47deba3e421e896a1fb98"));
            Assert.That(SpatialLayoutCompatibilityProfiles.ComputeStarterProfileHash(currentStarter),Is.EqualTo(currentStarter.CanonicalHash));
            Assert.That(currentContract.CanonicalLayoutContractVersion,Is.EqualTo(1));
            Assert.That(result.Value.SelectContract(8).Success,Is.True);
            Assert.That(result.Value.SelectMigration(1,7,1).Success,Is.True);
            Assert.That(result.Value.SelectMigration(6,7,1).Success,Is.True);
            Assert.That(result.Value.SelectMigration(0,7,1).Code,Is.EqualTo("gd66.profile.missing"));
            Assert.That(result.Value.SelectMigration(7,7,1).Code,Is.EqualTo("gd66.profile.missing"));
            Assert.That(result.Value.SelectMigration(1,8,1).Code,Is.EqualTo("gd66.profile.version_mismatch"));
            Assert.That(result.Value.SelectMigration(1,7,2).Code,Is.EqualTo("gd66.profile.version_mismatch"));
            Assert.That(result.Value.SelectStarter(7,1).Success,Is.True);
            Assert.That(result.Value.SelectStarter(8,1).Success,Is.True);
            Assert.That(result.Value.SelectStarter(7,2).Code,Is.EqualTo("gd66.starter_profile.version_mismatch"));
            Assert.That(result.Value.SelectContract(6).Code,Is.EqualTo("gd66.layout_contract.selection_missing"));
            Assert.That(SaveMigration.LatestSchemaVersion,Is.EqualTo(8));
            Assert.That(SaveMigration.LegacyCompatibilitySchemaVersion,Is.EqualTo(6));
            Assert.That(CompatibilityReleasePolicy.IsAuthorized(data),Is.True);
            CollectionAssert.AreEqual(profiles.bytes,result.Value.CanonicalBytes);
            Assert.That(geometry.FloorDefinitionId,Is.EqualTo("spatial.floor.01")); Assert.That(geometry.FloorIndex,Is.EqualTo(0));
            Assert.That(geometry.BasicRoomDefinitionId,Is.EqualTo("spatial.room.basic"));
            Assert.That(geometry.EntranceStructureDefinitionId,Is.EqualTo("spatial.fixed.entrance_hall"));
            Assert.That(geometry.CompletionStructureDefinitionId,Is.EqualTo("spatial.fixed.completion_terminal"));
            Assert.That(geometry.SocketTypeId,Is.EqualTo("spatial.socket.standard_passage"));
            AssertPlacement(geometry.Layouts[0],CompatibilityRouteRole.Entrance,0,0);
            AssertPlacement(geometry.Layouts[0],CompatibilityRouteRole.BasicRoom0,0,2);
            AssertPlacement(geometry.Layouts[0],CompatibilityRouteRole.Completion,1,6);
            AssertPlacement(geometry.Layouts[1],CompatibilityRouteRole.BasicRoom1,0,6);
            AssertPlacement(geometry.Layouts[1],CompatibilityRouteRole.Completion,1,10);
        }

        [Test] public void ProductionReleasePolicyPreservesFrozenSevenAndCurrentEightBoundaries()
        {
            SpatialLayoutCompatibilityProfilesData production =
                JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            Assert.That(CompatibilityReleasePolicy.IsAuthorized(production), Is.True);
            Assert.That(production.MigrationProfiles[0].MaximumSourceSchemaVersion,
                Is.EqualTo(SaveMigration.LegacyCompatibilitySchemaVersion));
            Assert.That(production.MigrationProfiles[0].TargetSchemaVersion,
                Is.EqualTo(CanonicalSaveSchemaVersions.FrozenLegacyCanonicalMigrationTarget));
            Assert.That(production.StarterProfiles.Single(value => value.TargetSchemaVersion == 7)
                .TargetSchemaVersion, Is.EqualTo(7));
            Assert.That(production.ContractSelections.Single(value => value.TargetSchemaVersion == 7)
                .TargetSchemaVersion, Is.EqualTo(7));
            Assert.That(production.StarterProfiles.Single(value => value.TargetSchemaVersion == 8)
                .TargetSchemaVersion, Is.EqualTo(CanonicalSaveSchemaVersions.CurrentWritableTarget));
            Assert.That(production.ContractSelections.Single(value => value.TargetSchemaVersion == 8)
                .TargetSchemaVersion, Is.EqualTo(CanonicalSaveSchemaVersions.CurrentWritableTarget));

            SpatialLayoutCompatibilityProfilesData schemaEight =
                JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            schemaEight.MigrationProfiles[0].TargetSchemaVersion = 8;
            Assert.That(CompatibilityReleasePolicy.IsAuthorized(schemaEight), Is.False);

            SpatialLayoutCompatibilityProfilesData schemaSevenSource =
                JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            schemaSevenSource.MigrationProfiles[0].MaximumSourceSchemaVersion = 7;
            Assert.That(CompatibilityReleasePolicy.IsAuthorized(schemaSevenSource), Is.False);
        }

        [Test] public void StrictParser_FailsClosedAndPreservesPreviouslyPublishedSnapshot()
        {
            var service=new ContentService();
            Assert.That(service.LoadSpatialLayoutCompatibilityProfiles(profiles,spatial,limits).Success,Is.True);
            SpatialLayoutCompatibilitySnapshot published=service.SpatialLayoutCompatibilityProfiles;
            Assert.That(service.LoadSpatialLayoutCompatibilityProfiles(new TextAsset("{}\n"),spatial,limits).Success,Is.False);
            Assert.That(service.SpatialLayoutCompatibilityProfiles,Is.SameAs(published));
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate((byte[])null,spatial,limits).Diagnostics[0],Is.EqualTo(SpatialLayoutCompatibilityDiagnostic.MissingInput));
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(new byte[0],spatial,limits).Diagnostics[0],Is.EqualTo(SpatialLayoutCompatibilityDiagnostic.EmptyInput));
        }

        [Test] public void PurposeSelectors_RespectActiveAndRetiredLifecycle()
        {
            SpatialMigrationCompatibilityProfile migrationProfile=Migration(CompatibilityProfileLifecycle.Active);
            CanonicalStarterLayoutProfile starterProfile=Starter(CompatibilityProfileLifecycle.Retired);
            SpatialLayoutCompatibilitySnapshot snapshot=ValidatedSnapshot(new[]{migrationProfile},new[]{starterProfile},
                new[]{new CanonicalLayoutContractSelection{Lifecycle=CompatibilityProfileLifecycle.Active,TargetSchemaVersion=10,CanonicalLayoutContractVersion=2}});
            Assert.That(snapshot.SelectMigration(2,10,2).Code,Is.EqualTo(string.Empty));
            Assert.That(snapshot.SelectMigration(4,10,2).Code,Is.EqualTo("gd66.profile.missing"));
            Assert.That(snapshot.SelectMigration(2,11,2).Code,Is.EqualTo("gd66.profile.version_mismatch"));
            Assert.That(snapshot.SelectMigration(2,10,3).Code,Is.EqualTo("gd66.profile.version_mismatch"));
            Assert.That(snapshot.SelectStarter(10,2).Code,Is.EqualTo("gd66.starter_profile.missing"));
            Assert.That(snapshot.SelectContract(10).Success,Is.True);
            Assert.That(snapshot.SelectContract(11).Code,Is.EqualTo("gd66.layout_contract.selection_missing"));
            SpatialLayoutCompatibilityProfilesData detached=snapshot.Value;
            detached.MigrationProfiles[0].GeometryCanonicalHash="mutated";
            Assert.That(snapshot.SelectMigration(2,10,2).Success,Is.True);
        }

        [TestCase("bom")][TestCase("crlf")][TestCase("no-newline")][TestCase("two-newlines")]
        [TestCase("malformed")][TestCase("trailing")][TestCase("duplicate")][TestCase("unknown")]
        [TestCase("ambiguous")][TestCase("missing")][TestCase("wrong-type")][TestCase("decimal")][TestCase("overflow")]
        [TestCase("unknown-enum")][TestCase("invalid-utf8")][TestCase("noncanonical")]
        public void StrictParser_RejectsRepresentativeInvalidInputs(string mutation)
        {
            byte[] bytes=Mutate(profiles.bytes,mutation); byte[] before=(byte[])bytes.Clone();
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(bytes,spatial,limits).Success,Is.False,mutation);
            CollectionAssert.AreEqual(before,bytes,mutation);
        }

        [Test] public void GeometryTotalAndMigrationRangeMutationsChangeCanonicalHashes()
        {
            SpatialLayoutCompatibilityProfilesData data=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            string geometryHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            data.GeometryRecords[0].Layouts[0].ExpectedOccupiedTileTotal++;
            Assert.That(SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]),Is.Not.EqualTo(geometryHash));
            var profile=new SpatialMigrationCompatibilityProfile{ProfileId="test.profile",ProfileVersion=1,Lifecycle=CompatibilityProfileLifecycle.Retired,MinimumSourceSchemaVersion=1,MaximumSourceSchemaVersion=6,TargetSchemaVersion=8,TargetCanonicalLayoutContractVersion=1,GeometryId="test.geometry",GeometryVersion=1,GeometryCanonicalHash=new string('a',64)};
            string hash=SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(profile); profile.MaximumSourceSchemaVersion++;
            Assert.That(SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(profile),Is.Not.EqualTo(hash));
        }

        [Test] public void ExtremeCoordinatesFailWithoutOverflowAndPreservePublishedSnapshot()
        {
            SpatialLayoutCompatibilityProfilesData data=GeometryOnlyData();
            data.GeometryRecords[0].Layouts[0].Placements[0].Anchor=new TileCoordinate(int.MinValue,int.MinValue);
            data.GeometryRecords[0].Layouts[0].Placements[1].Anchor=new TileCoordinate(int.MaxValue,int.MaxValue);
            data.GeometryRecords[0].CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            TextAsset extreme=CanonicalAsset(data); byte[] before=(byte[])extreme.bytes.Clone();
            SpatialLayoutCompatibilityResult result=null;
            Assert.DoesNotThrow(()=>result=SpatialLayoutCompatibilityProfiles.ParseAndValidate(extreme,spatial,limits));
            Assert.That(result.Success,Is.False); Assert.That(result.Diagnostics.Contains(SpatialLayoutCompatibilityDiagnostic.InvalidGeometry),Is.True);
            CollectionAssert.AreEqual(before,extreme.bytes);

            Assert.That(SpatialLayoutCompatibilityProfiles.TryTransformPoint(new TileCoordinate(1,1),
                new TileCoordinate(int.MaxValue,int.MaxValue),CardinalOrientation.Zero,
                new RectangularFootprintDefinition(2,2),out _),Is.False);
            var service=new ContentService(); Assert.That(service.LoadSpatialLayoutCompatibilityProfiles(profiles,spatial,limits).Success,Is.True);
            SpatialLayoutCompatibilitySnapshot previous=service.SpatialLayoutCompatibilityProfiles;
            Assert.DoesNotThrow(()=>service.LoadSpatialLayoutCompatibilityProfiles(extreme,spatial,limits));
            Assert.That(service.SpatialLayoutCompatibilityProfiles,Is.SameAs(previous));
        }

        [Test] public void RetiredMigrationRecoveryRequiresCompletePinnedIdentity()
        {
            SpatialMigrationCompatibilityProfile profile=Migration(CompatibilityProfileLifecycle.Retired);
            SpatialLayoutCompatibilitySnapshot snapshot=ValidatedSnapshot(new[]{profile},null,null);
            Assert.That(snapshot.TryRecoverMigration(profile.ProfileId,profile.ProfileVersion,
                profile.CanonicalHash,profile.GeometryId,profile.GeometryVersion,profile.GeometryCanonicalHash,out var recovered),Is.True);
            Assert.That(recovered.Lifecycle,Is.EqualTo(CompatibilityProfileLifecycle.Retired));
            Assert.That(snapshot.TryRecoverMigration(profile.ProfileId,profile.ProfileVersion,
                new string('c',64),profile.GeometryId,profile.GeometryVersion,profile.GeometryCanonicalHash,out _),Is.False);
            Assert.That(snapshot.TryRecoverMigration(profile.ProfileId,profile.ProfileVersion,
                profile.CanonicalHash,profile.GeometryId,profile.GeometryVersion+1,profile.GeometryCanonicalHash,out _),Is.False);
        }

        [Test] public void ActiveStarterSelectionUsesValidatedSnapshotAndExactKey()
        {
            SpatialLayoutCompatibilitySnapshot snapshot=ValidatedSnapshot(null,
                new[]{Starter(CompatibilityProfileLifecycle.Active)},null);
            Assert.That(snapshot.SelectStarter(10,2).Success,Is.True);
            Assert.That(snapshot.SelectStarter(10,3).Code,Is.EqualTo("gd66.starter_profile.version_mismatch"));
            Assert.That(snapshot.SelectMigration(2,10,2).Code,Is.EqualTo("gd66.profile.missing"));
        }

        [Test] public void ConfigurationResolutionExposesEveryApprovedStableSelectionCodeWithoutPublishingFailures()
        {
            Assert.That(SpatialLayoutCompatibilityProfiles.ResolveMigration(profiles,spatial,limits,0,7,1).Selection.Code,
                Is.EqualTo("gd66.profile.missing"));
            SpatialMigrationCompatibilityProfile migration=Migration(CompatibilityProfileLifecycle.Active);
            TextAsset validMigration=ConfigurationAsset(new[]{migration},null,null,true);
            Assert.That(SpatialLayoutCompatibilityProfiles.ResolveMigration(validMigration,spatial,limits,2,11,2).Selection.Code,
                Is.EqualTo("gd66.profile.version_mismatch"));
            SpatialLayoutCompatibilityProfilesData invalidMigration=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(validMigration.text);
            invalidMigration.MigrationProfiles[0].CanonicalHash="invalid";
            CompatibilityConfigurationResolution<SpatialMigrationCompatibilityProfile> invalidMigrationResult=
                SpatialLayoutCompatibilityProfiles.ResolveMigration(CanonicalAsset(invalidMigration),spatial,limits,2,10,2);
            Assert.That(invalidMigrationResult.Selection.Code,Is.EqualTo("gd66.profile.invalid")); Assert.That(invalidMigrationResult.Snapshot,Is.Null);
            SpatialMigrationCompatibilityProfile duplicateMigration=Migration(CompatibilityProfileLifecycle.Active);
            duplicateMigration.ProfileId="test.migration.second";
            CompatibilityConfigurationResolution<SpatialMigrationCompatibilityProfile> duplicateMigrationResult=
                SpatialLayoutCompatibilityProfiles.ResolveMigration(ConfigurationAsset(new[]{migration,duplicateMigration},null,null,true),spatial,limits,2,10,2);
            Assert.That(duplicateMigrationResult.Selection.Code,Is.EqualTo("gd66.profile.duplicate")); Assert.That(duplicateMigrationResult.Snapshot,Is.Null);
            SpatialLayoutCompatibilityProfilesData duplicateAndInvalid=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(
                ConfigurationAsset(new[]{migration,duplicateMigration},null,null,true).text);
            duplicateAndInvalid.MigrationProfiles[0].CanonicalHash="invalid";
            Assert.That(SpatialLayoutCompatibilityProfiles.ResolveMigration(CanonicalAsset(duplicateAndInvalid),spatial,limits,2,10,2).Selection.Code,
                Is.EqualTo("gd66.profile.duplicate"));

            Assert.That(SpatialLayoutCompatibilityProfiles.ResolveStarter(profiles,spatial,limits,10,2).Selection.Code,
                Is.EqualTo("gd66.starter_profile.missing"));
            CanonicalStarterLayoutProfile starter=Starter(CompatibilityProfileLifecycle.Active);
            TextAsset validStarter=ConfigurationAsset(null,new[]{starter},null,true);
            Assert.That(SpatialLayoutCompatibilityProfiles.ResolveStarter(validStarter,spatial,limits,10,3).Selection.Code,
                Is.EqualTo("gd66.starter_profile.version_mismatch"));
            SpatialLayoutCompatibilityProfilesData invalidStarter=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(validStarter.text);
            invalidStarter.StarterProfiles[0].CanonicalHash="invalid";
            Assert.That(SpatialLayoutCompatibilityProfiles.ResolveStarter(CanonicalAsset(invalidStarter),spatial,limits,10,2).Selection.Code,
                Is.EqualTo("gd66.starter_profile.invalid"));
            CanonicalStarterLayoutProfile duplicateStarter=Starter(CompatibilityProfileLifecycle.Active); duplicateStarter.ProfileId="test.starter.second";
            Assert.That(SpatialLayoutCompatibilityProfiles.ResolveStarter(ConfigurationAsset(null,new[]{starter,duplicateStarter},null,true),spatial,limits,10,2).Selection.Code,
                Is.EqualTo("gd66.starter_profile.duplicate"));

            Assert.That(SpatialLayoutCompatibilityProfiles.ResolveContract(profiles,spatial,limits,10).Selection.Code,
                Is.EqualTo("gd66.layout_contract.selection_missing"));
            var selections=new[]{new CanonicalLayoutContractSelection{Lifecycle=CompatibilityProfileLifecycle.Active,TargetSchemaVersion=10,CanonicalLayoutContractVersion=1},
                new CanonicalLayoutContractSelection{Lifecycle=CompatibilityProfileLifecycle.Active,TargetSchemaVersion=10,CanonicalLayoutContractVersion=2}};
            Assert.That(SpatialLayoutCompatibilityProfiles.ResolveContract(ConfigurationAsset(null,null,selections,false),spatial,limits,10).Selection.Code,
                Is.EqualTo("gd66.layout_contract.selection_duplicate"));
        }

        [Test]
        public void MigrationDuplicateClassificationCoversIdentityAndGlobalRangeOverlapInEitherOrder()
        {
            AssertMigrationDuplicatePair(Migration(CompatibilityProfileLifecycle.Active),
                Migration(CompatibilityProfileLifecycle.Active),2);
            AssertMigrationDuplicatePair(Migration(CompatibilityProfileLifecycle.Active),
                Migration(CompatibilityProfileLifecycle.Retired),2);
            AssertMigrationDuplicatePair(Migration(CompatibilityProfileLifecycle.Retired),
                Migration(CompatibilityProfileLifecycle.Retired),99);

            SpatialMigrationCompatibilityProfile first=Migration(CompatibilityProfileLifecycle.Active);
            SpatialMigrationCompatibilityProfile second=Migration(CompatibilityProfileLifecycle.Active);
            second.ProfileId="test.migration.second"; second.MinimumSourceSchemaVersion=3; second.MaximumSourceSchemaVersion=5;
            AssertMigrationDuplicatePair(first,second,2);
            AssertMigrationDuplicatePair(first,second,99);

            SpatialMigrationCompatibilityProfile nonoverlap=Migration(CompatibilityProfileLifecycle.Active);
            nonoverlap.ProfileId="test.migration.nonoverlap"; nonoverlap.MinimumSourceSchemaVersion=4; nonoverlap.MaximumSourceSchemaVersion=6;
            CompatibilityConfigurationResolution<SpatialMigrationCompatibilityProfile> valid=
                SpatialLayoutCompatibilityProfiles.ResolveMigration(ConfigurationAsset(new[]{first,nonoverlap},null,null,true),spatial,limits,2,10,2);
            Assert.That(valid.Selection.Code,Is.EqualTo(string.Empty)); Assert.That(valid.Success,Is.True);
        }

        [Test]
        public void ResolutionStableCodesAreIsolatedToTheirRequestedPurpose()
        {
            SpatialMigrationCompatibilityProfile migration=Migration(CompatibilityProfileLifecycle.Active);
            CanonicalStarterLayoutProfile starter=Starter(CompatibilityProfileLifecycle.Active);
            CanonicalLayoutContractSelection contract=new CanonicalLayoutContractSelection{Lifecycle=CompatibilityProfileLifecycle.Active,
                TargetSchemaVersion=10,CanonicalLayoutContractVersion=2};

            TextAsset invalidStarter=ConfigurationAsset(new[]{migration},new[]{starter},null,true);
            SpatialLayoutCompatibilityProfilesData invalidStarterData=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(invalidStarter.text);
            invalidStarterData.StarterProfiles[0].CanonicalHash="invalid"; invalidStarter=CanonicalAsset(invalidStarterData);
            AssertNoStableCode(SpatialLayoutCompatibilityProfiles.ResolveMigration(invalidStarter,spatial,limits,2,10,2));

            TextAsset invalidContract=ConfigurationAsset(new[]{migration},new[]{starter},new[]{new CanonicalLayoutContractSelection{
                Lifecycle=(CompatibilityProfileLifecycle)99,TargetSchemaVersion=10,CanonicalLayoutContractVersion=2}},true);
            AssertNoStableCode(SpatialLayoutCompatibilityProfiles.ResolveMigration(invalidContract,spatial,limits,2,10,2));
            AssertNoStableCode(SpatialLayoutCompatibilityProfiles.ResolveStarter(invalidContract,spatial,limits,10,2));

            TextAsset invalidMigration=ConfigurationAsset(new[]{migration},new[]{starter},new[]{contract},true);
            SpatialLayoutCompatibilityProfilesData invalidMigrationData=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(invalidMigration.text);
            invalidMigrationData.MigrationProfiles[0].CanonicalHash="invalid"; invalidMigration=CanonicalAsset(invalidMigrationData);
            AssertNoStableCode(SpatialLayoutCompatibilityProfiles.ResolveStarter(invalidMigration,spatial,limits,10,2));
            AssertNoStableCode(SpatialLayoutCompatibilityProfiles.ResolveContract(invalidMigration,spatial,limits,10));

            TextAsset invalidStarterWithContract=ConfigurationAsset(null,new[]{starter},new[]{contract},true);
            SpatialLayoutCompatibilityProfilesData invalidStarterWithContractData=
                JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(invalidStarterWithContract.text);
            invalidStarterWithContractData.StarterProfiles[0].CanonicalHash="invalid";
            AssertNoStableCode(SpatialLayoutCompatibilityProfiles.ResolveContract(CanonicalAsset(invalidStarterWithContractData),
                spatial,limits,10));

            TextAsset starterOnlyInvalid=ConfigurationAsset(null,new[]{starter},null,true);
            SpatialLayoutCompatibilityProfilesData starterOnlyData=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(starterOnlyInvalid.text);
            starterOnlyData.StarterProfiles[0].CanonicalHash="invalid";
            AssertNoStableCode(SpatialLayoutCompatibilityProfiles.ResolveMigration(CanonicalAsset(starterOnlyData),spatial,limits,2,10,2));
            TextAsset migrationOnlyInvalid=ConfigurationAsset(new[]{migration},null,null,true);
            SpatialLayoutCompatibilityProfilesData migrationOnlyData=
                JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(migrationOnlyInvalid.text);
            migrationOnlyData.MigrationProfiles[0].CanonicalHash="invalid";
            AssertNoStableCode(SpatialLayoutCompatibilityProfiles.ResolveStarter(CanonicalAsset(migrationOnlyData),
                spatial,limits,10,2));
        }

        [Test] public void InvalidProfileReferencesAndStaleGeometryNeverPublishSelectableSnapshot()
        {
            SpatialLayoutCompatibilityProfilesData missing=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            SpatialMigrationCompatibilityProfile profile=Migration(CompatibilityProfileLifecycle.Active);
            profile.GeometryId="missing.geometry"; profile.GeometryVersion=1; profile.GeometryCanonicalHash=missing.GeometryRecords[0].CanonicalHash;
            profile.CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(profile);
            missing.MigrationProfiles=new[]{profile};
            Assert.That(ParseData(missing).Value,Is.Null);

            SpatialLayoutCompatibilityProfilesData stale=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            stale.GeometryRecords[0].Layouts[0].Placements[0].Anchor.X++;
            Assert.That(ParseData(stale).Value,Is.Null);
        }

        [Test] public void GeometryStructuralMutationsFailPureValidation()
        {
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts=data.GeometryRecords[0].Layouts.Skip(1).ToArray());
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts=data.GeometryRecords[0].Layouts.Take(1).ToArray());
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].LayoutId="compat.layout.r2");
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Placements=data.GeometryRecords[0].Layouts[0].Placements.Skip(1).ToArray());
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Placements[1].Role=CompatibilityRouteRole.Entrance);
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Placements=data.GeometryRecords[0].Layouts[0].Placements.Concat(new[]{new CompatibilityLayoutPlacement{Role=CompatibilityRouteRole.BasicRoom1,Anchor=new TileCoordinate(0,6),Orientation=CardinalOrientation.Zero}}).ToArray());
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Connections=data.GeometryRecords[0].Layouts[0].Connections.Skip(1).ToArray());
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Connections=data.GeometryRecords[0].Layouts[0].Connections.Concat(new[]{data.GeometryRecords[0].Layouts[0].Connections[0]}).ToArray());
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Connections[0].SourceConnectionPointId="north");
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Connections[0].DestinationConnectionPointId="north");
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Connections[0].SocketTypeId="wrong.socket");
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Connections[0].ConnectionKind=FloorRouteConnectionKind.PhysicalCorridor);
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Connections[0].CorridorDefinitionId="spatial.corridor.straight_stone");
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Placements[1].Orientation=CardinalOrientation.Ninety);
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Placements[0].Anchor.X=99);
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Placements[1].Anchor.Y=0);
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].Placements[1].Anchor.Y=4);
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].FloorIndex=1,
                SpatialLayoutCompatibilityDiagnostic.InvalidProductionReference);
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].Layouts[0].ExpectedOccupiedTileTotal=25);
        }

        [Test]
        public void ClockwiseSocketTransformsUseTheFacingOrientationConvention()
        {
            var footprint=new RectangularFootprintDefinition(4,3);
            var anchor=new TileCoordinate(10,20);
            var offset=new TileCoordinate(1,2);
            AssertTransform(offset,anchor,CardinalOrientation.Zero,footprint,11,22);
            AssertTransform(offset,anchor,CardinalOrientation.Ninety,footprint,12,22);
            AssertTransform(offset,anchor,CardinalOrientation.OneEighty,footprint,12,20);
            AssertTransform(offset,anchor,CardinalOrientation.TwoSeventy,footprint,10,21);

            var northEdge=new TileCoordinate(1,2);
            AssertTransform(northEdge,anchor,CardinalOrientation.Ninety,footprint,12,22);
            Assert.That(((int)CardinalOrientation.Zero+(int)CardinalOrientation.Ninety)%4,Is.EqualTo(1));
            AssertTransform(northEdge,anchor,CardinalOrientation.TwoSeventy,footprint,10,21);
            Assert.That(((int)CardinalOrientation.Zero+(int)CardinalOrientation.TwoSeventy)%4,Is.EqualTo(3));
        }

        [Test]
        public void SyntheticClockwiseRotatedDirectDoorwaysValidateWithoutMutatingInputs()
        {
            SpatialContentCatalog catalog=spatial.Catalog;
            RoomSpatialDefinition basic=catalog.Rooms.Single(value=>value.RoomDefinitionId=="spatial.room.basic");
            basic.AllowedOrientations=new[]{CardinalOrientation.Zero,CardinalOrientation.Ninety};
            var rotatedSpatial=new ProductionSpatialContentSnapshot(spatial.Manifest,catalog,spatial.Languages);
            SpatialLayoutCompatibilityProfilesData data=GeometryOnlyData();
            string sourceBefore=profiles.text; string catalogBefore=JsonUtility.ToJson(catalog);
            SetRotated(data.GeometryRecords[0].Layouts[0],new[]{new TileCoordinate(0,1),new TileCoordinate(2,0),new TileCoordinate(6,1)});
            SetRotated(data.GeometryRecords[0].Layouts[1],new[]{new TileCoordinate(0,1),new TileCoordinate(2,0),new TileCoordinate(6,0),new TileCoordinate(10,1)});
            data.GeometryRecords[0].CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            data=SpatialLayoutCompatibilityProfiles.Canonicalize(data);
            TextAsset rotated=new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(data)));
            string rotatedBefore=rotated.text;
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(rotated,rotatedSpatial,limits).Success,Is.True);
            Assert.That(rotated.text,Is.EqualTo(rotatedBefore)); Assert.That(profiles.text,Is.EqualTo(sourceBefore));
            Assert.That(JsonUtility.ToJson(catalog),Is.EqualTo(catalogBefore));

            data.GeometryRecords[0].Layouts[0].Placements[1].Anchor=new TileCoordinate(1,0);
            data.GeometryRecords[0].CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            AssertGeometryDiagnostic(SpatialLayoutCompatibilityProfiles.ParseAndValidate(
                CanonicalAsset(data),rotatedSpatial,limits));
            data.GeometryRecords[0].Layouts[0].Placements[1].Anchor=new TileCoordinate(3,0);
            data.GeometryRecords[0].CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            AssertGeometryDiagnostic(SpatialLayoutCompatibilityProfiles.ParseAndValidate(
                CanonicalAsset(data),rotatedSpatial,limits));

            data.GeometryRecords[0].Layouts[0].Placements[1].Anchor=new TileCoordinate(2,0);
            SpatialContentCatalog sameFacingCatalog=rotatedSpatial.Catalog;
            sameFacingCatalog.Rooms.Single(value=>value.RoomDefinitionId=="spatial.room.basic").ConnectionPoints
                .Single(value=>value.ConnectionPointId=="south").Facing=CardinalOrientation.Zero;
            var sameFacingSpatial=new ProductionSpatialContentSnapshot(rotatedSpatial.Manifest,sameFacingCatalog,rotatedSpatial.Languages);
            data.GeometryRecords[0].CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            AssertGeometryDiagnostic(SpatialLayoutCompatibilityProfiles.ParseAndValidate(
                CanonicalAsset(data),sameFacingSpatial,limits));
        }

        [TestCase(".leading")][TestCase("trailing-")][TestCase("two..segments")]
        [TestCase("UPPER")][TestCase("white space")][TestCase("nönascii")]
        public void StableIdGrammarRejectsInvalidSegments(string id)
        {
            SpatialLayoutCompatibilityProfilesData data=GeometryOnlyData();
            data.GeometryRecords[0].GeometryId=id;
            data.GeometryRecords[0].CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            TextAsset asset=new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(data)));
            SpatialLayoutCompatibilityResult result=SpatialLayoutCompatibilityProfiles.ParseAndValidate(asset,spatial,limits);
            Assert.That(result.Diagnostics.Contains(SpatialLayoutCompatibilityDiagnostic.InvalidStableId),Is.True,id);
            Assert.That(result.Diagnostics.Contains(SpatialLayoutCompatibilityDiagnostic.InvalidLifecycleSelection),Is.False,id);
        }

        [Test] public void SemanticDiagnosticLimitFailsAsWorkloadWithoutPublication()
        {
            SpatialLayoutCompatibilityProfilesData data=GeometryOnlyData();
            data.GeometryRecords[0].GeometryId="INVALID";
            data.GeometryRecords[0].GeometryVersion=0;
            data.GeometryRecords[0].CanonicalHash="bad";
            TextAsset asset=new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(data)));
            var oneIssue=new SpatialContentValidationWorkloadLimits(limits.MaximumTopLevelRecords,
                limits.MaximumNestedRecords,limits.MaximumMaterializedTiles,1,limits.MaximumStringCharacters);
            SpatialLayoutCompatibilityResult result=SpatialLayoutCompatibilityProfiles.ParseAndValidate(asset,spatial,oneIssue);
            Assert.That(result.Success,Is.False); Assert.That(result.Value,Is.Null);
            CollectionAssert.AreEqual(new[]{SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded},result.Diagnostics);
        }

        [Test] public void DifferentSemanticReasonsAreAcceptedAtExactIssueBoundary()
        {
            SpatialLayoutCompatibilityProfilesData data=GeometryOnlyData();
            data.GeometryRecords[0].GeometryId="INVALID"; data.GeometryRecords[0].GeometryVersion=0;
            data.GeometryRecords[0].CanonicalHash="bad";
            var exact=new SpatialContentValidationWorkloadLimits(limits.MaximumTopLevelRecords,
                limits.MaximumNestedRecords,limits.MaximumMaterializedTiles,3,limits.MaximumStringCharacters);
            TextAsset asset=new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(data)));
            CollectionAssert.AreEqual(new[]{SpatialLayoutCompatibilityDiagnostic.InvalidStableId,
                SpatialLayoutCompatibilityDiagnostic.InvalidVersion,SpatialLayoutCompatibilityDiagnostic.InvalidHash},
                SpatialLayoutCompatibilityProfiles.ParseAndValidate(asset,spatial,exact).Diagnostics);
        }

        [Test] public void RepeatedSemanticReasonsCountOccurrencesAtExactBoundary()
        {
            TextAsset invalid=RepeatedInvalidGeometryAsset(2);
            var exact=new SpatialContentValidationWorkloadLimits(limits.MaximumTopLevelRecords,
                limits.MaximumNestedRecords,limits.MaximumMaterializedTiles,2,limits.MaximumStringCharacters);
            SpatialLayoutCompatibilityResult exactResult=SpatialLayoutCompatibilityProfiles.ParseAndValidate(invalid,spatial,exact);
            CollectionAssert.AreEqual(new[]{SpatialLayoutCompatibilityDiagnostic.InvalidStableId},exactResult.Diagnostics);
            var oneOver=new SpatialContentValidationWorkloadLimits(limits.MaximumTopLevelRecords,
                limits.MaximumNestedRecords,limits.MaximumMaterializedTiles,1,limits.MaximumStringCharacters);
            SpatialLayoutCompatibilityResult overflow=SpatialLayoutCompatibilityProfiles.ParseAndValidate(invalid,spatial,oneOver);
            CollectionAssert.AreEqual(new[]{SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded},overflow.Diagnostics);
            Assert.That(overflow.Value,Is.Null);
            SpatialLayoutCompatibilityProfilesData reversed=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(invalid.text);
            System.Array.Reverse(reversed.GeometryRecords);
            TextAsset reversedAsset=CanonicalAsset(reversed);
            CollectionAssert.AreEqual(overflow.Diagnostics,
                SpatialLayoutCompatibilityProfiles.ParseAndValidate(reversedAsset,spatial,oneOver).Diagnostics);
        }

        [Test] public void OverflowReloadPreservesPreviousPublishedSnapshot()
        {
            var service=new ContentService();
            Assert.That(service.LoadSpatialLayoutCompatibilityProfiles(profiles,spatial,limits).Success,Is.True);
            SpatialLayoutCompatibilitySnapshot previous=service.SpatialLayoutCompatibilityProfiles;
            var oneIssue=new SpatialContentValidationWorkloadLimits(limits.MaximumTopLevelRecords,
                limits.MaximumNestedRecords,limits.MaximumMaterializedTiles,1,limits.MaximumStringCharacters);
            Assert.That(service.LoadSpatialLayoutCompatibilityProfiles(RepeatedInvalidGeometryAsset(2),spatial,oneIssue).Success,Is.False);
            Assert.That(service.SpatialLayoutCompatibilityProfiles,Is.SameAs(previous));
        }

        [Test] public void MaterializedTileBoundaryUsesWorkloadDiagnostic()
        {
            var exact=new SpatialContentValidationWorkloadLimits(limits.MaximumTopLevelRecords,
                limits.MaximumNestedRecords,42,limits.MaximumIssues,limits.MaximumStringCharacters);
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(profiles,spatial,exact).Success,Is.True);
            var oneOver=new SpatialContentValidationWorkloadLimits(limits.MaximumTopLevelRecords,
                limits.MaximumNestedRecords,41,limits.MaximumIssues,limits.MaximumStringCharacters);
            SpatialLayoutCompatibilityResult result=SpatialLayoutCompatibilityProfiles.ParseAndValidate(profiles,spatial,oneOver);
            Assert.That(result.Value,Is.Null);
            Assert.That(result.Diagnostics.Contains(SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded),Is.True);
            Assert.That(result.Diagnostics.Contains(SpatialLayoutCompatibilityDiagnostic.InvalidGeometry),Is.False);
        }

        [Test] public void StrictCompatibilityWorkloadDimensionsHonorExactAndOneOverBoundaries()
        {
            SpatialLayoutCompatibilityProfilesData data=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            int authoredTopLevelRecords = CountAuthoredTopLevelRecords(data);
            int authoredNestedRecords = CountAuthoredNestedRecords(data);
            AssertWorkloadBoundary(profiles,authoredTopLevelRecords,authoredNestedRecords,
                AuthoredCharacters(data));

            CompatibilityLayoutGeometryRecord copy=JsonUtility.FromJson<CompatibilityLayoutGeometryRecord>(
                JsonUtility.ToJson(data.GeometryRecords[0]));
            copy.GeometryId="compat.geometry.second"; copy.CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(copy);
            data.GeometryRecords=new[]{data.GeometryRecords[0],copy}; data=SpatialLayoutCompatibilityProfiles.Canonicalize(data);
            TextAsset twoTopLevel=new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(data)));
            SpatialContentValidationWorkloadLimits topOne=Limits(4,limits.MaximumNestedRecords,
                limits.MaximumMaterializedTiles,limits.MaximumStringCharacters);
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(twoTopLevel,spatial,topOne).Diagnostics
                .Contains(SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded),Is.True);
        }

        [TestCase("bom",SpatialLayoutCompatibilityDiagnostic.InvalidEncoding)]
        [TestCase("crlf",SpatialLayoutCompatibilityDiagnostic.InvalidEncoding)]
        [TestCase("no-newline",SpatialLayoutCompatibilityDiagnostic.InvalidEncoding)]
        [TestCase("invalid-utf8",SpatialLayoutCompatibilityDiagnostic.InvalidEncoding)]
        [TestCase("malformed",SpatialLayoutCompatibilityDiagnostic.InvalidJson)]
        [TestCase("trailing",SpatialLayoutCompatibilityDiagnostic.InvalidJson)]
        [TestCase("duplicate",SpatialLayoutCompatibilityDiagnostic.InvalidJson)]
        [TestCase("unknown",SpatialLayoutCompatibilityDiagnostic.InvalidJson)]
        [TestCase("ambiguous",SpatialLayoutCompatibilityDiagnostic.InvalidJson)]
        [TestCase("missing",SpatialLayoutCompatibilityDiagnostic.InvalidJson)]
        [TestCase("wrong-type",SpatialLayoutCompatibilityDiagnostic.InvalidJson)]
        [TestCase("noncanonical",SpatialLayoutCompatibilityDiagnostic.NoncanonicalInput)]
        [TestCase("schema-id",SpatialLayoutCompatibilityDiagnostic.InvalidSchema)]
        [TestCase("schema-version",SpatialLayoutCompatibilityDiagnostic.InvalidSchema)]
        public void AllResolversPreserveStrictStructuralDiagnostics(string mutation,
            SpatialLayoutCompatibilityDiagnostic expected)
        {
            byte[] bytes=Mutate(profiles.bytes,mutation); byte[] before=(byte[])bytes.Clone();
            AssertResolverFailure(bytes,limits,expected); CollectionAssert.AreEqual(before,bytes);
        }

        [Test] public void AllResolversPreserveMissingAndWorkloadDiagnostics()
        {
            SpatialLayoutCompatibilityProfilesData data =
                JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            int exactTop = CountAuthoredTopLevelRecords(data);
            int exactNested = CountAuthoredNestedRecords(data);
            int exactCharacters = AuthoredCharacters(data);
            AssertResolverFailure(null,limits,SpatialLayoutCompatibilityDiagnostic.MissingInput);
            AssertResolverFailure(new byte[0],limits,SpatialLayoutCompatibilityDiagnostic.EmptyInput);
            AssertResolverFailure(RepeatedInvalidGeometryAsset(2).bytes,Limits(1,limits.MaximumNestedRecords,
                limits.MaximumMaterializedTiles,limits.MaximumStringCharacters),
                SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded);
            AssertResolverFailure(profiles.bytes,Limits(exactTop,exactNested-1,limits.MaximumMaterializedTiles,
                limits.MaximumStringCharacters),SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded);
            AssertResolverFailure(profiles.bytes,Limits(exactTop,exactNested,limits.MaximumMaterializedTiles,
                exactCharacters-1),
                SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded);
            AssertResolverFailure(profiles.bytes,Limits(exactTop,exactNested,41,limits.MaximumStringCharacters),
                SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded);
            var oneIssue=new SpatialContentValidationWorkloadLimits(limits.MaximumTopLevelRecords,
                limits.MaximumNestedRecords,limits.MaximumMaterializedTiles,1,limits.MaximumStringCharacters);
            AssertResolverFailure(RepeatedInvalidGeometryAsset(2).bytes,oneIssue,
                SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded);
        }

        [Test] public void Canonicalization_DetachesAndOrdersWithoutMutatingSource()
        {
            SpatialLayoutCompatibilityProfilesData source=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            System.Array.Reverse(source.GeometryRecords[0].Layouts);
            string before=JsonUtility.ToJson(source);
            SpatialLayoutCompatibilityProfilesData canonical=SpatialLayoutCompatibilityProfiles.Canonicalize(source);
            Assert.That(JsonUtility.ToJson(source),Is.EqualTo(before));
            Assert.That(canonical.GeometryRecords[0].Layouts.Select(x=>x.LayoutId),Is.EqualTo(new[]{"compat.layout.r1","compat.layout.r2"}));
            CollectionAssert.AreEqual(profiles.bytes,SpatialLayoutCompatibilityProfiles.SerializeCanonical(canonical));
        }

        private static TextAsset Asset(string path)
        { TextAsset value=AssetDatabase.LoadAssetAtPath<TextAsset>(path); Assert.That(value,Is.Not.Null,path); return value; }
        private SpatialLayoutCompatibilitySnapshot ValidatedSnapshot(
            SpatialMigrationCompatibilityProfile[] migrations, CanonicalStarterLayoutProfile[] starters,
            CanonicalLayoutContractSelection[] selections)
        {
            SpatialLayoutCompatibilityProfilesData data=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            CompatibilityLayoutGeometryRecord geometry=data.GeometryRecords[0];
            foreach(SpatialMigrationCompatibilityProfile profile in migrations??System.Array.Empty<SpatialMigrationCompatibilityProfile>())
            { profile.GeometryId=geometry.GeometryId; profile.GeometryVersion=geometry.GeometryVersion; profile.GeometryCanonicalHash=geometry.CanonicalHash; profile.CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(profile); }
            foreach(CanonicalStarterLayoutProfile profile in starters??System.Array.Empty<CanonicalStarterLayoutProfile>())
            { profile.GeometryId=geometry.GeometryId; profile.GeometryVersion=geometry.GeometryVersion; profile.GeometryCanonicalHash=geometry.CanonicalHash; profile.CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeStarterProfileHash(profile); }
            data.MigrationProfiles=migrations??System.Array.Empty<SpatialMigrationCompatibilityProfile>();
            data.StarterProfiles=starters??System.Array.Empty<CanonicalStarterLayoutProfile>();
            data.ContractSelections=selections??System.Array.Empty<CanonicalLayoutContractSelection>();
            data=SpatialLayoutCompatibilityProfiles.Canonicalize(data);
            TextAsset asset=new TextAsset(System.Text.Encoding.UTF8.GetString(SpatialLayoutCompatibilityProfiles.SerializeCanonical(data)));
            SpatialLayoutCompatibilityResult result=SpatialLayoutCompatibilityProfiles.ParseAndValidate(asset,spatial,limits);
            Assert.That(result.Success,Is.True,string.Join(",",result.Diagnostics.Select(value=>value.ToString()).ToArray()));
            return result.Value;
        }
        private TextAsset ConfigurationAsset(SpatialMigrationCompatibilityProfile[] migrations,
            CanonicalStarterLayoutProfile[] starters,CanonicalLayoutContractSelection[] selections,bool computeHashes)
        {
            SpatialLayoutCompatibilityProfilesData data=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            CompatibilityLayoutGeometryRecord geometry=data.GeometryRecords[0];
            foreach(SpatialMigrationCompatibilityProfile profile in migrations??System.Array.Empty<SpatialMigrationCompatibilityProfile>())
            { profile.GeometryId=geometry.GeometryId; profile.GeometryVersion=geometry.GeometryVersion; profile.GeometryCanonicalHash=geometry.CanonicalHash;
                if(computeHashes) profile.CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(profile); }
            foreach(CanonicalStarterLayoutProfile profile in starters??System.Array.Empty<CanonicalStarterLayoutProfile>())
            { profile.GeometryId=geometry.GeometryId; profile.GeometryVersion=geometry.GeometryVersion; profile.GeometryCanonicalHash=geometry.CanonicalHash;
                if(computeHashes) profile.CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeStarterProfileHash(profile); }
            data.MigrationProfiles=migrations??System.Array.Empty<SpatialMigrationCompatibilityProfile>(); data.StarterProfiles=starters??System.Array.Empty<CanonicalStarterLayoutProfile>();
            data.ContractSelections=selections??System.Array.Empty<CanonicalLayoutContractSelection>(); return CanonicalAsset(data);
        }
        private static TextAsset CanonicalAsset(SpatialLayoutCompatibilityProfilesData data)
        { data=SpatialLayoutCompatibilityProfiles.Canonicalize(data); return new TextAsset(System.Text.Encoding.UTF8.GetString(SpatialLayoutCompatibilityProfiles.SerializeCanonical(data))); }
        private static SpatialMigrationCompatibilityProfile Migration(CompatibilityProfileLifecycle lifecycle)
        { return new SpatialMigrationCompatibilityProfile{ProfileId="test.migration",ProfileVersion=1,Lifecycle=lifecycle,MinimumSourceSchemaVersion=1,MaximumSourceSchemaVersion=3,TargetSchemaVersion=10,TargetCanonicalLayoutContractVersion=2}; }
        private static CanonicalStarterLayoutProfile Starter(CompatibilityProfileLifecycle lifecycle)
        { return new CanonicalStarterLayoutProfile{ProfileId="test.starter",ProfileVersion=1,Lifecycle=lifecycle,TargetSchemaVersion=10,CanonicalLayoutContractVersion=2}; }
        private TextAsset RepeatedInvalidGeometryAsset(int count)
        {
            SpatialLayoutCompatibilityProfilesData data=GeometryOnlyData();
            CompatibilityLayoutGeometryRecord template=data.GeometryRecords[0];
            data.GeometryRecords=Enumerable.Range(0,count).Select(index=>
            {
                CompatibilityLayoutGeometryRecord copy=JsonUtility.FromJson<CompatibilityLayoutGeometryRecord>(JsonUtility.ToJson(template));
                copy.GeometryId="INVALID"+index; copy.CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(copy); return copy;
            }).ToArray();
            data=SpatialLayoutCompatibilityProfiles.Canonicalize(data);
            return new TextAsset(System.Text.Encoding.UTF8.GetString(SpatialLayoutCompatibilityProfiles.SerializeCanonical(data)));
        }
        private SpatialLayoutCompatibilityResult ParseData(SpatialLayoutCompatibilityProfilesData data)
        {
            data=SpatialLayoutCompatibilityProfiles.Canonicalize(data);
            TextAsset asset=new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(data)));
            return SpatialLayoutCompatibilityProfiles.ParseAndValidate(asset,spatial,limits);
        }
        private void AssertGeometryMutationFails(System.Action<SpatialLayoutCompatibilityProfilesData> mutation)
        {
            SpatialLayoutCompatibilityProfilesData data=GeometryOnlyData();
            mutation(data);
            data.GeometryRecords[0].CanonicalHash=
                SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            AssertGeometryDiagnostic(ParseData(data));
        }
        private void AssertGeometryMutationFails(
            System.Action<SpatialLayoutCompatibilityProfilesData> mutation,
            SpatialLayoutCompatibilityDiagnostic expected)
        {
            SpatialLayoutCompatibilityProfilesData data=GeometryOnlyData();
            mutation(data);
            data.GeometryRecords[0].CanonicalHash=
                SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            SpatialLayoutCompatibilityResult result=ParseData(data);
            Assert.That(result.Success,Is.False);
            Assert.That(result.Diagnostics.Contains(expected),Is.True,
                string.Join(",",result.Diagnostics.Select(value=>value.ToString()).ToArray()));
            Assert.That(result.Diagnostics.Contains(SpatialLayoutCompatibilityDiagnostic.InvalidLifecycleSelection),
                Is.False);
        }
        private SpatialLayoutCompatibilityProfilesData GeometryOnlyData()
        {
            SpatialLayoutCompatibilityProfilesData data=
                JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            data.MigrationProfiles=System.Array.Empty<SpatialMigrationCompatibilityProfile>();
            data.StarterProfiles=System.Array.Empty<CanonicalStarterLayoutProfile>();
            data.ContractSelections=System.Array.Empty<CanonicalLayoutContractSelection>();
            return data;
        }
        private static void AssertGeometryDiagnostic(SpatialLayoutCompatibilityResult result)
        {
            Assert.That(result.Success,Is.False);
            Assert.That(result.Diagnostics.Any(value=>value==SpatialLayoutCompatibilityDiagnostic.InvalidGeometry ||
                value==SpatialLayoutCompatibilityDiagnostic.IncompleteGeometry ||
                value==SpatialLayoutCompatibilityDiagnostic.DuplicateLayout),Is.True,
                string.Join(",",result.Diagnostics.Select(value=>value.ToString()).ToArray()));
            Assert.That(result.Diagnostics.Contains(SpatialLayoutCompatibilityDiagnostic.InvalidLifecycleSelection),
                Is.False);
        }
        private static void AssertTransform(TileCoordinate offset,TileCoordinate anchor,
            CardinalOrientation orientation,RectangularFootprintDefinition footprint,int x,int y)
        {
            Assert.That(SpatialLayoutCompatibilityProfiles.TryTransformPoint(offset,anchor,orientation,footprint,
                out TileCoordinate transformed),Is.True);
            Assert.That(transformed.X,Is.EqualTo(x)); Assert.That(transformed.Y,Is.EqualTo(y));
        }
        private static void SetRotated(CompatibilityLayoutVariant layout,TileCoordinate[] anchors)
        {
            for(int index=0;index<layout.Placements.Length;index++)
            { layout.Placements[index].Anchor=anchors[index]; layout.Placements[index].Orientation=CardinalOrientation.Ninety; }
        }
        private void AssertWorkloadBoundary(TextAsset asset,int top,int nested,int characters)
        {
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(asset,spatial,
                Limits(top,nested,limits.MaximumMaterializedTiles,characters)).Diagnostics
                .Contains(SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded),Is.False);
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(asset,spatial,
                Limits(top,nested-1,limits.MaximumMaterializedTiles,characters)).Diagnostics
                .Contains(SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded),Is.True);
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(asset,spatial,
                Limits(top,nested,limits.MaximumMaterializedTiles,characters-1)).Diagnostics
                .Contains(SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded),Is.True);
        }
        private void AssertResolverFailure(byte[] bytes,SpatialContentValidationWorkloadLimits suppliedLimits,
            SpatialLayoutCompatibilityDiagnostic expected)
        {
            AssertFailedResolution(SpatialLayoutCompatibilityProfiles.ResolveMigration(bytes,spatial,suppliedLimits,2,10,2),expected);
            AssertFailedResolution(SpatialLayoutCompatibilityProfiles.ResolveStarter(bytes,spatial,suppliedLimits,10,2),expected);
            AssertFailedResolution(SpatialLayoutCompatibilityProfiles.ResolveContract(bytes,spatial,suppliedLimits,10),expected);
        }
        private static void AssertFailedResolution<T>(CompatibilityConfigurationResolution<T> result,
            SpatialLayoutCompatibilityDiagnostic expected) where T:class
        {
            Assert.That(result.Snapshot,Is.Null); Assert.That(result.Selection.Value,Is.Null);
            Assert.That(result.Selection.Code,Is.EqualTo(string.Empty));
            CollectionAssert.AreEqual(new[]{expected},result.Diagnostics);
        }
        private void AssertMigrationDuplicatePair(SpatialMigrationCompatibilityProfile first,
            SpatialMigrationCompatibilityProfile second,int rawSchema)
        {
            TextAsset forward=ConfigurationAsset(new[]{first,second},null,null,true);
            TextAsset reverse=ConfigurationAsset(new[]{second,first},null,null,true);
            AssertMigrationDuplicate(SpatialLayoutCompatibilityProfiles.ResolveMigration(forward,spatial,limits,rawSchema,10,2));
            AssertMigrationDuplicate(SpatialLayoutCompatibilityProfiles.ResolveMigration(reverse,spatial,limits,rawSchema,10,2));
            if(first.ProfileId==second.ProfileId&&first.ProfileVersion==second.ProfileVersion)
            {
                SpatialLayoutCompatibilityProfilesData mismatchedGeometry=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(forward.text);
                mismatchedGeometry.MigrationProfiles[1].GeometryVersion++;
                mismatchedGeometry.MigrationProfiles[1].CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(
                    mismatchedGeometry.MigrationProfiles[1]);
                AssertMigrationDuplicate(SpatialLayoutCompatibilityProfiles.ResolveMigration(CanonicalAsset(mismatchedGeometry),
                    spatial,limits,rawSchema,10,2));
            }
        }
        private static void AssertMigrationDuplicate(
            CompatibilityConfigurationResolution<SpatialMigrationCompatibilityProfile> result)
        {
            Assert.That(result.Selection.Code,Is.EqualTo("gd66.profile.duplicate"));
            Assert.That(result.Snapshot,Is.Null); Assert.That(result.Selection.Value,Is.Null);
        }
        private static void AssertNoStableCode<T>(CompatibilityConfigurationResolution<T> result) where T:class
        {
            Assert.That(result.Selection.Code,Is.EqualTo(string.Empty)); Assert.That(result.Snapshot,Is.Null);
            Assert.That(result.Selection.Value,Is.Null); Assert.That(result.Diagnostics,Is.Not.Empty);
        }
        private SpatialContentValidationWorkloadLimits Limits(int top,int nested,int tiles,int characters)
        { return new SpatialContentValidationWorkloadLimits(top,nested,tiles,limits.MaximumIssues,characters); }
        private static int CountAuthoredTopLevelRecords(SpatialLayoutCompatibilityProfilesData data) =>
            (data.GeometryRecords?.Length ?? 0) + (data.MigrationProfiles?.Length ?? 0) +
            (data.StarterProfiles?.Length ?? 0) + (data.ContractSelections?.Length ?? 0);
        private static int CountAuthoredNestedRecords(SpatialLayoutCompatibilityProfilesData data) =>
            (data.GeometryRecords ?? Array.Empty<CompatibilityLayoutGeometryRecord>()).Sum(geometry =>
                (geometry.Layouts?.Length ?? 0) +
                (geometry.Layouts ?? Array.Empty<CompatibilityLayoutVariant>()).Sum(layout =>
                    (layout.Placements?.Length ?? 0) + (layout.Connections?.Length ?? 0)));
        private static int AuthoredCharacters(SpatialLayoutCompatibilityProfilesData data)
        {
            CompatibilityLayoutGeometryRecord geometry=data.GeometryRecords[0];
            int total=(data.Schema??string.Empty).Length+(geometry.GeometryId??string.Empty).Length+
                (geometry.CanonicalHash??string.Empty).Length+(geometry.FloorDefinitionId??string.Empty).Length+
                (geometry.EntranceStructureDefinitionId??string.Empty).Length+(geometry.EntranceConnectionPointId??string.Empty).Length+
                (geometry.CompletionStructureDefinitionId??string.Empty).Length+(geometry.CompletionConnectionPointId??string.Empty).Length+
                (geometry.BasicRoomDefinitionId??string.Empty).Length+(geometry.BasicRoomSouthConnectionPointId??string.Empty).Length+
                (geometry.BasicRoomNorthConnectionPointId??string.Empty).Length+(geometry.SocketTypeId??string.Empty).Length;
            foreach(CompatibilityLayoutVariant layout in geometry.Layouts)
            { total+=(layout.LayoutId??string.Empty).Length; foreach(CompatibilityLayoutConnection connection in layout.Connections)
                total+=(connection.SourceConnectionPointId??string.Empty).Length+(connection.DestinationConnectionPointId??string.Empty).Length+
                    (connection.SocketTypeId??string.Empty).Length+(connection.CorridorDefinitionId??string.Empty).Length; }
            foreach(SpatialMigrationCompatibilityProfile profile in data.MigrationProfiles)
                total+=(profile.ProfileId??string.Empty).Length+(profile.CanonicalHash??string.Empty).Length+
                    (profile.GeometryId??string.Empty).Length+(profile.GeometryCanonicalHash??string.Empty).Length;
            foreach(CanonicalStarterLayoutProfile profile in data.StarterProfiles)
                total+=(profile.ProfileId??string.Empty).Length+(profile.CanonicalHash??string.Empty).Length+
                    (profile.GeometryId??string.Empty).Length+(profile.GeometryCanonicalHash??string.Empty).Length;
            return total;
        }
        private static void AssertPlacement(CompatibilityLayoutVariant layout,CompatibilityRouteRole role,int x,int y)
        { CompatibilityLayoutPlacement value=layout.Placements.Single(item=>item.Role==role); Assert.That(value.Anchor.X,Is.EqualTo(x)); Assert.That(value.Anchor.Y,Is.EqualTo(y)); Assert.That(value.Orientation,Is.EqualTo(CardinalOrientation.Zero)); }
        private static byte[] Mutate(byte[] source,string mutation)
        {
            string json=System.Text.Encoding.UTF8.GetString(source);
            if(mutation=="bom") return new byte[]{0xef,0xbb,0xbf}.Concat(source).ToArray();
            if(mutation=="invalid-utf8") { byte[] invalid=(byte[])source.Clone(); invalid[10]=0xff; return invalid; }
            if(mutation=="crlf") return System.Text.Encoding.UTF8.GetBytes(json.Replace("\n","\r\n"));
            if(mutation=="no-newline") return source.Take(source.Length-1).ToArray();
            if(mutation=="two-newlines") return source.Concat(new byte[]{(byte)'\n'}).ToArray();
            if(mutation=="malformed") json="{\n";
            else if(mutation=="trailing") json=json+"{}\n";
            else if(mutation=="duplicate") json=json.Replace("\"Schema\":", "\"Schema\": \"spatial_layout_compatibility_profiles\",\n    \"Schema\":");
            else if(mutation=="unknown") json=json.Replace("{\n", "{\n    \"Unknown\": 1,\n");
            else if(mutation=="ambiguous") json=json.Replace("\"Schema\":", "\"schema\":");
            else if(mutation=="missing") json=json.Replace("    \"SchemaVersion\": 1,\n", string.Empty);
            else if(mutation=="wrong-type") json=json.Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": \"1\"");
            else if(mutation=="decimal") json=json.Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": 1.0");
            else if(mutation=="overflow") json=json.Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": 999999999999");
            else if(mutation=="unknown-enum") json=json.Replace("\"Role\": 1", "\"Role\": 99");
            else if(mutation=="noncanonical") json=json.Replace("    \"Schema\"", "  \"Schema\"");
            else if(mutation=="schema-id") json=json.Replace("spatial_layout_compatibility_profiles","wrong_schema");
            else if(mutation=="schema-version") json=json.Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": 2");
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
    }
}
#endif
