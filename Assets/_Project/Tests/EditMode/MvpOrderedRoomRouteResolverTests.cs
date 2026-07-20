using System.Collections.Generic;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;
using NUnit.Framework;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class MvpOrderedRoomRouteResolverTests
    {
        [Test]
        public void Resolve_OrdersRoomsAndPreservesIndependentContent()
        {
            var save = new SaveData { mvpSelectedRoomSlotIndex = 1, mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection { Rooms = new List<MvpRoomSlotAssignmentState> {
                new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 1, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterOptionIds = new[] { MvpDungeonPlacementIds.GoblinOptionId }, TrapOptionIds = new[] { MvpDungeonPlacementIds.ChillingSigilOptionId }, LootNodeOptionIds = new[] { MvpDungeonPlacementIds.GlitteringHoardOptionId } },
                new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 0, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterOptionIds = new[] { MvpDungeonPlacementIds.SkeletonOptionId }, TrapOptionIds = new[] { MvpDungeonPlacementIds.SnareTrapOptionId }, LootNodeOptionIds = new[] { MvpDungeonPlacementIds.BasicLootNodeOptionId } }
            } } };
            MvpOrderedRouteRoom[] route = MvpOrderedRoomRouteResolver.Resolve(save, Config());
            Assert.That(route, Has.Length.EqualTo(2));
            Assert.That(route[0].RoomIndex, Is.EqualTo(0));
            Assert.That(route[1].RoomIndex, Is.EqualTo(1));
            Assert.That(route[0].AssignedMonsterOptionIds[0], Is.EqualTo(MvpDungeonPlacementIds.SkeletonOptionId));
            Assert.That(route[1].AssignedMonsterOptionIds[0], Is.EqualTo(MvpDungeonPlacementIds.GoblinOptionId));
        }

        [Test]
        public void Resolve_DuplicateRoomIndexUsesLastRecordAndIgnoresNegativeRoom()
        {
            var save = new SaveData { mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection { Rooms = new List<MvpRoomSlotAssignmentState> {
                new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = -1, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId },
                new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 0, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterOptionIds = new[] { MvpDungeonPlacementIds.SkeletonOptionId } },
                new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 0, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterOptionIds = new[] { MvpDungeonPlacementIds.GoblinOptionId } }
            } } };
            MvpOrderedRouteRoom[] route = MvpOrderedRoomRouteResolver.Resolve(save, Config());
            Assert.That(route, Has.Length.EqualTo(1));
            Assert.That(route[0].AssignedMonsterOptionIds[0], Is.EqualTo(MvpDungeonPlacementIds.GoblinOptionId));
        }

        private static RunSimulationConfig Config() => new RunSimulationConfig { MvpRoomSlotCapacities = new[] { new MvpRoomSlotCapacityConfig { RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterCapacity = 1, TrapCapacity = 1, LootCapacity = 1 } } };
    }
}
