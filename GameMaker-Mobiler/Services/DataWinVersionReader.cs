using System;
using System.Linq;
using System.IO;
using System.Text;
using UndertaleModLib;
using UndertaleModLib.Models;
namespace GameMaker_Mobiler.Services;

/// <summary>
/// 从 GameMaker data.win / data.unx / data.ios / data.droid 文件解析出的版本信息。
/// </summary>
public sealed record DataWinVersion(
    uint Major,
    uint Minor,
    uint Release,
    uint Build,
    byte BytecodeVersion,
    bool IsGameMaker2,
    bool IsYyc,
    bool IsValid,
    string DisplayVersion,
    string RawGen8Version,
    string ChunkNameFloor,
    string StructuralFloor)
{
    public override string ToString() => DisplayVersion;
}

/// <summary>
/// 使用 UndertaleModLib 的完整读取器获取版本信息和编译方式。
/// 失败时返回 <see cref="Invalid"/>。
/// </summary>
public static class DataWinVersionReader
{
    /// <summary>
    /// Loads a data file and returns the loaded object to the caller.
    /// The caller owns the returned object and must dispose it.
    /// </summary>
    public static UndertaleData ReadData(
        string dataWinPath,
        Action<string, bool>? warningHandler = null,
        Action<string>? messageHandler = null)
    {
        if (!File.Exists(dataWinPath))
            throw new FileNotFoundException("data file not found", dataWinPath);

        using var fs = File.OpenRead(dataWinPath);
        UndertaleReader.WarningHandlerDelegate? warningDelegate =
            warningHandler is null
                ? null
                : (warning, isImportant) => warningHandler(warning, isImportant);
        UndertaleReader.MessageHandlerDelegate? messageDelegate =
            messageHandler is null
                ? null
                : message => messageHandler(message);
        return UndertaleIO.Read(
            fs,
            warningHandler: warningDelegate,
            messageHandler: messageDelegate);
    }

    /// <summary>
    /// Extracts version information from an already loaded data object.
    /// </summary>
    public static DataWinVersion FromData(UndertaleData? data)
    {
        if (data?.GeneralInfo == null)
            return Invalid;

        var gi = data.GeneralInfo;
        var chunkNames = data.FORM?.Chunks.Keys.ToArray() ?? Array.Empty<string>();

        return new DataWinVersion(
            Major: gi.Major,
            Minor: gi.Minor,
            Release: gi.Release,
            Build: gi.Build,
            BytecodeVersion: gi.BytecodeVersion,
            IsGameMaker2: gi.Major >= 2,
            IsYyc: data.IsYYC(),
            IsValid: true,
            DisplayVersion: FormatDisplayVersion(gi.Major, gi.Minor, gi.Release, gi.Build, gi.BytecodeVersion),
            RawGen8Version: $"{gi.Major}.{gi.Minor}.{gi.Release}.{gi.Build}",
            ChunkNameFloor: chunkNames.Any() ? string.Join(", ", chunkNames) : "(none)",
            StructuralFloor: "UndertaleModLib full read + IsYYC");
    }

    /// <summary>解析 data.win。失败时返回 <see cref="Invalid"/>。</summary>
    /// <exception cref="FileNotFoundException">文件不存在。</exception>
    public static DataWinVersion Read(string dataWinPath)
    {
        if (!File.Exists(dataWinPath))
            throw new FileNotFoundException("data file not found", dataWinPath);

        try
        {
            // 现代 GMS2 文件的 GEN8 版本字段通常固定为 2.0.0.0。
            // 完整读取会先收集所有 chunk，再由 UndertaleModLib 根据文件特征推断实际版本。
            using var data = ReadData(dataWinPath);
            return FromData(data);
        }
        catch
        {
            return Invalid;
        }
    }

    /// <summary>格式化显示版本字符串（与原始逻辑一致）。</summary>
    private static string FormatDisplayVersion(uint major, uint minor, uint release, uint build, byte bytecodeVersion)
    {
        string engine = major >= 2 ? "GMS2" : "GMS1";
        string versionCore;

        if (major == 1)
        {
            versionCore = $"{major}.{minor}.{release}.{build}";
        }
        else if (major < 2022)
        {
            versionCore = build == 0
                ? $"{major}.{minor}.{release}"
                : $"{major}.{minor}.{release}.{build}";
        }
        else
        {
            var sb = new StringBuilder();
            sb.Append(major);
            if (minor != 0)
            {
                sb.Append('.').Append(minor);
                if (release != 0)
                {
                    sb.Append('.').Append(release);
                    if (build != 0)
                        sb.Append('.').Append(build);
                }
            }
            versionCore = sb.ToString();
        }

        return $"{engine} {versionCore} (bytecode {bytecodeVersion})";
    }

    /// <summary>解析失败的占位版本信息（与原始定义完全一致）。</summary>
    public static DataWinVersion Invalid { get; } = new(
        Major: 0,
        Minor: 0,
        Release: 0,
        Build: 0,
        BytecodeVersion: 0,
        IsGameMaker2: false,
        IsYyc: false,
        IsValid: false,
        DisplayVersion: "Unknown",
        RawGen8Version: "-",
        ChunkNameFloor: "(none)",
        StructuralFloor: "(none)");
}
