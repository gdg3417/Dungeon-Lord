using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Economy
{
    public sealed class StructuralEconomyPreview
    {
        public StructuralEditPreview Spatial { get; internal set; }
        public double CurrentMana { get; internal set; }
        public double BaseCost { get; internal set; }
        public double Cost { get; internal set; }
        public double RefundBasis { get; internal set; }
        public double Refund { get; internal set; }
        public double CreditedRefund { get; internal set; }
        public double ResultingMana { get; internal set; }
        public string Reason { get; internal set; }
        public bool IsAffordable => Reason == null;
        internal StructuralInvestmentRecord[] Investment { get; set; }
    }

    public static class StructuralEconomyService
    {
        public const string InvalidReason = "structural.economy.invalid";
        public const string InsufficientReason = "structural.economy.insufficient_mana";
        public const string UndoUnavailableReason = "structural.economy.undo_unavailable";

        public static StructuralEconomyPreview Preview(StructuralEditPreview spatial,
            DetachedCanonicalSpatialSaveState current, StructuralInvestmentRecord[] investment,
            double balance, StructuralEconomySnapshot config, IReadOnlyList<FormulaModifier> modifiers = null)
        {
            if (spatial?.IsValid != true) return new StructuralEconomyPreview { Spatial = spatial,
                CurrentMana = balance, ResultingMana = balance, Reason = spatial?.ReasonCodes.FirstOrDefault() ?? InvalidReason };
            var result = Prepare(current, spatial.DetachedCandidate, investment, balance, config,
                spatial.Operation, spatial.TargetRoomInstanceId, modifiers);
            result.Spatial = spatial; return result;
        }

        internal static StructuralEconomyPreview Prepare(DetachedCanonicalSpatialSaveState current,
            DetachedCanonicalSpatialSaveState candidate, StructuralInvestmentRecord[] investment,
            double balance, StructuralEconomySnapshot config, StructuralEditOperation operation,
            string target, IReadOnlyList<FormulaModifier> modifiers = null)
        {
            var result = new StructuralEconomyPreview { CurrentMana = balance, ResultingMana = balance, Reason = InvalidReason };
            if (config == null || current == null || candidate == null || !StructuralEconomySnapshot.Nonnegative(balance) ||
                !StructuralInvestment.Valid(investment, current, int.MaxValue)) return result;
            try
            {
                var old = investment.ToDictionary(r => r.StructureId, r => r.Copy(), StringComparer.Ordinal);
                var next = StructuralInvestment.Zero(candidate).ToDictionary(r => r.StructureId, StringComparer.Ordinal);
                foreach (string id in next.Keys.ToArray()) if (old.TryGetValue(id, out var retained)) next[id] = retained.Copy();
                var beforeRooms = current.Floors.SelectMany(f => f.Layout.Rooms).ToArray();
                var afterRooms = candidate.Floors.SelectMany(f => f.Layout.Rooms).ToArray();
                var beforeEdges = current.Floors.SelectMany(f => f.Layout.Edges).ToArray();
                var afterEdges = candidate.Floors.SelectMany(f => f.Layout.Edges).ToArray();
                var carriedPhysicalTiles = new Dictionary<string, HashSet<TileCoordinate>>(StringComparer.Ordinal);

                // A replaced relationship carries investment only for infrastructure that survives.
                // Construction: old tail -> new incoming. Deletion: incoming -> new terminal.
                // Physical ownership follows the occupied tiles common to both relationships, rather
                // than the fresh edge ID. Renovation retains endpoint/edge IDs directly.
                foreach (var removed in beforeEdges.Where(e => !next.ContainsKey(e.EdgeId)))
                {
                    var successors = afterEdges.Where(e => !old.ContainsKey(e.EdgeId) &&
                        e.SourceNodeId == removed.SourceNodeId).ToArray();
                    if (successors.Length > 1) return result;
                    if (successors.Length == 1)
                    {
                        FloorRouteEdge successor = successors[0];
                        if (removed.ConnectionKind == FloorRouteConnectionKind.DirectDoorway &&
                            successor.ConnectionKind == FloorRouteConnectionKind.DirectDoorway)
                        {
                            Add(next[successor.EdgeId], old[removed.EdgeId]);
                            old[removed.EdgeId].ConstructionMana = 0;
                            old[removed.EdgeId].RenovationMana = 0;
                        }
                        else if (removed.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor &&
                            successor.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor &&
                            string.Equals(removed.CorridorDefinitionId, successor.CorridorDefinitionId,
                                StringComparison.Ordinal))
                        {
                            var previousTiles = new HashSet<TileCoordinate>(
                                removed.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>());
                            var carried = new HashSet<TileCoordinate>(
                                successor.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>());
                            carried.IntersectWith(previousTiles);
                            if (carried.Count != 0 && previousTiles.Count != 0)
                            {
                                StructuralInvestmentRecord retained = Share(old[removed.EdgeId],
                                    carried.Count, previousTiles.Count);
                                Add(next[successor.EdgeId], retained);
                                old[removed.EdgeId].ConstructionMana -= retained.ConstructionMana;
                                old[removed.EdgeId].RenovationMana -= retained.RenovationMana;
                                carriedPhysicalTiles[successor.EdgeId] = carried;
                            }
                        }
                    }
                }
                result.RefundBasis = old.Values.Where(r => !next.ContainsKey(r.StructureId))
                    .Sum(r => r.ConstructionMana + r.RenovationMana);
                result.Refund = Math.Floor(result.RefundBasis * config.RefundPercentage);
                if (!StructuralEconomySnapshot.Nonnegative(result.RefundBasis) ||
                    !StructuralEconomySnapshot.Nonnegative(result.Refund)) return result;
                if (operation == StructuralEditOperation.Deletion)
                {
                    result.ResultingMana = config.AddWithinCapacity(balance, result.Refund);
                    result.CreditedRefund = result.ResultingMana - balance;
                }
                else
                {
                    RoomSpatialInstance room;
                    double roomBase;
                    var corridorBases = new Dictionary<string, double>(StringComparer.Ordinal);
                    if (operation == StructuralEditOperation.Construction)
                    {
                        room = afterRooms.Single(r => !old.ContainsKey(r.RoomInstanceId));
                        if (!config.TryRoom(room.RoomDefinitionId, out roomBase)) return result;
                        foreach (var edge in afterEdges.Where(e => !old.ContainsKey(e.EdgeId) &&
                            e.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor).OrderBy(e => e.EdgeId, StringComparer.Ordinal))
                        {
                            if (!config.TryCorridor(edge.CorridorDefinitionId, out double perTile)) return result;
                            var newlyMaterialized = new HashSet<TileCoordinate>(
                                edge.Footprint?.OccupiedTiles ?? Array.Empty<TileCoordinate>());
                            if (carriedPhysicalTiles.TryGetValue(edge.EdgeId, out var carried))
                                newlyMaterialized.ExceptWith(carried);
                            corridorBases.Add(edge.EdgeId, perTile * newlyMaterialized.Count);
                        }
                        result.BaseCost = roomBase + corridorBases.Values.Sum();
                    }
                    else
                    {
                        room = beforeRooms.Single(r => r.RoomInstanceId == target);
                        if (!config.TryRoom(room.RoomDefinitionId, out roomBase)) return result;
                        if (operation == StructuralEditOperation.Movement) result.BaseCost = roomBase * config.MovementFactor;
                        else if (operation == StructuralEditOperation.Replacement)
                        {
                            var replacement = afterRooms.Single(r => r.RoomInstanceId == target);
                            if (!config.TryRoom(replacement.RoomDefinitionId, out double replacementBase)) return result;
                            result.BaseCost = roomBase * config.ReplacementFactor + Math.Max(0, replacementBase - roomBase);
                        }
                        else return result;
                    }
                    if (!StructuralEconomySnapshot.Nonnegative(result.BaseCost)) return result;
                    result.Cost = new FormulaEngine().Evaluate(new FormulaInput(result.BaseCost, modifiers)).Value;
                    if (!StructuralEconomySnapshot.Nonnegative(result.Cost)) return result;
                    result.ResultingMana = balance - result.Cost;
                    if (balance < result.Cost) { result.Reason = InsufficientReason; return result; }
                    // Reject balances too large to represent the exact configured charge in the existing double wallet.
                    if (balance - result.ResultingMana != result.Cost) return result;
                    if (operation == StructuralEditOperation.Construction)
                    {
                        double assigned = 0;
                        foreach (var pair in corridorBases)
                        {
                            double share = result.BaseCost == 0 ? 0 : Math.Floor(result.Cost * (pair.Value / result.BaseCost));
                            next[pair.Key].ConstructionMana += share; assigned += share;
                        }
                        next[room.RoomInstanceId].ConstructionMana += result.Cost - assigned;
                    }
                    else next[room.RoomInstanceId].RenovationMana += result.Cost;
                    double afterSpend = result.ResultingMana;
                    result.ResultingMana = config.AddWithinCapacity(afterSpend, result.Refund);
                    result.CreditedRefund = result.ResultingMana - afterSpend;
                }
                result.Investment = next.Values.OrderBy(r => r.StructureId, StringComparer.Ordinal).ToArray();
                if (!StructuralEconomySnapshot.Nonnegative(result.ResultingMana) ||
                    !StructuralInvestment.Valid(result.Investment, candidate, int.MaxValue)) return result;
                result.Reason = null; return result;
            }
            catch { return result; }
        }

        private static StructuralInvestmentRecord Share(StructuralInvestmentRecord source,
            int carriedTiles, int previousTiles) => new StructuralInvestmentRecord
        {
            StructureId = source.StructureId,
            ConstructionMana = Math.Floor(source.ConstructionMana * carriedTiles / previousTiles),
            RenovationMana = Math.Floor(source.RenovationMana * carriedTiles / previousTiles)
        };

        private static void Add(StructuralInvestmentRecord destination, StructuralInvestmentRecord source)
        {
            destination.ConstructionMana += source.ConstructionMana;
            destination.RenovationMana += source.RenovationMana;
        }

        internal static StructuralEditOperation Operation(DetachedCanonicalMutationRequest request) =>
            request.Kind == DetachedCanonicalMutationKind.StructuralConstruction ? StructuralEditOperation.Construction :
            request.Kind == DetachedCanonicalMutationKind.StructuralMovement ? StructuralEditOperation.Movement :
            request.Kind == DetachedCanonicalMutationKind.StructuralReplacement ? StructuralEditOperation.Replacement : StructuralEditOperation.Deletion;
        internal static bool IsStructural(DetachedCanonicalMutationRequest request) => request != null &&
            request.Kind >= DetachedCanonicalMutationKind.StructuralConstruction && request.Kind <= DetachedCanonicalMutationKind.StructuralDeletion;
        internal static string Target(DetachedCanonicalMutationRequest request) =>
            request.MovementIntent?.RoomInstanceId ?? request.ReplacementIntent?.RoomInstanceId ?? request.DeletionIntent?.TargetRoomInstanceId;
    }
}
