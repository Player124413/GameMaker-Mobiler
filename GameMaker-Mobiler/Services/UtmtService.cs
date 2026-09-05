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

        if (options is null || options.Length < 7)
        {
            throw new ArgumentException("options must contain 7 values", nameof(options));
        }

        var addMobileKey = options[0];
        var mobileF2 = options[1];
        var mobileHeal = options[2];
        var mobileCn = options[3];
        var androidSystemKeyboard = options[4];
        var dualControls = options[5];
        var embedMusicIntoDataWin = options[6];
        // Optional switches; older callers pass only 7 values, so defaults are used then.
        var autoTouchLayer = options.Length > 7 && options[7];
        var mobileOptimization = options.Length > 8 && options[8];

        var gameDir = Path.GetDirectoryName(dataWinPath) ?? string.Empty;
        var isUte = DetectUteTemplate(gameDir);

        // ASCII-only working directory (see SafeWorkspace): non-ASCII paths break the toolchain.
        var workingDirectory = SafeWorkspace.CreateDirectory("utmt");
        var workingOutputPath = Path.Combine(workingDirectory, Path.GetFileName(finalOutputPath));

        try
        {
            File.Copy(dataWinPath, workingOutputPath, overwrite: true);
            using var data = DataWinVersionReader.ReadData(
                workingOutputPath,
                warningHandler: (warning, isImportant) =>
                    _log?.Invoke($"data.win warning: {warning}", isImportant),
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

            // Step 1: mobile integration script (safe for any game)
            if (addMobileKey)
            {
                var integrationScriptPath = GetIntegrationScriptPath();
                if (!File.Exists(integrationScriptPath))
                {
                    throw new FileNotFoundException("Mobile integration script not found", integrationScriptPath);
                }

                _log?.Invoke("Running the mobile integration script...", false);
                await scriptGlobals.RunScriptFileAsync(integrationScriptPath).ConfigureAwait(false);
            }
            else
            {
                _log?.Invoke("Main switch is off: skipping the mobile integration script.", false);
            }

            // Step 2: UTE repair script (auto-detected, UTE template games only)
            if (isUte)
            {
                var uteRepairScriptPath = GetUteRepairScriptPathByVersion(majorVer, minorVer);
                if (!File.Exists(uteRepairScriptPath))
                {
                    throw new FileNotFoundException($"UTE repair script not found: {uteRepairScriptPath}", uteRepairScriptPath);
                }

                _log?.Invoke($"UTE template game detected, running the UTE repair script: {Path.GetFileName(uteRepairScriptPath)}", false);
                await scriptGlobals.RunScriptFileAsync(uteRepairScriptPath).ConfigureAwait(false);
            }
            else
            {
                _log?.Invoke("Not a UTE template game: skipping the UTE repair script.", false);
            }

            // Step 3: embed the music next to data.win into data.win (opt-in)
            if (embedMusicIntoDataWin)
            {
                var importMusicScriptPath = GetImportAllMusicScriptPath();
                if (!File.Exists(importMusicScriptPath))
                {
                    throw new FileNotFoundException("Import all music script not found", importMusicScriptPath);
                }

                _log?.Invoke($"Music embedding enabled, running the import-all-music script: {Path.GetFileName(importMusicScriptPath)}", false);
                _log?.Invoke($"Music import folder (original game folder): {gameDir}", false);
                await scriptGlobals.RunScriptFileAsync(importMusicScriptPath).ConfigureAwait(false);
            }
            else
            {
                _log?.Invoke("Music embedding disabled: skipping the import-all-music script.", false);
            }

            // Step 4: auto touch layer (draws only the keys the game really uses)
            if (autoTouchLayer)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _log?.Invoke("Analyzing which keyboard keys the game actually uses...", false);

                var report = KeyUsageAnalyzer.Analyze(data, _log);
                if (report.IsEmpty)
                {
                    _log?.Invoke("No keyboard calls detected; falling back to the default layout (arrow keys + Z/X/C/Enter/Esc).", true);
                    report = KeyUsageAnalyzer.CreateFallback();
                }

                var touchOptions = new TouchLayerOptions
                {
                    EnableJoystick = true,
                    EnableOptimization = mobileOptimization,
                    IsGameMaker2 = data.IsGameMaker2()
                };

                _log?.Invoke("Injecting the touch layer (joystick / buttons / EDIT mode)...", false);
                TouchLayerInjector.Inject(data, report, touchOptions, scriptGlobals.MainThreadAction, _log);
            }
            else
            {
                _log?.Invoke("Auto touch layer disabled: skipping key analysis and touch layer injection.", false);
            }

            if (!addMobileKey)
            {
                _log?.Invoke("Main switch is off: skipping the mb_cont_mobile globals patch.", false);
                await SaveDataAsync(data, workingOutputPath, finalOutputPath, cancellationToken).ConfigureAwait(false);
                return;
            }

            var templatePath = GetMobileContTemplatePath();
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException("Template gml not found", templatePath);
            }

            var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken).ConfigureAwait(false);
            var patchedContent = PatchMobileGlobals(templateContent, addMobileKey, mobileF2, mobileHeal, mobileCn, androidSystemKeyboard, dualControls);

            _log?.Invoke($"Writing global variable configuration into {Path.GetFileName(workingOutputPath)}...", false);
            cancellationToken.ThrowIfCancellationRequested();
            var mobileControlCode = data.Code.ByName("gml_Object_mb_cont_mobile_Create_0");
            if (mobileControlCode is null)
            {
                throw new InvalidDataException(
                    "gml_Object_mb_cont_mobile_Create_0 does not exist in data.win.");
            }

            var importGroup = new CodeImportGroup(data)
            {
                MainThreadAction = scriptGlobals.MainThreadAction
            };
            importGroup.QueueReplace(mobileControlCode, patchedContent);
            importGroup.Import();

            await SaveDataAsync(data, workingOutputPath, finalOutputPath, cancellationToken).ConfigureAwait(false);
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

    private Task SaveDataAsync(
        UndertaleData data,
        string workingOutputPath,
        string finalOutputPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _log?.Invoke("Saving the patched data.win...", false);

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

        _log?.Invoke($"Saved to: {finalOutputPath}", false);
        return Task.CompletedTask;
    }

    private static string PatchMobileGlobals(
        string template,
        bool addMobileKey,
        bool mobileF2,
        bool mobileHeal,
        bool mobileCn,
        bool androidSystemKeyboard,
        bool dualControls)
    {
        var output = template;

        output = ReplaceGlobalAssignment(output, "add_mobilekey", addMobileKey);
        output = ReplaceGlobalAssignment(output, "mobile_f2", mobileF2);
        output = ReplaceGlobalAssignment(output, "mobile_heal", mobileHeal);
        output = ReplaceGlobalAssignment(output, "mobile_cn", mobileCn);
        output = ReplaceGlobalAssignment(output, "Android_System_Keyboard", androidSystemKeyboard);
        output = ReplaceGlobalAssignment(output, "dual_controls", dualControls);

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
