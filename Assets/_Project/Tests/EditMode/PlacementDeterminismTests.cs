using System;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonLayout;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
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
            var slot = new DungeonSlot(3, 12, string.Empty);
            Assert.That(slot.StableKey, Is.EqualTo("03:012"));
        }

        [Test]
        public void Legacy_Save_Gets_Empty_Layout_On_Migration_Without_Changing_Sprint1_Fields()
        {
            var legacy = new SaveRoot
            {
                schemaVersion = 1,
                primary = new SaveData
                {
                    totalTicks = 50,
                    dungeonLayout = null
                }
            };

            var migrated = SaveMigration.MigrateToLatest(legacy);

            Assert.That(migrated.schemaVersion, Is.EqualTo(SaveMigration.LatestSchemaVersion));
            Assert.That(migrated.primary.dungeonLayout, Is.Not.Null);
            Assert.That(migrated.primary.dungeonLayout.Slots.Count, Is.EqualTo(
                SaveMigration.DefaultFloorCount * SaveMigration.DefaultSlotsPerFloor));
            Assert.That(migrated.primary.totalTicks, Is.EqualTo(50));
        }

        [Test]
        public void PlaceStructure_Writes_Expected_StructureId_To_Expected_Slot()
        {
            var state = DungeonLayoutState.CreateEmpty(1, 2);
            var service = new PlacementService();

            service.PlaceStructure(state, 0, 1, "structure.mana_farm");

            var placed = state.Slots.Single(x => x.FloorIndex == 0 && x.SlotIndex == 1);
            Assert.That(placed.StructureId, Is.EqualTo("structure.mana_farm"));
        }

        [Test]
        public void PlaceStructure_Rejects_Unknown_Slot()
        {
            var state = DungeonLayoutState.CreateEmpty(1, 1);
            var service = new PlacementService();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                service.PlaceStructure(state, 5, 9, "structure.mana_farm"));

            Assert.That(ex.Message, Is.EqualTo("Unknown slot floor=5, slot=9"));
        }

        [Test]
        public void PlaceStructure_Rejects_Overwrite_When_Not_Allowed()
        {
            var state = DungeonLayoutState.CreateEmpty(1, 1);
            var service = new PlacementService();

            service.PlaceStructure(state, 0, 0, "structure.a");

            var ex = Assert.Throws<InvalidOperationException>(() =>
                service.PlaceStructure(state, 0, 0, "structure.b"));

            Assert.That(ex.Message, Is.EqualTo("Slot already occupied floor=0, slot=0"));
        }

        [Test]
        public void PlaceStructure_Rejects_Empty_StructureId()
        {
            var state = DungeonLayoutState.CreateEmpty(1, 1);
            var service = new PlacementService();

            Assert.Throws<ArgumentException>(() => service.PlaceStructure(state, 0, 0, ""));
            Assert.Throws<ArgumentException>(() => service.PlaceStructure(state, 0, 0, "   "));
            Assert.Throws<ArgumentException>(() => service.PlaceStructure(state, 0, 0, null));
        }

        [Test]
        public void DungeonSlot_Does_Not_Override_GetHashCode()
        {
            var method = typeof(DungeonSlot).GetMethod(nameof(GetHashCode));
            Assert.That(method.DeclaringType, Is.EqualTo(typeof(ValueType)));
        }
    }
}
