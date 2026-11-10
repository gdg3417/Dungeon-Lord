using System;
using System.Collections.Generic;
using System.Linq;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;
using DungeonBuilder.M0.Gameplay.MvpDungeonPlacements;

namespace DungeonBuilder.M0.Gameplay.RunSimulation
{
    internal readonly struct CombatC0RoomBounds
    {
        internal readonly string FloorId, RoomId;
        internal readonly int Width, Height;
        internal CombatC0RoomBounds(string floor, string room, int width, int height)
        { FloorId = floor; RoomId = room; Width = width; Height = height; }
    }
    internal sealed class CombatC0Group
    {
        internal string[] Assignments, Participating;
        internal CombatC0AttackLink[] Links;
        internal Dictionary<string, TileCoordinate> Positions;
        internal Dictionary<string, TileCoordinate[]> Movement;
    }

    // Read-only adapter over canonical room-local tiles and captured gross boundaries.
    // Production has no authored interior opacity. Only explicitly supplied synthetic
    // solid cells are opaque; occupied/reserved/nontraversable cells are NOT inferred walls.
    internal sealed class CombatC0Geometry
    {
        private readonly CombatC0Kernel kernel;
        private readonly CombatC0RoomBounds bounds;
        private readonly TileCoordinate[] tiles;
        private readonly Dictionary<TileCoordinate, int> index = new Dictionary<TileCoordinate, int>();
        private readonly HashSet<TileCoordinate> opaque;
        private readonly int[][] neighbours;
        private readonly int[] queue, distance, parent;

        internal CombatC0Geometry(IntraroomSnapshot spatial, CombatC0RoomBounds bounds, CombatC0Kernel kernel,
            TileCoordinate[] confirmedSyntheticSolidCells = null)
        {
            this.kernel = kernel; this.bounds = bounds;
            if (spatial == null || spatial.TraversableTiles == null || bounds.FloorId != spatial.FloorInstanceId ||
                bounds.RoomId != spatial.RoomInstanceId || bounds.Width <= 0 || bounds.Height <= 0 ||
                (long)bounds.Width * bounds.Height > spatial.MaximumMaterializedTiles ||
                spatial.TraversableTiles.Length > spatial.MaximumMaterializedTiles) throw new ArgumentException(CombatC0Kernel.InvalidState);
            tiles = (TileCoordinate[])spatial.TraversableTiles.Clone();
            Array.Sort(tiles, Comparer<TileCoordinate>.Create((a, b) => { Charge(); return a.CompareTo(b); }));
            opaque = new HashSet<TileCoordinate>();
            if (confirmedSyntheticSolidCells != null)
            {
                if (confirmedSyntheticSolidCells.Length > spatial.MaximumMaterializedTiles) throw new ArgumentException(CombatC0Kernel.Workload);
                foreach (var cell in confirmedSyntheticSolidCells)
                { Charge(); if (!Inside(cell) || !opaque.Add(cell)) throw new ArgumentException(CombatC0Kernel.InvalidState); }
            }
            for (int i = 0; i < tiles.Length; i++)
            {
                Charge();
                if (!Inside(tiles[i]) || opaque.Contains(tiles[i]) || index.ContainsKey(tiles[i])) throw new ArgumentException(CombatC0Kernel.InvalidState);
                index.Add(tiles[i], i);
            }
            neighbours = new int[tiles.Length][];
            for (int i = 0; i < tiles.Length; i++)
            {
                var list = new List<int>();
                foreach (var cell in Adjacent(tiles[i])) { Charge(); if (index.TryGetValue(cell, out int next)) list.Add(next); }
                neighbours[i] = list.ToArray();
            }
            queue = new int[tiles.Length]; distance = new int[tiles.Length]; parent = new int[tiles.Length];
        }
        private void Charge() => kernel.ChargeGeometry(1);
        private bool Inside(TileCoordinate t) => t.X >= 0 && t.Y >= 0 && t.X < bounds.Width && t.Y < bounds.Height;
        private static TileCoordinate[] Adjacent(TileCoordinate t) => new[] {
            new TileCoordinate(checked(t.X - 1), t.Y), new TileCoordinate(t.X, checked(t.Y - 1)),
            new TileCoordinate(t.X, checked(t.Y + 1)), new TileCoordinate(checked(t.X + 1), t.Y) };

        internal CombatC0Group Collect(TileCoordinate anchor, RunRoomAssignment[] assignments,
            IReadOnlyDictionary<string, TileCoordinate> current, ISet<string> resolved)
        {
            if (!index.ContainsKey(anchor)) throw new ArgumentException(CombatC0Kernel.InvalidState);
            kernel.ChargeGeometry(checked((long)assignments.Length * assignments.Length + current.Count +
                (long)kernel.Party.Members.Count * kernel.Party.Members.Count));
            var candidates = assignments.Where(a => !resolved.Contains(a.AssignmentId) && kernel.MonsterHealth(a.AssignmentId) > 0)
                .OrderBy(a => a.Sequence).ThenBy(a => a.AssignmentId, StringComparer.Ordinal).ToArray();
            var positions = current.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
            var moves = new Dictionary<string, TileCoordinate[]>(StringComparer.Ordinal);
            var melee = new HashSet<string>(StringComparer.Ordinal);
            foreach (var a in candidates)
            {
                Charge(); var profile = kernel.MonsterProfile(a.AssignmentId);
                var start = positions[a.AssignmentId];
                if (profile.AttackType == CombatC0Config.Melee || start.Equals(anchor))
                {
                    var blocked = new HashSet<TileCoordinate>();
                    foreach (var other in positions)
                    { Charge(); if (other.Key != a.AssignmentId && kernel.MonsterHealth(other.Key) > 0) blocked.Add(other.Value); }
                    if (TryEngagement(start, anchor, blocked, out var path))
                    {
                        positions[a.AssignmentId] = path[path.Length - 1];
                        moves[a.AssignmentId] = path;
                        if (profile.AttackType == CombatC0Config.Melee) melee.Add(a.AssignmentId);
                    }
                }
            }
            var links = new List<CombatC0AttackLink>(); var participating = new HashSet<string>(StringComparer.Ordinal);
            foreach (var a in candidates)
            {
                var monster = kernel.MonsterProfile(a.AssignmentId); var position = positions[a.AssignmentId];
                foreach (var member in kernel.Party.Members.OrderBy(m => m.MemberOrdinal))
                {
                    Charge(); var adventurer = kernel.MemberProfile(member.ClassId);
                    var incoming = Eligibility(adventurer, anchor, position, position.Equals(anchor) ? false : true);
                    var outgoing = Eligibility(monster, position, anchor, monster.AttackType != CombatC0Config.Melee || melee.Contains(a.AssignmentId));
                    if (member.IsActive && (incoming == CombatC0Reason.Legal || outgoing == CombatC0Reason.Legal)) participating.Add(a.AssignmentId);
                    links.Add(new CombatC0AttackLink(CombatC0Kernel.MemberId(kernel.Party, member.MemberOrdinal),
                        CombatC0Kernel.MonsterId(kernel.Party, a.AssignmentId), anchor, position, incoming));
                    links.Add(new CombatC0AttackLink(CombatC0Kernel.MonsterId(kernel.Party, a.AssignmentId),
                        CombatC0Kernel.MemberId(kernel.Party, member.MemberOrdinal), position, anchor, outgoing));
                }
            }
            return new CombatC0Group { Assignments = candidates.Select(a => a.AssignmentId).ToArray(),
                Participating = candidates.Where(a => participating.Contains(a.AssignmentId)).Select(a => a.AssignmentId).ToArray(),
                Links = links.ToArray(), Positions = positions, Movement = moves };
        }
        private bool TryEngagement(TileCoordinate start, TileCoordinate anchor, HashSet<TileCoordinate> blocked, out TileCoordinate[] path)
        {
            path = Array.Empty<TileCoordinate>();
            for (int i = 0; i < tiles.Length; i++) { Charge(); distance[i] = -1; parent[i] = -1; }
            if (!index.TryGetValue(start, out int first)) throw new ArgumentException(CombatC0Kernel.InvalidState);
            int head = 0, tail = 0; queue[tail++] = first; distance[first] = 0;
            while (head < tail)
            {
                int vertex = queue[head++]; Charge();
                foreach (int next in neighbours[vertex])
                {
                    Charge();
                    if (distance[next] >= 0 || tiles[next].Equals(anchor) || blocked.Contains(tiles[next])) continue;
                    distance[next] = distance[vertex] + 1; parent[next] = vertex; queue[tail++] = next;
                }
            }
            int last = -1, best = int.MaxValue;
            foreach (var cell in Adjacent(anchor))
            {
                Charge();
                if (!index.TryGetValue(cell, out int target) || blocked.Contains(cell) || distance[target] < 0 || distance[target] >= best) continue;
                best = distance[target]; last = target;
            }
            if (last < 0) return false;
            path = new TileCoordinate[best + 1];
            for (int i = path.Length - 1; i >= 0; i--) { Charge(); path[i] = tiles[last]; last = parent[last]; }
            return true;
        }
        internal CombatC0Reason Eligibility(CombatC0AttackProfile profile, TileCoordinate source, TileCoordinate target, bool positioned = true)
        {
            Charge();
            if (!Inside(source) || !Inside(target)) throw new ArgumentException(CombatC0Kernel.InvalidState);
            long range = Math.Abs((long)source.X - target.X) + Math.Abs((long)source.Y - target.Y);
            if (range == 0) return CombatC0Reason.PartyAnchorConflict;
            if (profile.AttackType == CombatC0Config.Melee && (!positioned || range != 1)) return CombatC0Reason.MeleePositionUnavailable;
            if (!profile.Range.HasValue || range > profile.Range.Value) return CombatC0Reason.OutOfRange;
            return profile.AttackType == CombatC0Config.Ranged && !LineOfSight(source, target) ? CombatC0Reason.LineOfSightBlocked : CombatC0Reason.Legal;
        }
        private bool Solid(TileCoordinate tile) { Charge(); return !Inside(tile) || opaque.Contains(tile); }
        internal bool LineOfSight(TileCoordinate source, TileCoordinate target)
        {
            if (Solid(source) || Solid(target)) return false;
            long dx = Math.Abs((long)target.X - source.X), dy = Math.Abs((long)target.Y - source.Y);
            int sx = Math.Sign(target.X - source.X), sy = Math.Sign(target.Y - source.Y);
            long ix = 0, iy = 0; int x = source.X, y = source.Y;
            while (ix < dx || iy < dy)
            {
                Charge(); long crossX = checked((1 + 2 * ix) * dy), crossY = checked((1 + 2 * iy) * dx);
                if (crossX == crossY)
                {
                    if (Solid(new TileCoordinate(x + sx, y)) || Solid(new TileCoordinate(x, y + sy))) return false;
                    x += sx; y += sy; ix++; iy++;
                }
                else if (crossX < crossY) { x += sx; ix++; }
                else { y += sy; iy++; }
                if (Solid(new TileCoordinate(x, y))) return false;
            }
            return true;
        }
    }
}
