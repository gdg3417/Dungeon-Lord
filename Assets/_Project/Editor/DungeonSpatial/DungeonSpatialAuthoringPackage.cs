#if UNITY_EDITOR
using System;
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
        ProjectedCatalogInvalid = 33, ProjectedCatalogWorkloadExceeded = 34,
        InvalidJsonFieldType = 35, ManifestValueMismatch = 36, InvalidUtf8 = 37,
        DuplicateSourcePath = 38, InvalidFormat = 39, InvalidSchema = 40
    }

    public sealed class DungeonSpatialAuthoringIssue
    {
        public DungeonSpatialAuthoringDiagnostic Diagnostic { get; internal set; }
        public string RelativePath { get; internal set; }
        public string TableId { get; internal set; }
        public string RecordKey { get; internal set; }
        public string Column { get; internal set; }
        public override string ToString() => string.Join(":", Diagnostic, RelativePath, TableId, RecordKey, Column);
    }

    public sealed class DungeonSpatialAuthoringSource
    {
        private readonly SortedDictionary<string, byte[]> files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        private readonly SortedSet<string> duplicates = new SortedSet<string>(StringComparer.Ordinal);

        public DungeonSpatialAuthoringSource(IEnumerable<KeyValuePair<string, byte[]>> sourceFiles)
        {
            if (sourceFiles == null) return;
            foreach (KeyValuePair<string, byte[]> pair in sourceFiles)
            {
                if (files.ContainsKey(pair.Key)) duplicates.Add(pair.Key);
                else files.Add(pair.Key, pair.Value == null ? null : (byte[])pair.Value.Clone());
            }
        }

        public IReadOnlyDictionary<string, byte[]> Snapshot() => files.ToDictionary(
            pair => pair.Key, pair => pair.Value == null ? null : (byte[])pair.Value.Clone(), StringComparer.Ordinal);
        internal string[] Paths => files.Keys.ToArray();
        internal string[] DuplicatePaths => duplicates.ToArray();
        internal bool TryGet(string path, out byte[] bytes)
        {
            if (!files.TryGetValue(path, out byte[] stored)) { bytes = null; return false; }
            bytes = stored == null ? null : (byte[])stored.Clone();
            return true;
        }
    }

    public static class DungeonSpatialAuthoringRepository
    {
        public const string PackageRoot = "ContentAuthoring/DungeonSpatial";
        public static DungeonSpatialAuthoringSource Read(string root = PackageRoot)
        {
            if (!Directory.Exists(root)) return null;
            return new DungeonSpatialAuthoringSource(Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => new KeyValuePair<string, byte[]>(
                    path.Substring(root.Length).TrimStart('/', '\\').Replace('\\', '/'), File.ReadAllBytes(path))));
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
            Issues = issues.OrderBy(issue => (int)issue.Diagnostic)
                .ThenBy(issue => issue.RelativePath, StringComparer.Ordinal)
                .ThenBy(issue => issue.TableId, StringComparer.Ordinal)
                .ThenBy(issue => issue.RecordKey, StringComparer.Ordinal)
                .ThenBy(issue => issue.Column, StringComparer.Ordinal).ToArray();
        }
        public bool Success => Projection != null && Issues.Length == 0;
        public DungeonSpatialAuthoringProjection Projection { get; }
        public DungeonSpatialAuthoringIssue[] Issues { get; }
    }

    public sealed class AuthoringSchema
    {
        internal AuthoringSchema(IReadOnlyDictionary<string, string> formats, IReadOnlyDictionary<string, string[]> enums,
            AuthoringTable[] tables, AuthoringForeignKey[] foreignKeys, AuthoringChildRelationship[] relationships)
        { Formats = formats; Enums = enums; Tables = tables; ForeignKeys = foreignKeys; ChildRelationships = relationships; }
        public IReadOnlyDictionary<string, string> Formats { get; }
        public IReadOnlyDictionary<string, string[]> Enums { get; }
        public AuthoringTable[] Tables { get; }
        public AuthoringForeignKey[] ForeignKeys { get; }
        public AuthoringChildRelationship[] ChildRelationships { get; }
        public AuthoringTable Table(string id) => Tables.FirstOrDefault(table => table.Id == id);
    }
    public sealed class AuthoringTable
    {
        internal AuthoringTable(string id, string path, AuthoringColumn[] columns, string[] primaryKey, string[][] uniqueKeys, string[] canonicalOrder)
        { Id=id; Path=path; Columns=columns; PrimaryKey=primaryKey; UniqueKeys=uniqueKeys; CanonicalOrder=canonicalOrder; }
        public string Id { get; } public string Path { get; } public AuthoringColumn[] Columns { get; }
        public string[] PrimaryKey { get; } public string[][] UniqueKeys { get; } public string[] CanonicalOrder { get; }
        public int IndexOf(string name) => Array.FindIndex(Columns, column => column.Name == name);
    }
    public sealed class AuthoringColumn
    {
        internal AuthoringColumn(string name,string type,bool required,bool allowBlank,string enumId)
        { Name=name; Type=type; Required=required; AllowBlank=allowBlank; EnumId=enumId; }
        public string Name { get; } public string Type { get; } public bool Required { get; }
        public bool AllowBlank { get; } public string EnumId { get; }
    }
    public sealed class AuthoringForeignKey
    {
        internal AuthoringForeignKey(string table,string[] columns,string[] referenceTables,string[] referenceColumns)
        { Table=table; Columns=columns; ReferenceTables=referenceTables; ReferenceColumns=referenceColumns; }
        public string Table { get; } public string[] Columns { get; } public string[] ReferenceTables { get; } public string[] ReferenceColumns { get; }
    }
    public sealed class AuthoringChildRelationship
    {
        internal AuthoringChildRelationship(string parent,string[] children) { Parent=parent; Children=children; }
        public string Parent { get; } public string[] Children { get; }
    }

    internal sealed class AuthoringManifest
    {
        internal string Schema, ContentVersion, CatalogSchemaId, StringTableSchemaId, RequiredLanguage;
        internal int SchemaVersion, CatalogSchemaVersion, StringTableSchemaVersion;
        internal string[] Tables;
    }
    internal sealed class AuthoringRows
    {
        internal AuthoringRows(AuthoringTable table, List<string[]> rows) { Table=table; Rows=rows; }
        internal AuthoringTable Table { get; } internal List<string[]> Rows { get; }
        internal string Value(string[] row,string column) => row[Table.IndexOf(column)];
    }

    public static class DungeonSpatialAuthoringPackageParser
    {
        private const string ManifestPath = "authoring_manifest.json";
        private const string SchemaPath = "authoring_schema.json";
        private static readonly string[] ManifestFields = { "schema", "schemaVersion", "contentVersion", "catalogSchemaId", "catalogSchemaVersion", "stringTableSchemaId", "stringTableSchemaVersion", "requiredLanguage", "tables" };
        private static readonly string[] SchemaFields = { "formats", "enums", "tables", "foreignKeys", "childRelationships" };
        private static readonly HashSet<string> ColumnTypes = new HashSet<string>(new[] { "spatialId", "ownerScopedId", "localizationKey", "localizedText", "int32", "enum" }, StringComparer.Ordinal);

        private static readonly string[] V1FormatSignatures =
        {
            "spatialId=lowercase_dot_identifier_v1",
            "ownerScopedId=lowercase_owner_identifier_v1",
            "localizationKey=display_name_localization_key_v1",
            "localizedText=nonblank_source_text_v1",
            "int32=invariant_int32_v1",
            "textNormalization=utf8_lf_single_newline_v1"
        };
        private static readonly string[] V1TableSignatures =
        {
            "floors|tables/floors.csv|FloorDefinitionId:spatialId:true:false:,FloorIndex:int32:true:false:,MinimumX:int32:true:false:,MinimumY:int32:true:false:,Width:int32:true:false:,Height:int32:true:false:,FinalFloorSpaceCapacity:int32:true:false:,OptionalBranchAllowance:int32:true:false:,EntranceStructureDefinitionId:spatialId:true:false:,CompletionStructureDefinitionId:spatialId:true:false:|FloorDefinitionId|FloorIndex|FloorDefinitionId",
            "floor_allowed_rooms|tables/floor_allowed_rooms.csv|FloorDefinitionId:spatialId:true:false:,RoomDefinitionId:spatialId:true:false:|FloorDefinitionId,RoomDefinitionId||FloorDefinitionId,RoomDefinitionId",
            "floor_allowed_corridors|tables/floor_allowed_corridors.csv|FloorDefinitionId:spatialId:true:false:,CorridorDefinitionId:spatialId:true:false:|FloorDefinitionId,CorridorDefinitionId||FloorDefinitionId,CorridorDefinitionId",
            "rooms|tables/rooms.csv|RoomDefinitionId:spatialId:true:false:,Width:int32:true:false:,Height:int32:true:false:,MaximumConnectionCount:int32:true:false:,MonsterCapacity:int32:true:false:,TrapCapacity:int32:true:false:,LootCapacity:int32:true:false:,LocalizationKey:localizationKey:true:false:|RoomDefinitionId||RoomDefinitionId",
            "room_orientations|tables/room_orientations.csv|RoomDefinitionId:spatialId:true:false:,Orientation:enum:true:false:CardinalOrientation|RoomDefinitionId,Orientation||RoomDefinitionId,Orientation",
            "room_reserved_offsets|tables/room_reserved_offsets.csv|RoomDefinitionId:spatialId:true:false:,OffsetX:int32:true:false:,OffsetY:int32:true:false:|RoomDefinitionId,OffsetX,OffsetY||RoomDefinitionId,OffsetX,OffsetY",
            "room_connection_points|tables/room_connection_points.csv|RoomDefinitionId:spatialId:true:false:,ConnectionPointId:ownerScopedId:true:false:,OffsetX:int32:true:false:,OffsetY:int32:true:false:,Facing:enum:true:false:CardinalOrientation,SocketTypeId:spatialId:true:false:|RoomDefinitionId,ConnectionPointId||RoomDefinitionId,ConnectionPointId",
            "corridors|tables/corridors.csv|CorridorDefinitionId:spatialId:true:false:,LocalizationKey:localizationKey:true:false:,Category:enum:true:false:CorridorSpatialCategory,MinimumLength:int32:true:false:,MaximumLength:int32:true:false:,Width:int32:true:false:,MonsterCapacity:int32:true:false:,TrapCapacity:int32:true:false:,LootCapacity:int32:true:false:|CorridorDefinitionId||CorridorDefinitionId",
            "corridor_orientations|tables/corridor_orientations.csv|CorridorDefinitionId:spatialId:true:false:,Orientation:enum:true:false:CardinalOrientation|CorridorDefinitionId,Orientation||CorridorDefinitionId,Orientation",
            "corridor_compatible_sockets|tables/corridor_compatible_sockets.csv|CorridorDefinitionId:spatialId:true:false:,SocketTypeId:spatialId:true:false:|CorridorDefinitionId,SocketTypeId||CorridorDefinitionId,SocketTypeId",
            "fixed_structures|tables/fixed_structures.csv|StructureDefinitionId:spatialId:true:false:,LocalizationKey:localizationKey:true:false:,Kind:enum:true:false:FixedSpatialStructureKind,Width:int32:true:false:,Height:int32:true:false:,MaximumConnectionCount:int32:true:false:|StructureDefinitionId||StructureDefinitionId",
            "fixed_structure_orientations|tables/fixed_structure_orientations.csv|StructureDefinitionId:spatialId:true:false:,Orientation:enum:true:false:CardinalOrientation|StructureDefinitionId,Orientation||StructureDefinitionId,Orientation",
            "fixed_structure_reserved_offsets|tables/fixed_structure_reserved_offsets.csv|StructureDefinitionId:spatialId:true:false:,OffsetX:int32:true:false:,OffsetY:int32:true:false:|StructureDefinitionId,OffsetX,OffsetY||StructureDefinitionId,OffsetX,OffsetY",
            "fixed_structure_connection_points|tables/fixed_structure_connection_points.csv|StructureDefinitionId:spatialId:true:false:,ConnectionPointId:ownerScopedId:true:false:,OffsetX:int32:true:false:,OffsetY:int32:true:false:,Facing:enum:true:false:CardinalOrientation,SocketTypeId:spatialId:true:false:|StructureDefinitionId,ConnectionPointId||StructureDefinitionId,ConnectionPointId",
            "socket_types|tables/socket_types.csv|SocketTypeId:spatialId:true:false:|SocketTypeId||SocketTypeId",
            "socket_compatibility|tables/socket_compatibility.csv|SocketTypeId:spatialId:true:false:,CompatibleSocketTypeId:spatialId:true:false:|SocketTypeId,CompatibleSocketTypeId||SocketTypeId,CompatibleSocketTypeId",
            "localization_en|tables/localization_en.csv|Key:localizationKey:true:false:,Text:localizedText:true:false:|Key||Key"
        };
        private static readonly string[] V1ForeignKeySignatures =
        {
            "floors|EntranceStructureDefinitionId,CompletionStructureDefinitionId|fixed_structures.StructureDefinitionId,fixed_structures.StructureDefinitionId",
            "floor_allowed_rooms|FloorDefinitionId,RoomDefinitionId|floors.FloorDefinitionId,rooms.RoomDefinitionId",
            "floor_allowed_corridors|FloorDefinitionId,CorridorDefinitionId|floors.FloorDefinitionId,corridors.CorridorDefinitionId",
            "room_orientations|RoomDefinitionId|rooms.RoomDefinitionId",
            "room_reserved_offsets|RoomDefinitionId|rooms.RoomDefinitionId",
            "room_connection_points|RoomDefinitionId,SocketTypeId|rooms.RoomDefinitionId,socket_types.SocketTypeId",
            "corridor_orientations|CorridorDefinitionId|corridors.CorridorDefinitionId",
            "corridor_compatible_sockets|CorridorDefinitionId,SocketTypeId|corridors.CorridorDefinitionId,socket_types.SocketTypeId",
            "fixed_structure_orientations|StructureDefinitionId|fixed_structures.StructureDefinitionId",
            "fixed_structure_reserved_offsets|StructureDefinitionId|fixed_structures.StructureDefinitionId",
            "fixed_structure_connection_points|StructureDefinitionId,SocketTypeId|fixed_structures.StructureDefinitionId,socket_types.SocketTypeId",
            "socket_compatibility|SocketTypeId,CompatibleSocketTypeId|socket_types.SocketTypeId,socket_types.SocketTypeId",
            "rooms|LocalizationKey|localization_en.Key",
            "corridors|LocalizationKey|localization_en.Key",
            "fixed_structures|LocalizationKey|localization_en.Key"
        };
        private static readonly string[] V1RelationshipSignatures =
        {
            "floors|floor_allowed_rooms,floor_allowed_corridors",
            "rooms|room_orientations,room_reserved_offsets,room_connection_points",
            "corridors|corridor_orientations,corridor_compatible_sockets",
            "fixed_structures|fixed_structure_orientations,fixed_structure_reserved_offsets,fixed_structure_connection_points",
            "socket_types|socket_compatibility"
        };

        public static DungeonSpatialAuthoringResult ParseAndProject(DungeonSpatialAuthoringSource source,
            SpatialContentValidationWorkloadLimits limits, bool requireCanonicalRows = false)
        {
            var issues = new List<DungeonSpatialAuthoringIssue>();
            if (source == null) { Add(issues, DungeonSpatialAuthoringDiagnostic.MissingSource); return Failed(issues); }
            foreach (string path in source.DuplicatePaths) Add(issues, DungeonSpatialAuthoringDiagnostic.DuplicateSourcePath, path);

            if (!ReadJson(source, ManifestPath, DungeonSpatialAuthoringDiagnostic.MissingManifest, issues, out Dictionary<string, object> manifestObject)) return Failed(issues);
            AuthoringManifest manifest = ParseManifest(manifestObject, issues);
            if (manifest == null || issues.Count != 0) return Failed(issues);
            if (!ReadJson(source, SchemaPath, DungeonSpatialAuthoringDiagnostic.MissingSchema, issues, out Dictionary<string, object> schemaObject)) return Failed(issues);
            AuthoringSchema schema = ParseSchema(schemaObject, issues);
            if (schema == null || issues.Count != 0) return Failed(issues);
            ValidateManifestTables(manifest, schema, issues);
            ValidateProjectorCompatibility(schema, issues);
            if (issues.Count != 0) return Failed(issues);

            HashSet<string> approved = new HashSet<string>(manifest.Tables.Concat(new[] { ManifestPath, SchemaPath }), StringComparer.Ordinal);
            foreach (string path in source.Paths.Where(path => !approved.Contains(path))) Add(issues, DungeonSpatialAuthoringDiagnostic.UnexpectedFile, path);

            var tables = new Dictionary<string, AuthoringRows>(StringComparer.Ordinal);
            foreach (string path in manifest.Tables)
            {
                AuthoringTable table = schema.Tables.Single(item => item.Path == path);
                if (!ReadText(source, path, DungeonSpatialAuthoringDiagnostic.MissingTable, issues, out string csv)) continue;
                if (!StrictCsv.TryParse(csv, out List<string[]> records)) { Add(issues, DungeonSpatialAuthoringDiagnostic.MalformedCsv, path, table.Id); continue; }
                ParseTable(table, records, requireCanonicalRows, issues, out AuthoringRows parsed);
                if (parsed != null) tables.Add(table.Id, parsed);
            }
            if (issues.Count != 0) return Failed(issues);
            ValidateRows(schema, tables, issues);
            if (issues.Count != 0) return Failed(issues);

            DungeonSpatialAuthoringProjection projection;
            try { projection = Project(manifest, tables); }
            catch (KeyNotFoundException) { Add(issues, DungeonSpatialAuthoringDiagnostic.InvalidSchema, SchemaPath); return Failed(issues); }
            catch (InvalidOperationException) { Add(issues, DungeonSpatialAuthoringDiagnostic.InvalidSchema, SchemaPath); return Failed(issues); }
            HashSet<string> localizationKeys = new HashSet<string>(projection.English.entries.Select(entry => entry.key), StringComparer.Ordinal);
            long additionalEnglishCharacters = CountAdditionalEnglishCharacters(projection.English);
            SpatialContentValidationResult validation = SpatialContentValidator.Validate(
                projection.Catalog, limits, localizationKeys, additionalEnglishCharacters);
            if (!validation.IsValid)
                Add(issues, validation.Issues.Any(issue => issue.Reason == SpatialContentValidationReason.WorkloadExceeded)
                    ? DungeonSpatialAuthoringDiagnostic.ProjectedCatalogWorkloadExceeded : DungeonSpatialAuthoringDiagnostic.ProjectedCatalogInvalid);
            if (!SpatialContentCanonicalizer.TryCanonicalize(projection.Catalog, limits, out SpatialContentCatalog canonical))
                Add(issues, DungeonSpatialAuthoringDiagnostic.ProjectedCatalogWorkloadExceeded);
            if (issues.Count != 0) return Failed(issues);
            projection.Catalog = canonical;
            projection.English.entries = projection.English.entries.OrderBy(entry => entry.key, StringComparer.Ordinal).ToArray();
            return new DungeonSpatialAuthoringResult(projection, issues);
        }

        private static AuthoringManifest ParseManifest(Dictionary<string, object> root, List<DungeonSpatialAuthoringIssue> issues)
        {
            ValidateExactFields(root, ManifestFields, ManifestPath, issues);
            string[] strings = { "schema", "contentVersion", "catalogSchemaId", "stringTableSchemaId", "requiredLanguage" };
            string[] integers = { "schemaVersion", "catalogSchemaVersion", "stringTableSchemaVersion" };
            foreach (string field in strings) if (root.TryGetValue(field, out object value) && !(value is string)) Add(issues, DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType, ManifestPath, column:field);
            foreach (string field in integers) if (root.TryGetValue(field, out object value) && !(value is long)) Add(issues, DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType, ManifestPath, column:field);
            if (root.TryGetValue("tables", out object paths) && !(paths is List<object>)) Add(issues, DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType, ManifestPath, column:"tables");
            if (issues.Count != 0) return null;
            var result = new AuthoringManifest {
                Schema=(string)root["schema"], SchemaVersion=ToInt(root["schemaVersion"]), ContentVersion=(string)root["contentVersion"],
                CatalogSchemaId=(string)root["catalogSchemaId"], CatalogSchemaVersion=ToInt(root["catalogSchemaVersion"]),
                StringTableSchemaId=(string)root["stringTableSchemaId"], StringTableSchemaVersion=ToInt(root["stringTableSchemaVersion"]),
                RequiredLanguage=(string)root["requiredLanguage"] };
            List<object> list=(List<object>)root["tables"];
            if (list.Any(value => !(value is string))) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType,ManifestPath,column:"tables");
            else result.Tables=list.Cast<string>().ToArray();
            foreach(string field in strings) if(string.IsNullOrWhiteSpace((string)root[field])) Add(issues,DungeonSpatialAuthoringDiagnostic.BlankRequiredValue,ManifestPath,column:field);
            if(result.Schema!="dungeon_spatial_authoring" || result.SchemaVersion!=1) Add(issues,DungeonSpatialAuthoringDiagnostic.UnsupportedAuthoringSchema,ManifestPath);
            CheckManifest(result.ContentVersion=="0.1.0","contentVersion",issues);
            CheckManifest(result.CatalogSchemaId=="dungeon_spatial_content" && result.CatalogSchemaVersion==1,"catalogSchemaId",issues);
            CheckManifest(result.StringTableSchemaId=="string_table" && result.StringTableSchemaVersion==1,"stringTableSchemaId",issues);
            CheckManifest(result.RequiredLanguage=="en","requiredLanguage",issues);
            if(result.Tables!=null) ValidatePaths(result.Tables,issues);
            return result;
        }

        private static AuthoringSchema ParseSchema(Dictionary<string, object> root,List<DungeonSpatialAuthoringIssue> issues)
        {
            ValidateExactFields(root,SchemaFields,SchemaPath,issues);
            if(root.ContainsKey("schema")||root.ContainsKey("schemaVersion")) Add(issues,DungeonSpatialAuthoringDiagnostic.DuplicateAuthority,SchemaPath);
            if(!TryObject(root,"formats",out Dictionary<string,object> formatObject) || !TryObject(root,"enums",out Dictionary<string,object> enumObject) ||
                !TryArray(root,"tables",out List<object> tableArray) || !TryArray(root,"foreignKeys",out List<object> fkArray) || !TryArray(root,"childRelationships",out List<object> childArray))
            { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType,SchemaPath); return null; }
            var formats=new Dictionary<string,string>(StringComparer.Ordinal);
            ValidateExactFields(formatObject, new[] { "spatialId", "ownerScopedId", "localizationKey", "localizedText", "int32", "textNormalization" }, SchemaPath, issues);
            foreach(var pair in formatObject) { if(!(pair.Value is string)||string.IsNullOrWhiteSpace((string)pair.Value)) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType,SchemaPath,column:pair.Key); else formats[pair.Key]=(string)pair.Value; }
            foreach(string required in new[]{"spatialId","ownerScopedId","localizationKey","int32","textNormalization"}) if(!formats.ContainsKey(required)) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath,column:required);
            var enums=new Dictionary<string,string[]>(StringComparer.Ordinal);
            ValidateExactFields(enumObject, new[] { "CardinalOrientation", "CorridorSpatialCategory", "FixedSpatialStructureKind" }, SchemaPath, issues);
            foreach(var pair in enumObject) { if(!(pair.Value is List<object> values)||values.Any(v=>!(v is string))||values.Count==0||values.Cast<string>().Distinct(StringComparer.Ordinal).Count()!=values.Count) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath,column:pair.Key); else enums[pair.Key]=values.Cast<string>().ToArray(); }
            var tables=new List<AuthoringTable>();
            foreach(object value in tableArray) { AuthoringTable table=ParseSchemaTable(value,formats,enums,issues); if(table!=null) tables.Add(table); }
            bool identitiesValid = tables.Count == 17 &&
                tables.All(table => !string.IsNullOrWhiteSpace(table.Id) && !string.IsNullOrWhiteSpace(table.Path)) &&
                tables.Select(table => table.Id).Distinct(StringComparer.Ordinal).Count() == tables.Count &&
                tables.Select(table => table.Path).Distinct(StringComparer.Ordinal).Count() == tables.Count;
            if (!identitiesValid) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath);
            if (!identitiesValid) return null;
            var tableIndex=tables.ToDictionary(t=>t.Id,StringComparer.Ordinal);
            var foreignKeys=new List<AuthoringForeignKey>(); foreach(object value in fkArray) { AuthoringForeignKey key=ParseForeignKey(value,tableIndex,issues); if(key!=null) foreignKeys.Add(key); }
            var relationships=new List<AuthoringChildRelationship>(); var owners=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(object value in childArray) { AuthoringChildRelationship relation=ParseRelationship(value,tableIndex,owners,issues); if(relation!=null) relationships.Add(relation); }
            ValidateRelationshipForeignKeys(relationships, foreignKeys, tableIndex, issues);
            return issues.Count==0 ? new AuthoringSchema(formats,enums,tables.ToArray(),foreignKeys.ToArray(),relationships.ToArray()) : null;
        }
        private static AuthoringTable ParseSchemaTable(object value,Dictionary<string,string> formats,Dictionary<string,string[]> enums,List<DungeonSpatialAuthoringIssue> issues)
        {
            if(!(value is Dictionary<string,object> root)) { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType,SchemaPath); return null; }
            string[] fields={"id","path","columns","primaryKey","canonicalOrder"};
            string[] optional={"uniqueKeys"}; ValidateExactFields(root,fields.Concat(optional).ToArray(),SchemaPath,issues,optional);
            if(!GetString(root,"id",out string id)||!GetString(root,"path",out string path)||!TryArray(root,"columns",out List<object> columns)||
               !StringArray(root,"primaryKey",out string[] primary)||!StringArray(root,"canonicalOrder",out string[] order))
            { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType,SchemaPath); return null; }
            if (string.IsNullOrWhiteSpace(id)) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath);
            if(!IsNormalizedPath(path)) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidOrDuplicateTablePath,SchemaPath,id);
            if(IsNormalizedPath(path) && Path.GetFileNameWithoutExtension(path) != id) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath,id);
            var parsedColumns=new List<AuthoringColumn>();
            foreach(object item in columns)
            {
                if(!(item is Dictionary<string,object> column)) { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType,SchemaPath,id); continue; }
                string[] columnFields={"name","type","required","allowBlank","enum"}; ValidateExactFields(column,columnFields,SchemaPath,issues,new[]{"enum"});
                if(!GetString(column,"name",out string name)||!GetString(column,"type",out string type)||!GetBool(column,"required",out bool required)||!GetBool(column,"allowBlank",out bool allowBlank))
                { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType,SchemaPath,id); continue; }
                string enumId=column.TryGetValue("enum",out object enumValue)?enumValue as string:null;
                if (string.IsNullOrWhiteSpace(name)) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath,id);
                if(!ColumnTypes.Contains(type)||(type!="enum"&&!formats.ContainsKey(type))) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidFormat,SchemaPath,id,column:name);
                if(type=="enum" && (enumId==null||!enums.ContainsKey(enumId))) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath,id,column:name);
                if(type!="enum" && column.ContainsKey("enum")) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath,id,column:name);
                parsedColumns.Add(new AuthoringColumn(name,type,required,allowBlank,enumId));
            }
            if(parsedColumns.Select(c=>c.Name).Distinct(StringComparer.Ordinal).Count()!=parsedColumns.Count) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath,id);
            string[][] unique=Array.Empty<string[]>();
            if(root.TryGetValue("uniqueKeys",out object uniqueValue))
            {
                if(!(uniqueValue is List<object> arrays)||arrays.Any(item=>!(item is List<object>)||((List<object>)item).Any(x=>!(x is string)))) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType,SchemaPath,id);
                else unique=arrays.Cast<List<object>>().Select(array=>array.Cast<string>().ToArray()).ToArray();
            }
            var names=new HashSet<string>(parsedColumns.Select(c=>c.Name),StringComparer.Ordinal);
            if (primary.Length == 0 || order.Length == 0 || primary.Distinct(StringComparer.Ordinal).Count() != primary.Length ||
                order.Distinct(StringComparer.Ordinal).Count() != order.Length || unique.Any(key => key.Length == 0 || key.Distinct(StringComparer.Ordinal).Count() != key.Length))
                Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath,id);
            foreach(string key in primary.Concat(order).Concat(unique.SelectMany(x=>x))) if(!names.Contains(key)) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath,id,column:key);
            return new AuthoringTable(id,path,parsedColumns.ToArray(),primary,unique,order);
        }

        private static AuthoringForeignKey ParseForeignKey(object value,Dictionary<string,AuthoringTable> tables,List<DungeonSpatialAuthoringIssue> issues)
        {
            if(!(value is Dictionary<string,object> root)) { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType,SchemaPath); return null; }
            ValidateExactFields(root,new[]{"table","columns","references"},SchemaPath,issues);
            if(!GetString(root,"table",out string table)||!StringArray(root,"columns",out string[] columns)||!StringArray(root,"references",out string[] references)||columns.Length==0||columns.Length!=references.Length)
            { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath); return null; }
            if(!tables.TryGetValue(table,out AuthoringTable owner)) { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath,table); return null; }
            var referenceTables=new string[references.Length]; var referenceColumns=new string[references.Length];
            for(int i=0;i<references.Length;i++)
            {
                int separator=references[i].IndexOf('.'); if(separator<=0||separator==references[i].Length-1) { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath,table,column:columns[i]); continue; }
                referenceTables[i]=references[i].Substring(0,separator); referenceColumns[i]=references[i].Substring(separator+1);
                if(owner.IndexOf(columns[i])<0||!tables.TryGetValue(referenceTables[i],out AuthoringTable target)||target.IndexOf(referenceColumns[i])<0) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath,table,column:columns[i]);
            }
            return new AuthoringForeignKey(table,columns,referenceTables,referenceColumns);
        }

        private static AuthoringChildRelationship ParseRelationship(object value,Dictionary<string,AuthoringTable> tables,Dictionary<string,string> owners,List<DungeonSpatialAuthoringIssue> issues)
        {
            if(!(value is Dictionary<string,object> root)) { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidJsonFieldType,SchemaPath); return null; }
            ValidateExactFields(root,new[]{"parent","children"},SchemaPath,issues);
            if(!GetString(root,"parent",out string parent)||!StringArray(root,"children",out string[] children)||!tables.ContainsKey(parent)) { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath); return null; }
            foreach(string child in children)
            {
                if(!tables.ContainsKey(child)) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidSchema,SchemaPath,parent,column:child);
                else if(owners.ContainsKey(child)) Add(issues,DungeonSpatialAuthoringDiagnostic.DuplicateAuthority,SchemaPath,child);
                else owners[child]=parent;
            }
            return new AuthoringChildRelationship(parent,children);
        }

        private static void ValidateRelationshipForeignKeys(
            IEnumerable<AuthoringChildRelationship> relationships,
            IEnumerable<AuthoringForeignKey> foreignKeys,
            IReadOnlyDictionary<string, AuthoringTable> tables,
            List<DungeonSpatialAuthoringIssue> issues)
        {
            foreach (AuthoringChildRelationship relationship in relationships)
            {
                AuthoringTable parent = tables[relationship.Parent];
                foreach (string childId in relationship.Children)
                {
                    AuthoringTable child = tables[childId];
                    var ownerMappings = foreignKeys.Where(key => key.Table == childId)
                        .SelectMany(key => key.Columns.Select((column, index) => new
                        {
                            Column = column,
                            ReferenceTable = key.ReferenceTables[index],
                            ReferenceColumn = key.ReferenceColumns[index]
                        }))
                        .Where(mapping => mapping.ReferenceTable == parent.Id).ToArray();
                    bool ownsParent = parent.PrimaryKey.All(parentColumn => ownerMappings.Count(mapping =>
                            mapping.Column == parentColumn && mapping.ReferenceColumn == parentColumn &&
                            child.IndexOf(mapping.Column) >= 0 &&
                            child.Columns[child.IndexOf(mapping.Column)].Type ==
                            parent.Columns[parent.IndexOf(parentColumn)].Type) == 1);
                    if (!ownsParent)
                        Add(issues, DungeonSpatialAuthoringDiagnostic.InvalidSchema, SchemaPath, childId);
                }
            }
        }

        private static void ValidateProjectorCompatibility(AuthoringSchema schema, List<DungeonSpatialAuthoringIssue> issues)
        {
            string[] formats = schema.Formats.Select(pair => pair.Key + "=" + pair.Value).ToArray();
            string[] tables = schema.Tables.Select(BuildTableSignature).ToArray();
            string[] foreignKeys = schema.ForeignKeys.Select(key => key.Table + "|" +
                string.Join(",", key.Columns) + "|" + string.Join(",", key.ReferenceTables
                    .Select((table, index) => table + "." + key.ReferenceColumns[index]))).ToArray();
            string[] relationships = schema.ChildRelationships.Select(relationship =>
                relationship.Parent + "|" + string.Join(",", relationship.Children)).ToArray();

            RequireExactSignature(formats.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                V1FormatSignatures.OrderBy(value => value, StringComparer.Ordinal).ToArray(), "formats", issues);
            RequireExactSignature(tables, V1TableSignatures, "tables", issues);
            RequireExactSignature(foreignKeys, V1ForeignKeySignatures, "foreignKeys", issues);
            RequireExactSignature(relationships, V1RelationshipSignatures, "childRelationships", issues);
            RequireEnum(schema, "CardinalOrientation", new[] { "Zero", "Ninety", "OneEighty", "TwoSeventy" }, issues);
            RequireEnum(schema, "CorridorSpatialCategory", new[] { "Straight" }, issues);
            RequireEnum(schema, "FixedSpatialStructureKind", new[] { "Entrance", "CompletionTerminal" }, issues);
        }

        private static string BuildTableSignature(AuthoringTable table)
        {
            string columns = string.Join(",", table.Columns.Select(column => string.Join(":",
                column.Name, column.Type, column.Required.ToString().ToLowerInvariant(),
                column.AllowBlank.ToString().ToLowerInvariant(), column.EnumId ?? string.Empty)));
            string unique = string.Join(";", table.UniqueKeys.Select(key => string.Join(",", key)));
            return string.Join("|", table.Id, table.Path, columns, string.Join(",", table.PrimaryKey),
                unique, string.Join(",", table.CanonicalOrder));
        }

        private static void RequireExactSignature(string[] actual, string[] expected, string section,
            List<DungeonSpatialAuthoringIssue> issues)
        {
            if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
                Add(issues, DungeonSpatialAuthoringDiagnostic.InvalidSchema, SchemaPath, column: section);
        }

        private static void RequireEnum(AuthoringSchema schema, string id, string[] expected, List<DungeonSpatialAuthoringIssue> issues)
        {
            if (!schema.Enums.TryGetValue(id, out string[] actual) || !actual.SequenceEqual(expected, StringComparer.Ordinal))
                Add(issues, DungeonSpatialAuthoringDiagnostic.InvalidSchema, SchemaPath, column: id);
        }

        private static long CountAdditionalEnglishCharacters(StringTable table)
        {
            long characters = (table.schema?.Length ?? 0) + (table.language?.Length ?? 0);
            foreach (StringEntry entry in table.entries ?? Array.Empty<StringEntry>())
                characters += entry?.text?.Length ?? 0;
            return characters;
        }

        private static void ValidateManifestTables(AuthoringManifest manifest,AuthoringSchema schema,List<DungeonSpatialAuthoringIssue> issues)
        {
            string[] schemaPaths=schema.Tables.Select(table=>table.Path).ToArray();
            foreach(string path in schemaPaths.Except(manifest.Tables,StringComparer.Ordinal)) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidOrDuplicateTablePath,ManifestPath,column:path);
            foreach(string path in manifest.Tables.Except(schemaPaths,StringComparer.Ordinal)) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidOrDuplicateTablePath,ManifestPath,column:path);
            if(!manifest.Tables.SequenceEqual(schemaPaths,StringComparer.Ordinal)) Add(issues,DungeonSpatialAuthoringDiagnostic.ManifestSchemaTableMismatch,ManifestPath);
        }

        private static void ParseTable(AuthoringTable table,List<string[]> records,bool requireCanonical,List<DungeonSpatialAuthoringIssue> issues,out AuthoringRows result)
        {
            result=null;
            if(records.Count==0) { Add(issues,DungeonSpatialAuthoringDiagnostic.HeaderMismatch,table.Path,table.Id); return; }
            string[] expected=table.Columns.Select(column=>column.Name).ToArray(); string[] header=records[0];
            if(!header.SequenceEqual(expected,StringComparer.Ordinal))
            {
                if(header.Distinct(StringComparer.Ordinal).Count()!=header.Length) Add(issues,DungeonSpatialAuthoringDiagnostic.HeaderMismatch,table.Path,table.Id);
                foreach(string column in expected.Except(header,StringComparer.Ordinal)) Add(issues,DungeonSpatialAuthoringDiagnostic.MissingColumn,table.Path,table.Id,column:column);
                foreach(string column in header.Except(expected,StringComparer.Ordinal)) Add(issues,DungeonSpatialAuthoringDiagnostic.UnknownColumn,table.Path,table.Id,column:column);
                if(header.Length==expected.Length) Add(issues,DungeonSpatialAuthoringDiagnostic.HeaderMismatch,table.Path,table.Id);
                return;
            }
            List<string[]> rows=records.Skip(1).ToList();
            foreach(string[] row in rows) if(row.Length!=expected.Length) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidFieldCount,table.Path,table.Id);
            if(issues.Any(issue=>issue.RelativePath==table.Path)) return;
            result=new AuthoringRows(table,rows);
            if(requireCanonical)
            {
                string[] supplied=rows.Select(row=>BuildKey(table,row,table.CanonicalOrder)).ToArray();
                if(!supplied.SequenceEqual(supplied.OrderBy(key=>key,StringComparer.Ordinal))) Add(issues,DungeonSpatialAuthoringDiagnostic.NoncanonicalCommittedRowOrder,table.Path,table.Id);
            }
        }

        private static void ValidateRows(AuthoringSchema schema,Dictionary<string,AuthoringRows> tables,List<DungeonSpatialAuthoringIssue> issues)
        {
            foreach(AuthoringRows data in tables.Values)
            {
                ValidateFields(schema,data,issues);
                ValidateKey(data,data.Table.PrimaryKey,DungeonSpatialAuthoringDiagnostic.DuplicatePrimaryKey,issues);
                foreach(string[] key in data.Table.UniqueKeys) ValidateKey(data,key,DungeonSpatialAuthoringDiagnostic.DuplicateUniqueKey,issues);
            }
            foreach(AuthoringForeignKey foreignKey in schema.ForeignKeys)
            {
                AuthoringRows source=tables[foreignKey.Table];
                for(int mapping=0;mapping<foreignKey.Columns.Length;mapping++)
                {
                    int sourceIndex=source.Table.IndexOf(foreignKey.Columns[mapping]); AuthoringRows target=tables[foreignKey.ReferenceTables[mapping]];
                    int targetIndex=target.Table.IndexOf(foreignKey.ReferenceColumns[mapping]);
                    HashSet<string> values=new HashSet<string>(target.Rows.Select(row=>row[targetIndex]),StringComparer.Ordinal);
                    foreach(string[] row in source.Rows.Where(row=>!values.Contains(row[sourceIndex]))) Add(issues,DungeonSpatialAuthoringDiagnostic.MissingForeignKey,source.Table.Path,source.Table.Id,BuildKey(source.Table,row,source.Table.PrimaryKey),foreignKey.Columns[mapping]);
                }
            }
        }

        private static void ValidateFields(AuthoringSchema schema,AuthoringRows data,List<DungeonSpatialAuthoringIssue> issues)
        {
            foreach(string[] row in data.Rows) for(int index=0;index<data.Table.Columns.Length;index++)
            {
                AuthoringColumn column=data.Table.Columns[index]; string value=row[index]; string key=BuildKey(data.Table,row,data.Table.PrimaryKey);
                if(column.Required&&value.Length==0&&!column.AllowBlank) { Add(issues,DungeonSpatialAuthoringDiagnostic.BlankRequiredValue,data.Table.Path,data.Table.Id,key,column.Name); continue; }
                if(value.Length==0&&column.AllowBlank) continue;
                DungeonSpatialAuthoringDiagnostic diagnostic=ValidateValue(schema,column,value);
                if(diagnostic!=DungeonSpatialAuthoringDiagnostic.None) Add(issues,diagnostic,data.Table.Path,data.Table.Id,key,column.Name);
            }
        }

        private static DungeonSpatialAuthoringDiagnostic ValidateValue(AuthoringSchema schema,AuthoringColumn column,string value)
        {
            if(value!=value.Trim()||value.Any(c=>c=='\t'||char.IsControl(c))) return DungeonSpatialAuthoringDiagnostic.InvalidFormat;
            if(column.Type=="int32") { if(TryInt(value,out _,out bool overflow)) return DungeonSpatialAuthoringDiagnostic.None; return overflow?DungeonSpatialAuthoringDiagnostic.Int32Overflow:DungeonSpatialAuthoringDiagnostic.InvalidInt32; }
            if(column.Type=="enum") return schema.Enums[column.EnumId].Contains(value,StringComparer.Ordinal)?DungeonSpatialAuthoringDiagnostic.None:DungeonSpatialAuthoringDiagnostic.InvalidEnumToken;
            if(column.Type=="localizedText") return value.Length>0?DungeonSpatialAuthoringDiagnostic.None:DungeonSpatialAuthoringDiagnostic.BlankRequiredValue;
            if(column.Type=="ownerScopedId") return IsIdentifier(value,false)?DungeonSpatialAuthoringDiagnostic.None:DungeonSpatialAuthoringDiagnostic.InvalidFormat;
            if(column.Type=="spatialId") return IsIdentifier(value,true)?DungeonSpatialAuthoringDiagnostic.None:DungeonSpatialAuthoringDiagnostic.InvalidFormat;
            if(column.Type=="localizationKey") return IsIdentifier(value,true)&&value.EndsWith(".display_name",StringComparison.Ordinal)?DungeonSpatialAuthoringDiagnostic.None:DungeonSpatialAuthoringDiagnostic.InvalidFormat;
            return DungeonSpatialAuthoringDiagnostic.InvalidFormat;
        }

        private static bool IsIdentifier(string value,bool requirePeriod)
        {
            if(string.IsNullOrEmpty(value)||value[0]=='.'||value[value.Length-1]=='.'||value.Contains("..")||(requirePeriod&&!value.Contains("."))) return false;
            return value.All(c=>(c>='a'&&c<='z')||(c>='0'&&c<='9')||c=='_'||c=='.');
        }
        private static void ValidateKey(AuthoringRows data,string[] columns,DungeonSpatialAuthoringDiagnostic diagnostic,List<DungeonSpatialAuthoringIssue> issues)
        {
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(string[] row in data.Rows) { string key=BuildKey(data.Table,row,columns); if(!seen.Add(key)) Add(issues,diagnostic,data.Table.Path,data.Table.Id,key); }
        }
        private static string BuildKey(AuthoringTable table,string[] row,string[] columns) => string.Join("\u001f",columns.Select(column=>row[table.IndexOf(column)]));
        private static DungeonSpatialAuthoringProjection Project(AuthoringManifest manifest,Dictionary<string,AuthoringRows> tables)
        {
            AuthoringRows floors=tables["floors"], rooms=tables["rooms"], corridors=tables["corridors"], structures=tables["fixed_structures"], sockets=tables["socket_types"], localization=tables["localization_en"];
            string[] Children(string table,string owner,string column) => tables[table].Rows.Where(row=>tables[table].Value(row,tables[table].Table.Columns[0].Name)==owner).Select(row=>tables[table].Value(row,column)).OrderBy(value=>value,StringComparer.Ordinal).ToArray();
            TileCoordinate[] Offsets(string table,string owner) => tables[table].Rows.Where(row=>tables[table].Value(row,tables[table].Table.Columns[0].Name)==owner).Select(row=>new TileCoordinate(ParseInt(tables[table].Value(row,"OffsetX")),ParseInt(tables[table].Value(row,"OffsetY")))).OrderBy(value=>value).ToArray();
            SpatialConnectionPointDefinition[] Points(string table,string owner) => tables[table].Rows.Where(row=>tables[table].Value(row,tables[table].Table.Columns[0].Name)==owner).OrderBy(row=>tables[table].Value(row,"ConnectionPointId"),StringComparer.Ordinal).Select(row=>new SpatialConnectionPointDefinition { ConnectionPointId=tables[table].Value(row,"ConnectionPointId"), Offset=new TileCoordinate(ParseInt(tables[table].Value(row,"OffsetX")),ParseInt(tables[table].Value(row,"OffsetY"))), Facing=ParseEnum<CardinalOrientation>(tables[table].Value(row,"Facing")), SocketTypeId=tables[table].Value(row,"SocketTypeId") }).ToArray();
            var catalog=new SpatialContentCatalog {
                Metadata=new SpatialContentExportMetadata { SchemaId=manifest.CatalogSchemaId, SchemaVersion=manifest.CatalogSchemaVersion, ContentVersion=manifest.ContentVersion },
                Floors=floors.Rows.Select(row=>new FloorSpatialConfiguration { FloorDefinitionId=floors.Value(row,"FloorDefinitionId"), FloorIndex=ParseInt(floors.Value(row,"FloorIndex")), Bounds=new RectangularFloorBounds(new TileCoordinate(ParseInt(floors.Value(row,"MinimumX")),ParseInt(floors.Value(row,"MinimumY"))),ParseInt(floors.Value(row,"Width")),ParseInt(floors.Value(row,"Height"))), FinalFloorSpaceCapacity=ParseInt(floors.Value(row,"FinalFloorSpaceCapacity")), OptionalBranchAllowance=ParseInt(floors.Value(row,"OptionalBranchAllowance")), EntranceStructureDefinitionId=floors.Value(row,"EntranceStructureDefinitionId"), CompletionStructureDefinitionId=floors.Value(row,"CompletionStructureDefinitionId"), AllowedRoomDefinitionIds=Children("floor_allowed_rooms",floors.Value(row,"FloorDefinitionId"),"RoomDefinitionId"), AllowedCorridorDefinitionIds=Children("floor_allowed_corridors",floors.Value(row,"FloorDefinitionId"),"CorridorDefinitionId") }).ToArray(),
                Rooms=rooms.Rows.Select(row=>new RoomSpatialDefinition { RoomDefinitionId=rooms.Value(row,"RoomDefinitionId"), GrossFootprint=new RectangularFootprintDefinition(ParseInt(rooms.Value(row,"Width")),ParseInt(rooms.Value(row,"Height"))), MaximumConnectionCount=ParseInt(rooms.Value(row,"MaximumConnectionCount")), MonsterCapacity=ParseInt(rooms.Value(row,"MonsterCapacity")), TrapCapacity=ParseInt(rooms.Value(row,"TrapCapacity")), LootCapacity=ParseInt(rooms.Value(row,"LootCapacity")), LocalizationKey=rooms.Value(row,"LocalizationKey"), AllowedOrientations=Children("room_orientations",rooms.Value(row,"RoomDefinitionId"),"Orientation").Select(ParseEnum<CardinalOrientation>).ToArray(), ReservedTileOffsets=Offsets("room_reserved_offsets",rooms.Value(row,"RoomDefinitionId")), ConnectionPoints=Points("room_connection_points",rooms.Value(row,"RoomDefinitionId")) }).ToArray(),
                Corridors=corridors.Rows.Select(row=>new CorridorSpatialDefinition { CorridorDefinitionId=corridors.Value(row,"CorridorDefinitionId"), LocalizationKey=corridors.Value(row,"LocalizationKey"), Category=ParseEnum<CorridorSpatialCategory>(corridors.Value(row,"Category")), MinimumLength=ParseInt(corridors.Value(row,"MinimumLength")), MaximumLength=ParseInt(corridors.Value(row,"MaximumLength")), Width=ParseInt(corridors.Value(row,"Width")), MonsterCapacity=ParseInt(corridors.Value(row,"MonsterCapacity")), TrapCapacity=ParseInt(corridors.Value(row,"TrapCapacity")), LootCapacity=ParseInt(corridors.Value(row,"LootCapacity")), AllowedOrientations=Children("corridor_orientations",corridors.Value(row,"CorridorDefinitionId"),"Orientation").Select(ParseEnum<CardinalOrientation>).ToArray(), CompatibleSocketTypeIds=Children("corridor_compatible_sockets",corridors.Value(row,"CorridorDefinitionId"),"SocketTypeId") }).ToArray(),
                FixedStructures=structures.Rows.Select(row=>new FixedSpatialStructureDefinition { StructureDefinitionId=structures.Value(row,"StructureDefinitionId"), LocalizationKey=structures.Value(row,"LocalizationKey"), Kind=ParseEnum<FixedSpatialStructureKind>(structures.Value(row,"Kind")), GrossFootprint=new RectangularFootprintDefinition(ParseInt(structures.Value(row,"Width")),ParseInt(structures.Value(row,"Height"))), MaximumConnectionCount=ParseInt(structures.Value(row,"MaximumConnectionCount")), AllowedOrientations=Children("fixed_structure_orientations",structures.Value(row,"StructureDefinitionId"),"Orientation").Select(ParseEnum<CardinalOrientation>).ToArray(), ReservedTileOffsets=Offsets("fixed_structure_reserved_offsets",structures.Value(row,"StructureDefinitionId")), ConnectionPoints=Points("fixed_structure_connection_points",structures.Value(row,"StructureDefinitionId")) }).ToArray(),
                SocketTypes=sockets.Rows.Select(row=>new SpatialSocketTypeDefinition { SocketTypeId=sockets.Value(row,"SocketTypeId"), CompatibleSocketTypeIds=Children("socket_compatibility",sockets.Value(row,"SocketTypeId"),"CompatibleSocketTypeId") }).ToArray() };
            return new DungeonSpatialAuthoringProjection { Catalog=catalog, English=new StringTable { schema=manifest.StringTableSchemaId, schemaVersion=manifest.StringTableSchemaVersion, language=manifest.RequiredLanguage, entries=localization.Rows.Select(row=>new StringEntry { key=localization.Value(row,"Key"), text=localization.Value(row,"Text") }).OrderBy(entry=>entry.key,StringComparer.Ordinal).ToArray() } };
        }

        private static bool ReadJson(DungeonSpatialAuthoringSource source,string path,DungeonSpatialAuthoringDiagnostic missing,List<DungeonSpatialAuthoringIssue> issues,out Dictionary<string,object> root)
        {
            root=null; if(!ReadText(source,path,missing,issues,out string text)) return false;
            if(!StrictJson.TryParse(text,out object value,out DungeonSpatialAuthoringDiagnostic error)) { Add(issues,error,path); return false; }
            root=value as Dictionary<string,object>; if(root==null) { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidJsonRoot,path); return false; }
            return true;
        }
        private static bool ReadText(DungeonSpatialAuthoringSource source,string path,DungeonSpatialAuthoringDiagnostic missing,List<DungeonSpatialAuthoringIssue> issues,out string text)
        {
            text=null; if(!source.TryGet(path,out byte[] bytes)||bytes==null) { Add(issues,missing,path); return false; }
            if(bytes.Length==0) { Add(issues,DungeonSpatialAuthoringDiagnostic.EmptyFile,path); return false; }
            if(bytes.Length>=3&&bytes[0]==0xef&&bytes[1]==0xbb&&bytes[2]==0xbf) { Add(issues,DungeonSpatialAuthoringDiagnostic.BomPresent,path); return false; }
            if(bytes.Contains((byte)'\r')) { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidLineEnding,path); return false; }
            if(bytes[bytes.Length-1]!=(byte)'\n'||(bytes.Length>1&&bytes[bytes.Length-2]==(byte)'\n')) { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidTrailingNewline,path); return false; }
            if(bytes.Any(value=>(value<32&&value!=10)||value==127)) { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidFormat,path); return false; }
            try { text=new UTF8Encoding(false,true).GetString(bytes); return true; }
            catch(DecoderFallbackException) { Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidUtf8,path); return false; }
        }
        private static void ValidateExactFields(Dictionary<string,object> root,IEnumerable<string> fields,string path,List<DungeonSpatialAuthoringIssue> issues,IEnumerable<string> optional=null)
        {
            string[] allowed=fields.ToArray(); var optionalSet=new HashSet<string>(optional??Array.Empty<string>(),StringComparer.Ordinal);
            foreach(string key in root.Keys) if(!allowed.Contains(key,StringComparer.Ordinal)) Add(issues,allowed.Any(field=>string.Equals(field,key,StringComparison.OrdinalIgnoreCase))?DungeonSpatialAuthoringDiagnostic.AmbiguousJsonField:DungeonSpatialAuthoringDiagnostic.UnknownJsonField,path,column:key);
            foreach(string field in allowed) if(!optionalSet.Contains(field)&&!root.ContainsKey(field)) Add(issues,DungeonSpatialAuthoringDiagnostic.MissingRequiredJsonField,path,column:field);
        }
        private static void ValidatePaths(string[] paths,List<DungeonSpatialAuthoringIssue> issues)
        {
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(string path in paths) { if(!seen.Add(path)) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidOrDuplicateTablePath,ManifestPath,column:path); if(!IsNormalizedPath(path)) Add(issues,DungeonSpatialAuthoringDiagnostic.InvalidOrDuplicateTablePath,ManifestPath,column:path); }
        }
        private static bool IsNormalizedPath(string path) => !string.IsNullOrWhiteSpace(path)&&!Path.IsPathRooted(path)&&!path.Contains("\\")&&!path.Split('/').Any(part=>part.Length==0||part=="."||part=="..")&&path==path.Trim();
        private static void CheckManifest(bool valid,string field,List<DungeonSpatialAuthoringIssue> issues) { if(!valid) Add(issues,DungeonSpatialAuthoringDiagnostic.ManifestValueMismatch,ManifestPath,column:field); }
        private static bool TryObject(Dictionary<string,object> root,string field,out Dictionary<string,object> value) { value=root.TryGetValue(field,out object item)?item as Dictionary<string,object>:null; return value!=null; }
        private static bool TryArray(Dictionary<string,object> root,string field,out List<object> value) { value=root.TryGetValue(field,out object item)?item as List<object>:null; return value!=null; }
        private static bool StringArray(Dictionary<string,object> root,string field,out string[] value) { value=null; if(!TryArray(root,field,out List<object> list)||list.Any(item=>!(item is string))) return false; value=list.Cast<string>().ToArray(); return true; }
        private static bool GetString(Dictionary<string,object> root,string field,out string value) { value=root.TryGetValue(field,out object item)?item as string:null; return value!=null; }
        private static bool GetBool(Dictionary<string,object> root,string field,out bool value) { value=false; if(!root.TryGetValue(field,out object item)||!(item is bool)) return false; value=(bool)item; return true; }
        private static int ToInt(object value) => value is long number&&number>=int.MinValue&&number<=int.MaxValue?(int)number:0;
        private static bool TryInt(string value,out int result,out bool overflow) { result=0; overflow=false; if(string.IsNullOrEmpty(value)||value[0]=='+'||(value.Length>1&&value[0]=='0')||value.StartsWith("-0",StringComparison.Ordinal)) return false; if(!long.TryParse(value,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out long number)) { overflow=value.All(c=>c=='-'||(c>='0'&&c<='9')); return false; } if(number<int.MinValue||number>int.MaxValue) { overflow=true; return false; } result=(int)number; return true; }
        private static int ParseInt(string value) => int.Parse(
            value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        private static T ParseEnum<T>(string value) where T:struct => (T)Enum.Parse(typeof(T),value,false);
        private static DungeonSpatialAuthoringResult Failed(List<DungeonSpatialAuthoringIssue> issues) => new DungeonSpatialAuthoringResult(null,issues);
        private static void Add(List<DungeonSpatialAuthoringIssue> issues,DungeonSpatialAuthoringDiagnostic diagnostic,string path="",string table="",string record="",string column="") => issues.Add(new DungeonSpatialAuthoringIssue { Diagnostic=diagnostic, RelativePath=path??"", TableId=table??"", RecordKey=record??"", Column=column??"" });
    }

    internal static class StrictCsv
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
