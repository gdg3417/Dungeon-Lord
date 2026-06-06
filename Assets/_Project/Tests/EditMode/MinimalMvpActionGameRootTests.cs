using DungeonBuilder.M0;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using DungeonBuilder.M0.Gameplay.Structures;
using NUnit.Framework;
using UnityEngine;

namespace DungeonBuilder.Tests.EditMode
{
    public class MinimalMvpActionGameRootTests
    {
        [Test]
        public void TryMvpPlaceOrModifySelectedStructure_ReusesPlacementPathAndAllowsSelectedSlotReplacement()
        {
            var go = new GameObject("GameRoot_MvpActionPlacement_Test");
            try
            {
                var root = go.AddComponent<GameRoot>();
                SaveData save = new SaveData
                {
                    dungeonLayout = DungeonLayoutState.CreateEmpty(1, 1),
                    structureRuntime = new StructureRuntimeState()
                };
                new PlacementService().PlaceStructure(save.dungeonLayout, 0, 0, StructureSimulationPass.HeatScrubberBasicId);
                typeof(GameRoot).GetProperty(nameof(GameRoot.Save))?.SetValue(root, save);

                bool devPanelPlacement = root.TryPlaceSelectedStructure(StructureSimulationPass.ManaGeneratorBasicId, out string devBannerKey);
                bool playerPlacement = root.TryMvpPlaceOrModifySelectedStructure(StructureSimulationPass.ManaGeneratorBasicId, out string playerBannerKey);

                Assert.That(devPanelPlacement, Is.False);
                Assert.That(devBannerKey, Is.EqualTo("ui.banner.place_failed"));
                Assert.That(playerPlacement, Is.True);
                Assert.That(playerBannerKey, Is.EqualTo("ui.banner.place_success"));
                Assert.That(root.GetSelectedSlotStructureId(), Is.EqualTo(StructureSimulationPass.ManaGeneratorBasicId));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
