using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public readonly struct CanonicalSpatialSerializationLimits
    {
        public CanonicalSpatialSerializationLimits(SpatialSerializedInputLimits serialized, CanonicalSpatialSaveWorkloadLimits spatial)
        { Serialized=serialized; Spatial=spatial; }
        public SpatialSerializedInputLimits Serialized { get; }
        public CanonicalSpatialSaveWorkloadLimits Spatial { get; }
        public bool IsValid=>Serialized.IsValid&&Spatial.IsValid;
    }

    public static class CanonicalSpatialSaveSerializer
    {
        private static readonly Dictionary<Type,string[]> Fields=new Dictionary<Type,string[]>
        {
            {typeof(DetachedCanonicalSpatialSaveState),new[]{"Authority","Floors"}},
            {typeof(CanonicalSpatialAuthorityMarker),new[]{"CanonicalLayoutContractVersion","CreationKind","MigrationTransactionId","MigrationDescriptorFingerprint"}},
            {typeof(SavedSpatialFloor),new[]{"FloorInstanceId","FloorDefinitionId","FloorIndex","Layout","FixedStructures","RoomContents"}},
            {typeof(FloorSpatialLayout),new[]{"FloorId","Rooms","Nodes","Edges"}},
            {typeof(RoomSpatialInstance),new[]{"RoomInstanceId","RoomDefinitionId","FloorId","Anchor","Orientation"}},
            {typeof(TileCoordinate),new[]{"X","Y"}},
            {typeof(FloorRouteNode),new[]{"NodeId","FloorId","Kind","RoomInstanceId"}},
            {typeof(FloorRouteEdge),new[]{"EdgeId","CorridorDefinitionId","FloorId","SourceNodeId","DestinationNodeId","Footprint","Classification","OptionalBranchId","ConnectionKind"}},
            {typeof(ResolvedTileFootprint),new[]{"OccupiedTiles"}},
            {typeof(SavedFixedSpatialStructure),new[]{"FixedStructureInstanceId","FixedStructureDefinitionId","FloorInstanceId","Anchor","Orientation","Kind"}},
            {typeof(FloorRoomContentState),new[]{"Assignments","RoomSemantics","NextSequence"}},
            {typeof(RoomContentAssignment),new[]{"AssignmentId","RoomInstanceId","CategoryId","OptionId","Sequence"}},
            {typeof(CanonicalRoomSemantics),new[]{"RoomInstanceId","LegacyRoomOriginKind"}}
        };

        public static SpatialContractResult<byte[]> Serialize(DetachedCanonicalSpatialSaveState source, CanonicalSpatialSerializationLimits limits)
        {
            var issues=new List<SpatialContractIssue>();
            if(!limits.IsValid){issues.Add(SpatialContractIssue.InvalidLimits);return new SpatialContractResult<byte[]>(null,issues);}
            if(!CanonicalSpatialSaveContracts.TryCanonicalize(source,limits.Spatial,out DetachedCanonicalSpatialSaveState canonical))issues.Add(SpatialContractIssue.StructuralValidationFailed);
            else if(!CanonicalSpatialSaveContracts.Validate(canonical,limits.Spatial,true).IsValid)issues.Add(SpatialContractIssue.StructuralValidationFailed);
            if(issues.Count!=0)return new SpatialContractResult<byte[]>(null,issues);
            var b=new StringBuilder();Write(b,canonical,typeof(DetachedCanonicalSpatialSaveState));byte[] bytes=ContractJson.Bytes(b.ToString());
            if(bytes.Length>limits.Serialized.MaximumInputBytes)issues.Add(SpatialContractIssue.InputByteLimitExceeded);
            return new SpatialContractResult<byte[]>(issues.Count==0?bytes:null,issues);
        }

        public static SpatialContractResult<DetachedCanonicalSpatialSaveState> Parse(byte[] bytes, CanonicalSpatialSerializationLimits limits)
        {
            var issues=new List<SpatialContractIssue>();if(!limits.IsValid){issues.Add(SpatialContractIssue.InvalidLimits);return new SpatialContractResult<DetachedCanonicalSpatialSaveState>(null,issues);}
            if(!ContractJson.TryParse(bytes,limits.Serialized,issues,out ContractJsonNode root))return new SpatialContractResult<DetachedCanonicalSpatialSaveState>(null,issues);
            ValidateNode(root,typeof(DetachedCanonicalSpatialSaveState),issues);
            if(issues.Count!=0)return new SpatialContractResult<DetachedCanonicalSpatialSaveState>(null,issues);
            DetachedCanonicalSpatialSaveState value;
            try{value=JsonUtility.FromJson<DetachedCanonicalSpatialSaveState>(Encoding.UTF8.GetString(bytes));}catch{issues.Add(SpatialContractIssue.MalformedJson);return new SpatialContractResult<DetachedCanonicalSpatialSaveState>(null,issues);}
            if(!CanonicalSpatialSaveContracts.Validate(value,limits.Spatial,true).IsValid)issues.Add(SpatialContractIssue.StructuralValidationFailed);
            if(issues.Count==0){SpatialContractResult<byte[]> again=Serialize(value,limits);if(!again.IsValid||!Equal(bytes,again.Value))issues.Add(SpatialContractIssue.NonCanonicalBytes);}
            return new SpatialContractResult<DetachedCanonicalSpatialSaveState>(issues.Count==0?value:null,issues);
        }

        private static void Write(StringBuilder b,object value,Type type)
        {
            if(value==null){b.Append("null");return;}if(type==typeof(string)){ContractJson.String(b,(string)value);return;}
            if(type.IsEnum){b.Append(Convert.ToInt32(value,CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));return;}
            if(type==typeof(int)||type==typeof(long)){b.Append(Convert.ToString(value,CultureInfo.InvariantCulture));return;}
            if(type.IsArray){b.Append('[');int i=0;foreach(object item in (IEnumerable)value){if(i++>0)b.Append(',');Write(b,item,type.GetElementType());}b.Append(']');return;}
            b.Append('{');string[] names=Fields[type];for(int i=0;i<names.Length;i++){if(i>0)b.Append(',');ContractJson.String(b,names[i]);b.Append(':');FieldInfo field=type.GetField(names[i]);Write(b,field.GetValue(value),field.FieldType);}b.Append('}');
        }
        private static void ValidateNode(ContractJsonNode n,Type type,IList<SpatialContractIssue> issues)
        {
            if(type==typeof(string)){if(n.Kind!=ContractJsonKind.String)issues.Add(SpatialContractIssue.WrongFieldType);return;}
            if(type==typeof(int)||type==typeof(long)||type.IsEnum){if(n.Kind!=ContractJsonKind.Number){issues.Add(SpatialContractIssue.WrongFieldType);return;}int iv=0;long lv;bool parsed=type==typeof(long)?long.TryParse(n.Text,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out lv):int.TryParse(n.Text,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out iv);if(!parsed){issues.Add(SpatialContractIssue.IntegerOverflow);return;}if(type.IsEnum&&!Enum.IsDefined(type,iv))issues.Add(SpatialContractIssue.UndefinedEnum);return;}
            if(type.IsArray){if(n.Kind!=ContractJsonKind.Array){issues.Add(SpatialContractIssue.WrongFieldType);return;}foreach(ContractJsonNode item in n.Items)ValidateNode(item,type.GetElementType(),issues);return;}
            if(n.Kind==ContractJsonKind.Null)return;string[] names=Fields[type];if(!ContractJson.Shape(n,names,issues))return;for(int i=0;i<names.Length;i++)ValidateNode(ContractJson.Field(n,i),type.GetField(names[i]).FieldType,issues);
        }
        private static bool Equal(byte[] a,byte[] b){if(a==null||b==null||a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    }
}
