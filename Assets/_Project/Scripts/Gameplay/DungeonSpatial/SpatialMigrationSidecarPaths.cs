using System;
using System.IO;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public sealed class SpatialMigrationSidecarNames
    {
        internal SpatialMigrationSidecarNames(string journal,string backup,string candidate,string receipt){Journal=journal;OriginalBackup=backup;CandidateStaging=candidate;FinalizedReceipt=receipt;}
        public string Journal{get;} public string OriginalBackup{get;} public string CandidateStaging{get;} public string FinalizedReceipt{get;}
    }
    public static class SpatialMigrationSidecarPaths
    {
        public const int MaximumStemCharacters=80; public const int MaximumGeneratedFilenameCharacters=180; public const int WindowsMaximumAbsolutePathCharacters=240;
        public static SpatialContractResult<SpatialMigrationSidecarNames> Derive(string activeFilename,string transactionId)
        {
            var issues=new System.Collections.Generic.List<SpatialContractIssue>();if(!ValidRelative(activeFilename,MaximumGeneratedFilenameCharacters)||Path.GetFileName(activeFilename)!=activeFilename)issues.Add(SpatialContractIssue.InvalidPath);if(!SpatialMigrationTransactionIdentity.IsCanonicalTransactionId(transactionId))issues.Add(SpatialContractIssue.InvalidIdentity);
            string stem=issues.Count==0?Path.GetFileNameWithoutExtension(activeFilename):string.Empty;if(stem.Length==0||stem.Length>MaximumStemCharacters)issues.Add(SpatialContractIssue.InvalidPath);if(issues.Count!=0)return new SpatialContractResult<SpatialMigrationSidecarNames>(null,issues);
            var n=new SpatialMigrationSidecarNames(stem+"."+transactionId+".journal.json",stem+"."+transactionId+".original.bak",stem+"."+transactionId+".candidate.tmp",stem+"."+transactionId+".finalized");
            if(!ValidRelative(n.Journal,MaximumGeneratedFilenameCharacters)||!ValidRelative(n.OriginalBackup,MaximumGeneratedFilenameCharacters)||!ValidRelative(n.CandidateStaging,MaximumGeneratedFilenameCharacters)||!ValidRelative(n.FinalizedReceipt,MaximumGeneratedFilenameCharacters))issues.Add(SpatialContractIssue.InvalidPath);
            return new SpatialContractResult<SpatialMigrationSidecarNames>(issues.Count==0?n:null,issues);
        }
        public static bool TryResolveContained(string saveDirectory,string relativeFilename,int maximumAbsolutePathCharacters,out string absolutePath)
        {
            absolutePath=null;if(string.IsNullOrEmpty(saveDirectory)||maximumAbsolutePathCharacters<=0||!ValidRelative(relativeFilename,MaximumGeneratedFilenameCharacters))return false;
            try{string directory=Path.GetFullPath(saveDirectory);string combined=Path.Combine(directory,relativeFilename);string normalized=Path.GetFullPath(combined);if(!string.Equals(combined,normalized,StringComparison.Ordinal))return false;string prefix=directory.EndsWith(Path.DirectorySeparatorChar.ToString(),StringComparison.Ordinal)?directory:directory+Path.DirectorySeparatorChar;if(!normalized.StartsWith(prefix,StringComparison.Ordinal)||normalized.Length>maximumAbsolutePathCharacters)return false;absolutePath=normalized;return true;}catch{return false;}
        }
        public static bool ValidRelative(string value,int maximumCharacters)
        {
            if(string.IsNullOrEmpty(value)||value.Length>maximumCharacters||value=="."||value==".."||value.IndexOf('/')>=0||value.IndexOf('\\')>=0||value.IndexOf(":",StringComparison.Ordinal)>=0||Uri.IsWellFormedUriString(value,UriKind.Absolute)||Path.IsPathRooted(value))return false;
            try{return string.Equals(Path.GetFileName(value),value,StringComparison.Ordinal)&&string.Equals(Path.GetFullPath(value),Path.Combine(Path.GetFullPath("."),value),StringComparison.Ordinal);}catch{return false;}
        }
    }
}
