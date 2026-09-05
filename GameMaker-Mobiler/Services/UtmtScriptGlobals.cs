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
    /// Physical path of the data.win currently being modified (may be a temporary copy).
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The game folder the user actually selected (where the original data.win lives).
    /// Used for music import and audiogroup .dat access so scripts do not look inside the temporary copy.
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
    /// All dialogs are suppressed in the pipeline: every question is answered with "yes".
    /// The bundled integration script relies on this to run without user interaction.
    /// </summary>
    public bool ScriptQuestion(string message) => true;

    /// <summary>
    /// Returns the real game folder for the music import directory prompt,
    /// so audio files are found even though data.win itself was copied to a temporary path.
    /// </summary>
    public string? PromptChooseDirectory()
    {
        var dir = !string.IsNullOrEmpty(GameDirectory) && Directory.Exists(GameDirectory)
            ? GameDirectory
            : Path.GetDirectoryName(FilePath);
        return string.IsNullOrEmpty(dir) || !Directory.Exists(dir) ? null : dir;
    }

    /// <summary>
    /// UTMT SyncBinding hook; this host keeps no bindings, so an empty implementation is fine.
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
                $"Script compilation failed: {Path.GetFileName(fullPath)}\n{string.Join(Environment.NewLine, ex.Diagnostics)}",
                ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Script execution failed: {Path.GetFileName(fullPath)}\n{ScriptingUtil.PrettifyException(ex)}",
                ex);
        }
        finally
        {
            _scriptPath = previousScriptPath;
        }
    }
}
