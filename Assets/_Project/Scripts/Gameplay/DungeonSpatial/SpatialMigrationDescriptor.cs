using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum SpatialRawEnvelopeClassification { WrappedSaveRoot=1, UnwrappedSaveData=2 }

    [Serializable]
    public sealed class SpatialValidationInputHash
    {
        public SpatialValidationInputHash(string inputId,string sha256){InputId=inputId;Sha256=sha256;}
        public string InputId { get; }
        public string Sha256 { get; }
    }

    [Serializable]
    public sealed class SpatialMigrationInputDescriptor
    {
        public SpatialMigrationInputDescriptor(string originalPayloadSha256,int rawSourceSchemaVersion,
            SpatialRawEnvelopeClassification rawEnvelopeClassification,int selectedTargetSchemaVersion,
            int authorityMarkerContractVersion,int migrationContractVersion,string migrationProfileId,
            int migrationProfileVersion,string migrationProfileCanonicalHash,string sharedGeometryId,
            int sharedGeometryVersion,string sharedGeometryCanonicalHash,string productionManifestSha256,
            string productionCatalogSha256,IEnumerable<SpatialValidationInputHash> validationInputHashes,
            string legacyGameplayConfigurationSha256,string canonicalSerializerId,int canonicalSerializerVersion)
        {
            OriginalPayloadSha256=originalPayloadSha256;RawSourceSchemaVersion=rawSourceSchemaVersion;RawEnvelopeClassification=rawEnvelopeClassification;
            SelectedTargetSchemaVersion=selectedTargetSchemaVersion;AuthorityMarkerContractVersion=authorityMarkerContractVersion;MigrationContractVersion=migrationContractVersion;
            MigrationProfileId=migrationProfileId;MigrationProfileVersion=migrationProfileVersion;MigrationProfileCanonicalHash=migrationProfileCanonicalHash;
            SharedGeometryId=sharedGeometryId;SharedGeometryVersion=sharedGeometryVersion;SharedGeometryCanonicalHash=sharedGeometryCanonicalHash;
            ProductionManifestSha256=productionManifestSha256;ProductionCatalogSha256=productionCatalogSha256;
            ValidationInputHashes=(validationInputHashes??Enumerable.Empty<SpatialValidationInputHash>()).OrderBy(x=>x==null?null:x.InputId,StringComparer.Ordinal).ToArray();
            LegacyGameplayConfigurationSha256=legacyGameplayConfigurationSha256;CanonicalSerializerId=canonicalSerializerId;CanonicalSerializerVersion=canonicalSerializerVersion;
        }
        public string OriginalPayloadSha256{get;} public int RawSourceSchemaVersion{get;} public SpatialRawEnvelopeClassification RawEnvelopeClassification{get;}
        public int SelectedTargetSchemaVersion{get;} public int AuthorityMarkerContractVersion{get;} public int MigrationContractVersion{get;}
        public string MigrationProfileId{get;} public int MigrationProfileVersion{get;} public string MigrationProfileCanonicalHash{get;}
        public string SharedGeometryId{get;} public int SharedGeometryVersion{get;} public string SharedGeometryCanonicalHash{get;}
        public string ProductionManifestSha256{get;} public string ProductionCatalogSha256{get;} public SpatialValidationInputHash[] ValidationInputHashes{get;}
        public string LegacyGameplayConfigurationSha256{get;} public string CanonicalSerializerId{get;} public int CanonicalSerializerVersion{get;}
    }

    public static class SpatialMigrationDescriptorContracts
    {
        private static readonly string[] Names={"OriginalPayloadSha256","RawSourceSchemaVersion","RawEnvelopeClassification","SelectedTargetSchemaVersion","AuthorityMarkerContractVersion","MigrationContractVersion","MigrationProfileId","MigrationProfileVersion","MigrationProfileCanonicalHash","SharedGeometryId","SharedGeometryVersion","SharedGeometryCanonicalHash","ProductionManifestSha256","ProductionCatalogSha256","ValidationInputHashes","LegacyGameplayConfigurationSha256","CanonicalSerializerId","CanonicalSerializerVersion"};
        private static readonly string[] HashNames={"InputId","Sha256"};
        public static SpatialContractResult<byte[]> Serialize(SpatialMigrationInputDescriptor value,SpatialSerializedInputLimits limits)
        {
            var issues=new List<SpatialContractIssue>();if(!limits.IsValid)issues.Add(SpatialContractIssue.InvalidLimits);Validate(value,issues);
            if(issues.Count!=0)return new SpatialContractResult<byte[]>(null,issues);var b=new StringBuilder();b.Append('{');
            AddS(b,Names[0],value.OriginalPayloadSha256,true);AddI(b,Names[1],value.RawSourceSchemaVersion);AddI(b,Names[2],(int)value.RawEnvelopeClassification);AddI(b,Names[3],value.SelectedTargetSchemaVersion);AddI(b,Names[4],value.AuthorityMarkerContractVersion);AddI(b,Names[5],value.MigrationContractVersion);AddS(b,Names[6],value.MigrationProfileId);AddI(b,Names[7],value.MigrationProfileVersion);AddS(b,Names[8],value.MigrationProfileCanonicalHash);AddS(b,Names[9],value.SharedGeometryId);AddI(b,Names[10],value.SharedGeometryVersion);AddS(b,Names[11],value.SharedGeometryCanonicalHash);AddS(b,Names[12],value.ProductionManifestSha256);AddS(b,Names[13],value.ProductionCatalogSha256);
            b.Append(',');ContractJson.String(b,Names[14]);b.Append(":");b.Append('[');for(int i=0;i<value.ValidationInputHashes.Length;i++){if(i>0)b.Append(',');b.Append('{');AddS(b,HashNames[0],value.ValidationInputHashes[i].InputId,true);AddS(b,HashNames[1],value.ValidationInputHashes[i].Sha256);b.Append('}');}b.Append(']');AddS(b,Names[15],value.LegacyGameplayConfigurationSha256);AddS(b,Names[16],value.CanonicalSerializerId);AddI(b,Names[17],value.CanonicalSerializerVersion);b.Append('}');byte[] bytes=ContractJson.Bytes(b.ToString());if(bytes.Length>limits.MaximumInputBytes)issues.Add(SpatialContractIssue.InputByteLimitExceeded);return new SpatialContractResult<byte[]>(issues.Count==0?bytes:null,issues);
        }
        public static SpatialContractResult<SpatialMigrationInputDescriptor> Parse(byte[] bytes,SpatialSerializedInputLimits limits)
        {
            var issues=new List<SpatialContractIssue>();if(!ContractJson.TryParse(bytes,limits,issues,out ContractJsonNode n))return new SpatialContractResult<SpatialMigrationInputDescriptor>(null,issues);if(!ContractJson.Shape(n,Names,issues))return new SpatialContractResult<SpatialMigrationInputDescriptor>(null,issues);
            var ss=new string[18];var ii=new int[18];int[] intAt={1,2,3,4,5,7,10,17};foreach(int x in intAt)if(!ContractJson.Int(ContractJson.Field(n,x),out ii[x]))issues.Add(SpatialContractIssue.WrongFieldType);int[] strAt={0,6,8,9,11,12,13,15,16};foreach(int x in strAt)if(!ContractJson.Str(ContractJson.Field(n,x),out ss[x]))issues.Add(SpatialContractIssue.WrongFieldType);
            var hashes=new List<SpatialValidationInputHash>();ContractJsonNode array=ContractJson.Field(n,14);if(array.Kind!=ContractJsonKind.Array)issues.Add(SpatialContractIssue.WrongFieldType);else foreach(ContractJsonNode item in array.Items){if(!ContractJson.Shape(item,HashNames,issues))continue;if(!ContractJson.Str(ContractJson.Field(item,0),out string id)||!ContractJson.Str(ContractJson.Field(item,1),out string hash))issues.Add(SpatialContractIssue.WrongFieldType);else hashes.Add(new SpatialValidationInputHash(id,hash));}
            if(issues.Count!=0)return new SpatialContractResult<SpatialMigrationInputDescriptor>(null,issues);var d=new SpatialMigrationInputDescriptor(ss[0],ii[1],(SpatialRawEnvelopeClassification)ii[2],ii[3],ii[4],ii[5],ss[6],ii[7],ss[8],ss[9],ii[10],ss[11],ss[12],ss[13],hashes,ss[15],ss[16],ii[17]);Validate(d,issues);if(issues.Count==0){var again=Serialize(d,limits);if(!again.IsValid||!bytes.SequenceEqual(again.Value))issues.Add(SpatialContractIssue.NonCanonicalBytes);}return new SpatialContractResult<SpatialMigrationInputDescriptor>(issues.Count==0?d:null,issues);
        }
        public static string ComputeInputFingerprint(SpatialMigrationInputDescriptor value,SpatialSerializedInputLimits limits){var r=Serialize(value,limits);if(!r.IsValid)throw new ArgumentException(nameof(value));return SpatialContractSha256.Compute(r.Value);}
        private static void Validate(SpatialMigrationInputDescriptor d,IList<SpatialContractIssue> x)
        {
            if(d==null){x.Add(SpatialContractIssue.InvalidField);return;}string[] hashes={d.OriginalPayloadSha256,d.MigrationProfileCanonicalHash,d.SharedGeometryCanonicalHash,d.ProductionManifestSha256,d.ProductionCatalogSha256,d.LegacyGameplayConfigurationSha256};foreach(string h in hashes)if(!SpatialContractSha256.IsCanonical(h))x.Add(SpatialContractIssue.InvalidHash);
            if(d.RawSourceSchemaVersion<1||d.SelectedTargetSchemaVersion<1||d.AuthorityMarkerContractVersion!=SpatialMigrationContractIdentity.AuthorityMarkerContractVersion||d.MigrationContractVersion!=SpatialMigrationContractIdentity.MigrationContractVersion||d.MigrationProfileVersion<1||d.SharedGeometryVersion<1||d.CanonicalSerializerVersion!=SpatialMigrationContractIdentity.CanonicalSerializerVersion||!Enum.IsDefined(typeof(SpatialRawEnvelopeClassification),d.RawEnvelopeClassification))x.Add(SpatialContractIssue.InvalidIdentity);
            if(!SpatialContractSha256.IsStableId(d.MigrationProfileId)||!SpatialContractSha256.IsStableId(d.SharedGeometryId)||!SpatialContractSha256.IsStableId(d.CanonicalSerializerId)||!string.Equals(d.CanonicalSerializerId,SpatialMigrationContractIdentity.CanonicalSerializerId,StringComparison.Ordinal))x.Add(SpatialContractIssue.InvalidStableId);
            var ids=new HashSet<string>(StringComparer.Ordinal);foreach(var h in d.ValidationInputHashes??Array.Empty<SpatialValidationInputHash>())if(h==null||!SpatialContractSha256.IsStableId(h.InputId)||!SpatialContractSha256.IsCanonical(h.Sha256)||!ids.Add(h.InputId))x.Add(SpatialContractIssue.InvalidField);
        }
        private static void AddS(StringBuilder b,string n,string v,bool first=false){if(!first)b.Append(',');ContractJson.String(b,n);b.Append(':');ContractJson.String(b,v);}private static void AddI(StringBuilder b,string n,int v){b.Append(',');ContractJson.String(b,n);b.Append(':').Append(v.ToString(CultureInfo.InvariantCulture));}
    }

    public static class SpatialMigrationTransactionIdentity
    {
        public const string TransactionIdPrefix="gd66-";
        public static byte[] CanonicalIdentityBytes(string original,string fingerprint){if(!SpatialContractSha256.IsCanonical(original)||!SpatialContractSha256.IsCanonical(fingerprint))throw new ArgumentException();var b=new StringBuilder("{\"OriginalPayloadSha256\":\"");b.Append(original).Append("\",\"InputFingerprintSha256\":\"").Append(fingerprint).Append("\"}");return ContractJson.Bytes(b.ToString());}
        public static string ComputeIdentity(string original,string fingerprint)=>SpatialContractSha256.Compute(CanonicalIdentityBytes(original,fingerprint));
        public static string CreateTransactionId(string identity){if(!SpatialContractSha256.IsCanonical(identity))throw new ArgumentException(nameof(identity));return TransactionIdPrefix+identity;}
        public static bool IsCanonicalTransactionId(string value)=>value!=null&&value.Length==69&&value.StartsWith(TransactionIdPrefix,StringComparison.Ordinal)&&SpatialContractSha256.IsCanonical(value.Substring(5));
    }
}
