#if UNITY_EDITOR
using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonBuilder.M0.EditorTools.DungeonSpatial.Tests
{
    public sealed class DungeonVisualCatalogTests
    {
        private const string Path="Assets/_Project/UI/ProductionDungeon/Visuals.asset";
        [Test]
        public void ImportedCatalogAndEverySpriteArePersistentAndSurviveReimport()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<DungeonVisualCatalog>(Path);
            Assert.That(catalog,Is.Not.Null); Assert.That(catalog.IsComplete,Is.True);
            var mapped=catalog.RoomStone.Concat(catalog.Boundaries).Concat(catalog.SelectedEdges).Concat(catalog.Contents.Select(c=>c.Sprite)).Concat(new[] {
                catalog.Entrance,catalog.Terminal,catalog.Selection,catalog.Invalid,catalog.Grid,catalog.Anchor,
                catalog.Corridor,catalog.Surrounding,catalog.MonsterFallback,catalog.TrapFallback,catalog.LootFallback,catalog.DoorwayThreshold,catalog.CorridorThreshold }).ToArray();
            var imported=AssetDatabase.FindAssets("t:Texture2D",new[] {"Assets/_Project/UI/ProductionDungeon/Art"})
                .Select(AssetDatabase.GUIDToAssetPath).SelectMany(p=>AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>()).ToArray();
            Assert.That(imported.Length,Is.GreaterThanOrEqualTo(mapped.Distinct().Count()));
            var sprites=mapped.Concat(imported).Distinct().ToArray();
            foreach(var sprite in sprites)
            {
                Assert.That(sprite,Is.Not.Null); Assert.That(EditorUtility.IsPersistent(sprite),Is.True);
                var path=AssetDatabase.GetAssetPath(sprite);
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer.textureType,Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.mipmapEnabled,Is.False);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                Assert.That(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Any(s=>s.name==sprite.name),Is.True);
            }
            Assert.That(AssetDatabase.LoadAssetAtPath<DungeonPresentationPolicy>("Assets/_Project/UI/ProductionDungeon/Presentation.asset").Visuals,
                Is.EqualTo(catalog));
        }
        [TestCase("placement.category.monster","placement.option.monster.skeleton","skeleton")]
        [TestCase("placement.category.trap","placement.option.trap.spike","spike")]
        [TestCase("placement.category.loot_node","placement.option.loot_node.glittering_hoard","hoard")]
        public void ActualAuthoredOptionsResolveByCategoryAndStableIdentity(string category,string option,string spriteName)
        {
            var catalog=AssetDatabase.LoadAssetAtPath<DungeonVisualCatalog>(Path);
            Assert.That(catalog.Resolve(category,option).name,Is.EqualTo(spriteName));
            var config=JsonUtility.FromJson<RunSimulationConfig>(AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Data/Bootstrap/run_simulation_config.json").text);
            Assert.That(config,Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Data/Bootstrap/run_simulation_config.json").text,
                Does.Contain(option),"Mapping must reference authored identity");
        }
        [Test]
        public void ThresholdArtIsOpenAndMasonrySeamsHaveOpaqueContrast()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<DungeonVisualCatalog>(Path);
            foreach(var sprite in new[] {catalog.DoorwayThreshold,catalog.CorridorThreshold})
            {
                var texture=sprite.texture;
                Assert.That(texture.GetPixel(14,64).a,Is.GreaterThan(.9f),"Raised stone jamb");
                Assert.That(texture.GetPixel(64,80).a,Is.LessThan(.01f),"No closed leaf or barrier in the passage");
            }
            var lip=catalog.Boundaries[2].texture;
            Assert.That(lip.GetPixel(112,64).a,Is.GreaterThan(.9f));
            Assert.That(lip.GetPixel(127,64).grayscale-lip.GetPixel(112,64).grayscale,Is.GreaterThan(.25f),"Dark inset and bright cap survive overview reduction");
        }
        [Test]
        public void UnknownOptionsHaveDistinctSafeFallbacksIndependentOfMappingOrder()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<DungeonVisualCatalog>(Path);
            Assert.That(catalog.Resolve(CanonicalSpatialSaveContracts.MonsterCategoryId,"test.unknown"),Is.EqualTo(catalog.MonsterFallback));
            Assert.That(catalog.Resolve(CanonicalSpatialSaveContracts.TrapCategoryId,"test.unknown"),Is.EqualTo(catalog.TrapFallback));
            Assert.That(catalog.Resolve(CanonicalSpatialSaveContracts.LootNodeCategoryId,"test.unknown"),Is.EqualTo(catalog.LootFallback));
            Assert.That(new[] {catalog.MonsterFallback,catalog.TrapFallback,catalog.LootFallback}.Distinct().Count(),Is.EqualTo(3));
            var copy=UnityEngine.Object.Instantiate(catalog);
            try
            {
                Array.Reverse(copy.Contents);
                foreach(var value in catalog.Contents) Assert.That(copy.Resolve(value.CategoryId,value.OptionId),Is.EqualTo(value.Sprite));
                Assert.That(copy.Resolve("test.unknown.category","test.unknown.option"),Is.Not.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
        }
        [Test]
        public void CatalogContainsOnlyPresentationReferencesAndDoesNotOwnAuthorityFields()
        {
            var fields=typeof(DungeonContentVisual).GetFields().Select(f=>f.Name).ToArray();
            CollectionAssert.AreEquivalent(new[] {"CategoryId","OptionId","Sprite"},fields);
            Assert.That(typeof(DungeonVisualCatalog).GetFields().Any(f=>f.FieldType==typeof(SaveData)),Is.False);
        }
    }
}
#endif
