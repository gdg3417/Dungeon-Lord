using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum CompatibilityProfileLifecycle { Active = 1, Retired = 2 }
    public enum CompatibilityRouteRole { Entrance = 1, BasicRoom0 = 2, BasicRoom1 = 3, Completion = 4 }

    [Serializable] public sealed class CompatibilityLayoutPlacement
    {
        public CompatibilityRouteRole Role; public TileCoordinate Anchor; public CardinalOrientation Orientation;
    }
    [Serializable] public sealed class CompatibilityLayoutConnection
    {
        public CompatibilityRouteRole SourceRole; public string SourceConnectionPointId;
        public CompatibilityRouteRole DestinationRole; public string DestinationConnectionPointId;
        public string SocketTypeId; public FloorRouteConnectionKind ConnectionKind; public string CorridorDefinitionId;
    }
    [Serializable] public sealed class CompatibilityLayoutVariant
    {
        public string LayoutId; public CompatibilityLayoutPlacement[] Placements = Array.Empty<CompatibilityLayoutPlacement>();
        public CompatibilityLayoutConnection[] Connections = Array.Empty<CompatibilityLayoutConnection>();
        public int ExpectedOccupiedTileTotal;
    }
    [Serializable] public sealed class CompatibilityLayoutGeometryRecord
    {
        public string GeometryId; public int GeometryVersion; public string CanonicalHash;
        public string FloorDefinitionId; public int FloorIndex;
        public string EntranceStructureDefinitionId; public string EntranceConnectionPointId;
        public string CompletionStructureDefinitionId; public string CompletionConnectionPointId;
        public string BasicRoomDefinitionId; public string BasicRoomSouthConnectionPointId;
        public string BasicRoomNorthConnectionPointId; public string SocketTypeId;
        public CompatibilityLayoutVariant[] Layouts = Array.Empty<CompatibilityLayoutVariant>();
    }
    [Serializable] public sealed class SpatialMigrationCompatibilityProfile
    {
        public string ProfileId; public int ProfileVersion; public CompatibilityProfileLifecycle Lifecycle;
        public int MinimumSourceSchemaVersion; public int MaximumSourceSchemaVersion; public int TargetSchemaVersion;
        public int TargetCanonicalLayoutContractVersion; public string GeometryId; public int GeometryVersion;
        public string GeometryCanonicalHash;
    }
    [Serializable] public sealed class CanonicalStarterLayoutProfile
    {
        public string ProfileId; public int ProfileVersion; public CompatibilityProfileLifecycle Lifecycle;
        public int TargetSchemaVersion; public int CanonicalLayoutContractVersion;
        public string GeometryId; public int GeometryVersion; public string GeometryCanonicalHash;
    }
    [Serializable] public sealed class CanonicalLayoutContractSelection
    {
        public int TargetSchemaVersion; public int CanonicalLayoutContractVersion;
        public CompatibilityProfileLifecycle Lifecycle;
    }
    [Serializable] public sealed class SpatialLayoutCompatibilityProfilesData
    {
        public string Schema; public int SchemaVersion;
        public CompatibilityLayoutGeometryRecord[] GeometryRecords = Array.Empty<CompatibilityLayoutGeometryRecord>();
        public SpatialMigrationCompatibilityProfile[] MigrationProfiles = Array.Empty<SpatialMigrationCompatibilityProfile>();
        public CanonicalStarterLayoutProfile[] StarterProfiles = Array.Empty<CanonicalStarterLayoutProfile>();
        public CanonicalLayoutContractSelection[] ContractSelections = Array.Empty<CanonicalLayoutContractSelection>();
    }

    public enum SpatialLayoutCompatibilityDiagnostic
    {
        None = 0, MissingInput = 1, EmptyInput = 2, InvalidEncoding = 3, InvalidJson = 4,
        InvalidSchema = 5, NoncanonicalInput = 6, WorkloadExceeded = 7, InvalidStableId = 8,
        InvalidVersion = 9, InvalidHash = 10, DuplicateGeometry = 11, MissingGeometry = 12,
        InvalidLifecycleSelection = 13, InvalidProductionReference = 14, InvalidGeometry = 15,
        UnauthorizedActiveProductionSelection = 16
    }

    public sealed class SpatialLayoutCompatibilityResult
    {
        internal SpatialLayoutCompatibilityResult(SpatialLayoutCompatibilitySnapshot value,
            IEnumerable<SpatialLayoutCompatibilityDiagnostic> diagnostics)
        { Value=value; Diagnostics=diagnostics.Distinct().OrderBy(x=>(int)x).ToArray(); }
        public bool Success => Value != null && Diagnostics.Length == 0;
        public SpatialLayoutCompatibilitySnapshot Value { get; }
        public SpatialLayoutCompatibilityDiagnostic[] Diagnostics { get; }
    }

    public sealed class SpatialLayoutCompatibilitySnapshot
    {
        private readonly byte[] bytes;
        internal SpatialLayoutCompatibilitySnapshot(SpatialLayoutCompatibilityProfilesData value)
        { bytes=SpatialLayoutCompatibilityProfiles.SerializeCanonical(value); }
        public SpatialLayoutCompatibilityProfilesData Value => JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(Encoding.UTF8.GetString(bytes));
        public byte[] CanonicalBytes => (byte[])bytes.Clone();
    }

    public static class SpatialLayoutCompatibilityProfiles
    {
        public const string ProductionPath="Assets/_Project/Data/Production/DungeonSpatial/spatial_layout_compatibility_profiles.json";
        private static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);

        public static SpatialLayoutCompatibilityResult ParseAndValidate(TextAsset asset,
            ProductionSpatialContentSnapshot spatial, SpatialContentValidationWorkloadLimits limits,
            Action<SpatialLayoutCompatibilityDiagnostic> sink=null, bool requireInactiveProduction=false)
        {
            if(asset==null) return Failure(SpatialLayoutCompatibilityDiagnostic.MissingInput,sink);
            return ParseAndValidate(asset.bytes,spatial,limits,sink,requireInactiveProduction);
        }

        public static SpatialLayoutCompatibilityResult ParseAndValidate(byte[] bytes,
            ProductionSpatialContentSnapshot spatial, SpatialContentValidationWorkloadLimits limits,
            Action<SpatialLayoutCompatibilityDiagnostic> sink=null, bool requireInactiveProduction=false)
        {
            var issues=new List<SpatialLayoutCompatibilityDiagnostic>();
            if(bytes==null){issues.Add(SpatialLayoutCompatibilityDiagnostic.MissingInput);return Finish(null,issues,sink);}
            if(bytes.Length==0){issues.Add(SpatialLayoutCompatibilityDiagnostic.EmptyInput);return Finish(null,issues,sink);}
            if(!limits.IsValid){issues.Add(SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded);return Finish(null,issues,sink);}
            if(bytes.Length>=3&&bytes[0]==0xef&&bytes[1]==0xbb&&bytes[2]==0xbf || bytes.Contains((byte)'\r') ||
                bytes[bytes.Length-1]!=(byte)'\n' || bytes.Length>1&&bytes[bytes.Length-2]==(byte)'\n')
            {issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidEncoding);return Finish(null,issues,sink);}
            try{Utf8.GetCharCount(bytes,0,bytes.Length-1);}catch{issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidEncoding);return Finish(null,issues,sink);}
            var strictIssues=new DiagnosticCollector(limits.MaximumIssues);
            var budget=new StrictJsonWorkloadBudget(limits);
            if(!StrictJson.TryParse(bytes,bytes.Length-1,typeof(SpatialLayoutCompatibilityProfilesData),strictIssues,budget,
                out JsonNode root,out ProductionSpatialGeneratedSetDiagnostic parseDiagnostic) || root.Kind!=JsonKind.Object)
            {issues.Add(parseDiagnostic==ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded||parseDiagnostic==ProductionSpatialGeneratedSetDiagnostic.DiagnosticLimitExceeded?SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded:SpatialLayoutCompatibilityDiagnostic.InvalidJson);return Finish(null,issues,sink);}
            StrictJson.Validate(typeof(SpatialLayoutCompatibilityProfilesData),root,strictIssues);
            if(strictIssues.HasAny){issues.Add(strictIssues.Diagnostics.Any(x=>x==ProductionSpatialGeneratedSetDiagnostic.WorkloadExceeded||x==ProductionSpatialGeneratedSetDiagnostic.DiagnosticLimitExceeded)?SpatialLayoutCompatibilityDiagnostic.WorkloadExceeded:SpatialLayoutCompatibilityDiagnostic.InvalidJson);return Finish(null,issues,sink);}
            SpatialLayoutCompatibilityProfilesData data;
            try{data=JsonUtility.FromJson<SpatialLayoutCompatibilityProfilesData>(StrictJson.ToCompactJson(root));}
            catch{issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidJson);return Finish(null,issues,sink);}
            if(data==null||data.Schema!="spatial_layout_compatibility_profiles"||data.SchemaVersion!=1)issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidSchema);
            SpatialLayoutCompatibilityProfilesData canonical=Canonicalize(data);
            if(!bytes.SequenceEqual(SerializeCanonical(canonical)))issues.Add(SpatialLayoutCompatibilityDiagnostic.NoncanonicalInput);
            Validate(canonical,spatial?.Catalog,limits,issues,requireInactiveProduction);
            return issues.Count==0?Finish(new SpatialLayoutCompatibilitySnapshot(canonical),issues,sink):Finish(null,issues,sink);
        }

        public static byte[] SerializeCanonical(SpatialLayoutCompatibilityProfilesData value)=>Utf8.GetBytes(JsonUtility.ToJson(value,true)+"\n");
        public static string ComputeGeometryHash(CompatibilityLayoutGeometryRecord geometry)
        {
            CompatibilityLayoutGeometryRecord copy=Clone(geometry); copy.CanonicalHash=string.Empty;
            using(var sha=SHA256.Create()) return string.Concat(sha.ComputeHash(Utf8.GetBytes(JsonUtility.ToJson(copy,false))).Select(x=>x.ToString("x2")));
        }
        public static SpatialLayoutCompatibilityProfilesData Canonicalize(SpatialLayoutCompatibilityProfilesData source)
        {
            var copy=Clone(source)??new SpatialLayoutCompatibilityProfilesData();
            copy.GeometryRecords=(copy.GeometryRecords??Array.Empty<CompatibilityLayoutGeometryRecord>()).OrderBy(x=>x?.GeometryId,StringComparer.Ordinal).ThenBy(x=>x?.GeometryVersion??0).ToArray();
            foreach(var geometry in copy.GeometryRecords.Where(x=>x!=null))
            {
                geometry.Layouts=(geometry.Layouts??Array.Empty<CompatibilityLayoutVariant>()).OrderBy(x=>x?.LayoutId,StringComparer.Ordinal).ToArray();
                foreach(var layout in geometry.Layouts.Where(x=>x!=null))
                {
                    layout.Placements=(layout.Placements??Array.Empty<CompatibilityLayoutPlacement>()).OrderBy(x=>x==null?0:(int)x.Role).ToArray();
                    layout.Connections=(layout.Connections??Array.Empty<CompatibilityLayoutConnection>()).OrderBy(x=>x==null?0:(int)x.SourceRole).ThenBy(x=>x==null?0:(int)x.DestinationRole).ToArray();
                }
            }
            copy.MigrationProfiles=(copy.MigrationProfiles??Array.Empty<SpatialMigrationCompatibilityProfile>()).OrderBy(x=>x?.ProfileId,StringComparer.Ordinal).ThenBy(x=>x?.ProfileVersion??0).ToArray();
            copy.StarterProfiles=(copy.StarterProfiles??Array.Empty<CanonicalStarterLayoutProfile>()).OrderBy(x=>x?.ProfileId,StringComparer.Ordinal).ThenBy(x=>x?.ProfileVersion??0).ToArray();
            copy.ContractSelections=(copy.ContractSelections??Array.Empty<CanonicalLayoutContractSelection>()).OrderBy(x=>x?.TargetSchemaVersion??0).ThenBy(x=>x?.CanonicalLayoutContractVersion??0).ToArray();
            return copy;
        }

        public static bool TrySelectMigration(SpatialLayoutCompatibilityProfilesData data,int rawSchema,out SpatialMigrationCompatibilityProfile profile)
        {var matches=(data?.MigrationProfiles??Array.Empty<SpatialMigrationCompatibilityProfile>()).Where(x=>x!=null&&x.Lifecycle==CompatibilityProfileLifecycle.Active&&rawSchema>=x.MinimumSourceSchemaVersion&&rawSchema<=x.MaximumSourceSchemaVersion).ToArray();profile=matches.Length==1?Clone(matches[0]):null;return profile!=null;}
        public static bool TrySelectStarter(SpatialLayoutCompatibilityProfilesData data,int target,int contract,out CanonicalStarterLayoutProfile profile)
        {var matches=(data?.StarterProfiles??Array.Empty<CanonicalStarterLayoutProfile>()).Where(x=>x!=null&&x.Lifecycle==CompatibilityProfileLifecycle.Active&&x.TargetSchemaVersion==target&&x.CanonicalLayoutContractVersion==contract).ToArray();profile=matches.Length==1?Clone(matches[0]):null;return profile!=null;}
        public static bool TrySelectContract(SpatialLayoutCompatibilityProfilesData data,int target,out CanonicalLayoutContractSelection selection)
        {var matches=(data?.ContractSelections??Array.Empty<CanonicalLayoutContractSelection>()).Where(x=>x!=null&&x.Lifecycle==CompatibilityProfileLifecycle.Active&&x.TargetSchemaVersion==target).ToArray();selection=matches.Length==1?Clone(matches[0]):null;return selection!=null;}
        public static bool TryRecoverMigration(SpatialLayoutCompatibilityProfilesData data,string id,int version,string hash,out SpatialMigrationCompatibilityProfile profile)
        {var matches=(data?.MigrationProfiles??Array.Empty<SpatialMigrationCompatibilityProfile>()).Where(x=>x!=null&&x.ProfileId==id&&x.ProfileVersion==version&&x.GeometryCanonicalHash==hash).Take(2).ToArray();profile=matches.Length==1?Clone(matches[0]):null;return profile!=null;}
        public static bool TryRecoverStarter(SpatialLayoutCompatibilityProfilesData data,string id,int version,string hash,out CanonicalStarterLayoutProfile profile)
        {var matches=(data?.StarterProfiles??Array.Empty<CanonicalStarterLayoutProfile>()).Where(x=>x!=null&&x.ProfileId==id&&x.ProfileVersion==version&&x.GeometryCanonicalHash==hash).Take(2).ToArray();profile=matches.Length==1?Clone(matches[0]):null;return profile!=null;}

        private static void Validate(SpatialLayoutCompatibilityProfilesData data,SpatialContentCatalog catalog,SpatialContentValidationWorkloadLimits limits,List<SpatialLayoutCompatibilityDiagnostic> issues,bool production)
        {
            var geometries=data.GeometryRecords??Array.Empty<CompatibilityLayoutGeometryRecord>();
            if(geometries.Any(x=>x==null)){issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidGeometry);return;}
            if(geometries.GroupBy(x=>x.GeometryId+"\0"+x.GeometryVersion,StringComparer.Ordinal).Any(g=>g.Count()!=1))issues.Add(SpatialLayoutCompatibilityDiagnostic.DuplicateGeometry);
            foreach(var g in geometries)
            {
                if(!Stable(g.GeometryId)||!Stable(g.FloorDefinitionId)||!Stable(g.EntranceStructureDefinitionId)||!Stable(g.CompletionStructureDefinitionId)||!Stable(g.BasicRoomDefinitionId)||!Stable(g.SocketTypeId)||!Stable(g.EntranceConnectionPointId)||!Stable(g.CompletionConnectionPointId)||!Stable(g.BasicRoomSouthConnectionPointId)||!Stable(g.BasicRoomNorthConnectionPointId))issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidStableId);
                if(g.GeometryVersion<=0)issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidVersion);
                if(!ValidHash(g.CanonicalHash)||g.CanonicalHash!=ComputeGeometryHash(g))issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidHash);
                ValidateGeometry(g,catalog,limits,issues);
            }
            foreach(var p in data.MigrationProfiles??Array.Empty<SpatialMigrationCompatibilityProfile>()) if(p==null||!Stable(p.ProfileId)||p.ProfileVersion<=0||p.MinimumSourceSchemaVersion<0||p.MaximumSourceSchemaVersion<p.MinimumSourceSchemaVersion||p.TargetSchemaVersion<=0||p.TargetCanonicalLayoutContractVersion<=0||!ReferenceExists(geometries,p?.GeometryId,p?.GeometryVersion,p?.GeometryCanonicalHash))issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidLifecycleSelection);
            foreach(var p in data.StarterProfiles??Array.Empty<CanonicalStarterLayoutProfile>()) if(p==null||!Stable(p.ProfileId)||p.ProfileVersion<=0||p.TargetSchemaVersion<=0||p.CanonicalLayoutContractVersion<=0||!ReferenceExists(geometries,p?.GeometryId,p?.GeometryVersion,p?.GeometryCanonicalHash))issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidLifecycleSelection);
            var activeM=(data.MigrationProfiles??Array.Empty<SpatialMigrationCompatibilityProfile>()).Where(x=>x?.Lifecycle==CompatibilityProfileLifecycle.Active).OrderBy(x=>x.MinimumSourceSchemaVersion).ToArray();
            for(int i=1;i<activeM.Length;i++)if(activeM[i].MinimumSourceSchemaVersion<=activeM[i-1].MaximumSourceSchemaVersion)issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidLifecycleSelection);
            if((data.StarterProfiles??Array.Empty<CanonicalStarterLayoutProfile>()).Where(x=>x?.Lifecycle==CompatibilityProfileLifecycle.Active).GroupBy(x=>x.TargetSchemaVersion+"\0"+x.CanonicalLayoutContractVersion).Any(g=>g.Count()!=1)|| (data.ContractSelections??Array.Empty<CanonicalLayoutContractSelection>()).Any(x=>x==null||x.TargetSchemaVersion<=0||x.CanonicalLayoutContractVersion<=0)|| (data.ContractSelections??Array.Empty<CanonicalLayoutContractSelection>()).Where(x=>x?.Lifecycle==CompatibilityProfileLifecycle.Active).GroupBy(x=>x.TargetSchemaVersion).Any(g=>g.Count()!=1))issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidLifecycleSelection);
            if(production&&((data.MigrationProfiles??Array.Empty<SpatialMigrationCompatibilityProfile>()).Any(x=>x?.Lifecycle==CompatibilityProfileLifecycle.Active)||(data.StarterProfiles??Array.Empty<CanonicalStarterLayoutProfile>()).Any(x=>x?.Lifecycle==CompatibilityProfileLifecycle.Active)||(data.ContractSelections??Array.Empty<CanonicalLayoutContractSelection>()).Any(x=>x?.Lifecycle==CompatibilityProfileLifecycle.Active)))issues.Add(SpatialLayoutCompatibilityDiagnostic.UnauthorizedActiveProductionSelection);
        }
        private static void ValidateGeometry(CompatibilityLayoutGeometryRecord g,SpatialContentCatalog c,SpatialContentValidationWorkloadLimits limits,List<SpatialLayoutCompatibilityDiagnostic> issues)
        {
            if(c==null){issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidProductionReference);return;}
            var floor=c.Floors?.SingleOrDefault(x=>x?.FloorDefinitionId==g.FloorDefinitionId);var room=c.Rooms?.SingleOrDefault(x=>x?.RoomDefinitionId==g.BasicRoomDefinitionId);var entrance=c.FixedStructures?.SingleOrDefault(x=>x?.StructureDefinitionId==g.EntranceStructureDefinitionId);var completion=c.FixedStructures?.SingleOrDefault(x=>x?.StructureDefinitionId==g.CompletionStructureDefinitionId);var socket=c.SocketTypes?.SingleOrDefault(x=>x?.SocketTypeId==g.SocketTypeId);
            if(floor==null||room==null||entrance==null||completion==null||socket==null||floor.FloorIndex!=g.FloorIndex||floor.EntranceStructureDefinitionId!=g.EntranceStructureDefinitionId||floor.CompletionStructureDefinitionId!=g.CompletionStructureDefinitionId||!floor.AllowedRoomDefinitionIds.Contains(g.BasicRoomDefinitionId)||!HasPoint(entrance,g.EntranceConnectionPointId,g.SocketTypeId)||!HasPoint(completion,g.CompletionConnectionPointId,g.SocketTypeId)||!HasPoint(room,g.BasicRoomSouthConnectionPointId,g.SocketTypeId)||!HasPoint(room,g.BasicRoomNorthConnectionPointId,g.SocketTypeId)){issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidProductionReference);return;}
            foreach(var layout in g.Layouts??Array.Empty<CompatibilityLayoutVariant>())
            {
                if(layout==null||!Stable(layout.LayoutId)||layout.ExpectedOccupiedTileTotal<=0){issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidGeometry);continue;}
                var occupied=new HashSet<TileCoordinate>(); long total=0; bool bad=false;
                foreach(var p in layout.Placements??Array.Empty<CompatibilityLayoutPlacement>())
                {
                    if(p==null||p.Orientation!=CardinalOrientation.Zero){bad=true;continue;} RectangularFootprintDefinition footprint=p.Role==CompatibilityRouteRole.Entrance?entrance.GrossFootprint:p.Role==CompatibilityRouteRole.Completion?completion.GrossFootprint:room.GrossFootprint;
                    if(!TileFootprintResolver.TryResolveRectangle(footprint,p.Anchor,p.Orientation,new SpatialValidationWorkloadLimits(limits.MaximumMaterializedTiles),out ResolvedTileFootprint resolved)){bad=true;continue;}
                    foreach(var tile in resolved.OccupiedTiles){total++;if(!floor.Bounds.Contains(tile)||!occupied.Add(tile))bad=true;}
                }
                foreach(var edge in layout.Connections??Array.Empty<CompatibilityLayoutConnection>())
                {if(edge==null||edge.ConnectionKind!=FloorRouteConnectionKind.DirectDoorway||edge.CorridorDefinitionId!=""||edge.SocketTypeId!=g.SocketTypeId||!Adjacent(g,layout,edge,room,entrance,completion))bad=true;}
                if(total!=layout.ExpectedOccupiedTileTotal||total>floor.FinalFloorSpaceCapacity||total>limits.MaximumMaterializedTiles)bad=true;
                if(bad)issues.Add(SpatialLayoutCompatibilityDiagnostic.InvalidGeometry);
            }
        }
        private static bool Adjacent(CompatibilityLayoutGeometryRecord g,CompatibilityLayoutVariant l,CompatibilityLayoutConnection e,RoomSpatialDefinition room,FixedSpatialStructureDefinition entrance,FixedSpatialStructureDefinition completion)
        {var aa=(l.Placements??Array.Empty<CompatibilityLayoutPlacement>()).Where(x=>x!=null&&x.Role==e.SourceRole).Take(2).ToArray();var bb=(l.Placements??Array.Empty<CompatibilityLayoutPlacement>()).Where(x=>x!=null&&x.Role==e.DestinationRole).Take(2).ToArray();if(aa.Length!=1||bb.Length!=1)return false;var a=aa[0];var b=bb[0];var ap=Point(e.SourceRole,e.SourceConnectionPointId,room,entrance,completion);var bp=Point(e.DestinationRole,e.DestinationConnectionPointId,room,entrance,completion);if(ap==null||bp==null||ap.SocketTypeId!=e.SocketTypeId||bp.SocketTypeId!=e.SocketTypeId)return false;var ac=new TileCoordinate(a.Anchor.X+ap.Offset.X,a.Anchor.Y+ap.Offset.Y);var bc=new TileCoordinate(b.Anchor.X+bp.Offset.X,b.Anchor.Y+bp.Offset.Y);return Math.Abs(ac.X-bc.X)+Math.Abs(ac.Y-bc.Y)==1&&((int)ap.Facing+2)%4==(int)bp.Facing;}
        private static SpatialConnectionPointDefinition Point(CompatibilityRouteRole role,string id,RoomSpatialDefinition room,FixedSpatialStructureDefinition entrance,FixedSpatialStructureDefinition completion)=> (role==CompatibilityRouteRole.Entrance?entrance.ConnectionPoints:role==CompatibilityRouteRole.Completion?completion.ConnectionPoints:room.ConnectionPoints).Where(x=>x!=null&&x.ConnectionPointId==id).Take(2).SingleOrDefault();
        private static bool HasPoint(FixedSpatialStructureDefinition owner,string id,string socket)=>owner.ConnectionPoints.Count(x=>x?.ConnectionPointId==id&&x.SocketTypeId==socket)==1;
        private static bool HasPoint(RoomSpatialDefinition owner,string id,string socket)=>owner.ConnectionPoints.Count(x=>x?.ConnectionPointId==id&&x.SocketTypeId==socket)==1;
        private static bool ReferenceExists(IEnumerable<CompatibilityLayoutGeometryRecord> gs,string id,int? version,string hash)=>gs.Count(x=>x.GeometryId==id&&x.GeometryVersion==version&&x.CanonicalHash==hash)==1;
        private static bool Stable(string s)=>!string.IsNullOrEmpty(s)&&s.All(c=>c>='a'&&c<='z'||c>='0'&&c<='9'||c=='.'||c=='_'||c=='-');
        private static bool ValidHash(string s)=>s!=null&&s.Length==64&&s.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f');
        private static T Clone<T>(T value) where T:class=>value==null?null:JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        private static SpatialLayoutCompatibilityResult Failure(SpatialLayoutCompatibilityDiagnostic issue,Action<SpatialLayoutCompatibilityDiagnostic> sink)=>Finish(null,new[]{issue},sink);
        private static SpatialLayoutCompatibilityResult Finish(SpatialLayoutCompatibilitySnapshot value,IEnumerable<SpatialLayoutCompatibilityDiagnostic> issues,Action<SpatialLayoutCompatibilityDiagnostic> sink){var result=new SpatialLayoutCompatibilityResult(value,issues);foreach(var issue in result.Diagnostics)sink?.Invoke(issue);return result;}
    }
}
