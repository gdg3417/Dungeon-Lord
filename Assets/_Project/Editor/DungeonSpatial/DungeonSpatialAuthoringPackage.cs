#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DungeonBuilder.M0.Gameplay.DungeonSpatial;

namespace DungeonBuilder.M0.Editor.DungeonSpatial
{
    public enum DungeonSpatialAuthoringDiagnostic
    {
        None = 0, MissingSource = 1, MissingManifest = 2, MissingSchema = 3, MissingTable = 4,
        UnexpectedFile = 5, EmptyFile = 6, BomPresent = 7, InvalidLineEnding = 8,
        InvalidTrailingNewline = 9, MalformedJson = 10, InvalidJsonRoot = 11,
        DuplicateJsonField = 12, AmbiguousJsonField = 13, UnknownJsonField = 14,
        MissingRequiredJsonField = 15, UnsupportedAuthoringSchema = 16,
        InvalidOrDuplicateTablePath = 17, ManifestSchemaTableMismatch = 18, MalformedCsv = 19,
        HeaderMismatch = 20, MissingColumn = 21, UnknownColumn = 22, InvalidFieldCount = 23,
        BlankRequiredValue = 24, InvalidInt32 = 25, Int32Overflow = 26, InvalidEnumToken = 27,
        DuplicatePrimaryKey = 28, DuplicateUniqueKey = 29, MissingForeignKey = 30,
        DuplicateAuthority = 31, NoncanonicalCommittedRowOrder = 32,
        ProjectedCatalogInvalid = 33, ProjectedCatalogWorkloadExceeded = 34
    }

    public sealed class DungeonSpatialAuthoringIssue
    {
        public DungeonSpatialAuthoringDiagnostic Diagnostic { get; internal set; }
        public string RelativePath { get; internal set; }
        public string TableId { get; internal set; }
        public string RecordKey { get; internal set; }
        public string Column { get; internal set; }
        public override string ToString() => Diagnostic + ":" + RelativePath + ":" + TableId + ":" + RecordKey + ":" + Column;
    }

    public sealed class DungeonSpatialAuthoringSource
    {
        private readonly SortedDictionary<string, byte[]> files;
        public DungeonSpatialAuthoringSource(IEnumerable<KeyValuePair<string, byte[]>> sourceFiles)
        {
            files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
            if (sourceFiles == null) return;
            foreach (var pair in sourceFiles)
                files[pair.Key] = pair.Value == null ? null : (byte[])pair.Value.Clone();
        }
        public IReadOnlyDictionary<string, byte[]> Snapshot() => files.ToDictionary(p => p.Key,
            p => p.Value == null ? null : (byte[])p.Value.Clone(), StringComparer.Ordinal);
        internal bool TryGet(string path, out byte[] bytes)
        {
            if (!files.TryGetValue(path, out byte[] stored)) { bytes = null; return false; }
            bytes = stored == null ? null : (byte[])stored.Clone(); return true;
        }
        internal string[] Paths => files.Keys.ToArray();
    }

    public static class DungeonSpatialAuthoringRepository
    {
        public const string PackageRoot = "ContentAuthoring/DungeonSpatial";
        public static DungeonSpatialAuthoringSource Read(string root = PackageRoot)
        {
            if (!Directory.Exists(root)) return null;
            return new DungeonSpatialAuthoringSource(Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => new KeyValuePair<string, byte[]>(path.Substring(root.Length).TrimStart('/', '\\').Replace('\\', '/'), File.ReadAllBytes(path))));
        }
    }

    public sealed class DungeonSpatialAuthoringProjection
    {
        public SpatialContentCatalog Catalog { get; internal set; }
        public StringTable English { get; internal set; }
    }

    public sealed class DungeonSpatialAuthoringResult
    {
        internal DungeonSpatialAuthoringResult(DungeonSpatialAuthoringProjection projection, IEnumerable<DungeonSpatialAuthoringIssue> issues)
        {
            Projection = projection;
            Issues = issues.OrderBy(i => (int)i.Diagnostic).ThenBy(i => i.RelativePath, StringComparer.Ordinal)
                .ThenBy(i => i.TableId, StringComparer.Ordinal).ThenBy(i => i.RecordKey, StringComparer.Ordinal)
                .ThenBy(i => i.Column, StringComparer.Ordinal).ToArray();
        }
        public bool Success => Projection != null && Issues.Length == 0;
        public DungeonSpatialAuthoringProjection Projection { get; }
        public DungeonSpatialAuthoringIssue[] Issues { get; }
    }

    public static class DungeonSpatialAuthoringPackageParser
    {
        private const string ManifestPath = "authoring_manifest.json", SchemaPath = "authoring_schema.json";
        private static readonly string[] ManifestFields = { "schema", "schemaVersion", "contentVersion", "catalogSchemaId", "catalogSchemaVersion", "stringTableSchemaId", "stringTableSchemaVersion", "requiredLanguage", "tables" };
        private static readonly Table[] Tables =
        {
            T("floors", "FloorDefinitionId","FloorIndex","MinimumX","MinimumY","Width","Height","FinalFloorSpaceCapacity","OptionalBranchAllowance","EntranceStructureDefinitionId","CompletionStructureDefinitionId"),
            T("floor_allowed_rooms","FloorDefinitionId","RoomDefinitionId"), T("floor_allowed_corridors","FloorDefinitionId","CorridorDefinitionId"),
            T("rooms","RoomDefinitionId","Width","Height","MaximumConnectionCount","MonsterCapacity","TrapCapacity","LootCapacity","LocalizationKey"),
            T("room_orientations","RoomDefinitionId","Orientation"), T("room_reserved_offsets","RoomDefinitionId","OffsetX","OffsetY"),
            T("room_connection_points","RoomDefinitionId","ConnectionPointId","OffsetX","OffsetY","Facing","SocketTypeId"),
            T("corridors","CorridorDefinitionId","LocalizationKey","Category","MinimumLength","MaximumLength","Width","MonsterCapacity","TrapCapacity","LootCapacity"),
            T("corridor_orientations","CorridorDefinitionId","Orientation"), T("corridor_compatible_sockets","CorridorDefinitionId","SocketTypeId"),
            T("fixed_structures","StructureDefinitionId","LocalizationKey","Kind","Width","Height","MaximumConnectionCount"),
            T("fixed_structure_orientations","StructureDefinitionId","Orientation"), T("fixed_structure_reserved_offsets","StructureDefinitionId","OffsetX","OffsetY"),
            T("fixed_structure_connection_points","StructureDefinitionId","ConnectionPointId","OffsetX","OffsetY","Facing","SocketTypeId"),
            T("socket_types","SocketTypeId"), T("socket_compatibility","SocketTypeId","CompatibleSocketTypeId"), T("localization_en","Key","Text")
        };
        private static Table T(string id, params string[] columns) => new Table { Id=id, Path="tables/"+id+".csv", Columns=columns };

        public static DungeonSpatialAuthoringResult ParseAndProject(DungeonSpatialAuthoringSource source, SpatialContentValidationWorkloadLimits limits, bool requireCanonicalRows = false)
        {
            var issues = new List<DungeonSpatialAuthoringIssue>();
            if (source == null) { Add(issues, DungeonSpatialAuthoringDiagnostic.MissingSource); return Result(issues); }
            if (!ReadText(source, ManifestPath, DungeonSpatialAuthoringDiagnostic.MissingManifest, issues, out string manifestText)) return Result(issues);
            if (!StrictJson.TryParse(manifestText, out object manifestValue, out var jsonError)) { Add(issues, jsonError, ManifestPath); return Result(issues); }
            var manifest = manifestValue as Dictionary<string, object>;
            if (manifest == null) { Add(issues, DungeonSpatialAuthoringDiagnostic.InvalidJsonRoot, ManifestPath); return Result(issues); }
            ValidateFields(manifest, ManifestFields, ManifestPath, issues);
            string schema = S(manifest,"schema"), contentVersion=S(manifest,"contentVersion"), catalogId=S(manifest,"catalogSchemaId"), stringId=S(manifest,"stringTableSchemaId"), language=S(manifest,"requiredLanguage");
            int schemaVersion=I(manifest,"schemaVersion"), catalogVersion=I(manifest,"catalogSchemaVersion"), stringVersion=I(manifest,"stringTableSchemaVersion");
            if (schema!="dungeon_spatial_authoring" || schemaVersion!=1) Add(issues,DungeonSpatialAuthoringDiagnostic.UnsupportedAuthoringSchema,ManifestPath);
            if (contentVersion!="0.1.0" || catalogId!="dungeon_spatial_content" || catalogVersion!=1 || stringId!="string_table" || stringVersion!=1 || language!="en") Add(issues,DungeonSpatialAuthoringDiagnostic.DuplicateAuthority,ManifestPath);
            var listed = manifest.ContainsKey("tables") ? manifest["tables"] as List<object> : null;
            string[] expected=Tables.Select(t=>t.Path).ToArray();
            if (listed==null || listed.Any(v=>!(v is string)) || !listed.Cast<string>().SequenceEqual(expected, StringComparer.Ordinal) || listed.Cast<object>().Distinct().Count()!=listed.Count)
                Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidOrDuplicateTablePath,ManifestPath);
            if (!ReadText(source, SchemaPath, DungeonSpatialAuthoringDiagnostic.MissingSchema, issues, out string schemaText)) return Result(issues);
            if (!StrictJson.TryParse(schemaText,out object schemaValue,out jsonError)) Add(issues,jsonError,SchemaPath);
            else if (!ValidateSchema(schemaValue,issues)) Add(issues,DungeonSpatialAuthoringDiagnostic.ManifestSchemaTableMismatch,SchemaPath);
            var approved = new HashSet<string>(expected.Concat(new[]{ManifestPath,SchemaPath,"README.md"}),StringComparer.Ordinal);
            foreach(string path in source.Paths.Where(p=>!approved.Contains(p))) Add(issues,DungeonSpatialAuthoringDiagnostic.UnexpectedFile,path);
            var rows=new Dictionary<string,List<string[]>>(StringComparer.Ordinal);
            foreach(Table table in Tables)
            {
                if (!ReadText(source,table.Path,DungeonSpatialAuthoringDiagnostic.MissingTable,issues,out string csv)) continue;
                if (!Csv.TryParse(csv,out List<string[]> parsed)) { Add(issues,DungeonSpatialAuthoringDiagnostic.MalformedCsv,table.Path,table.Id); continue; }
                if (parsed.Count==0 || !parsed[0].SequenceEqual(table.Columns,StringComparer.Ordinal)) { DiagnoseHeader(parsed.Count==0?Array.Empty<string>():parsed[0],table,issues); continue; }
                var body=parsed.Skip(1).ToList();
                foreach(var row in body) if(row.Length!=table.Columns.Length) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidFieldCount,table.Path,table.Id);
                if(body.Any(r=>r.Length==table.Columns.Length && r.Any(string.IsNullOrEmpty))) Add(issues,DungeonSpatialAuthoringDiagnostic.BlankRequiredValue,table.Path,table.Id);
                rows[table.Id]=body.Where(r=>r.Length==table.Columns.Length).ToList();
                if(requireCanonicalRows && !Keys(body).SequenceEqual(Keys(body).OrderBy(x=>x,StringComparer.Ordinal))) Add(issues,DungeonSpatialAuthoringDiagnostic.NoncanonicalCommittedRowOrder,table.Path,table.Id);
            }
            if(issues.Count>0) return Result(issues);
            ValidateRows(rows,issues);
            if(issues.Count>0) return Result(issues);
            DungeonSpatialAuthoringProjection projection=Project(manifest,rows);
            var keys=new HashSet<string>(projection.English.entries.Select(e=>e.key),StringComparer.Ordinal);
            var validation=SpatialContentValidator.Validate(projection.Catalog,limits,keys);
            if(!validation.IsValid) Add(issues, validation.Issues.Any(i=>i.Reason==SpatialContentValidationReason.WorkloadExceeded)?DungeonSpatialAuthoringDiagnostic.ProjectedCatalogWorkloadExceeded:DungeonSpatialAuthoringDiagnostic.ProjectedCatalogInvalid);
            if(!SpatialContentCanonicalizer.TryCanonicalize(projection.Catalog,limits,out SpatialContentCatalog canonical)) Add(issues,DungeonSpatialAuthoringDiagnostic.ProjectedCatalogWorkloadExceeded);
            if(issues.Count>0) return Result(issues);
            projection.Catalog=canonical;
            projection.English.entries=projection.English.entries.OrderBy(e=>e.key,StringComparer.Ordinal).ToArray();
            return new DungeonSpatialAuthoringResult(projection,issues);
        }

        private static bool ValidateSchema(object value,List<DungeonSpatialAuthoringIssue> issues)
        {
            var root=value as Dictionary<string,object>; if(root==null) return false;
            if(root.ContainsKey("schema")||root.ContainsKey("schemaVersion")) return false;
            if(!root.TryGetValue("tables",out object tv) || !(tv is List<object> list) || list.Count!=Tables.Length) return false;
            for(int i=0;i<Tables.Length;i++)
            {
                var t=list[i] as Dictionary<string,object>; if(t==null || S(t,"id")!=Tables[i].Id || S(t,"path")!=Tables[i].Path) return false;
                if(!t.TryGetValue("columns",out object cv)||!(cv is List<object> cols)||cols.Count!=Tables[i].Columns.Length) return false;
                for(int c=0;c<cols.Count;c++) if(!(cols[c] is Dictionary<string,object> col)||S(col,"name")!=Tables[i].Columns[c]) return false;
            }
            return true;
        }

        private static void ValidateRows(Dictionary<string,List<string[]>> r,List<DungeonSpatialAuthoringIssue> issues)
        {
            foreach(var pair in r)
            {
                var seen=new HashSet<string>(StringComparer.Ordinal); foreach(var row in pair.Value)
                { string key=Key(pair.Key,row); if(!seen.Add(key)) Add(issues,DungeonSpatialAuthoringDiagnostic.DuplicatePrimaryKey,"tables/"+pair.Key+".csv",pair.Key,key); }
            }
            if(r["floors"].GroupBy(x=>x[1],StringComparer.Ordinal).Any(g=>g.Count()>1)) Add(issues,DungeonSpatialAuthoringDiagnostic.DuplicateUniqueKey,Tables[0].Path,"floors");
            foreach(var pair in r) foreach(var row in pair.Value) for(int c=0;c<row.Length;c++)
            {
                string col=Tables.First(t=>t.Id==pair.Key).Columns[c], value=row[c];
                if(value!=value.Trim()) Add(issues,DungeonSpatialAuthoringDiagnostic.BlankRequiredValue,"tables/"+pair.Key+".csv",pair.Key,Key(pair.Key,row),col);
                if(IsInt(col) && !TryInt(value,out _,out bool overflow)) Add(issues,overflow?DungeonSpatialAuthoringDiagnostic.Int32Overflow:DungeonSpatialAuthoringDiagnostic.InvalidInt32,"tables/"+pair.Key+".csv",pair.Key,Key(pair.Key,row),col);
                if((col=="Orientation"||col=="Facing")&&!Enum.GetNames(typeof(CardinalOrientation)).Contains(value)) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidEnumToken,"tables/"+pair.Key+".csv",pair.Key,Key(pair.Key,row),col);
                if(col=="Category"&&value!="Straight") Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidEnumToken,"tables/"+pair.Key+".csv",pair.Key,Key(pair.Key,row),col);
                if(col=="Kind"&&value!="Entrance"&&value!="CompletionTerminal") Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidEnumToken,"tables/"+pair.Key+".csv",pair.Key,Key(pair.Key,row),col);
            }
            var floors=Set(r,"floors",0); var rooms=Set(r,"rooms",0); var corridors=Set(r,"corridors",0); var fixeds=Set(r,"fixed_structures",0); var sockets=Set(r,"socket_types",0); var loc=Set(r,"localization_en",0);
            FK(r,"floor_allowed_rooms",0,floors,issues); FK(r,"floor_allowed_rooms",1,rooms,issues); FK(r,"floor_allowed_corridors",0,floors,issues); FK(r,"floor_allowed_corridors",1,corridors,issues);
            foreach(string t in new[]{"room_orientations","room_reserved_offsets","room_connection_points"}) FK(r,t,0,rooms,issues);
            foreach(string t in new[]{"corridor_orientations","corridor_compatible_sockets"}) FK(r,t,0,corridors,issues);
            foreach(string t in new[]{"fixed_structure_orientations","fixed_structure_reserved_offsets","fixed_structure_connection_points"}) FK(r,t,0,fixeds,issues);
            foreach(string t in new[]{"room_connection_points","corridor_compatible_sockets","fixed_structure_connection_points"}) FK(r,t,r[t][0].Length-1,sockets,issues);
            FK(r,"socket_compatibility",0,sockets,issues); FK(r,"socket_compatibility",1,sockets,issues);
            FK(r,"rooms",7,loc,issues); FK(r,"corridors",1,loc,issues); FK(r,"fixed_structures",1,loc,issues);
        }

        private static DungeonSpatialAuthoringProjection Project(Dictionary<string,object> m,Dictionary<string,List<string[]>> r)
        {
            string[] Child(string table,string owner,int value)=>(r[table].Where(x=>x[0]==owner).Select(x=>x[value]).OrderBy(x=>x,StringComparer.Ordinal).ToArray());
            TileCoordinate[] Offsets(string table,string owner)=>r[table].Where(x=>x[0]==owner).Select(x=>new TileCoordinate(P(x[1]),P(x[2]))).OrderBy(x=>x).ToArray();
            SpatialConnectionPointDefinition[] Points(string table,string owner)=>r[table].Where(x=>x[0]==owner).OrderBy(x=>x[1],StringComparer.Ordinal).Select(x=>new SpatialConnectionPointDefinition{ConnectionPointId=x[1],Offset=new TileCoordinate(P(x[2]),P(x[3])),Facing=E<CardinalOrientation>(x[4]),SocketTypeId=x[5]}).ToArray();
            var catalog=new SpatialContentCatalog { Metadata=new SpatialContentExportMetadata{SchemaId=S(m,"catalogSchemaId"),SchemaVersion=I(m,"catalogSchemaVersion"),ContentVersion=S(m,"contentVersion")},
                Floors=r["floors"].Select(x=>new FloorSpatialConfiguration{FloorDefinitionId=x[0],FloorIndex=P(x[1]),Bounds=new RectangularFloorBounds(new TileCoordinate(P(x[2]),P(x[3])),P(x[4]),P(x[5])),FinalFloorSpaceCapacity=P(x[6]),OptionalBranchAllowance=P(x[7]),EntranceStructureDefinitionId=x[8],CompletionStructureDefinitionId=x[9],AllowedRoomDefinitionIds=Child("floor_allowed_rooms",x[0],1),AllowedCorridorDefinitionIds=Child("floor_allowed_corridors",x[0],1)}).ToArray(),
                Rooms=r["rooms"].Select(x=>new RoomSpatialDefinition{RoomDefinitionId=x[0],GrossFootprint=new RectangularFootprintDefinition(P(x[1]),P(x[2])),MaximumConnectionCount=P(x[3]),MonsterCapacity=P(x[4]),TrapCapacity=P(x[5]),LootCapacity=P(x[6]),LocalizationKey=x[7],AllowedOrientations=Child("room_orientations",x[0],1).Select(E<CardinalOrientation>).ToArray(),ReservedTileOffsets=Offsets("room_reserved_offsets",x[0]),ConnectionPoints=Points("room_connection_points",x[0])}).ToArray(),
                Corridors=r["corridors"].Select(x=>new CorridorSpatialDefinition{CorridorDefinitionId=x[0],LocalizationKey=x[1],Category=E<CorridorSpatialCategory>(x[2]),MinimumLength=P(x[3]),MaximumLength=P(x[4]),Width=P(x[5]),MonsterCapacity=P(x[6]),TrapCapacity=P(x[7]),LootCapacity=P(x[8]),AllowedOrientations=Child("corridor_orientations",x[0],1).Select(E<CardinalOrientation>).ToArray(),CompatibleSocketTypeIds=Child("corridor_compatible_sockets",x[0],1)}).ToArray(),
                FixedStructures=r["fixed_structures"].Select(x=>new FixedSpatialStructureDefinition{StructureDefinitionId=x[0],LocalizationKey=x[1],Kind=E<FixedSpatialStructureKind>(x[2]),GrossFootprint=new RectangularFootprintDefinition(P(x[3]),P(x[4])),MaximumConnectionCount=P(x[5]),AllowedOrientations=Child("fixed_structure_orientations",x[0],1).Select(E<CardinalOrientation>).ToArray(),ReservedTileOffsets=Offsets("fixed_structure_reserved_offsets",x[0]),ConnectionPoints=Points("fixed_structure_connection_points",x[0])}).ToArray(),
                SocketTypes=r["socket_types"].Select(x=>new SpatialSocketTypeDefinition{SocketTypeId=x[0],CompatibleSocketTypeIds=Child("socket_compatibility",x[0],1)}).ToArray() };
            return new DungeonSpatialAuthoringProjection{Catalog=catalog,English=new StringTable{schema=S(m,"stringTableSchemaId"),schemaVersion=I(m,"stringTableSchemaVersion"),language=S(m,"requiredLanguage"),entries=r["localization_en"].Select(x=>new StringEntry{key=x[0],text=x[1]}).OrderBy(x=>x.key,StringComparer.Ordinal).ToArray()}};
        }

        private static bool ReadText(DungeonSpatialAuthoringSource s,string path,DungeonSpatialAuthoringDiagnostic missing,List<DungeonSpatialAuthoringIssue> issues,out string text)
        {
            text=null; if(!s.TryGet(path,out byte[] bytes)||bytes==null){Add(issues,missing,path);return false;} if(bytes.Length==0){Add(issues,DungeonSpatialAuthoringDiagnostic.EmptyFile,path);return false;}
            if(bytes.Length>=3&&bytes[0]==0xef&&bytes[1]==0xbb&&bytes[2]==0xbf){Add(issues,DungeonSpatialAuthoringDiagnostic.BomPresent,path);return false;}
            if(bytes.Contains((byte)'\r')){Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidLineEnding,path);return false;}
            if(bytes[bytes.Length-1]!=(byte)'\n'||(bytes.Length>1&&bytes[bytes.Length-2]==(byte)'\n')){Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidTrailingNewline,path);return false;}
            try{text=new UTF8Encoding(false,true).GetString(bytes);return true;}catch(DecoderFallbackException){Add(issues,DungeonSpatialAuthoringDiagnostic.MalformedCsv,path);return false;}
        }
        private static void ValidateFields(Dictionary<string,object> o,string[] allowed,string path,List<DungeonSpatialAuthoringIssue> issues){foreach(string k in o.Keys)if(!allowed.Contains(k))Add(issues,allowed.Any(a=>string.Equals(a,k,StringComparison.OrdinalIgnoreCase))?DungeonSpatialAuthoringDiagnostic.AmbiguousJsonField:DungeonSpatialAuthoringDiagnostic.UnknownJsonField,path,column:k);foreach(string k in allowed)if(!o.ContainsKey(k))Add(issues,DungeonSpatialAuthoringDiagnostic.MissingRequiredJsonField,path,column:k);}
        private static void DiagnoseHeader(string[] h,Table t,List<DungeonSpatialAuthoringIssue> i){if(h.Distinct(StringComparer.Ordinal).Count()!=h.Length)Add(i,DungeonSpatialAuthoringDiagnostic.HeaderMismatch,t.Path,t.Id);foreach(string c in t.Columns.Except(h,StringComparer.Ordinal))Add(i,DungeonSpatialAuthoringDiagnostic.MissingColumn,t.Path,t.Id,column:c);foreach(string c in h.Except(t.Columns,StringComparer.Ordinal))Add(i,DungeonSpatialAuthoringDiagnostic.UnknownColumn,t.Path,t.Id,column:c);if(h.Length==t.Columns.Length)Add(i,DungeonSpatialAuthoringDiagnostic.HeaderMismatch,t.Path,t.Id);}
        private static string Key(string t,string[] r){int[] x=t=="floors"||t=="rooms"||t=="corridors"||t=="fixed_structures"||t=="socket_types"||t=="localization_en"?new[]{0}:t=="room_reserved_offsets"||t=="fixed_structure_reserved_offsets"?new[]{0,1,2}:new[]{0,1};return string.Join("\u001f",x.Where(n=>n<r.Length).Select(n=>r[n]));}
        private static IEnumerable<string> Keys(IEnumerable<string[]> rows)=>rows.Select(r=>string.Join("\u001f",r));
        private static HashSet<string> Set(Dictionary<string,List<string[]>> r,string t,int c)=>new HashSet<string>(r[t].Select(x=>x[c]),StringComparer.Ordinal);
        private static void FK(Dictionary<string,List<string[]>> r,string t,int c,HashSet<string> parent,List<DungeonSpatialAuthoringIssue> i){foreach(var row in r[t])if(!parent.Contains(row[c]))Add(i,DungeonSpatialAuthoringDiagnostic.MissingForeignKey,"tables/"+t+".csv",t,Key(t,row),Tables.First(x=>x.Id==t).Columns[c]);}
        private static bool IsInt(string c)=>new[]{"FloorIndex","MinimumX","MinimumY","Width","Height","FinalFloorSpaceCapacity","OptionalBranchAllowance","MaximumConnectionCount","MonsterCapacity","TrapCapacity","LootCapacity","OffsetX","OffsetY","MinimumLength","MaximumLength"}.Contains(c);
        private static bool TryInt(string s,out int value,out bool overflow){overflow=false;value=0;if(string.IsNullOrEmpty(s)||s[0]=='+'||(s.Length>1&&s[0]=='0')||(s.StartsWith("-0",StringComparison.Ordinal)))return false;long l;if(!long.TryParse(s,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out l)){overflow=s.All(c=>c=='-'||(c>='0'&&c<='9'));return false;}if(l<int.MinValue||l>int.MaxValue){overflow=true;return false;}value=(int)l;return true;}
        private static int P(string s){TryInt(s,out int v,out _);return v;} private static TEnum E<TEnum>(string s) where TEnum:struct=>(TEnum)Enum.Parse(typeof(TEnum),s,false);
        private static string S(Dictionary<string,object> o,string k)=>o.TryGetValue(k,out object v)?v as string:null; private static int I(Dictionary<string,object> o,string k)=>o.TryGetValue(k,out object v)&&v is long?(int)(long)v:0;
        private static DungeonSpatialAuthoringResult Result(List<DungeonSpatialAuthoringIssue> i)=>new DungeonSpatialAuthoringResult(null,i);
        private static void Add(List<DungeonSpatialAuthoringIssue> i,DungeonSpatialAuthoringDiagnostic d,string p="",string t="",string r="",string column="")=>i.Add(new DungeonSpatialAuthoringIssue{Diagnostic=d,RelativePath=p??"",TableId=t??"",RecordKey=r??"",Column=column??""});
        private sealed class Table{public string Id,Path;public string[] Columns;}
    }

    internal static class Csv
    {
        internal static bool TryParse(string text,out List<string[]> rows){rows=new List<string[]>();var row=new List<string>();var field=new StringBuilder();bool quoted=false,closed=false;for(int i=0;i<text.Length;i++){char c=text[i];if(quoted){if(c=='"'){if(i+1<text.Length&&text[i+1]=='"'){field.Append('"');i++;}else{quoted=false;closed=true;}}else if(c=='\n'||c=='\r')return false;else field.Append(c);}else{if(closed&&c!=','&&c!='\n')return false;if(c=='"'){if(field.Length!=0||closed)return false;quoted=true;}else if(c==','){row.Add(field.ToString());field.Clear();closed=false;}else if(c=='\n'){row.Add(field.ToString());if(row.Count==1&&row[0].Length==0)return false;rows.Add(row.ToArray());row.Clear();field.Clear();closed=false;}else{if(closed)return false;field.Append(c);}}}return !quoted&&row.Count==0&&field.Length==0&&!closed;}
    }

    internal static class StrictJson
    {
        internal static bool TryParse(string s,out object value,out DungeonSpatialAuthoringDiagnostic error){var p=new Parser(s);try{value=p.Value();p.Space();if(p.Index!=s.Length)throw new FormatException();error=DungeonSpatialAuthoringDiagnostic.None;return true;}catch(JsonProblem e){value=null;error=e.Diagnostic;return false;}catch{value=null;error=DungeonSpatialAuthoringDiagnostic.MalformedJson;return false;}}
        private sealed class JsonProblem:Exception{internal readonly DungeonSpatialAuthoringDiagnostic Diagnostic;internal JsonProblem(DungeonSpatialAuthoringDiagnostic d){Diagnostic=d;}}
        private sealed class Parser{private readonly string s;internal int Index;internal Parser(string value){s=value;}internal void Space(){while(Index<s.Length&&" \n\r\t".IndexOf(s[Index])>=0)Index++;}internal object Value(){Space();if(Index>=s.Length)throw new FormatException();char c=s[Index];if(c=='{')return Obj();if(c=='[')return Arr();if(c=='"')return Str();if(c=='-'||char.IsDigit(c))return Num();if(Take("true"))return true;if(Take("false"))return false;if(Take("null"))return null;throw new FormatException();}private Dictionary<string,object> Obj(){Index++;var d=new Dictionary<string,object>(StringComparer.Ordinal);var insensitive=new HashSet<string>(StringComparer.OrdinalIgnoreCase);Space();if(Pop('}'))return d;while(true){Space();if(Index>=s.Length||s[Index]!='"')throw new FormatException();string k=Str();Space();if(!Pop(':'))throw new FormatException();object v=Value();if(d.ContainsKey(k))throw new JsonProblem(DungeonSpatialAuthoringDiagnostic.DuplicateJsonField);if(!insensitive.Add(k))throw new JsonProblem(DungeonSpatialAuthoringDiagnostic.AmbiguousJsonField);d.Add(k,v);Space();if(Pop('}'))return d;if(!Pop(','))throw new FormatException();}}private List<object> Arr(){Index++;var a=new List<object>();Space();if(Pop(']'))return a;while(true){a.Add(Value());Space();if(Pop(']'))return a;if(!Pop(','))throw new FormatException();}}private string Str(){Index++;var b=new StringBuilder();while(Index<s.Length){char c=s[Index++];if(c=='"')return b.ToString();if(c<' ')throw new FormatException();if(c=='\\'){if(Index>=s.Length)throw new FormatException();char e=s[Index++];if(e=='u'){if(Index+4>s.Length)throw new FormatException();string h=s.Substring(Index,4);if(!h.All(Uri.IsHexDigit))throw new FormatException();b.Append((char)Convert.ToInt32(h,16));Index+=4;}else{int n="\"\\/bfnrt".IndexOf(e);if(n<0)throw new FormatException();b.Append("\"\\/\b\f\n\r\t"[n]);}}else b.Append(c);}throw new FormatException();}private long Num(){int start=Index;if(s[Index]=='-')Index++;if(Index>=s.Length||!char.IsDigit(s[Index]))throw new FormatException();if(s[Index]=='0'&&Index+1<s.Length&&char.IsDigit(s[Index+1]))throw new FormatException();while(Index<s.Length&&char.IsDigit(s[Index]))Index++;if(Index<s.Length&&".eE".IndexOf(s[Index])>=0)throw new FormatException();return long.Parse(s.Substring(start,Index-start),CultureInfo.InvariantCulture);}private bool Pop(char c){Space();if(Index<s.Length&&s[Index]==c){Index++;return true;}return false;}private bool Take(string x){if(Index+x.Length<=s.Length&&s.Substring(Index,x.Length)==x){Index+=x.Length;return true;}return false;}}
    }
}
#endif
