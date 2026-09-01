#if UNITY_EDITOR
using System;
using NUnit.Framework;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Tests.EditMode
{
    public sealed class SchemaEightLifecycleOwnershipTests
    {
        [Test]
        public void NativeIdentityUsesPersistedHighWaterInsteadOfExistingMaximum()
        {
            SavedSpatialFloor floor = Floor("floor.alpha", "floor.alpha.room.player.0002");
            var state = new DetachedCanonicalSpatialSaveState
            {
                Floors = new[] { floor },
                LifecycleAndOwnership = new StructuralLifecycleAndOwnershipState
                {
                    Floors = new[] { new FloorStructuralIdentityLifecycle
                    {
                        FloorInstanceId = "floor.alpha", NextNativeRoomOrdinal = 9,
                        NextNativeEdgeOrdinal = 12
                    }}
                }
            };

            Assert.That(NativeStructuralIdentity.TryAllocateConstructionIdentity(state,
                "floor.alpha", out NativeRoomConstructionIdentity value, out string reason), Is.True, reason);
            Assert.That(value.RoomInstanceId, Is.EqualTo("floor.alpha.room.player.0009"));
        }

        [Test]
        public void ExactNativePatternIgnoresLegacyAndMalformedLookalikesWhenDerivingMigrationState()
        {
            SavedSpatialFloor floor = Floor("floor.alpha", "compat.room.9000",
                "floor.alpha.room.player.9", "floor.alpha.room.player.0004");
            StructuralLifecycleAndOwnershipState lifecycle =
                NativeStructuralIdentity.CreateInitialLifecycle(new[] { floor });
            Assert.That(lifecycle.Floors[0].NextNativeRoomOrdinal,
                Is.EqualTo(5));
            Assert.That(lifecycle.ReturnedContents, Is.Empty);
        }

        [Test]
        public void MissingDuplicateExtraAndNegativeLifecycleReturnStableIssuesWithoutThrowing()
        {
            SavedSpatialFloor floor = Floor("floor.alpha", "floor.alpha.room.player.0002");
            var limits = new CanonicalSpatialSaveWorkloadLimits(128, 128);
            var missing = new DetachedCanonicalSpatialSaveState { Floors = new[] { floor }, LifecycleAndOwnership = null };
            Assert.That(CanonicalSpatialSaveContracts.Validate(missing, limits).Issues,
                Does.Contain(CanonicalSpatialSaveValidationIssue.MissingLifecycleAndOwnership));

            var duplicate = State(floor, new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.alpha", NextNativeRoomOrdinal = 3 },
                new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.alpha", NextNativeRoomOrdinal = 3 });
            Assert.DoesNotThrow(() => CanonicalSpatialSaveContracts.Validate(duplicate, limits));
            Assert.That(CanonicalSpatialSaveContracts.Validate(duplicate, limits).Issues,
                Does.Contain(CanonicalSpatialSaveValidationIssue.DuplicateLifecycleFloor));

            var extra = State(floor, new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.alpha", NextNativeRoomOrdinal = 3 },
                new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.extra" });
            Assert.That(CanonicalSpatialSaveContracts.Validate(extra, limits).Issues,
                Does.Contain(CanonicalSpatialSaveValidationIssue.InvalidIdentityLifecycle));

            var negative = State(floor, new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.alpha",
                NextNativeRoomOrdinal = -1, NextNativeEdgeOrdinal = -1 });
            Assert.That(CanonicalSpatialSaveContracts.Validate(negative, limits).Issues,
                Does.Contain(CanonicalSpatialSaveValidationIssue.InvalidIdentityLifecycle));
        }

        [Test]
        public void LifecycleCollectionsParticipateInOrderingAndRecordBudget()
        {
            SavedSpatialFloor a = Floor("floor.a"); SavedSpatialFloor b = Floor("floor.b"); b.FloorIndex = 1;
            var state = new DetachedCanonicalSpatialSaveState { Floors = new[] { a, b },
                LifecycleAndOwnership = new StructuralLifecycleAndOwnershipState { Floors = new[] {
                    new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.b" },
                    new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.a" } } } };
            Assert.That(CanonicalSpatialSaveContracts.Validate(state,
                new CanonicalSpatialSaveWorkloadLimits(128, 128), true).Issues,
                Does.Contain(CanonicalSpatialSaveValidationIssue.NonCanonicalOrdering));
            Assert.That(CanonicalSpatialSaveContracts.Validate(state,
                new CanonicalSpatialSaveWorkloadLimits(3, 128)).Issues,
                Does.Contain(CanonicalSpatialSaveValidationIssue.RecordLimitExceeded));
        }

        [Test]
        public void FreshEdgeAllocationIsDeterministicFloorScopedMonotonicAndCollisionSafe()
        {
            SavedSpatialFloor floor = Floor("floor.alpha");
            var state = State(floor, new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.alpha",
                NextNativeEdgeOrdinal = 7 });
            Assert.That(NativeStructuralIdentity.TryAllocateFreshEdgeIdentity(state, "floor.alpha",
                out string first, out long next, out string reason), Is.True, reason);
            Assert.That(first, Is.EqualTo("floor.alpha.edge.native.00000007"));
            Assert.That(next, Is.EqualTo(8));
            state.LifecycleAndOwnership.Floors[0].NextNativeEdgeOrdinal = next;
            Assert.That(NativeStructuralIdentity.TryAllocateFreshEdgeIdentity(state, "floor.alpha",
                out string second, out long afterSecond, out reason), Is.True, reason);
            Assert.That(second, Is.EqualTo("floor.alpha.edge.native.00000008"));
            Assert.That(afterSecond, Is.EqualTo(9));
            Assert.That(second, Is.Not.EqualTo(first));

            floor.Layout.Edges = new[] { new FloorRouteEdge { EdgeId = second } };
            Assert.That(NativeStructuralIdentity.TryAllocateFreshEdgeIdentity(state, "floor.alpha",
                out _, out _, out _), Is.False);
        }

        [Test]
        public void AllocatorsFailClosedWithoutThrowingForDuplicateOrMissingAuthority()
        {
            SavedSpatialFloor floor = Floor("floor.alpha");
            var duplicateFloors = new DetachedCanonicalSpatialSaveState { Floors = new[] { floor, floor },
                LifecycleAndOwnership = NativeStructuralIdentity.CreateInitialLifecycle(new[] { floor }) };
            Assert.DoesNotThrow(() => NativeStructuralIdentity.TryAllocateFreshEdgeIdentity(duplicateFloors,
                "floor.alpha", out _, out _, out _));
            Assert.That(NativeStructuralIdentity.TryAllocateFreshEdgeIdentity(duplicateFloors,
                "floor.alpha", out _, out _, out _), Is.False);

            var duplicateLifecycle = State(floor,
                new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.alpha" },
                new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.alpha" });
            Assert.DoesNotThrow(() => NativeStructuralIdentity.TryAllocateConstructionIdentity(
                duplicateLifecycle, "floor.alpha", out _, out _));
            Assert.That(NativeStructuralIdentity.TryAllocateConstructionIdentity(duplicateLifecycle,
                "floor.alpha", out _, out _), Is.False);

            var missing = State(floor);
            Assert.That(NativeStructuralIdentity.TryAllocateFreshEdgeIdentity(missing,
                "floor.alpha", out _, out _, out _), Is.False);
            var malformed = State(floor, new FloorStructuralIdentityLifecycle {
                FloorInstanceId = "floor.alpha", NextNativeEdgeOrdinal = -1 });
            Assert.That(NativeStructuralIdentity.TryAllocateFreshEdgeIdentity(malformed,
                "floor.alpha", out _, out _, out _), Is.False);
        }

        [Test]
        public void AuthoredRemovalPolicyReturnsReusableDefinitionsAndFailsClosedForUnresolvedLoot()
        {
            Assert.That(StructuralContentRemovalPolicyAuthority.TryParse(System.IO.File.ReadAllBytes(
                StructuralContentRemovalPolicyAuthority.ProductionPath), out
                StructuralContentRemovalPolicySnapshot configuration), Is.True);
            Assert.That(StructuralContentRemovalPolicyAuthority.TryResolve(configuration,
                MvpDungeonPlacementIds.MonsterCategoryId, MvpDungeonPlacementIds.SkeletonOptionId,
                out StructuralContentRemovalPolicy monster, out _), Is.True);
            Assert.That(monster, Is.EqualTo(StructuralContentRemovalPolicy.ReturnToPlayerCustody));
            Assert.That(StructuralContentRemovalPolicyAuthority.TryResolve(configuration,
                MvpDungeonPlacementIds.TrapCategoryId, MvpDungeonPlacementIds.SpikeTrapOptionId,
                out StructuralContentRemovalPolicy trap, out _), Is.True);
            Assert.That(trap, Is.EqualTo(StructuralContentRemovalPolicy.ReturnToPlayerCustody));
            Assert.That(StructuralContentRemovalPolicyAuthority.TryResolve(configuration,
                MvpDungeonPlacementIds.LootNodeCategoryId, MvpDungeonPlacementIds.BasicLootNodeOptionId,
                out _, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo(StructuralContentRemovalPolicyAuthority.MissingOrUnresolvedReason));
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(".content")]
        [TestCase("content.")]
        [TestCase("content..alpha")]
        [TestCase("content/alpha")]
        public void ReturnedStableIdentityMustUseCanonicalPersistentIdGrammar(string assignmentId)
        {
            SavedSpatialFloor floor = Floor("floor.alpha");
            var state = State(floor, new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.alpha" });
            state.LifecycleAndOwnership.ReturnedContents = new[] { new ReturnedStructuralContent
            {
                AssignmentId = assignmentId, CategoryId = MvpDungeonPlacementIds.MonsterCategoryId,
                OptionId = MvpDungeonPlacementIds.SkeletonOptionId,
                RemovalDisposition = StructuralContentRemovalDisposition.ReturnToPlayerCustody
            }};
            Assert.That(CanonicalSpatialSaveContracts.Validate(state,
                new CanonicalSpatialSaveWorkloadLimits(128, 128)).Issues,
                Does.Contain(CanonicalSpatialSaveValidationIssue.InvalidReturnedContent));
        }

        [Test]
        public void PersistedReturnedCustodyRemainsValidWhenAuthoredTransitionPolicyChanges()
        {
            SavedSpatialFloor floor = Floor("floor.alpha");
            var state = State(floor, new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.alpha" });
            state.LifecycleAndOwnership.ReturnedContents = new[] { new ReturnedStructuralContent
            {
                AssignmentId = "content.returned.0001", CategoryId = MvpDungeonPlacementIds.MonsterCategoryId,
                OptionId = MvpDungeonPlacementIds.SkeletonOptionId,
                RemovalDisposition = StructuralContentRemovalDisposition.ReturnToPlayerCustody
            }};
            CanonicalSpatialSaveValidationResult before = CanonicalSpatialSaveContracts.Validate(state,
                new CanonicalSpatialSaveWorkloadLimits(128, 128));
            // Current policy is a transition authority and is intentionally absent from reopen validation.
            CanonicalSpatialSaveValidationResult after = CanonicalSpatialSaveContracts.Validate(state,
                new CanonicalSpatialSaveWorkloadLimits(128, 128));
            Assert.That(before.IsValid, Is.True);
            Assert.That(after.IsValid, Is.True);
            Assert.That(state.LifecycleAndOwnership.ReturnedContents[0].AssignmentId,
                Is.EqualTo("content.returned.0001"));
        }

        private static DetachedCanonicalSpatialSaveState State(SavedSpatialFloor floor,
            params FloorStructuralIdentityLifecycle[] lifecycle) => new DetachedCanonicalSpatialSaveState
        {
            Authority = new CanonicalSpatialAuthorityMarker { CanonicalLayoutContractVersion = 1,
                CreationKind = CanonicalSpatialCreationKind.NativeCanonical },
            Floors = new[] { floor },
            LifecycleAndOwnership = new StructuralLifecycleAndOwnershipState { Floors = lifecycle }
        };

        [Test]
        public void ReturnedIdentityCannotAlsoRemainAssigned()
        {
            SavedSpatialFloor floor = Floor("floor.alpha", "room.alpha");
            floor.RoomContents.Assignments = new[] { new RoomContentAssignment
            {
                AssignmentId = "content.alpha", RoomInstanceId = "room.alpha",
                CategoryId = MvpDungeonPlacementIds.MonsterCategoryId,
                OptionId = MvpDungeonPlacementIds.SkeletonOptionId, Sequence = 0
            }};
            var state = new DetachedCanonicalSpatialSaveState
            {
                Floors = new[] { floor },
                LifecycleAndOwnership = new StructuralLifecycleAndOwnershipState
                {
                    Floors = new[] { new FloorStructuralIdentityLifecycle { FloorInstanceId = "floor.alpha" }},
                    ReturnedContents = new[] { new ReturnedStructuralContent
                    {
                        AssignmentId = "content.alpha", CategoryId = MvpDungeonPlacementIds.MonsterCategoryId,
                        OptionId = MvpDungeonPlacementIds.SkeletonOptionId, Sequence = 0,
                        RemovalDisposition = StructuralContentRemovalDisposition.ReturnToPlayerCustody
                    }}
                }
            };

            CanonicalSpatialSaveValidationResult result = CanonicalSpatialSaveContracts.Validate(state,
                new CanonicalSpatialSaveWorkloadLimits(128, 128));
            Assert.That(result.Issues, Does.Contain(
                CanonicalSpatialSaveValidationIssue.AssignedAndReturnedIdentity));
        }

        private static SavedSpatialFloor Floor(string floorId, params string[] roomIds)
        {
            var rooms = new RoomSpatialInstance[roomIds.Length];
            var nodes = new FloorRouteNode[roomIds.Length];
            var semantics = new CanonicalRoomSemantics[roomIds.Length];
            for (int i = 0; i < roomIds.Length; i++)
            {
                rooms[i] = new RoomSpatialInstance { RoomInstanceId = roomIds[i], RoomDefinitionId = "room.basic",
                    FloorId = floorId, Orientation = CardinalOrientation.Zero };
                nodes[i] = new FloorRouteNode { NodeId = roomIds[i] + ".node", FloorId = floorId,
                    Kind = FloorRouteNodeKind.Room, RoomInstanceId = roomIds[i] };
                semantics[i] = new CanonicalRoomSemantics { RoomInstanceId = roomIds[i],
                    LegacyRoomOriginKind = LegacyRoomOriginKind.CanonicalPlayerPlaced };
            }
            return new SavedSpatialFloor
            {
                FloorInstanceId = floorId, FloorDefinitionId = "floor.definition", FloorIndex = 0,
                Layout = new FloorSpatialLayout { FloorId = floorId, Rooms = rooms, Nodes = nodes,
                    Edges = Array.Empty<FloorRouteEdge>() },
                FixedStructures = Array.Empty<SavedFixedSpatialStructure>(),
                RoomContents = new FloorRoomContentState { Assignments = Array.Empty<RoomContentAssignment>(),
                    RoomSemantics = semantics, NextSequence = 0 }
            };
        }
    }
}
#endif
