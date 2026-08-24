using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace GameMaker_Mobiler.Services;

public sealed class UtmtService
{
    private readonly Action<string, bool>? _log;

    public UtmtService(Action<string, bool>? log = null)
    {
        _log = log;
    }

    public Task ModifyDataWin(string dataWinPath, bool[] options)
    {
        return ModifyDataWin(dataWinPath, options, null, CancellationToken.None);
    }

    public Task ModifyDataWin(string dataWinPath, bool[] options, string? outputFileName)
    {
        return ModifyDataWin(dataWinPath, options, outputFileName, CancellationToken.None);
    }

    public Task ModifyDataWin(string dataWinPath, bool[] options, CancellationToken cancellationToken)
    {
        return ModifyDataWin(dataWinPath, options, null, cancellationToken);
    }

    public async Task ModifyDataWin(string dataWinPath, bool[] options, string? outputFileName, CancellationToken cancellationToken)
    {
        var gameDir = Path.GetDirectoryName(dataWinPath) ?? string.Empty;
        var outputPath = string.IsNullOrEmpty(outputFileName)
            ? dataWinPath
            : Path.Combine(gameDir, outputFileName);

        await ModifyDataWinToPath(dataWinPath, options, outputPath, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task ModifyDataWinToPath(
        string dataWinPath,
        bool[] options,
        string outputPath,
        CancellationToken cancellationToken)
    {
        return ModifyDataWinToPathCore(dataWinPath, options, outputPath, cancellationToken);
    }

    private async Task ModifyDataWinToPathCore(
        string dataWinPath,
        bool[] options,
        string finalOutputPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(dataWinPath))
        {
            throw new FileNotFoundException("data.win not found", dataWinPath);
        }

        if (options is null || options.Length < 6)
        {
            throw new ArgumentException("options must contain 6 values", nameof(options));
        }

        var addMobileKey = options[0];
        var mobileF2 = options[1];
        var mobileHeal = options[2];
        var mobileCn = options[3];
        var androidSystemKeyboard = options[4];
        var embedMusicIntoDataWin = options[5];

        var gameDir = Path.GetDirectoryName(dataWinPath) ?? string.Empty;
        var isUte = DetectUteTemplate(gameDir);

        var workingDirectory = Path.Combine(Path.GetTempPath(), $"gmm_utmt_{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
        var workingOutputPath = Path.Combine(workingDirectory, Path.GetFileName(finalOutputPath));

        try
        {
            File.Copy(dataWinPath, workingOutputPath, overwrite: true);
            using var data = DataWinVersionReader.ReadData(
                workingOutputPath,
                warningHandler: (warning, isImportant) =>
                    _log?.Invoke($"data.win 警告: {warning}", isImportant),
                messageHandler: message => _log?.Invoke(message, false));
            var version = DataWinVersionReader.FromData(data);
            var majorVer = (int)version.Major;
            var minorVer = (int)version.Minor;
            var scriptGlobals = new UtmtScriptGlobals(
                data,
                workingOutputPath,
                gameDir,
                _log,
                cancellationToken);

            // Step 1: Mobile 集成脚本（任何游戏均可执行）
            if (addMobileKey)
            {
                var integrationScriptPath = GetIntegrationScriptPath();
                if (!File.Exists(integrationScriptPath))
                {
                    throw new FileNotFoundException("Mobile integration script not found", integrationScriptPath);
                }

                _log?.Invoke("执行 Mobile 集成脚本...", false);
                await scriptGlobals.RunScriptFileAsync(integrationScriptPath).ConfigureAwait(false);
            }
            else
            {
                _log?.Invoke("主开关未启用：跳过 Mobile 集成脚本。", false);
            }

            // Step 2: UTE 修复脚本（自动检测，仅 UTE 模板游戏执行）
            if (isUte)
            {
                var uteRepairScriptPath = GetUteRepairScriptPathByVersion(majorVer, minorVer);
                if (!File.Exists(uteRepairScriptPath))
                {
                    throw new FileNotFoundException($"UTE repair script not found: {uteRepairScriptPath}", uteRepairScriptPath);
                }

                _log?.Invoke($"检测到 UTE 模板游戏，执行 UTE 修复脚本: {Path.GetFileName(uteRepairScriptPath)}", false);
                await scriptGlobals.RunScriptFileAsync(uteRepairScriptPath).ConfigureAwait(false);
            }
            else
            {
                _log?.Invoke("非 UTE 模板游戏，跳过 UTE 修复脚本。", false);
            }

            // Step 3: 将 data.win 目录下的音乐内置进 data.win（用户可勾选）
            if (embedMusicIntoDataWin)
            {
                var importMusicScriptPath = GetImportAllMusicScriptPath();
                if (!File.Exists(importMusicScriptPath))
                {
                    throw new FileNotFoundException("Import all music script not found", importMusicScriptPath);
                }

                _log?.Invoke($"已勾选音乐内置，执行导入所有音乐脚本: {Path.GetFileName(importMusicScriptPath)}", false);
                _log?.Invoke($"音乐导入目录（真实游戏目录）: {gameDir}", false);
                await scriptGlobals.RunScriptFileAsync(importMusicScriptPath).ConfigureAwait(false);
            }
            else
            {
                _log?.Invoke("未勾选音乐内置：跳过导入所有音乐脚本。", false);
            }

            var templatePath = GetMobileContTemplatePath();
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException("Template gml not found", templatePath);
            }

            var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken).ConfigureAwait(false);
            var patchedContent = PatchMobileGlobals(templateContent, addMobileKey, mobileF2, mobileHeal, mobileCn, androidSystemKeyboard);

            _log?.Invoke($"写入全局变量配置到 {Path.GetFileName(workingOutputPath)}...", false);
            cancellationToken.ThrowIfCancellationRequested();
            var mobileControlCode = data.Code.ByName("gml_Object_mb_cont_mobile_Create_0");
            if (mobileControlCode is null)
            {
                throw new InvalidDataException(
                    "data.win 中不存在 gml_Object_mb_cont_mobile_Create_0。");
            }

            var importGroup = new CodeImportGroup(data)
            {
                MainThreadAction = scriptGlobals.MainThreadAction
            };
            importGroup.QueueReplace(mobileControlCode, patchedContent);
            importGroup.Import();

            cancellationToken.ThrowIfCancellationRequested();
            _log?.Invoke("保存修改后的 data.win...", false);
            using (var dataWriteStream = new FileStream(
                       workingOutputPath,
                       FileMode.Create,
                       FileAccess.Write,
                       FileShare.None))
            {
                UndertaleIO.Write(dataWriteStream, data);
            }

            if (!string.Equals(workingOutputPath, finalOutputPath, StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(finalOutputPath)!);
                File.Copy(workingOutputPath, finalOutputPath, overwrite: true);
            }

            _log?.Invoke($"已保存为: {finalOutputPath}", false);
        }
        finally
        {
            try
            {
                if (Directory.Exists(workingDirectory))
                {
                    Directory.Delete(workingDirectory, recursive: true);
                }
            }
            catch
            {
                // Ignore temp cleanup errors.
            }
        }
    }

    private static string PatchMobileGlobals(
        string template,
        bool addMobileKey,
        bool mobileF2,
        bool mobileHeal,
        bool mobileCn,
        bool androidSystemKeyboard)
    {
        var output = template;

        output = ReplaceGlobalAssignment(output, "add_mobilekey", addMobileKey);
        output = ReplaceGlobalAssignment(output, "mobile_f2", mobileF2);
        output = ReplaceGlobalAssignment(output, "mobile_heal", mobileHeal);
        output = ReplaceGlobalAssignment(output, "mobile_cn", mobileCn);
        output = ReplaceGlobalAssignment(output, "Android_System_Keyboard", androidSystemKeyboard);

        return output;
    }

    private static string ReplaceGlobalAssignment(string content, string globalName, bool enabled)
    {
        var value = enabled ? "1" : "0";
        var pattern = $@"(?im)^\s*global\.{Regex.Escape(globalName)}\s*=\s*\d+\s*;";
        var replacement = $"global.{globalName} = {value};";
        var regex = new Regex(pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);

        return regex.Replace(content, replacement, 1);
    }

    private static bool DetectUteTemplate(string gameDir)
    {
        if (string.IsNullOrWhiteSpace(gameDir))
            return false;

        var binDir = Path.Combine(gameDir, "bin");
        var localeDir = Path.Combine(gameDir, "locale");
        var gmuConsole = Path.Combine(binDir, "gmu_console.dll");

        return Directory.Exists(binDir)
            && Directory.Exists(localeDir)
            && File.Exists(gmuConsole);
    }

    private static string GetIntegrationScriptPath()
    {
        return Path.Combine(RuntimePaths.ToolsDirectory, "移植脚本", "安卓脚本v2.0", "Mobile集成脚本.csx");
    }

    private static string GetImportAllMusicScriptPath()
    {
        return Path.Combine(RuntimePaths.ToolsDirectory, "移植脚本", "导入所有音乐.csx");
    }

    private static string GetMobileContTemplatePath()
    {
        return Path.Combine(RuntimePaths.ToolsDirectory, "移植脚本", "安卓脚本v2.0", "MobileScript", "mobilecont", "gml_Object_mb_cont_mobile_Create_0.gml");
    }

    private static string GetUteRepairScriptPath()
    {
        return Path.Combine(RuntimePaths.ToolsDirectory, "移植脚本", "Ute 修复脚本", "Ute控制台和路径修复.csx");
    }

    private static string GetUteRepairScriptPathForOldVersion()
    {
        return Path.Combine(RuntimePaths.ToolsDirectory, "移植脚本", "Ute 修复脚本", "低于GMS 2.3.0的脚本", "旧版Ute控制台和路径修复.csx");
    }

    public static string GetUteRepairScriptPathByVersion(int majorVersion, int minorVersion)
    {
        if (majorVersion < 2 || (majorVersion == 2 && minorVersion < 3))
            return GetUteRepairScriptPathForOldVersion();
        return GetUteRepairScriptPath();
    }
}
