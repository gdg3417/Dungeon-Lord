#if UNITY_EDITOR
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


        [Test]
        public void Resolve_RoomOneWithoutRoomZeroPreservesAuthoritativeIndex()
        {
            var save = new SaveData { mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection { Rooms = new List<MvpRoomSlotAssignmentState> {
                new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 1, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId }
            } } };
            MvpOrderedRouteRoom[] route = MvpOrderedRoomRouteResolver.Resolve(save, Config());
            Assert.That(route, Has.Length.EqualTo(1));
            Assert.That(route[0].RoomIndex, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_RoomIndexAboveGd60RangeIsIgnored()
        {
            var save = new SaveData { mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection { Rooms = new List<MvpRoomSlotAssignmentState> {
                new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 2, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId }
            } } };
            Assert.That(MvpOrderedRoomRouteResolver.Resolve(save, Config()), Is.Empty);
        }

        [Test]
        public void Resolve_ImplicitFallbackRoomDoesNotBecomeActiveRoomPlacement()
        {
            MvpOrderedRouteRoom[] route = MvpOrderedRoomRouteResolver.Resolve(new SaveData(), Config());
            Assert.That(route, Has.Length.EqualTo(1));
            Assert.That(route[0].IncludeRoomPlacement, Is.False);
            Assert.That(route[0].ToOrderedPlacements(), Is.Empty);
        }

        [Test]
        public void ConfiguredRouteEffects_IncludeBothRoomsEvenWhenSelectionIsRoomTwo()
        {
            RunSimulationConfig config = Config();
            config.MvpPlacementEffects = new[] {
                new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.SkeletonOptionId, Danger = 2 },
                new MvpPlacementEffectConfig { CategoryId = MvpDungeonPlacementIds.MonsterCategoryId, OptionId = MvpDungeonPlacementIds.GoblinOptionId, Danger = 3 }
            };
            var save = new SaveData { mvpSelectedRoomSlotIndex = 1, mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection { Rooms = new List<MvpRoomSlotAssignmentState> {
                new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 0, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterOptionIds = new[] { MvpDungeonPlacementIds.SkeletonOptionId } },
                new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 1, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterOptionIds = new[] { MvpDungeonPlacementIds.GoblinOptionId } }
            } } };
            MvpPlacementEffectsSummary effects = MvpPlacementEffectsResolver.ResolveConfiguredRouteForSave(save, config);
            Assert.That(effects.Danger, Is.EqualTo(5));
            Assert.That(effects.ContributingOptionIds, Does.Contain(MvpDungeonPlacementIds.SkeletonOptionId));
            Assert.That(effects.ContributingOptionIds, Does.Contain(MvpDungeonPlacementIds.GoblinOptionId));
        }

        [Test]
        public void Resolve_LegacyPlacementsRemainOneCompatibilityEncounter()
        {
            var save = new SaveData { mvpDungeonPlacements = new MvpDungeonPlacementState() };
            save.mvpDungeonPlacements.Entries.Add(new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId, 1));
            save.mvpDungeonPlacements.Entries.Add(new MvpDungeonPlacementEntry(MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId, 2));
            MvpOrderedRouteRoom[] route = MvpOrderedRoomRouteResolver.Resolve(save, Config());
            Assert.That(route, Has.Length.EqualTo(1));
            Assert.That(route[0].AssignedMonsterOptionIds, Does.Contain(MvpDungeonPlacementIds.SkeletonOptionId));
            Assert.That(route[0].AssignedLootNodeOptionIds, Does.Contain(MvpDungeonPlacementIds.BasicLootNodeOptionId));
        }

        [Test]
        public void Resolve_OnlyNegativePersistedRoomReturnsEmpty()
        {
            var save = new SaveData { mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection { Rooms = new List<MvpRoomSlotAssignmentState> { new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = -1 } } } };
            Assert.That(MvpOrderedRoomRouteResolver.Resolve(save, Config()), Is.Empty);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Resolve_ValidRoomMixedWithUnsupportedRoomPreservesAuthoritativeValidIndex(int validIndex)
        {
            var save = new SaveData { mvpRoomSlotAssignments = new MvpRoomSlotAssignmentCollection { Rooms = new List<MvpRoomSlotAssignmentState> {
                new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = validIndex, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId },
                new MvpRoomSlotAssignmentState { FloorIndex = 0, RoomIndex = 2, RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId } } } };
            MvpOrderedRouteRoom[] route = MvpOrderedRoomRouteResolver.Resolve(save, Config());
            Assert.That(route, Has.Length.EqualTo(1));
            Assert.That(route[0].RoomIndex, Is.EqualTo(validIndex));
        }

        private static RunSimulationConfig Config() => new RunSimulationConfig { MvpRoomSlotCapacities = new[] { new MvpRoomSlotCapacityConfig { RoomOptionId = MvpDungeonPlacementIds.BasicRoomOptionId, MonsterCapacity = 1, TrapCapacity = 1, LootCapacity = 1 } } };
    }
}
#endif
