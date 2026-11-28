using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Economy
{
    public static partial class StructuralEconomyService
    {
        // The baseline ledger is the only source of paid value. Allocate unchanged
        // infrastructure, then reuse, then refund eligible explicitly retired residue.
        private static StructuralEconomyPreview PreviewFinalRenovation(DetachedCanonicalSpatialSaveState baseline,
            DetachedCanonicalSpatialSaveState final, StructuralInvestmentRecord[] investment, string[] roomMoves,
            string[] explicitRetirements, double balance, StructuralEconomySnapshot config,
            IReadOnlyList<FormulaModifier> modifiers)
        {
            var quote = new StructuralEconomyPreview
            {
                Operation = StructuralEditOperation.Construction,
                UsesNetSettlement = true, CurrentMana = balance, ResultingMana = balance, Reason = InvalidReason
            };
            if (baseline == null || final == null || config == null || roomMoves == null || explicitRetirements == null ||
                !StructuralEconomySnapshot.Nonnegative(balance) || !StructuralInvestment.Valid(investment, baseline, int.MaxValue))
                return quote;

            var old = investment.ToDictionary(record => record.StructureId, record => record.Copy(), StringComparer.Ordinal);
            var next = StructuralInvestment.Zero(final).ToDictionary(record => record.StructureId, StringComparer.Ordinal);
            var oldRooms = baseline.Floors.SelectMany(floor => floor.Layout.Rooms)
                .ToDictionary(room => room.RoomInstanceId, StringComparer.Ordinal);
            var finalRooms = final.Floors.SelectMany(floor => floor.Layout.Rooms)
                .ToDictionary(room => room.RoomInstanceId, StringComparer.Ordinal);
            var changedRoomIds = new HashSet<string>(roomMoves, StringComparer.Ordinal);
            var explicitRefunds = new HashSet<string>(explicitRetirements, StringComparer.Ordinal);
            var formula = new FormulaEngine();

            foreach (var room in oldRooms.Values)
            {
                if (!finalRooms.TryGetValue(room.RoomInstanceId, out var changed) ||
                    changed.FloorId != room.FloorId || changed.RoomDefinitionId != room.RoomDefinitionId)
                    return quote;
                var record = old[room.RoomInstanceId].Copy();
                quote.RetainedInvestment += record.ConstructionMana + record.RenovationMana;
                if (changedRoomIds.Contains(room.RoomInstanceId))
                {
                    if (!config.TryRoom(room.RoomDefinitionId, out double roomPrice)) return quote;
                    double movement = roomPrice * config.MovementFactor;
                    double cost = formula.Evaluate(new FormulaInput(movement, modifiers)).Value;
                    quote.BaseCost += movement;
                    quote.Cost += cost;
                    record.RenovationMana += cost;
                }
                next[room.RoomInstanceId] = record;
            }
            if (changedRoomIds.Any(id => !oldRooms.ContainsKey(id))) return quote;

            foreach (var room in finalRooms.Values.Where(room => !oldRooms.ContainsKey(room.RoomInstanceId)))
            {
                if (!config.TryRoom(room.RoomDefinitionId, out double roomPrice)) return quote;
                double cost = formula.Evaluate(new FormulaInput(roomPrice, modifiers)).Value;
                quote.BaseCost += roomPrice;
                quote.Cost += cost;
                next[room.RoomInstanceId].ConstructionMana = cost;
            }

            foreach (var currentFloor in baseline.Floors)
            {
                var finalFloor = final.Floors.SingleOrDefault(floor => floor.FloorInstanceId == currentFloor.FloorInstanceId);
                if (finalFloor == null) return quote;
                string shell = StructuralInvestment.ShellId(currentFloor.FloorInstanceId);
                if (old.TryGetValue(shell, out var shellRecord) && next.ContainsKey(shell))
                {
                    next[shell] = shellRecord.Copy();
                    quote.RetainedInvestment += shellRecord.ConstructionMana + shellRecord.RenovationMana;
                }

                if (!AllocateCorridorInvestment(currentFloor, finalFloor, old, next, explicitRefunds,
                    config, formula, modifiers, quote)) return quote;
            }

            quote.Refund = Math.Floor(quote.RefundBasis * config.RefundPercentage);
            // Uncredited wallet-cap refund is reported separately from this write-off.
            quote.ForfeitedInvestment += quote.RefundBasis - quote.Refund;
            quote.GrossCost = quote.Cost + quote.ReusedInvestment;
            var ledger = next.Values.OrderBy(record => record.StructureId, StringComparer.Ordinal).ToArray();
            if (!StructuralEconomySnapshot.Nonnegative(quote.GrossCost) ||
                !StructuralEconomySnapshot.Nonnegative(quote.RefundBasis) ||
                !StructuralEconomySnapshot.Nonnegative(quote.Refund) ||
                !StructuralEconomySnapshot.Nonnegative(quote.RetainedInvestment) ||
                !StructuralEconomySnapshot.Nonnegative(quote.ForfeitedInvestment) ||
                !StructuralInvestment.Valid(ledger, final, int.MaxValue)) return quote;

            quote.Investment = ledger;
            quote.Reason = null;
            RefreshRenovationBalance(quote, balance, config);
            return quote;
        }

        private static bool AllocateCorridorInvestment(SavedSpatialFloor before, SavedSpatialFloor after,
            Dictionary<string, StructuralInvestmentRecord> old, Dictionary<string, StructuralInvestmentRecord> next,
            HashSet<string> explicitlyRetired, StructuralEconomySnapshot config, FormulaEngine formula,
            IReadOnlyList<FormulaModifier> modifiers, StructuralEconomyPreview quote)
        {
            var replacement = after.Layout.Edges.Where(ReusableCorridor)
                .OrderBy(edge => edge.SourceNodeId, StringComparer.Ordinal)
                .ThenBy(edge => edge.Classification)
                .ThenBy(edge => edge.Classification == RouteClassification.Required ? edge.DestinationNodeId : string.Empty, StringComparer.Ordinal)
                .ThenBy(edge => edge.EdgeId, StringComparer.Ordinal).ToArray();
            var source = before.Layout.Edges.Where(ReusableCorridor).OrderBy(edge => edge.EdgeId, StringComparer.Ordinal).ToArray();
            var materialized = replacement.ToDictionary(edge => edge.EdgeId, edge =>
                new HashSet<TileCoordinate>(edge.CorridorTiles), StringComparer.Ordinal);
            var byId = after.Layout.Edges.ToDictionary(edge => edge.EdgeId, StringComparer.Ordinal);
            if (after.Layout.Edges.Any(edge => edge.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor &&
                !ReusableCorridor(edge) && !before.Layout.Edges.Any(prior => prior.EdgeId == edge.EdgeId &&
                    prior.CorridorDefinitionId == edge.CorridorDefinitionId && prior.CorridorTiles != null &&
                    prior.CorridorTiles.SequenceEqual(edge.CorridorTiles ?? Array.Empty<TileCoordinate>())))) return false;
            foreach (var edge in before.Layout.Edges.Where(edge => !ReusableCorridor(edge)))
            {
                var record = old[edge.EdgeId];
                if (byId.TryGetValue(edge.EdgeId, out var survived) && survived.ConnectionKind == edge.ConnectionKind &&
                    survived.CorridorDefinitionId == edge.CorridorDefinitionId && survived.SourceNodeId == edge.SourceNodeId &&
                    survived.DestinationNodeId == edge.DestinationNodeId)
                {
                    next[edge.EdgeId] = record.Copy();
                    quote.RetainedInvestment += record.ConstructionMana + record.RenovationMana;
                }
                else if (record.ConstructionMana != 0 || record.RenovationMana != 0) return false;
            }

            foreach (var edge in source)
            {
                var originalTiles = new HashSet<TileCoordinate>(edge.CorridorTiles);
                var record = old[edge.EdgeId];
                int retained = 0;
                double construction = 0, renovation = 0;
                foreach (var corridor in replacement)
                {
                    var newTiles = materialized[corridor.EdgeId];
                    int shared = newTiles.Count(originalTiles.Contains);
                    if (shared == 0) continue;
                    retained += shared;
                    var share = retained == originalTiles.Count ? record : Share(record, retained, originalTiles.Count);
                    next[corridor.EdgeId].ConstructionMana += share.ConstructionMana - construction;
                    next[corridor.EdgeId].RenovationMana += share.RenovationMana - renovation;
                    construction = share.ConstructionMana;
                    renovation = share.RenovationMana;
                    newTiles.ExceptWith(originalTiles);
                }
                record.ConstructionMana -= construction;
                record.RenovationMana -= renovation;
                quote.RetainedInvestment += construction + renovation;
            }

            foreach (var corridor in replacement)
            {
                int newTiles = materialized[corridor.EdgeId].Count;
                if (newTiles == 0) continue;
                if (!config.TryCorridor(corridor.CorridorDefinitionId, out double price)) return false;
                double baseCost = price * newTiles;
                double gross = formula.Evaluate(new FormulaInput(baseCost, modifiers)).Value;
                if (!StructuralEconomySnapshot.Nonnegative(gross)) return false;
                double remaining = gross;
                foreach (var edge in source)
                {
                    if (remaining == 0) break;
                    var available = old[edge.EdgeId];
                    var allocation = next[corridor.EdgeId];
                    double construction = Math.Min(remaining, available.ConstructionMana);
                    available.ConstructionMana -= construction;
                    allocation.ConstructionMana += construction;
                    remaining -= construction;
                    double renovation = Math.Min(remaining, available.RenovationMana);
                    available.RenovationMana -= renovation;
                    allocation.RenovationMana += renovation;
                    remaining -= renovation;
                }
                next[corridor.EdgeId].ConstructionMana += remaining;
                quote.ReusedInvestment += gross - remaining;
                quote.BaseCost += baseCost;
                quote.Cost += remaining;
            }

            // A replacement can recover the logical connection ID. Retained and
            // reused value were already deducted from each retired corridor's ledger.
            foreach (var edge in source)
            {
                double residual = old[edge.EdgeId].ConstructionMana + old[edge.EdgeId].RenovationMana;
                if (explicitlyRetired.Contains(edge.EdgeId)) quote.RefundBasis += residual;
                else quote.ForfeitedInvestment += residual;
            }
            return true;
        }

        private static bool ReusableCorridor(FloorRouteEdge edge) =>
            edge.ConnectionKind == FloorRouteConnectionKind.PhysicalCorridor &&
            edge.CorridorDefinitionId == OptionalBranchStructuralEditService.CorridorDefinitionId;

        private static void RefreshRenovationBalance(StructuralEconomyPreview quote, double balance,
            StructuralEconomySnapshot config)
        {
            quote.CurrentMana = balance;
            double afterCharges = balance - quote.Cost;
            double credited = Math.Min(quote.Refund, Math.Max(0, config.ManaCapacity - afterCharges));
            double final = afterCharges + credited;
            quote.CreditedRefund = credited;
            quote.ResultingMana = final;
            quote.Reason = !StructuralEconomySnapshot.Nonnegative(balance) ||
                !StructuralEconomySnapshot.Nonnegative(quote.Cost) || balance - afterCharges != quote.Cost ||
                final - afterCharges != credited ? InvalidReason : final < 0 ? InsufficientReason : null;
            if (quote.Reason == null && (quote.Cost > 0 || quote.RequiredReserveAfter > quote.RequiredReserveBefore) &&
                final < quote.RequiredReserveAfter) quote.Reason = MonsterRecoveryRules.Insufficient;
        }
    }
}
