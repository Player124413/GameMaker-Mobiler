using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GameMaker_Mobiler.Services;

/// <summary>
/// Provides an ASCII-only working directory.
/// <para>
/// Apktool / apksigner / zipalign are Java and native tools that break when any path
/// passed to them contains non-ASCII characters (for example a Windows user name written
/// in Cyrillic, which makes %TEMP% look like C:\Users\Пользователь\AppData\Local\Temp).
/// The typical failure is:
/// <c>W: C:\Users\...\AppData\Local\Temp\... error: failed to open directory</c>.
/// </para>
/// <para>
/// Every temporary path used by the porting pipeline must therefore be created through
/// <see cref="Root"/>, which is guaranteed to be pure ASCII.
/// </para>
/// </summary>
public static class SafeWorkspace
{
    private const string FolderName = "GameMakerMobiler";

    private static readonly Lazy<string> _root = new(ResolveRoot);

    /// <summary>
    /// ASCII-only directory that is safe to hand over to external tools.
    /// </summary>
    public static string Root => _root.Value;

    /// <summary>
    /// True when the system temporary directory contains non-ASCII characters
    /// (so the fallback location is being used).
    /// </summary>
    public static bool SystemTempIsUnsafe { get; private set; }

    /// <summary>
    /// Creates a new uniquely named ASCII-only subdirectory.
    /// </summary>
    /// <param name="prefix">ASCII prefix for the directory name.</param>
    public static string CreateDirectory(string prefix = "work")
    {
        var safePrefix = ToAscii(prefix, "work");
        var path = Path.Combine(Root, $"{safePrefix}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Builds a path inside the ASCII-only workspace without creating it.
    /// </summary>
    public static string Combine(params string[] parts)
    {
        return Path.Combine(new[] { Root }.Concat(parts).ToArray());
    }

    /// <summary>
    /// True when the given path only contains characters external tools can handle.
    /// </summary>
    public static bool IsPathSafe(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return true;

        foreach (var c in path)
        {
            // Anything outside printable ASCII is refused by apktool's file handling.
            if (c > '\u007F')
                return false;
        }

        return true;
    }

    /// <summary>
    /// Replaces every non-ASCII character so the result is safe as a file name.
    /// </summary>
    public static string ToAscii(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c <= '\u007F' && !char.IsControl(c))
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }

        var result = sb.ToString().Trim().Trim('_', ' ', '.');
        return string.IsNullOrWhiteSpace(result) ? fallback : result;
    }

    /// <summary>
    /// Removes leftover directories from previous runs (best effort).
    /// </summary>
    public static void CleanupStaleDirectories(TimeSpan olderThan)
    {
        try
        {
            if (!Directory.Exists(Root))
                return;

            var threshold = DateTime.UtcNow - olderThan;
            foreach (var directory in Directory.EnumerateDirectories(Root))
            {
                try
                {
                    if (Directory.GetLastWriteTimeUtc(directory) < threshold)
                        Directory.Delete(directory, recursive: true);
                }
                catch
                {
                    // A directory may still be locked by another instance; skip it.
                }
            }
        }
        catch
        {
            // Cleanup must never break the build.
        }
    }

    private static string ResolveRoot()
    {
        // 1. Preferred: the regular temporary directory, when it is ASCII-only.
        var systemTemp = Path.Combine(Path.GetTempPath(), FolderName);
        if (IsPathSafe(systemTemp) && TryCreate(systemTemp))
        {
            SystemTempIsUnsafe = false;
            return systemTemp;
        }

        SystemTempIsUnsafe = true;

        // 2. Fallback candidates that never contain the (possibly Cyrillic) user name.
        foreach (var candidate in EnumerateFallbackRoots())
        {
            if (IsPathSafe(candidate) && TryCreate(candidate))
                return candidate;
        }

        // 3. Last resort: use the system temp anyway, so the app still runs.
        Directory.CreateDirectory(systemTemp);
        return systemTemp;
    }

    private static System.Collections.Generic.IEnumerable<string> EnumerateFallbackRoots()
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        if (!string.IsNullOrEmpty(programData))
            yield return Path.Combine(programData, FolderName, "temp");

        var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
        if (!string.IsNullOrEmpty(systemDrive))
            yield return Path.Combine(systemDrive, FolderName + "Temp");

        // The application directory itself is usually ASCII when installed normally.
        yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
    }

    private static bool TryCreate(string path)
    {
        try
        {
            Directory.CreateDirectory(path);

            // Verify we can actually write there.
            var probe = Path.Combine(path, $"probe_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
