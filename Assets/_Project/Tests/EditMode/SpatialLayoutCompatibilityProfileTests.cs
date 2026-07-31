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
            SpatialMigrationCompatibilityProfile migrationProfile=new SpatialMigrationCompatibilityProfile{ProfileId="test.migration",ProfileVersion=1,Lifecycle=CompatibilityProfileLifecycle.Active,MinimumSourceSchemaVersion=1,MaximumSourceSchemaVersion=3,TargetSchemaVersion=10,TargetCanonicalLayoutContractVersion=2,GeometryId="test.geometry",GeometryVersion=1,GeometryCanonicalHash=new string('a',64)};
            migrationProfile.CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(migrationProfile);
            CanonicalStarterLayoutProfile starterProfile=new CanonicalStarterLayoutProfile{ProfileId="test.starter",ProfileVersion=1,Lifecycle=CompatibilityProfileLifecycle.Retired,TargetSchemaVersion=10,CanonicalLayoutContractVersion=2,GeometryId="test.geometry",GeometryVersion=1,GeometryCanonicalHash=new string('a',64)};
            starterProfile.CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeStarterProfileHash(starterProfile);
            var data=new SpatialLayoutCompatibilityProfilesData {
                MigrationProfiles=new[]{migrationProfile}, StarterProfiles=new[]{starterProfile},
                ContractSelections=new[]{new CanonicalLayoutContractSelection{Lifecycle=CompatibilityProfileLifecycle.Active,TargetSchemaVersion=10,CanonicalLayoutContractVersion=2}}
            };
            Assert.That(SpatialLayoutCompatibilityProfiles.SelectMigration(data,2,10,2).Code,Is.EqualTo(string.Empty));
            Assert.That(SpatialLayoutCompatibilityProfiles.SelectMigration(data,4,10,2).Code,Is.EqualTo("gd66.profile.missing"));
            Assert.That(SpatialLayoutCompatibilityProfiles.SelectStarter(data,10,2).Code,Is.EqualTo("gd66.starter_profile.missing"));
            Assert.That(SpatialLayoutCompatibilityProfiles.SelectContract(data,10).Success,Is.True);
            Assert.That(SpatialLayoutCompatibilityProfiles.SelectContract(data,11).Code,Is.EqualTo("gd66.layout_contract.selection_missing"));
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
            var profile=new SpatialMigrationCompatibilityProfile{ProfileId="test.profile",ProfileVersion=2,Lifecycle=CompatibilityProfileLifecycle.Retired,MinimumSourceSchemaVersion=1,MaximumSourceSchemaVersion=6,TargetSchemaVersion=8,TargetCanonicalLayoutContractVersion=1,GeometryId="test.geometry",GeometryVersion=3,GeometryCanonicalHash=new string('b',64)};
            profile.CanonicalHash=SpatialLayoutCompatibilityProfiles.ComputeMigrationProfileHash(profile);
            var data=new SpatialLayoutCompatibilityProfilesData{MigrationProfiles=new[]{profile},GeometryRecords=new[]{
                new CompatibilityLayoutGeometryRecord{GeometryId=profile.GeometryId,GeometryVersion=3,CanonicalHash=profile.GeometryCanonicalHash}}};
            Assert.That(SpatialLayoutCompatibilityProfiles.TryRecoverMigration(data,profile.ProfileId,2,
                profile.CanonicalHash,profile.GeometryId,3,profile.GeometryCanonicalHash,out var recovered),Is.True);
            Assert.That(recovered.Lifecycle,Is.EqualTo(CompatibilityProfileLifecycle.Retired));
            Assert.That(SpatialLayoutCompatibilityProfiles.TryRecoverMigration(data,profile.ProfileId,2,
                new string('c',64),profile.GeometryId,3,profile.GeometryCanonicalHash,out _),Is.False);
            Assert.That(SpatialLayoutCompatibilityProfiles.TryRecoverMigration(data,profile.ProfileId,2,
                profile.CanonicalHash,profile.GeometryId,4,profile.GeometryCanonicalHash,out _),Is.False);
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
