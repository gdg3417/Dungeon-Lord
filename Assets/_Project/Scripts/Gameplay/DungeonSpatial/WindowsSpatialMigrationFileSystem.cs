using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using UnityEngine;

namespace DungeonBuilder.M0.Gameplay.DungeonSpatial
{
    public enum SpatialMigrationPlatform { Unsupported, WindowsEditor, WindowsStandalone }

    public static class SpatialMigrationCapabilityReason
    {
        public const string Ready = "gd66.preflight.ready";
        public const string PlatformUnsupported = "gd66.preflight.platform_unsupported";
        public const string PathInvalid = "gd66.preflight.path_invalid";
        public const string PathRedirected = "gd66.preflight.path_redirected";
        public const string VolumeUnsupported = "gd66.preflight.volume_unsupported";
        public const string NativeProbeFailed = "gd66.preflight.native_probe_failed";
    }

    public sealed class SpatialMigrationActivationPreflight
    {
        internal SpatialMigrationActivationPreflight(bool supported, string reason,
            SpatialMigrationPlatform platform, ISpatialMigrationFileSystem fileSystem,
            string qualifiedActiveSavePath = null)
        { IsSupported = supported; Reason = reason; Platform = platform; FileSystem = fileSystem;
          QualifiedActiveSavePath = supported ? qualifiedActiveSavePath : null; }
        public bool IsSupported { get; }
        public string Reason { get; }
        public SpatialMigrationPlatform Platform { get; }
        public ISpatialMigrationFileSystem FileSystem { get; }
        public string QualifiedActiveSavePath { get; }
    }

    public static class SpatialMigrationFileSystemSelector
    {
        public static SpatialMigrationActivationPreflight Evaluate(string activeSavePath)
        {
            RuntimePlatform runtime = Application.platform;
            SpatialMigrationPlatform platform = runtime == RuntimePlatform.WindowsEditor
                ? SpatialMigrationPlatform.WindowsEditor
                : runtime == RuntimePlatform.WindowsPlayer ? SpatialMigrationPlatform.WindowsStandalone
                : SpatialMigrationPlatform.Unsupported;
            return Evaluate(platform, activeSavePath);
        }

        public static SpatialMigrationActivationPreflight Evaluate(SpatialMigrationPlatform platform,
            string activeSavePath)
        {
            var fileSystem = new WindowsSpatialMigrationFileSystem();
            return Evaluate(platform, activeSavePath, fileSystem, fileSystem);
        }

        internal static SpatialMigrationActivationPreflight Evaluate(SpatialMigrationPlatform platform,
            string activeSavePath, IWindowsSpatialMigrationCapabilityProbe probe,
            ISpatialMigrationFileSystem fileSystem)
        {
            if (platform != SpatialMigrationPlatform.WindowsEditor &&
                platform != SpatialMigrationPlatform.WindowsStandalone)
                return Unsupported(SpatialMigrationCapabilityReason.PlatformUnsupported, platform);
            try
            {
                if (string.IsNullOrEmpty(activeSavePath) || HasDotSegment(activeSavePath))
                    return Unsupported(SpatialMigrationCapabilityReason.PathInvalid, platform);
                string suppliedPath = NormalizeWindowsSeparators(activeSavePath);
                if (!Path.IsPathFullyQualified(suppliedPath))
                    return Unsupported(SpatialMigrationCapabilityReason.PathInvalid, platform);
                string path = Path.GetFullPath(suppliedPath);
                string directory = Path.GetDirectoryName(path);
                if (!string.Equals(NormalizeWindowsSeparators(path), suppliedPath,
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Length > SpatialMigrationSidecarPaths.WindowsMaximumAbsolutePathCharacters)
                    return Unsupported(SpatialMigrationCapabilityReason.PathInvalid, platform);
                if (!probe.IsPathContainedWithoutRedirection(directory, path))
                    return Unsupported(SpatialMigrationCapabilityReason.PathRedirected, platform);
                string reason = probe.ProbeSupportedVolume(directory);
                return reason == null
                    ? new SpatialMigrationActivationPreflight(true, SpatialMigrationCapabilityReason.Ready,
                        platform, fileSystem, path)
                    : Unsupported(reason, platform);
            }
            catch (ArgumentException) { return Unsupported(SpatialMigrationCapabilityReason.PathInvalid, platform); }
            catch (NotSupportedException) { return Unsupported(SpatialMigrationCapabilityReason.PathInvalid, platform); }
            catch (PathTooLongException) { return Unsupported(SpatialMigrationCapabilityReason.PathInvalid, platform); }
            catch (Exception) { return Unsupported(SpatialMigrationCapabilityReason.NativeProbeFailed, platform); }
        }

        private static SpatialMigrationActivationPreflight Unsupported(string reason,
            SpatialMigrationPlatform platform) => new SpatialMigrationActivationPreflight(false, reason,
                platform, null);

        private static string NormalizeWindowsSeparators(string path) => path
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        private static bool HasDotSegment(string path)
        {
            string[] segments = path.Split(new[] { '/', '\\' }, StringSplitOptions.None);
            for (int index = 0; index < segments.Length; index++)
                if (segments[index] == "." || segments[index] == "..") return true;
            return false;
        }
    }

    public interface IWindowsSpatialMigrationCapabilityProbe
    {
        bool IsPathContainedWithoutRedirection(string directoryPath, string path);
        string ProbeSupportedVolume(string directoryPath);
    }

    public sealed class WindowsSpatialMigrationFileSystem : ISpatialMigrationFileSystem,
        IWindowsSpatialMigrationCapabilityProbe
    {
        public void DeleteFile(string path) => File.Delete(path);
        private const uint GenericWrite = 0x40000000;
        private const uint DeleteAccess = 0x00010000;
        private const uint FileShareRead = 0x1;
        private const uint FileShareWrite = 0x2;
        private const uint FileShareDelete = 0x4;
        private const uint CreateNew = 1;
        private const uint OpenExisting = 3;
        private const uint FileAttributeNormal = 0x80;
        private const uint FileFlagWriteThrough = 0x80000000;
        private const uint DriveFixed = 3;
        private const int FileRenameInfo = 3;

        [StructLayout(LayoutKind.Sequential)]
        private struct FileRenameInfoLayout
        {
            public byte ReplaceIfExists;
            public IntPtr RootDirectory;
            public uint FileNameLength;
            public ushort FileName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ByHandleFileInformation
        {
            public uint FileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        public bool Exists(string path) => File.Exists(path);
        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

        public void WriteAllBytesDurable(string path, byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            path = NormalizePath(path);
            using (SafeFileHandle handle = CreateFileW(path, GenericWrite, 0, IntPtr.Zero, CreateNew,
                FileAttributeNormal | FileFlagWriteThrough, IntPtr.Zero))
            {
                EnsureHandle(handle);
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int count = bytes.Length - offset;
                    if (!WriteFile(handle, bytes, count, out int written, IntPtr.Zero) || written <= 0)
                        ThrowLastWin32();
                    offset += written;
                    if (offset < bytes.Length)
                    {
                        byte[] remainder = new byte[bytes.Length - offset];
                        Buffer.BlockCopy(bytes, offset, remainder, 0, remainder.Length);
                        bytes = remainder; offset = 0;
                    }
                }
                if (!FlushFileBuffers(handle)) ThrowLastWin32();
            }
        }

        public void ReplaceSameDirectoryAtomic(string stagingPath, string activePath) =>
            Move(stagingPath, activePath, true);

        public void MoveSameDirectoryAtomic(string sourcePath, string destinationPath) =>
            Move(sourcePath, destinationPath, false);

        public void FlushDirectory(string directoryPath)
        {
            // Windows has no documented POSIX-directory-fsync equivalent. Every mutation in this
            // implementation completes through a documented write-through operation; this method
            // revalidates that the boundary is still a supported, local, nonredirected NTFS directory.
            string reason = ProbeSupportedVolume(directoryPath);
            if (reason != null) throw new IOException(reason);
        }

        public IReadOnlyList<string> EnumerateFiles(string directoryPath, string searchPattern,
            int maximumResults) => new RuntimeSpatialMigrationFileSystem().EnumerateFiles(
                directoryPath, searchPattern, maximumResults);

        public bool IsPathContainedWithoutRedirection(string directoryPath, string path)
        {
            string directory = NormalizePath(directoryPath);
            string candidate = NormalizePath(path);
            string prefix = directory.EndsWith("\\", StringComparison.Ordinal) ? directory : directory + "\\";
            if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            for (string current = candidate; !string.Equals(current, directory,
                StringComparison.OrdinalIgnoreCase); current = Path.GetDirectoryName(current))
            {
                if (string.IsNullOrEmpty(current)) return false;
                if ((File.Exists(current) || Directory.Exists(current)) &&
                    (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
            }
            return Directory.Exists(directory) &&
                (File.GetAttributes(directory) & FileAttributes.ReparsePoint) == 0;
        }

        public string ProbeSupportedVolume(string directoryPath)
        {
            string directory = NormalizePath(directoryPath);
            if (!Directory.Exists(directory) ||
                (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                return SpatialMigrationCapabilityReason.PathRedirected;
            var volumePath = new System.Text.StringBuilder(SpatialMigrationSidecarPaths.WindowsMaximumAbsolutePathCharacters + 1);
            if (!GetVolumePathNameW(directory, volumePath, volumePath.Capacity))
                return SpatialMigrationCapabilityReason.NativeProbeFailed;
            string actualRoot = Path.GetFullPath(volumePath.ToString());
            string lexicalRoot = Path.GetFullPath(Path.GetPathRoot(directory));
            if (!string.Equals(actualRoot, lexicalRoot, StringComparison.OrdinalIgnoreCase))
                return SpatialMigrationCapabilityReason.PathRedirected;
            if (!HasNoReparsePoint(directory, actualRoot))
                return SpatialMigrationCapabilityReason.PathRedirected;
            if (GetDriveTypeW(actualRoot) != DriveFixed)
                return SpatialMigrationCapabilityReason.VolumeUnsupported;
            var fileSystemName = new System.Text.StringBuilder(32);
            if (!GetVolumeInformationW(actualRoot, null, 0, out _, out _, out _, fileSystemName,
                fileSystemName.Capacity)) return SpatialMigrationCapabilityReason.NativeProbeFailed;
            return string.Equals(fileSystemName.ToString(), "NTFS", StringComparison.OrdinalIgnoreCase)
                ? null : SpatialMigrationCapabilityReason.VolumeUnsupported;
        }

        private static bool HasNoReparsePoint(string directory, string volumeRoot)
        {
            for (string current = directory;; current = Path.GetDirectoryName(current))
            {
                if (string.IsNullOrEmpty(current) || !Directory.Exists(current) ||
                    (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
                if (string.Equals(current.TrimEnd('\\'), volumeRoot.TrimEnd('\\'),
                    StringComparison.OrdinalIgnoreCase)) return true;
            }
        }

        private static void Move(string sourcePath, string destinationPath, bool replace)
        {
            sourcePath = NormalizePath(sourcePath); destinationPath = NormalizePath(destinationPath);
            if (!string.Equals(Path.GetDirectoryName(sourcePath), Path.GetDirectoryName(destinationPath),
                StringComparison.OrdinalIgnoreCase)) throw new IOException();
            IntPtr renameInfo = IntPtr.Zero;
            using (SafeFileHandle handle = CreateFileW(sourcePath, DeleteAccess | GenericWrite,
                FileShareRead | FileShareWrite | FileShareDelete, IntPtr.Zero, OpenExisting,
                FileAttributeNormal | FileFlagWriteThrough, IntPtr.Zero))
            {
                EnsureHandle(handle);
                try
                {
                    ByHandleFileInformation sourceIdentity = ReadIdentity(handle);
                    renameInfo = AllocateRenameInfo(destinationPath, replace, out int allocationSize);
                    if (!SetFileInformationByHandle(handle, FileRenameInfo, renameInfo,
                        (uint)allocationSize)) ThrowLastWin32();
                    if (!FlushFileBuffers(handle)) ThrowLastWin32();
                    using (SafeFileHandle destinationHandle = CreateFileW(destinationPath, 0,
                        FileShareRead | FileShareWrite | FileShareDelete, IntPtr.Zero, OpenExisting,
                        FileAttributeNormal, IntPtr.Zero))
                    {
                        EnsureHandle(destinationHandle);
                        ByHandleFileInformation destinationIdentity = ReadIdentity(destinationHandle);
                        if (!SameIdentity(sourceIdentity, destinationIdentity)) throw new IOException();
                    }
                }
                finally
                {
                    if (renameInfo != IntPtr.Zero) Marshal.FreeHGlobal(renameInfo);
                }
            }
            if (File.Exists(sourcePath) || !File.Exists(destinationPath)) throw new IOException();
        }

        private static IntPtr AllocateRenameInfo(string destinationPath, bool replace, out int allocationSize)
        {
            byte[] destinationBytes = System.Text.Encoding.Unicode.GetBytes(destinationPath);
            int fileNameOffset = Marshal.OffsetOf<FileRenameInfoLayout>(nameof(FileRenameInfoLayout.FileName)).ToInt32();
            int requiredCharactersEnd = checked(fileNameOffset + destinationBytes.Length + sizeof(ushort));
            allocationSize = Math.Max(Marshal.SizeOf<FileRenameInfoLayout>(), requiredCharactersEnd);
            IntPtr renameInfo = Marshal.AllocHGlobal(allocationSize);
            try
            {
                for (int index = 0; index < allocationSize; index++) Marshal.WriteByte(renameInfo, index, 0);
                Marshal.WriteByte(renameInfo,
                    Marshal.OffsetOf<FileRenameInfoLayout>(nameof(FileRenameInfoLayout.ReplaceIfExists)).ToInt32(),
                    replace ? (byte)1 : (byte)0);
                Marshal.WriteIntPtr(renameInfo,
                    Marshal.OffsetOf<FileRenameInfoLayout>(nameof(FileRenameInfoLayout.RootDirectory)).ToInt32(),
                    IntPtr.Zero);
                Marshal.WriteInt32(renameInfo,
                    Marshal.OffsetOf<FileRenameInfoLayout>(nameof(FileRenameInfoLayout.FileNameLength)).ToInt32(),
                    destinationBytes.Length);
                Marshal.Copy(destinationBytes, 0, IntPtr.Add(renameInfo, fileNameOffset), destinationBytes.Length);
                return renameInfo;
            }
            catch
            {
                Marshal.FreeHGlobal(renameInfo);
                throw;
            }
        }

        private static ByHandleFileInformation ReadIdentity(SafeFileHandle handle)
        {
            if (!GetFileInformationByHandle(handle, out ByHandleFileInformation information)) ThrowLastWin32();
            return information;
        }

        private static bool SameIdentity(ByHandleFileInformation left, ByHandleFileInformation right) =>
            left.VolumeSerialNumber == right.VolumeSerialNumber &&
            left.FileIndexHigh == right.FileIndexHigh && left.FileIndexLow == right.FileIndexLow;

        private static string NormalizePath(string path)
        {
            string normalized = Path.GetFullPath(path);
            if (normalized.Length > SpatialMigrationSidecarPaths.WindowsMaximumAbsolutePathCharacters)
                throw new PathTooLongException();
            return normalized;
        }

        private static void EnsureHandle(SafeFileHandle handle)
        { if (handle == null || handle.IsInvalid || handle.IsClosed) ThrowLastWin32(); }
        private static void ThrowLastWin32() => throw new Win32Exception(Marshal.GetLastWin32Error());

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(string name, uint access, uint share,
            IntPtr security, uint creation, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(SafeFileHandle file, byte[] buffer, int bytes,
            out int written, IntPtr overlapped);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FlushFileBuffers(SafeFileHandle file);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFileInformationByHandle(SafeFileHandle file, int informationClass,
            IntPtr fileInformation, uint bufferSize);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandle(SafeFileHandle file,
            out ByHandleFileInformation fileInformation);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetDriveTypeW(string root);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetVolumePathNameW(string fileName,
            System.Text.StringBuilder volumePathName, int bufferLength);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetVolumeInformationW(string root, System.Text.StringBuilder volume,
            int volumeLength, out uint serial, out uint maximumComponentLength, out uint flags,
            System.Text.StringBuilder fileSystemName, int fileSystemNameLength);
    }
}
