using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum SpatialContentValidationReason
    {
        CatalogMissing = 1, WorkloadLimitsInvalid = 2, WorkloadExceeded = 3,
        MetadataMissing = 4, SchemaIdentityMissing = 5, SchemaIdentityMalformed = 6,
        SchemaVersionNonpositive = 7, ContentVersionMissing = 8, DefinitionMissing = 9,
        StableIdMissing = 10, DuplicateStableId = 11, DuplicateFloorIndex = 12,
        UnknownEnumValue = 13, FootprintMissing = 14, FootprintDimensionsInvalid = 15,
        FootprintTileCountExceeded = 16, ReservedTileDuplicate = 17, ReservedTileOutsideFootprint = 18,
        OrientationDuplicate = 19, OrientationInvalid = 20, CapacityNegative = 21,
        MaximumConnectionsNegative = 22, ConnectionPointIdDuplicate = 23,
        ConnectionPointOffsetInvalid = 24, ConnectionPointBoundaryInvalid = 25,
        ConnectionPointFacingInvalid = 26, ForeignKeyMissing = 27, ForeignKeyAmbiguous = 28,
        CorridorLengthInvalid = 29, CorridorWidthInvalid = 30, FixedStructureKindInvalid = 31,
        FixedStructureCapacityInvalid = 32, DiagnosticLimitExceeded = 33
    }

    [Serializable]
    public sealed class SpatialContentValidationIssue
    {
        public SpatialContentValidationReason Reason;
        public string Path;
        public string RelatedId;
    }

    public sealed class SpatialContentValidationResult
    {
        public SpatialContentValidationResult(SpatialContentValidationIssue[] issues) { Issues = issues; }
        public SpatialContentValidationIssue[] Issues { get; }
        public bool IsValid => Issues.Length == 0;
    }

    public static class SpatialContentValidator
    {
        public static SpatialContentValidationResult Validate(SpatialContentCatalog catalog,
            SpatialContentValidationWorkloadLimits limits, ISet<string> localizationKeys = null)
        {
            var issues = new List<SpatialContentValidationIssue>();
            if (!limits.IsValid) return Result(SpatialContentValidationReason.WorkloadLimitsInvalid);
            if (catalog == null) return Result(SpatialContentValidationReason.CatalogMissing);
            long top = Length(catalog.Floors) + Length(catalog.Rooms) + Length(catalog.Corridors) +
                Length(catalog.FixedStructures) + Length(catalog.SocketTypes);
            if (top > limits.MaximumTopLevelRecords) return Result(SpatialContentValidationReason.WorkloadExceeded);
            long nested = CountNested(catalog);
            if (nested > limits.MaximumNestedRecords) return Result(SpatialContentValidationReason.WorkloadExceeded);
            // Conservative upper bound prevents diagnostic construction from exceeding the caller's budget.
            if (4L + top * 16L + nested * 6L > limits.MaximumIssues)
                return Result(SpatialContentValidationReason.DiagnosticLimitExceeded);
            // Validate a detached canonical copy so row permutations cannot affect paths or issue order.
            if (!SpatialContentCanonicalizer.TryCanonicalize(catalog, limits, out catalog))
                return Result(SpatialContentValidationReason.WorkloadExceeded);

            AddMetadata(catalog.Metadata, issues);
            var sockets = Index(catalog.SocketTypes, x => x?.SocketTypeId, "socketTypes", issues);
            var rooms = Index(catalog.Rooms, x => x?.RoomDefinitionId, "rooms", issues);
            var corridors = Index(catalog.Corridors, x => x?.CorridorDefinitionId, "corridors", issues);
            var structures = Index(catalog.FixedStructures, x => x?.StructureDefinitionId, "fixedStructures", issues);
            ValidateFloors(catalog.Floors, rooms, corridors, structures, catalog.FixedStructures, limits, issues);
            ForEach(catalog.Rooms, (x, p) => ValidateRoom(x, p, sockets, limits, localizationKeys, issues), "rooms");
            ForEach(catalog.Corridors, (x, p) => ValidateCorridor(x, p, sockets, localizationKeys, issues), "corridors");
            ForEach(catalog.FixedStructures, (x, p) => ValidateFixed(x, p, sockets, limits, localizationKeys, issues), "fixedStructures");
            ForEach(catalog.SocketTypes, (x, p) => ValidateSocket(x, p, sockets, issues), "socketTypes");
            return new SpatialContentValidationResult(issues.OrderBy(x => (int)x.Reason).ThenBy(x => x.Path, StringComparer.Ordinal)
                .ThenBy(x => x.RelatedId, StringComparer.Ordinal).ToArray());
        }

        private static void AddMetadata(SpatialContentExportMetadata m, List<SpatialContentValidationIssue> issues)
        {
            if (m == null) { Add(issues, SpatialContentValidationReason.MetadataMissing, "metadata"); return; }
            if (string.IsNullOrWhiteSpace(m.SchemaId)) Add(issues, SpatialContentValidationReason.SchemaIdentityMissing, "metadata.schemaId");
            else if (m.SchemaId.Any(char.IsWhiteSpace)) Add(issues, SpatialContentValidationReason.SchemaIdentityMalformed, "metadata.schemaId");
            if (m.SchemaVersion <= 0) Add(issues, SpatialContentValidationReason.SchemaVersionNonpositive, "metadata.schemaVersion");
            if (string.IsNullOrWhiteSpace(m.ContentVersion)) Add(issues, SpatialContentValidationReason.ContentVersionMissing, "metadata.contentVersion");
        }

        private static void ValidateFloors(FloorSpatialConfiguration[] values, Dictionary<string,int> rooms,
            Dictionary<string,int> corridors, Dictionary<string,int> structures,
            FixedSpatialStructureDefinition[] fixedDefinitions, SpatialContentValidationWorkloadLimits limits,
            List<SpatialContentValidationIssue> issues)
        {
            var indexes = new HashSet<int>();
            ForEach(values, (x,p) => { if (x == null) { Add(issues, SpatialContentValidationReason.DefinitionMissing,p); return; }
                if (!indexes.Add(x.FloorIndex)) Add(issues, SpatialContentValidationReason.DuplicateFloorIndex,p);
                if (x.Bounds == null) Add(issues, SpatialContentValidationReason.FootprintMissing,p+".bounds");
                else if (!x.Bounds.IsValid) Add(issues, SpatialContentValidationReason.FootprintDimensionsInvalid,p+".bounds");
                else if (x.Bounds.TileCount > limits.MaximumMaterializedTiles) Add(issues, SpatialContentValidationReason.FootprintTileCountExceeded,p+".bounds");
                Refs(x.AllowedRoomDefinitionIds, rooms,p+".allowedRooms",issues); Refs(x.AllowedCorridorDefinitionIds,corridors,p+".allowedCorridors",issues);
                Ref(x.EntranceStructureDefinitionId,structures,p+".entrance",issues); Ref(x.CompletionStructureDefinitionId,structures,p+".completion",issues);
                RequireFixedKind(x.EntranceStructureDefinitionId, FixedSpatialStructureKind.Entrance, fixedDefinitions, p+".entrance", issues);
                RequireFixedKind(x.CompletionStructureDefinitionId, FixedSpatialStructureKind.CompletionTerminal, fixedDefinitions, p+".completion", issues);
            }, "floors");
            Index(values, x => x?.FloorDefinitionId, "floors", issues);
        }

        private static void RequireFixedKind(string id, FixedSpatialStructureKind kind,
            FixedSpatialStructureDefinition[] values, string path, List<SpatialContentValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(id) || values == null) return;
            FixedSpatialStructureDefinition match = values.FirstOrDefault(x => x != null && string.Equals(x.StructureDefinitionId, id, StringComparison.Ordinal));
            if (match != null && match.Kind != kind) Add(issues, SpatialContentValidationReason.FixedStructureKindInvalid, path, id);
        }

        private static void ValidateRoom(RoomSpatialDefinition x,string p,Dictionary<string,int> sockets,
            SpatialContentValidationWorkloadLimits limits,ISet<string> loc,List<SpatialContentValidationIssue> issues)
        { if (x == null) return; ValidateShape(x.GrossFootprint,x.ReservedTileOffsets,x.AllowedOrientations,x.ConnectionPoints,p,sockets,limits,issues);
          Capacities(x.MonsterCapacity,x.TrapCapacity,x.LootCapacity,x.MaximumConnectionCount,p,issues); Localization(x.LocalizationKey,p,loc,issues); }

        private static void ValidateCorridor(CorridorSpatialDefinition x,string p,Dictionary<string,int> sockets,ISet<string> loc,List<SpatialContentValidationIssue> issues)
        { if (x == null) return; if (!Enum.IsDefined(typeof(CorridorSpatialCategory),x.Category)) Add(issues,SpatialContentValidationReason.UnknownEnumValue,p+".category");
          if (x.MinimumLength <= 0 || x.MaximumLength <= 0 || x.MinimumLength > x.MaximumLength) Add(issues,SpatialContentValidationReason.CorridorLengthInvalid,p+".length");
          if (x.Width <= 0) Add(issues,SpatialContentValidationReason.CorridorWidthInvalid,p+".width"); Orientations(x.AllowedOrientations,p,issues);
          Capacities(x.MonsterCapacity,x.TrapCapacity,x.LootCapacity,0,p,issues); Refs(x.CompatibleSocketTypeIds,sockets,p+".compatibleSockets",issues); Localization(x.LocalizationKey,p,loc,issues); }

        private static void ValidateFixed(FixedSpatialStructureDefinition x,string p,Dictionary<string,int> sockets,
            SpatialContentValidationWorkloadLimits limits,ISet<string> loc,List<SpatialContentValidationIssue> issues)
        { if (x == null) return; if (!Enum.IsDefined(typeof(FixedSpatialStructureKind),x.Kind)) Add(issues,SpatialContentValidationReason.FixedStructureKindInvalid,p+".kind");
          ValidateShape(x.GrossFootprint,x.ReservedTileOffsets,x.AllowedOrientations,x.ConnectionPoints,p,sockets,limits,issues);
          if (x.MaximumConnectionCount < 0) Add(issues,SpatialContentValidationReason.MaximumConnectionsNegative,p+".maximumConnections"); Localization(x.LocalizationKey,p,loc,issues); }

        private static void ValidateShape(RectangularFootprintDefinition f,TileCoordinate[] reserved,CardinalOrientation[] orientations,
            SpatialConnectionPointDefinition[] points,string p,Dictionary<string,int> sockets,SpatialContentValidationWorkloadLimits limits,List<SpatialContentValidationIssue> issues)
        { if (f == null) { Add(issues,SpatialContentValidationReason.FootprintMissing,p+".footprint"); return; }
          if (f.Width <= 0 || f.Height <= 0) Add(issues,SpatialContentValidationReason.FootprintDimensionsInvalid,p+".footprint");
          long area=(long)f.Width*f.Height; if (area > limits.MaximumMaterializedTiles) Add(issues,SpatialContentValidationReason.FootprintTileCountExceeded,p+".footprint");
          var seen=new HashSet<TileCoordinate>(); ForEach(reserved,(v,rp)=>{if(!seen.Add(v))Add(issues,SpatialContentValidationReason.ReservedTileDuplicate,rp); if(!Inside(v,f))Add(issues,SpatialContentValidationReason.ReservedTileOutsideFootprint,rp);},p+".reserved");
          Orientations(orientations,p,issues); var ids=new HashSet<string>(StringComparer.Ordinal); ForEach(points,(v,cp)=>{if(v==null){Add(issues,SpatialContentValidationReason.DefinitionMissing,cp);return;}
            if(string.IsNullOrWhiteSpace(v.ConnectionPointId))Add(issues,SpatialContentValidationReason.StableIdMissing,cp); else if(!ids.Add(v.ConnectionPointId))Add(issues,SpatialContentValidationReason.ConnectionPointIdDuplicate,cp,v.ConnectionPointId);
            if(!Inside(v.Offset,f))Add(issues,SpatialContentValidationReason.ConnectionPointOffsetInvalid,cp); else if(!Boundary(v.Offset,f))Add(issues,SpatialContentValidationReason.ConnectionPointBoundaryInvalid,cp);
            if(!Enum.IsDefined(typeof(CardinalOrientation),v.Facing))Add(issues,SpatialContentValidationReason.OrientationInvalid,cp); else if(Inside(v.Offset,f)&&Boundary(v.Offset,f)&&!Facing(v.Offset,f,v.Facing))Add(issues,SpatialContentValidationReason.ConnectionPointFacingInvalid,cp);
            Ref(v.SocketTypeId,sockets,cp+".socket",issues);},p+".connectionPoints"); }

        private static void ValidateSocket(SpatialSocketTypeDefinition x,string p,Dictionary<string,int> sockets,List<SpatialContentValidationIssue> issues)
        { if(x!=null) Refs(x.CompatibleSocketTypeIds,sockets,p+".compatible",issues); }
        private static void Capacities(int m,int t,int l,int max,string p,List<SpatialContentValidationIssue> issues)
        { if(m<0||t<0||l<0)Add(issues,SpatialContentValidationReason.CapacityNegative,p+".capacities"); if(max<0)Add(issues,SpatialContentValidationReason.MaximumConnectionsNegative,p+".maximumConnections"); }
        private static void Orientations(CardinalOrientation[] values,string p,List<SpatialContentValidationIssue> issues)
        { var seen=new HashSet<CardinalOrientation>(); ForEach(values,(v,vp)=>{if(!Enum.IsDefined(typeof(CardinalOrientation),v))Add(issues,SpatialContentValidationReason.OrientationInvalid,vp);else if(!seen.Add(v))Add(issues,SpatialContentValidationReason.OrientationDuplicate,vp);},p+".orientations"); }
        private static bool Inside(TileCoordinate v,RectangularFootprintDefinition f)=>v.X>=0&&v.Y>=0&&v.X<f.Width&&v.Y<f.Height;
        private static bool Boundary(TileCoordinate v,RectangularFootprintDefinition f)=>v.X==0||v.Y==0||v.X==f.Width-1||v.Y==f.Height-1;
        private static bool Facing(TileCoordinate v,RectangularFootprintDefinition f,CardinalOrientation o)=>
            (o==CardinalOrientation.Zero&&v.Y==f.Height-1)||(o==CardinalOrientation.Ninety&&v.X==f.Width-1)||(o==CardinalOrientation.OneEighty&&v.Y==0)||(o==CardinalOrientation.TwoSeventy&&v.X==0);
        private static void Localization(string key,string p,ISet<string> loc,List<SpatialContentValidationIssue> issues){if(loc!=null)Ref(key,loc.ToDictionary(x=>x,x=>1,StringComparer.Ordinal),p+".localization",issues);}
        private static void Refs(string[] refs,Dictionary<string,int> index,string p,List<SpatialContentValidationIssue> issues)=>ForEach(refs,(v,vp)=>Ref(v,index,vp,issues),p);
        private static void Ref(string id,Dictionary<string,int> index,string p,List<SpatialContentValidationIssue> issues){if(string.IsNullOrWhiteSpace(id)||!index.TryGetValue(id,out int count))Add(issues,SpatialContentValidationReason.ForeignKeyMissing,p,id);else if(count>1)Add(issues,SpatialContentValidationReason.ForeignKeyAmbiguous,p,id);}
        private static Dictionary<string,int> Index<T>(T[] values,Func<T,string> id,string p,List<SpatialContentValidationIssue> issues){var d=new Dictionary<string,int>(StringComparer.Ordinal);ForEach(values,(v,vp)=>{if(v==null){Add(issues,SpatialContentValidationReason.DefinitionMissing,vp);return;}string key=id(v);if(string.IsNullOrWhiteSpace(key)){Add(issues,SpatialContentValidationReason.StableIdMissing,vp);return;}d.TryGetValue(key,out int n);d[key]=n+1;if(n>0)Add(issues,SpatialContentValidationReason.DuplicateStableId,vp,key);},p);return d;}
        private static long CountNested(SpatialContentCatalog c){long n=0;ForEach(c.Rooms,(x,p)=>{if(x!=null)n+=Length(x.ReservedTileOffsets)+Length(x.AllowedOrientations)+Length(x.ConnectionPoints);},"");ForEach(c.FixedStructures,(x,p)=>{if(x!=null)n+=Length(x.ReservedTileOffsets)+Length(x.AllowedOrientations)+Length(x.ConnectionPoints);},"");ForEach(c.Corridors,(x,p)=>{if(x!=null)n+=Length(x.AllowedOrientations)+Length(x.CompatibleSocketTypeIds);},"");ForEach(c.SocketTypes,(x,p)=>{if(x!=null)n+=Length(x.CompatibleSocketTypeIds);},"");ForEach(c.Floors,(x,p)=>{if(x!=null)n+=Length(x.AllowedRoomDefinitionIds)+Length(x.AllowedCorridorDefinitionIds);},"");return n;}
        private static long Length(Array a)=>a?.LongLength??0;
        private static void ForEach<T>(T[] values,Action<T,string> action,string p){if(values==null)return;for(int i=0;i<values.Length;i++)action(values[i],p+"["+i+"]");}
        private static void Add(List<SpatialContentValidationIssue> x,SpatialContentValidationReason r,string p,string id="")=>x.Add(new SpatialContentValidationIssue{Reason=r,Path=p,RelatedId=id??string.Empty});
        private static SpatialContentValidationResult Result(SpatialContentValidationReason r)=>new SpatialContentValidationResult(new[]{new SpatialContentValidationIssue{Reason=r,Path=string.Empty,RelatedId=string.Empty}});
    }

    public static class SpatialContentCanonicalizer
    {
        public static bool TryCanonicalize(SpatialContentCatalog source, SpatialContentValidationWorkloadLimits limits, out SpatialContentCatalog result)
        {
            result=null; if(source==null||!limits.IsValid) return false;
            long top=(source.Floors?.LongLength??0)+(source.Rooms?.LongLength??0)+(source.Corridors?.LongLength??0)+(source.FixedStructures?.LongLength??0)+(source.SocketTypes?.LongLength??0);
            if(top>limits.MaximumTopLevelRecords) return false;
            long nested=0;
            AddNested(source.Rooms,x=>x==null?0:(x.ReservedTileOffsets?.LongLength??0)+(x.AllowedOrientations?.LongLength??0)+(x.ConnectionPoints?.LongLength??0),ref nested);
            AddNested(source.FixedStructures,x=>x==null?0:(x.ReservedTileOffsets?.LongLength??0)+(x.AllowedOrientations?.LongLength??0)+(x.ConnectionPoints?.LongLength??0),ref nested);
            AddNested(source.Corridors,x=>x==null?0:(x.AllowedOrientations?.LongLength??0)+(x.CompatibleSocketTypeIds?.LongLength??0),ref nested);
            AddNested(source.SocketTypes,x=>x==null?0:(x.CompatibleSocketTypeIds?.LongLength??0),ref nested);
            AddNested(source.Floors,x=>x==null?0:(x.AllowedRoomDefinitionIds?.LongLength??0)+(x.AllowedCorridorDefinitionIds?.LongLength??0),ref nested);
            if(nested>limits.MaximumNestedRecords) return false;
            // Unity JSON provides a compact detached copy while retaining nulls, duplicates, and unknown enum values.
            result=UnityEngine.JsonUtility.FromJson<SpatialContentCatalog>(UnityEngine.JsonUtility.ToJson(source));
            Sort(result.Floors,x=>x?.FloorDefinitionId); Sort(result.Rooms,x=>x?.RoomDefinitionId); Sort(result.Corridors,x=>x?.CorridorDefinitionId); Sort(result.FixedStructures,x=>x?.StructureDefinitionId); Sort(result.SocketTypes,x=>x?.SocketTypeId);
            ForEach(result.Floors,x=>{SortStrings(x.AllowedRoomDefinitionIds);SortStrings(x.AllowedCorridorDefinitionIds);});
            ForEach(result.Rooms,x=>Shape(x.ReservedTileOffsets,x.AllowedOrientations,x.ConnectionPoints)); ForEach(result.FixedStructures,x=>Shape(x.ReservedTileOffsets,x.AllowedOrientations,x.ConnectionPoints));
            ForEach(result.Corridors,x=>{if(x.AllowedOrientations!=null)Array.Sort(x.AllowedOrientations);SortStrings(x.CompatibleSocketTypeIds);}); ForEach(result.SocketTypes,x=>SortStrings(x.CompatibleSocketTypeIds)); return true;
        }
        private static void Shape(TileCoordinate[] t,CardinalOrientation[] o,SpatialConnectionPointDefinition[] p){if(t!=null)Array.Sort(t);if(o!=null)Array.Sort(o);if(p!=null)Array.Sort(p,(a,b)=>StringComparer.Ordinal.Compare(a?.ConnectionPointId,b?.ConnectionPointId));}
        private static void Sort<T>(T[] a,Func<T,string> id){if(a!=null)Array.Sort(a,(x,y)=>StringComparer.Ordinal.Compare(id(x),id(y)));}
        private static void SortStrings(string[] a){if(a!=null)Array.Sort(a,StringComparer.Ordinal);}
        private static void ForEach<T>(T[] a,Action<T> f){if(a==null)return;foreach(T x in a)if(x!=null)f(x);}
        private static void AddNested<T>(T[] a,Func<T,long> count,ref long total){if(a==null)return;foreach(T x in a)total+=count(x);}
    }
}
