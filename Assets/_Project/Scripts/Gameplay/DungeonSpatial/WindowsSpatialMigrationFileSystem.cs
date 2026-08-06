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
            SpatialMigrationPlatform platform, ISpatialMigrationFileSystem fileSystem)
        { IsSupported = supported; Reason = reason; Platform = platform; FileSystem = fileSystem; }
        public bool IsSupported { get; }
        public string Reason { get; }
        public SpatialMigrationPlatform Platform { get; }
        public ISpatialMigrationFileSystem FileSystem { get; }
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
                string path = Path.GetFullPath(activeSavePath);
                string directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(activeSavePath) || !string.Equals(path, activeSavePath,
                    StringComparison.Ordinal) || path.Length > SpatialMigrationSidecarPaths.WindowsMaximumAbsolutePathCharacters)
                    return Unsupported(SpatialMigrationCapabilityReason.PathInvalid, platform);
                if (!probe.IsPathContainedWithoutRedirection(directory, path))
                    return Unsupported(SpatialMigrationCapabilityReason.PathRedirected, platform);
                string reason = probe.ProbeSupportedVolume(directory);
                return reason == null
                    ? new SpatialMigrationActivationPreflight(true, SpatialMigrationCapabilityReason.Ready,
                        platform, fileSystem)
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
    }

    public interface IWindowsSpatialMigrationCapabilityProbe
    {
        bool IsPathContainedWithoutRedirection(string directoryPath, string path);
        string ProbeSupportedVolume(string directoryPath);
    }

    public sealed class WindowsSpatialMigrationFileSystem : ISpatialMigrationFileSystem,
        IWindowsSpatialMigrationCapabilityProbe
    {
        private const uint GenericWrite = 0x40000000;
        private const uint CreateNew = 1;
        private const uint FileAttributeNormal = 0x80;
        private const uint FileFlagWriteThrough = 0x80000000;
        private const uint MoveReplaceExisting = 0x1;
        private const uint MoveWriteThrough = 0x8;
        private const uint DriveFixed = 3;

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

        internal string ProbeSupportedVolume(string directoryPath)
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
            uint flags = MoveWriteThrough | (replace ? MoveReplaceExisting : 0);
            if (!MoveFileExW(sourcePath, destinationPath, flags)) ThrowLastWin32();
        }

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
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileExW(string existing, string destination, uint flags);
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
