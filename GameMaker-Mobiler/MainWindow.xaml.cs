using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Text.RegularExpressions;
using GameMaker_Mobiler.Services;
using Microsoft.Win32;

namespace GameMaker_Mobiler
{
    public partial class MainWindow : Window
    {
        private bool _useDarkTheme;
        private readonly Brush _sourceDropOriginalBorderBrush;
        private readonly UtmtService _utmtService;
        private readonly ApkBuilder _apkBuilder;
        private GameInfo? _currentGameInfo;
        private CancellationTokenSource? _portingCts;
        private string? _selectedIconPath;
        private string? _selectedSplashPath;
        private string? _lastOutputDir;

        public ObservableCollection<string> Logs { get; } = [];

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _sourceDropOriginalBorderBrush = SourceDropBorder.BorderBrush;
            _utmtService = new UtmtService(AddLog);
            _apkBuilder = new ApkBuilder(AddLog);

            SubOptionsContainer.IsHitTestVisible = false;
            SubOptionsContainer.Visibility = Visibility.Collapsed;

            Logs.CollectionChanged += OnLogsCollectionChanged;
            Logs.Add("Application started. Theme: Light.");
            Logs.Add("Select or drag in a data.win file to begin.");

            // Apktool and the other Java tools cannot handle non-ASCII paths (for example a
            // Cyrillic Windows user name in %TEMP%), so the build runs in an ASCII-only folder.
            Logs.Add($"Working folder: {SafeWorkspace.Root}");
            if (SafeWorkspace.SystemTempIsUnsafe)
            {
                AddLog(
                    "The system temporary folder contains non-ASCII characters (for example a Cyrillic "
                    + "user name). An ASCII-only working folder is used instead so Apktool does not fail.",
                    true);
            }

            SafeWorkspace.CleanupStaleDirectories(TimeSpan.FromDays(1));

            UpdateStartPortingAvailability();
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _useDarkTheme = !_useDarkTheme;

            if (Application.Current is App app)
            {
                app.ApplyTheme(_useDarkTheme);
            }

            var themeBackground = (System.Windows.Media.Brush)Application.Current.FindResource("WindowBackgroundBrush");
            Background = themeBackground;
            RootLayout.Background = themeBackground;

            ThemeToggleButton.Content = _useDarkTheme ? "Light mode" : "Dark mode";
            Logs.Add(_useDarkTheme ? "Theme switched to Dark." : "Theme switched to Light.");
        }

        private void OnLogsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (Logs.Count == 0)
            {
                return;
            }

            Dispatcher.InvokeAsync(() => LogListBox.ScrollIntoView(Logs[^1]));
        }

        private async void BrowseDataWinButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select data.win",
                Filter = "GameMaker data file (data.win)|data.win|All files|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                await HandleDataSourceSelectionAsync(dialog.FileName).ConfigureAwait(false);
            }
        }

        private void SourceDropBorder_DragEnter(object sender, DragEventArgs e)
        {
            UpdateDragVisual(e, true);
        }

        private void SourceDropBorder_DragOver(object sender, DragEventArgs e)
        {
            UpdateDragVisual(e, false);
        }

        private void SourceDropBorder_DragLeave(object sender, DragEventArgs e)
        {
            SourceDropBorder.BorderBrush = _sourceDropOriginalBorderBrush;
            SourceDropBorder.BorderThickness = new Thickness(1);
        }

        private async void SourceDropBorder_Drop(object sender, DragEventArgs e)
        {
            SourceDropBorder.BorderBrush = _sourceDropOriginalBorderBrush;
            SourceDropBorder.BorderThickness = new Thickness(1);

            if (!TryGetDroppedPath(e, out var droppedPath) || string.IsNullOrWhiteSpace(droppedPath))
            {
                AddLog("Error: no valid path was recognized in the drop.", true);
                return;
            }

            await HandleDataSourceSelectionAsync(droppedPath).ConfigureAwait(false);
        }

        private void UpdateDragVisual(DragEventArgs e, bool setBorderThickness)
        {
            if (TryGetDroppedPath(e, out _))
            {
                e.Effects = DragDropEffects.Copy;
                SourceDropBorder.BorderBrush = (Brush)Application.Current.FindResource("PrimaryBrush");

                if (setBorderThickness)
                {
                    SourceDropBorder.BorderThickness = new Thickness(2);
                }

                return;
            }

            e.Effects = DragDropEffects.None;
            SourceDropBorder.BorderBrush = _sourceDropOriginalBorderBrush;
            SourceDropBorder.BorderThickness = new Thickness(1);
        }

        private static bool TryGetDroppedPath(DragEventArgs e, out string? path)
        {
            path = null;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return false;
            }

            if (e.Data.GetData(DataFormats.FileDrop) is not string[] items || items.Length == 0)
            {
                return false;
            }

            path = items[0];
            return true;
        }

        private async Task HandleDataSourceSelectionAsync(string selectedPath)
        {
            var dataWinPath = ResolveDataWinPath(selectedPath);
            if (string.IsNullOrWhiteSpace(dataWinPath))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    GameVersionTextBlock.Text = "Version: detection failed";
                    DataWinPathTextBlock.Text = "data.win path: not detected";
                });

                AddLog("Error: the selected path does not contain data.win.", true);
                return;
            }

            var sourceDirectory = Path.GetDirectoryName(dataWinPath) ?? string.Empty;
            _currentGameInfo = new GameInfo(sourceDirectory, dataWinPath, DataWinVersionReader.Invalid, false);

            await Dispatcher.InvokeAsync(() =>
            {
                SourcePathTextBox.Text = sourceDirectory;
                DataWinPathTextBlock.Text = $"data.win path: {dataWinPath}";
                GameVersionTextBlock.Text = "Version: detecting...";
                StartPortingButton.IsEnabled = false;
                StatusTextBlock.Text = "Inspecting data.win...";
            });

            AddLog($"Selected folder: {sourceDirectory}");

            // The game folder itself is passed to Apktool, which fails on non-ASCII paths.
            if (!SafeWorkspace.IsPathSafe(sourceDirectory))
            {
                AddLog(
                    "Warning: the game folder path contains non-ASCII characters (for example Cyrillic). "
                    + "Apktool may fail with \"error: failed to open directory\". "
                    + "Move the game to a path made of Latin letters, e.g. C:\\Games\\MyGame.",
                    true);
            }

            var version = await Task.Run(() => DataWinVersionReader.Read(dataWinPath)).ConfigureAwait(false);
            var isUte = DetectUteTemplate(sourceDirectory);
            _currentGameInfo = _currentGameInfo with { Version = version, IsUteTemplate = isUte };

            await Dispatcher.InvokeAsync(() =>
            {
                GameVersionTextBlock.Text = version.IsValid
                    ? $"Version: {version.DisplayVersion}"
                    : "Version: unknown";

                UteStatusTextBlock.Text = isUte
                    ? "✓ Legacy UTE template game (repair script will run automatically)"
                    : "✗ Not a legacy UTE template game";
                UteStatusTextBlock.Foreground = isUte
                    ? (Brush)Application.Current.FindResource("SuccessBrush")
                    : (Brush)Application.Current.FindResource("TextMutedBrush");

                UpdateStartPortingAvailability();
            });

            if (version.IsValid)
            {
                AddLog($"Version detected: {version.DisplayVersion} (Major={version.Major}, Minor={version.Minor}, Release={version.Release}, Build={version.Build}, Bytecode={version.BytecodeVersion})");
                AddLog(version.IsYyc
                    ? "  - Compiler: YYC (script injection unsupported, porting disabled)"
                    : "  - Compiler: VM");
                AddLog($"  - Raw GEN8 version: {version.RawGen8Version}");
                AddLog($"  - Chunk-name floor: {version.ChunkNameFloor}");
                AddLog($"  - Structural floor: {version.StructuralFloor}");
                AddLog(isUte ? "  - UTE template: yes (repair script available)" : "  - UTE template: no (mobile integration only)");
            }
            else
            {
                AddLog("Version detection failed: could not identify the GMS version of data.win.", true);
            }
        }

        private static readonly Regex PackageNameRegex = new(
            @"^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Restricts the app name: single/double quotes, XML special characters and control characters are rejected.
        /// Those characters make aapt2 fail while compiling strings.xml (e.g. "unescaped apostrophe").
        /// </summary>
        private static readonly Regex AppNameAllowedRegex = new(
            @"^[^'\""<>&\x00-\x1F\x7F]{1,64}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly char[] VersionInvalidChars = new[]
        {
            '<', '>', ':', '"', '/', '\\', '|', '?', '*'
        };

        private (bool AppNameValid, bool PackageValid, bool VersionValid, string Status) _apkInputValidation;

        private void ApkInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (StartPortingButton is null)
            {
                return;
            }

            ValidateApkInputs();
            UpdateStartPortingAvailability();
        }

        /// <summary>
        /// Validates the app name, package name and version inputs.
        /// Hint labels turn red or grey to give live feedback.
        /// </summary>
        private void ValidateApkInputs()
        {
            if (AppNameHintTextBlock is null || PackageNameHintTextBlock is null || VersionHintTextBlock is null)
            {
                return;
            }

            var validBrush = (Brush)FindResource("TextMutedBrush");
            var errorBrush = (Brush?)TryFindResource("ErrorBrush") ?? Brushes.IndianRed;

            string status;

            // 1) App name
            var appName = AppNameTextBox.Text;
            bool appNameValid;
            if (string.IsNullOrWhiteSpace(appName))
            {
                appNameValid = false;
                AppNameHintTextBlock.Text = "App name must not be empty.";
            }
            else if (!AppNameAllowedRegex.IsMatch(appName))
            {
                appNameValid = false;
                AppNameHintTextBlock.Text =
                    "Invalid characters: ' \" < > & and control characters are not allowed; length 1-64.";
            }
            else
            {
                appNameValid = true;
                AppNameHintTextBlock.Text =
                    "Allowed: letters, digits, spaces, underscores and hyphens. Not allowed: ' \" < > &";
            }
          
            AppNameHintTextBlock.Foreground = appNameValid ? validBrush : errorBrush;
            ToolTipService.SetToolTip(AppNameTextBox, appNameValid ? null : AppNameHintTextBlock.Text);

            // 2) Package name
            var packageName = PackageNameTextBox.Text.Trim();
            bool packageValid;
            if (string.IsNullOrWhiteSpace(packageName))
            {
                packageValid = false;
                PackageNameHintTextBlock.Text = "Package name must not be empty.";
            }
            else if (!PackageNameRegex.IsMatch(packageName))
            {
                packageValid = false;
                PackageNameHintTextBlock.Text =
                    "Invalid format: must start with a letter, contain only letters / digits / underscores, be dot-separated with at least two parts. Example: com.example.mygame";
            }
            else
            {
                packageValid = true;
                PackageNameHintTextBlock.Text =
                    "Format: starts with a letter, only letters / digits / underscores, dot-separated, at least two parts. Example: com.example.mygame";
            }
            PackageNameHintTextBlock.Foreground = packageValid ? validBrush : errorBrush;
            ToolTipService.SetToolTip(PackageNameTextBox, packageValid ? null : PackageNameHintTextBlock.Text);

            // 3) Version
            var version = VersionTextBox.Text.Trim();
            bool versionValid;
            if (string.IsNullOrWhiteSpace(version))
            {
                versionValid = false;
                VersionHintTextBlock.Text = "Version must not be empty.";
            }
            else if (version.Length > 64)
            {
                versionValid = false;
                VersionHintTextBlock.Text = "Version is too long (64 characters max).";
            }
            else if (version.Any(c => char.IsControl(c) || VersionInvalidChars.Contains(c)))
            {
                versionValid = false;
                VersionHintTextBlock.Text = "Invalid characters: control characters and < > : \" / \\ | ? * are not allowed.";
            }
            else
            {
                versionValid = true;
                VersionHintTextBlock.Text =
                    "Display version (versionName). Any string, x.y.z recommended. Control characters and invalid path symbols are rejected.";
            }
            VersionHintTextBlock.Foreground = versionValid ? validBrush : errorBrush;
            ToolTipService.SetToolTip(VersionTextBox, versionValid ? null : VersionHintTextBlock.Text);

            if (!appNameValid) status = "Invalid app name";
            else if (!packageValid) status = "Invalid package name";
            else if (!versionValid) status = "Invalid version";
            else status = "Ready";

            _apkInputValidation = (appNameValid, packageValid, versionValid, status);
        }

        private void UpdateStartPortingAvailability()
        {
            ValidateApkInputs();

            if (_currentGameInfo is null)
            {
                StartPortingButton.IsEnabled = false;
                StatusTextBlock.Text = "Ready";
                return;
            }

            if (!_currentGameInfo.Version.IsValid)
            {
                StartPortingButton.IsEnabled = false;
                StatusTextBlock.Text = "Unknown version - cannot port";
                return;
            }

            if (_currentGameInfo.Version.IsYyc)
            {
                StartPortingButton.IsEnabled = false;
                StatusTextBlock.Text = "YYC build - cannot port";
                return;
            }

            if (!_apkInputValidation.AppNameValid ||
                !_apkInputValidation.PackageValid ||
                !_apkInputValidation.VersionValid)
            {
                StartPortingButton.IsEnabled = false;
                StatusTextBlock.Text = _apkInputValidation.Status;
                return;
            }

            StartPortingButton.IsEnabled = true;
            StatusTextBlock.Text = "Ready";
        }

        private static string? ResolveDataWinPath(string selectedPath)
        {
            if (File.Exists(selectedPath) &&
                string.Equals(Path.GetFileName(selectedPath), "data.win", StringComparison.OrdinalIgnoreCase))
            {
                return selectedPath;
            }

            if (!Directory.Exists(selectedPath))
            {
                return null;
            }

            var candidate = Path.Combine(selectedPath, "data.win");
            return File.Exists(candidate) ? candidate : null;
        }

        private void AddMobileKeyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AnimateSubOptions(expand: true);
        }

        private void AddMobileKeyCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AnimateSubOptions(expand: false);
        }

        private void AnimateSubOptions(bool expand)
        {
            var targetHeight = expand ? 175d : 0d;
            var targetOpacity = expand ? 1d : 0d;

            if (expand)
            {
                SubOptionsContainer.Visibility = Visibility.Visible;
                SubOptionsContainer.IsHitTestVisible = true;
            }

            var duration = TimeSpan.FromMilliseconds(220);
            var heightAnimation = new DoubleAnimation(targetHeight, duration)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var opacityAnimation = new DoubleAnimation(targetOpacity, duration)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            if (!expand)
            {
                opacityAnimation.Completed += (_, _) =>
                {
                    SubOptionsContainer.IsHitTestVisible = false;
                    SubOptionsContainer.Visibility = Visibility.Collapsed;
                };
            }

            SubOptionsContainer.BeginAnimation(HeightProperty, heightAnimation);
            SubOptionsContainer.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        private void AddLog(string message, bool isError = false)
        {
            var prefix = isError ? "[Error]" : "[Info]";
            var line = $"{DateTime.Now:HH:mm:ss} {prefix} {message}";

            _ = Dispatcher.InvokeAsync(() => Logs.Add(line));
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

        private void BrowseIconButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select app icon",
                Filter = "PNG image (*.png)|*.png|All files|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedIconPath = dialog.FileName;
                IconPathTextBox.Text = dialog.FileName;
                var fi = new FileInfo(dialog.FileName);
                IconPreviewTextBlock.Text = $"Size: {fi.Length / 1024} KB";
            }
        }

        private void BrowseSplashButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select splash image",
                Filter = "PNG image (*.png)|*.png|All files|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedSplashPath = dialog.FileName;
                SplashPathTextBox.Text = dialog.FileName;
                var fi = new FileInfo(dialog.FileName);
                SplashPreviewTextBlock.Text = $"Size: {fi.Length / 1024} KB";
            }
        }

        private async void StartPortingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGameInfo is null)
            {
                AddLog("Error: select a valid data.win first.", true);
                return;
            }

            if (!_currentGameInfo.Version.IsValid)
            {
                AddLog("Error: the version has not been identified yet.", true);
                return;
            }

            if (_currentGameInfo.Version.IsYyc)
            {
                AddLog("Error: YYC build detected; the script injection pipeline does not support it.", true);
                return;
            }

            ValidateApkInputs();
            if (!_apkInputValidation.AppNameValid ||
                !_apkInputValidation.PackageValid ||
                !_apkInputValidation.VersionValid)
            {
                AddLog($"Error: {_apkInputValidation.Status}. Fix the highlighted fields in the APK build settings.", true);
                MessageBox.Show(this,
                    $"Input validation failed: {_apkInputValidation.Status}\n\n"
                    + "Check the red hint text below each input field.",
                    "Invalid configuration",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var appName = AppNameTextBox.Text.Trim();
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeAppName = invalidChars.Aggregate(appName, (c, ch) => c.Replace(ch, '_'));
            if (string.IsNullOrWhiteSpace(safeAppName)) safeAppName = "MyGame";

            var defaultFileName = safeAppName;

            var saveDialog = new SaveFileDialog
            {
                Title = "Choose where to save the APK",
                Filter = "APK file (*.apk)|*.apk",
                FileName = $"{defaultFileName}.apk",
                AddExtension = true,
                DefaultExt = ".apk",
                RestoreDirectory = true
            };

            if (saveDialog.ShowDialog() != true)
                return;

            var outputPath = saveDialog.FileName;
            var outputDir = Path.GetDirectoryName(outputPath) ?? string.Empty;
            var outputFileName = Path.GetFileNameWithoutExtension(outputPath);

            var options = new[]
            {
                AddMobileKeyCheckBox.IsChecked == true,
                MobileF2CheckBox.IsChecked == true,
                MobileHealCheckBox.IsChecked == true,
                MobileCnCheckBox.IsChecked == true,
                AndroidKeyboardCheckBox.IsChecked == true,
                DualControlsCheckBox.IsChecked == true,
                EmbedMusicCheckBox.IsChecked == true,
                AutoTouchLayerCheckBox.IsChecked == true,
                MobileOptimizationCheckBox.IsChecked == true
            };

            var packageName = PackageNameTextBox.Text.Trim();
            var version = VersionTextBox.Text.Trim();

            StartPortingButton.IsEnabled = false;
            ResetButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
            BuildProgressBar.Value = 0;
            ProgressTextBlock.Text = "0%";
            StatusTextBlock.Text = "Porting...";

            _portingCts?.Dispose();
            _portingCts = new CancellationTokenSource();

            var progress = new Progress<(int Percent, string Message)>(p =>
            {
                void ApplyProgress()
                {
                    BuildProgressBar.Value = Math.Clamp(p.Percent / 100d, 0d, 1d);
                    ProgressTextBlock.Text = $"{p.Percent}%";
                    StatusTextBlock.Text = p.Message;
                }

                if (Dispatcher.CheckAccess())
                {
                    ApplyProgress();
                }
                else
                {
                    _ = Dispatcher.InvokeAsync(ApplyProgress);
                }
            });

            string? modifiedDataWinPath = null;

            try
            {
                AddLog("===== Porting started =====");

                AddLog("Step 1: patching data.win (built-in UTMT script engine)...");
                modifiedDataWinPath = Path.Combine(
                    SafeWorkspace.CreateDirectory("port"),
                    "game.droid");
                await _utmtService
                    .ModifyDataWinToPath(
                        _currentGameInfo.DataWinPath,
                        options,
                        modifiedDataWinPath,
                        _portingCts.Token);
                AddLog("data.win patched and written to the working directory.");

                AddLog("Step 2: selecting the APK template...");
                var templateApk = _apkBuilder.FindTemplateApk(_currentGameInfo.Version);
                AddLog($"Using template: {Path.GetFileName(templateApk)}");

                AddLog("Steps 3-5: building the APK...");
                bool embedMusic = EmbedMusicCheckBox.IsChecked == true;
                await _apkBuilder.BuildApkAsync(
                    templateApk,
                    _currentGameInfo.SourceDirectory,
                    modifiedDataWinPath,
                    outputPath,
                    appName,
                    packageName,
                    version,
                    _selectedIconPath,
                    _selectedSplashPath,
                    _currentGameInfo.IsUteTemplate,
                    embedMusic,
                    progress,
                    _portingCts.Token);

                _lastOutputDir = outputDir;

                await Dispatcher.InvokeAsync(() =>
                {
                    StatusTextBlock.Text = "Porting complete!";
                });

                AddLog("===== Porting succeeded =====");
                await Dispatcher.InvokeAsync(() =>
                    MessageBox.Show(this,
                        $"Porting succeeded.\n\nThe APK was saved to:\n{outputPath}",
                        "Porting complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information));
            }
            catch (OperationCanceledException)
            {
                AddLog("Porting cancelled.", true);
                await Dispatcher.InvokeAsync(() => StatusTextBlock.Text = "Cancelled");
            }
            catch (Exception ex)
            {
                AddLog($"Porting failed: {ex.Message}", true);
                await Dispatcher.InvokeAsync(() => StatusTextBlock.Text = "Porting failed");
                await Dispatcher.InvokeAsync(() =>
                    MessageBox.Show(this,
                        $"Porting failed:\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error));
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(modifiedDataWinPath) &&
                        File.Exists(modifiedDataWinPath))
                    {
                        File.Delete(modifiedDataWinPath);
                    }
                }
                catch
                {
                    // Ignore temporary data cleanup errors.
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    UpdateStartPortingAvailability();
                    ResetButton.IsEnabled = true;
                    CancelButton.IsEnabled = false;
                });
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _currentGameInfo = null;
            _selectedIconPath = null;
            _selectedSplashPath = null;
            _lastOutputDir = null;

            SourcePathTextBox.Text = "No folder selected";
            DataWinPathTextBlock.Text = "data.win path: not detected";
            GameVersionTextBlock.Text = "Version: not detected";
            UteStatusTextBlock.Text = "UTE template: waiting for detection...";
            UteStatusTextBlock.Foreground = (Brush)FindResource("TextMutedBrush");
            StartPortingButton.IsEnabled = false;

            AppNameTextBox.Text = "MyGame";
            PackageNameTextBox.Text = "com.example.mygame";
            VersionTextBox.Text = "1.0.0";
            IconPathTextBox.Text = "No icon selected (default will be used)";
            IconPreviewTextBlock.Text = "";
            SplashPathTextBox.Text = "No splash image selected (default will be used)";
            SplashPreviewTextBlock.Text = "";

            AddMobileKeyCheckBox.IsChecked = false;
            MobileF2CheckBox.IsChecked = true;
            MobileHealCheckBox.IsChecked = false;
            MobileCnCheckBox.IsChecked = true;
            AndroidKeyboardCheckBox.IsChecked = false;
            DualControlsCheckBox.IsChecked = false;
            EmbedMusicCheckBox.IsChecked = false;
            AutoTouchLayerCheckBox.IsChecked = true;
            MobileOptimizationCheckBox.IsChecked = true;

            BuildProgressBar.Value = 0;
            ProgressTextBlock.Text = "0%";
            StatusTextBlock.Text = "Ready";

            Logs.Add("Interface reset.");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _portingCts?.Cancel();
            AddLog("Cancelling...");
        }
    }
}
