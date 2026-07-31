#if UNITY_EDITOR
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

        [Test] public void ProductionConfiguration_IsCanonicalInactiveAndRecomputesR1R2()
        {
            SpatialLayoutCompatibilityResult result=SpatialLayoutCompatibilityProfiles.ParseAndValidate(profiles,spatial,limits,null,true);
            Assert.That(result.Success,Is.True,string.Join(",",result.Diagnostics.Select(x=>x.ToString()).ToArray()));
            SpatialLayoutCompatibilityProfilesData data=result.Value.Value;
            Assert.That(data.GeometryRecords,Has.Length.EqualTo(1));
            Assert.That(data.GeometryRecords[0].Layouts.Select(x=>x.ExpectedOccupiedTileTotal).ToArray(),Is.EqualTo(new[]{26,42}));
            Assert.That(data.MigrationProfiles,Is.Empty); Assert.That(data.StarterProfiles,Is.Empty); Assert.That(data.ContractSelections,Is.Empty);
            Assert.That(SaveMigration.LatestSchemaVersion,Is.EqualTo(6));
            CollectionAssert.AreEqual(profiles.bytes,result.Value.CanonicalBytes);
            CompatibilityLayoutGeometryRecord geometry=data.GeometryRecords[0];
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

        [Test] public void GeometryAndProfileHashesCoverEveryCanonicalFieldMutation()
        {
            SpatialLayoutCompatibilityProfilesData data=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            string geometryHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            data.GeometryRecords[0].Layouts[0].ExpectedOccupiedTileTotal++;
            Assert.That(SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]),Is.Not.EqualTo(geometryHash));
            var profile=new SpatialMigrationCompatibilityProfile{ProfileId="test.profile",ProfileVersion=1,Lifecycle=CompatibilityProfileLifecycle.Retired,MinimumSourceSchemaVersion=1,MaximumSourceSchemaVersion=6,TargetSchemaVersion=8,TargetCanonicalLayoutContractVersion=1,GeometryId="test.geometry",GeometryVersion=1,GeometryCanonicalHash=new string('a',64)};
            string hash=SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(profile); profile.MaximumSourceSchemaVersion++;
            Assert.That(SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(profile),Is.Not.EqualTo(hash));
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
            Assert.That(SpatialLayoutCompatibilityProfiles.ResolveMigration(profiles,spatial,limits,2,10,2).Selection.Code,
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
            AssertGeometryMutationFails(data=>data.GeometryRecords[0].FloorIndex=1);
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
            SpatialLayoutCompatibilityProfilesData data=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
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
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(CanonicalAsset(data),rotatedSpatial,limits).Success,Is.False);
            data.GeometryRecords[0].Layouts[0].Placements[1].Anchor=new TileCoordinate(3,0);
            data.GeometryRecords[0].CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(CanonicalAsset(data),rotatedSpatial,limits).Success,Is.False);

            data.GeometryRecords[0].Layouts[0].Placements[1].Anchor=new TileCoordinate(2,0);
            SpatialContentCatalog sameFacingCatalog=rotatedSpatial.Catalog;
            sameFacingCatalog.Rooms.Single(value=>value.RoomDefinitionId=="spatial.room.basic").ConnectionPoints
                .Single(value=>value.ConnectionPointId=="south").Facing=CardinalOrientation.Zero;
            var sameFacingSpatial=new ProductionSpatialContentSnapshot(rotatedSpatial.Manifest,sameFacingCatalog,rotatedSpatial.Languages);
            data.GeometryRecords[0].CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(CanonicalAsset(data),sameFacingSpatial,limits).Success,Is.False);
        }

        [TestCase(".leading")][TestCase("trailing-")][TestCase("two..segments")]
        [TestCase("UPPER")][TestCase("white space")][TestCase("nönascii")]
        public void StableIdGrammarRejectsInvalidSegments(string id)
        {
            SpatialLayoutCompatibilityProfilesData data=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            data.GeometryRecords[0].GeometryId=id;
            data.GeometryRecords[0].CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            TextAsset asset=new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(data)));
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(asset,spatial,limits).Success,Is.False,id);
        }

        [Test] public void SemanticDiagnosticLimitFailsAsWorkloadWithoutPublication()
        {
            SpatialLayoutCompatibilityProfilesData data=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
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
            SpatialLayoutCompatibilityProfilesData data=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
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
            TextAsset reversedAsset=new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(reversed)));
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
            Assert.That(result.Diagnostics,Does.Contain(SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded));
            Assert.That(result.Diagnostics,Does.Not.Contain(SpatialLayoutCompatibilityDiagnostic.InvalidGeometry));
        }

        [Test] public void StrictCompatibilityWorkloadDimensionsHonorExactAndOneOverBoundaries()
        {
            SpatialLayoutCompatibilityProfilesData data=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            AssertWorkloadBoundary(profiles,1,14,AuthoredCharacters(data));

            CompatibilityLayoutGeometryRecord copy=JsonUtility.FromJson<CompatibilityLayoutGeometryRecord>(
                JsonUtility.ToJson(data.GeometryRecords[0]));
            copy.GeometryId="compat.geometry.second"; copy.CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(copy);
            data.GeometryRecords=new[]{data.GeometryRecords[0],copy}; data=SpatialLayoutCompatibilityProfiles.Canonicalize(data);
            TextAsset twoTopLevel=new TextAsset(System.Text.Encoding.UTF8.GetString(
                SpatialLayoutCompatibilityProfiles.SerializeCanonical(data)));
            SpatialContentValidationWorkloadLimits topOne=Limits(1,limits.MaximumNestedRecords,
                limits.MaximumMaterializedTiles,limits.MaximumStringCharacters);
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(twoTopLevel,spatial,topOne).Diagnostics,
                Does.Contain(SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded));
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
            SpatialLayoutCompatibilityProfilesData data=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
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
            SpatialLayoutCompatibilityProfilesData data=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            mutation(data); data.GeometryRecords[0].CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeGeometryHash(data.GeometryRecords[0]);
            Assert.That(ParseData(data).Success,Is.False);
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
                Limits(top,nested,limits.MaximumMaterializedTiles,characters)).Diagnostics,
                Does.Not.Contain(SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded));
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(asset,spatial,
                Limits(top,nested-1,limits.MaximumMaterializedTiles,characters)).Diagnostics,
                Does.Contain(SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded));
            Assert.That(SpatialLayoutCompatibilityProfiles.ParseAndValidate(asset,spatial,
                Limits(top,nested,limits.MaximumMaterializedTiles,characters-1)).Diagnostics,
                Does.Contain(SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded));
        }
        private SpatialContentValidationWorkloadLimits Limits(int top,int nested,int tiles,int characters)
        { return new SpatialContentValidationWorkloadLimits(top,nested,tiles,limits.MaximumIssues,characters); }
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
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
    }
}
#endif
