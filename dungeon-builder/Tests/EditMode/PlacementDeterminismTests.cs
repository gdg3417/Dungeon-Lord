using System.Linq;
using DungeonBuilder.DungeonLayout;
using DungeonBuilder.Save;
using NUnit.Framework;

namespace DungeonBuilder.Tests.EditMode
{
    public class PlacementDeterminismTests
    {
        [Test]
        public void Placement_Order_Is_Deterministic_By_Floor_Then_Slot()
        {
            var state = DungeonLayoutState.CreateEmpty(2, 3);
            state.Slots.Reverse();

            var service = new PlacementService();
            var ordered = service.GetPlacementOrder(state).ToList();

            Assert.That(ordered[0].FloorIndex, Is.EqualTo(0));
            Assert.That(ordered[0].SlotIndex, Is.EqualTo(0));
            Assert.That(ordered[5].FloorIndex, Is.EqualTo(1));
            Assert.That(ordered[5].SlotIndex, Is.EqualTo(2));
        }

        [Test]
        public void Slot_StableKey_Is_Serialization_Safe()
        {
            var slot = new DungeonSlot(3, 12, "");
            Assert.That(slot.StableKey, Is.EqualTo("03:012"));
        }

        [Test]
        public void Legacy_Save_Gets_Empty_Layout_On_Migration()
        {
            var legacy = new SaveRoot
            {
                SchemaVersion = 1,
                Mana = 50,
                HeatTier = 1,
                Layout = null
            };

            var migrated = SaveMigration.MigrateToLatest(legacy);

            Assert.That(migrated.SchemaVersion, Is.EqualTo(SaveMigration.LatestSchemaVersion));
            Assert.That(migrated.Layout, Is.Not.Null);
            Assert.That(migrated.Layout.Slots.Count, Is.EqualTo(
                SaveMigration.DefaultFloorCount * SaveMigration.DefaultSlotsPerFloor));
            Assert.That(migrated.Mana, Is.EqualTo(50));
            Assert.That(migrated.HeatTier, Is.EqualTo(1));
        }
    }
}
