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
            CollectionAssert.AreEqual(profiles.bytes,result.Value.CanonicalBytes);
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
            var data=new SpatialLayoutCompatibilityProfilesData {
                MigrationProfiles=new[]{new SpatialMigrationCompatibilityProfile{ProfileId="test.migration",ProfileVersion=1,Lifecycle=CompatibilityProfileLifecycle.Active,MinimumSourceSchemaVersion=1,MaximumSourceSchemaVersion=3}},
                StarterProfiles=new[]{new CanonicalStarterLayoutProfile{ProfileId="test.starter",ProfileVersion=1,Lifecycle=CompatibilityProfileLifecycle.Retired,TargetSchemaVersion=10,CanonicalLayoutContractVersion=2}},
                ContractSelections=new[]{new CanonicalLayoutContractSelection{Lifecycle=CompatibilityProfileLifecycle.Active,TargetSchemaVersion=10,CanonicalLayoutContractVersion=2}}
            };
            Assert.That(SpatialLayoutCompatibilityProfiles.TrySelectMigration(data,2,out var migration),Is.True);
            Assert.That(migration.ProfileId,Is.EqualTo("test.migration"));
            Assert.That(SpatialLayoutCompatibilityProfiles.TrySelectStarter(data,10,2,out _),Is.False);
            Assert.That(SpatialLayoutCompatibilityProfiles.TrySelectContract(data,10,out var selection),Is.True);
            Assert.That(selection.CanonicalLayoutContractVersion,Is.EqualTo(2));
        }

        [Test] public void Canonicalization_DetachesAndOrdersWithoutMutatingSource()
        {
            SpatialLayoutCompatibilityProfilesData source=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(profiles.text);
            System.Array.Reverse(source.GeometryRecords[0].Layouts);
            string before=JsonUtility.ToJson(source);
            SpatialLayoutCompatibilityProfilesData canonical=SpatialLayoutCompatibilityProfiles.Canonicalize(source);
            Assert.That(JsonUtility.ToJson(source),Is.EqualTo(before));
            Assert.That(canonical.GeometryRecords[0].Layouts.Select(x=>x.LayoutId),Is.EqualTo(new[]{"compat.layout.r1","compat.layout.r2"}));
        }

        private static TextAsset Asset(string path)
        { TextAsset value=AssetDatabase.LoadAssetAtPath<TextAsset>(path); Assert.That(value,Is.Not.Null,path); return value; }
    }
}
#endif
