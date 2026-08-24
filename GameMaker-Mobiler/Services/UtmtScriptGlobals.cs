using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;
using UndertaleModLib.Util;

namespace GameMaker_Mobiler.Services;

/// <summary>
/// Minimal UTMT-compatible globals object used when executing bundled CSX scripts.
/// </summary>
public sealed class UtmtScriptGlobals
{
    private readonly Action<string, bool>? _log;
    private readonly CancellationToken _cancellationToken;
    private readonly ScriptOptions _scriptOptions;
    private string _scriptPath = string.Empty;
    private int _progress;

    public UtmtScriptGlobals(
        UndertaleData data,
        string filePath,
        string? gameDirectory,
        Action<string, bool>? log,
        CancellationToken cancellationToken)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        FilePath = Path.GetFullPath(filePath);
        GameDirectory = string.IsNullOrWhiteSpace(gameDirectory)
            ? Path.GetDirectoryName(FilePath) ?? string.Empty
            : Path.GetFullPath(gameDirectory);
        _log = log;
        _cancellationToken = cancellationToken;
        _scriptOptions = ScriptingUtil.CreateDefaultScriptOptions()
            .AddReferences(typeof(System.Windows.Application).Assembly);
        MainThreadAction = action =>
        {
            _cancellationToken.ThrowIfCancellationRequested();
            action();
        };
    }

    public UndertaleData Data { get; }

    /// <summary>
    /// 当前正在操作的 data.win 物理路径（可能是临时副本）。
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// 用户真实选中的游戏目录（即原始 data.win 所在目录）。
    /// 用于音乐导入、音频组 .dat 读写等需要访问源目录的场景，避免脚本在临时副本目录里找不到文件。
    /// </summary>
    public string GameDirectory { get; }

    public string ScriptPath => _scriptPath;

    public string ExePath =>
        Directory.GetParent(RuntimePaths.ToolsDirectory)?.FullName
        ?? AppDomain.CurrentDomain.BaseDirectory;

    public Action<Action> MainThreadAction { get; }

    public void EnsureDataLoaded()
    {
        if (Data is null)
            throw new ScriptException("No data file is currently loaded!");
    }

    public void ScriptMessage(string message) => _log?.Invoke(message, false);

    public void ScriptWarning(string message) => _log?.Invoke(message, true);

    public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
    {
        throw new ScriptException(error);
    }

    public bool RunUMTScript(string path)
    {
        try
        {
            RunScriptFileAsync(path).GetAwaiter().GetResult();
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log?.Invoke(ScriptingUtil.PrettifyException(ex), true);
            return false;
        }
    }

    public void SetProgressBar(string message, string status, double progressValue, double maxValue)
    {
        _progress = (int)Math.Clamp(progressValue, 0, int.MaxValue);
        _log?.Invoke(status, false);
    }

    public void StartProgressBarUpdater()
    {
    }

    public Task StopProgressBarUpdater() => Task.CompletedTask;

    public int GetProgress() => _progress;

    public void IncrementProgress()
    {
        _cancellationToken.ThrowIfCancellationRequested();
        _progress++;
    }

    public void HideProgressBar()
    {
    }

    /// <summary>
    /// 移植流水线中禁用所有弹窗询问：统一按照“是/确认”处理，避免阻塞非交互流水线。
    /// 项目约束：Mobile集成脚本.csx 必须将所有 ScriptQuestion hook 变量设为 true 以抑制对话框。
    /// </summary>
    public bool ScriptQuestion(string message) => true;

    /// <summary>
    /// 选择音乐导入目录时优先返回真实游戏目录（用户选中的源目录），
    /// 这样即使 data.win 被复制到临时路径，也能正确扫描到源目录下的音乐文件。
    /// </summary>
    public string? PromptChooseDirectory()
    {
        var dir = !string.IsNullOrEmpty(GameDirectory) && Directory.Exists(GameDirectory)
            ? GameDirectory
            : Path.GetDirectoryName(FilePath);
        return string.IsNullOrEmpty(dir) || !Directory.Exists(dir) ? null : dir;
    }

    /// <summary>
    /// UTMT 的 SyncBinding 解除钩子；当前实现不维护绑定，空实现即可。
    /// </summary>
    public void DisableAllSyncBindings()
    {
    }

    public async Task RunScriptFileAsync(string scriptPath)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        var fullPath = Path.GetFullPath(scriptPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("CSX script not found", fullPath);

        var previousScriptPath = _scriptPath;
        _scriptPath = fullPath;
        try
        {
            var scriptText = $"#line 1 \"{fullPath}\"\n" +
                             await File.ReadAllTextAsync(fullPath, Encoding.UTF8, _cancellationToken)
                                 .ConfigureAwait(false);
            await CSharpScript.EvaluateAsync(
                    scriptText,
                    _scriptOptions
                        .WithFilePath(fullPath)
                        .WithFileEncoding(Encoding.UTF8),
                    this,
                    typeof(UtmtScriptGlobals),
                    _cancellationToken)
                .ConfigureAwait(false);
        }
        catch (CompilationErrorException ex)
        {
            throw new InvalidOperationException(
                $"脚本编译失败: {Path.GetFileName(fullPath)}\n{string.Join(Environment.NewLine, ex.Diagnostics)}",
                ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"脚本执行失败: {Path.GetFileName(fullPath)}\n{ScriptingUtil.PrettifyException(ex)}",
                ex);
        }
        finally
        {
            _scriptPath = previousScriptPath;
        }
    }
}
