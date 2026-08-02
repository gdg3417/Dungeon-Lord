using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public static class SpatialMigrationContractIdentity
    {
        public const string CanonicalSerializerId = "gd66.serializer.canonical_spatial_save";
        public const int CanonicalSerializerVersion = 1;
        public const int AuthorityMarkerContractVersion = 1;
        public const int MigrationContractVersion = 1;
        public const int JournalSchemaVersion = 1;
    }

    public readonly struct SpatialSerializedInputLimits
    {
        public SpatialSerializedInputLimits(int maximumInputBytes, int maximumParsedNodes,
            int maximumCollectionRecords, int maximumStringCharacters, int maximumDiagnostics)
        { MaximumInputBytes=maximumInputBytes; MaximumParsedNodes=maximumParsedNodes; MaximumCollectionRecords=maximumCollectionRecords; MaximumStringCharacters=maximumStringCharacters; MaximumDiagnostics=maximumDiagnostics; }
        public int MaximumInputBytes { get; }
        public int MaximumParsedNodes { get; }
        public int MaximumCollectionRecords { get; }
        public int MaximumStringCharacters { get; }
        public int MaximumDiagnostics { get; }
        public bool IsValid => MaximumInputBytes>0 && MaximumParsedNodes>0 && MaximumCollectionRecords>=0 && MaximumStringCharacters>=0 && MaximumDiagnostics>0;
    }

    public enum SpatialContractIssue
    {
        InvalidLimits=1, InputByteLimitExceeded=2, InvalidUtf8=3, BomPresent=4, LeadingOrTrailingWhitespace=5,
        MalformedJson=6, UnknownField=7, DuplicateField=8, CaseAmbiguousField=9, WrongFieldOrder=10,
        WrongFieldType=11, UnsupportedNumber=12, IntegerOverflow=13, UndefinedEnum=14, WorkloadExceeded=15,
        InvalidField=16, InvalidStableId=17, InvalidHash=18, InvalidIdentity=19, InvalidPath=20,
        InvalidStage=21, InvalidStageData=22, NonCanonicalBytes=23, StructuralValidationFailed=24
    }

    public sealed class SpatialContractResult<T>
    {
        internal SpatialContractResult(T value, IList<SpatialContractIssue> issues) { Value=value; Issues=new List<SpatialContractIssue>(issues).ToArray(); }
        public T Value { get; }
        public SpatialContractIssue[] Issues { get; }
        public bool IsValid => Issues.Length==0;
    }

    public static class SpatialContractSha256
    {
        public static string Compute(byte[] bytes)
        {
            if (bytes==null) throw new ArgumentNullException(nameof(bytes));
            using (SHA256 hash=SHA256.Create())
            {
                byte[] digest=hash.ComputeHash(bytes); var text=new StringBuilder(64);
                foreach(byte value in digest) text.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return text.ToString();
            }
        }
        public static bool IsCanonical(string value)
        {
            if (value==null || value.Length!=64) return false;
            for(int i=0;i<value.Length;i++) if (!((value[i]>='0'&&value[i]<='9')||(value[i]>='a'&&value[i]<='f'))) return false;
            return true;
        }
        public static bool IsStableId(string value)
        {
            if (string.IsNullOrEmpty(value)) return false; bool separator=true;
            foreach(char c in value)
            {
                bool alpha=c>='a'&&c<='z', digit=c>='0'&&c<='9';
                if(alpha||digit) separator=false;
                else if((c=='.'||c=='_'||c=='-')&&!separator) separator=true;
                else return false;
            }
            return !separator;
        }
    }

    internal enum ContractJsonKind { Object, Array, String, Number, Null }
    internal sealed class ContractJsonNode { internal ContractJsonKind Kind; internal string Text; internal List<KeyValuePair<string,ContractJsonNode>> Fields; internal List<ContractJsonNode> Items; }
    internal static class ContractJson
    {
        private static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
        internal static byte[] Bytes(string text)=>Utf8.GetBytes(text);
        internal static void String(StringBuilder b,string value)
        {
            b.Append('"'); foreach(char c in value??string.Empty) switch(c) { case '"':b.Append("\\\"");break; case '\\':b.Append("\\\\");break; case '\b':b.Append("\\b");break; case '\f':b.Append("\\f");break; case '\n':b.Append("\\n");break; case '\r':b.Append("\\r");break; case '\t':b.Append("\\t");break; default: if(c<32)b.Append("\\u").Append(((int)c).ToString("x4",CultureInfo.InvariantCulture));else b.Append(c);break;} b.Append('"');
        }
        internal static bool TryParse(byte[] bytes, SpatialSerializedInputLimits limits, IList<SpatialContractIssue> issues, out ContractJsonNode node)
        {
            node=null; if(!limits.IsValid){issues.Add(SpatialContractIssue.InvalidLimits);return false;} if(bytes==null||bytes.Length>limits.MaximumInputBytes){issues.Add(SpatialContractIssue.InputByteLimitExceeded);return false;}
            if(bytes.Length>=3&&bytes[0]==0xef&&bytes[1]==0xbb&&bytes[2]==0xbf){issues.Add(SpatialContractIssue.BomPresent);return false;}
            string text; try{text=Utf8.GetString(bytes);}catch(DecoderFallbackException){issues.Add(SpatialContractIssue.InvalidUtf8);return false;}
            if(text.Length==0||char.IsWhiteSpace(text[0])||char.IsWhiteSpace(text[text.Length-1])){issues.Add(SpatialContractIssue.LeadingOrTrailingWhitespace);return false;}
            try { var reader=new Reader(text,limits); node=reader.Read(); return true; } catch(BudgetException){issues.Add(SpatialContractIssue.WorkloadExceeded);} catch(DuplicateException){issues.Add(SpatialContractIssue.DuplicateField);} catch(UnsupportedException){issues.Add(SpatialContractIssue.UnsupportedNumber);} catch{issues.Add(SpatialContractIssue.MalformedJson);} return false;
        }
        private sealed class BudgetException:Exception{}
        private sealed class Reader
        {
            readonly string s; readonly SpatialSerializedInputLimits l; int p,nodes,records,chars;
            internal Reader(string text,SpatialSerializedInputLimits limits){s=text;l=limits;}
            internal ContractJsonNode Read(){var n=Value();if(p!=s.Length)throw new FormatException();return n;}
            ContractJsonNode Value(){if(++nodes>l.MaximumParsedNodes)throw new BudgetException();if(p>=s.Length)throw new FormatException();char c=s[p];if(c=='{')return Object();if(c=='[')return Array();if(c=='"')return new ContractJsonNode{Kind=ContractJsonKind.String,Text=ReadString(true)};if(c=='n'&&p+4<=s.Length&&s.Substring(p,4)=="null"){p+=4;return new ContractJsonNode{Kind=ContractJsonKind.Null};}return Number();}
            ContractJsonNode Object(){p++;var f=new List<KeyValuePair<string,ContractJsonNode>>();var names=new HashSet<string>(StringComparer.Ordinal);if(Take('}'))return new ContractJsonNode{Kind=ContractJsonKind.Object,Fields=f};while(true){string k=ReadString(false);if(!names.Add(k))throw new DuplicateException();Need(':');f.Add(new KeyValuePair<string,ContractJsonNode>(k,Value()));if(Take('}'))break;Need(',');}return new ContractJsonNode{Kind=ContractJsonKind.Object,Fields=f};}
            ContractJsonNode Array(){p++;var a=new List<ContractJsonNode>();if(Take(']'))return new ContractJsonNode{Kind=ContractJsonKind.Array,Items=a};while(true){if(++records>l.MaximumCollectionRecords)throw new BudgetException();a.Add(Value());if(Take(']'))break;Need(',');}return new ContractJsonNode{Kind=ContractJsonKind.Array,Items=a};}
            ContractJsonNode Number(){int start=p;if(Take('-')){} if(p>=s.Length||s[p]<'0'||s[p]>'9')throw new FormatException();if(s[p]=='0'&&p+1<s.Length&&char.IsDigit(s[p+1]))throw new FormatException();while(p<s.Length&&char.IsDigit(s[p]))p++;if(p<s.Length&&(s[p]=='.'||s[p]=='e'||s[p]=='E'||s[p]=='+'))throw new UnsupportedException();return new ContractJsonNode{Kind=ContractJsonKind.Number,Text=s.Substring(start,p-start)};}
            string ReadString(bool count){Need('"');var b=new StringBuilder();while(p<s.Length){char c=s[p++];if(c=='"')return b.ToString();if(c<32)throw new FormatException();if(c=='\\'){if(p>=s.Length)throw new FormatException();char e=s[p++];if(e=='"'||e=='\\'||e=='/')c=e;else if(e=='b')c='\b';else if(e=='f')c='\f';else if(e=='n')c='\n';else if(e=='r')c='\r';else if(e=='t')c='\t';else if(e=='u'){if(p+4>s.Length)throw new FormatException();c=(char)int.Parse(s.Substring(p,4),NumberStyles.HexNumber,CultureInfo.InvariantCulture);p+=4;}else throw new FormatException();}if(count&&++chars>l.MaximumStringCharacters)throw new BudgetException();b.Append(c);}throw new FormatException();}
            bool Take(char c){if(p<s.Length&&s[p]==c){p++;return true;}return false;}void Need(char c){if(!Take(c))throw new FormatException();}
        }
        internal sealed class DuplicateException:Exception{} internal sealed class UnsupportedException:Exception{}
        internal static bool Shape(ContractJsonNode node,string[] fields,IList<SpatialContractIssue> issues)
        {
            if(node==null||node.Kind!=ContractJsonKind.Object){issues.Add(SpatialContractIssue.WrongFieldType);return false;} bool ok=true;
            if(node.Fields.Count!=fields.Length){ok=false;}
            for(int i=0;i<node.Fields.Count;i++){string name=node.Fields[i].Key;int exact=Array.IndexOf(fields,name);if(exact<0){bool amb=false;foreach(string f in fields)if(string.Equals(f,name,StringComparison.OrdinalIgnoreCase))amb=true;issues.Add(amb?SpatialContractIssue.CaseAmbiguousField:SpatialContractIssue.UnknownField);ok=false;}else if(exact!=i){issues.Add(SpatialContractIssue.WrongFieldOrder);ok=false;}}
            if(node.Fields.Count!=fields.Length)issues.Add(SpatialContractIssue.InvalidField); return ok;
        }
        internal static string Compact(ContractJsonNode node) { var b=new StringBuilder(); Append(b,node); return b.ToString(); }
        private static void Append(StringBuilder b,ContractJsonNode n) { if(n.Kind==ContractJsonKind.Object){b.Append('{');for(int i=0;i<n.Fields.Count;i++){if(i>0)b.Append(',');String(b,n.Fields[i].Key);b.Append(':');Append(b,n.Fields[i].Value);}b.Append('}');}else if(n.Kind==ContractJsonKind.Array){b.Append('[');for(int i=0;i<n.Items.Count;i++){if(i>0)b.Append(',');Append(b,n.Items[i]);}b.Append(']');}else if(n.Kind==ContractJsonKind.String)String(b,n.Text);else if(n.Kind==ContractJsonKind.Number)b.Append(n.Text);else b.Append("null"); }
        internal static ContractJsonNode Field(ContractJsonNode n,int i)=>n.Fields[i].Value;
        internal static bool Int(ContractJsonNode n,out int v){v=0;return n.Kind==ContractJsonKind.Number&&int.TryParse(n.Text,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out v);}
        internal static bool Long(ContractJsonNode n,out long v){v=0;return n.Kind==ContractJsonKind.Number&&long.TryParse(n.Text,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out v);}
        internal static bool Str(ContractJsonNode n,out string v){v=n.Kind==ContractJsonKind.String?n.Text:null;return v!=null;}
    }
}
