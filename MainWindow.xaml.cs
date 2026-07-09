using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using KnockingTool.Models;
using KnockingTool.Services;
using Microsoft.Win32;

namespace KnockingTool;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<KnockNode> _nodes = [];
    private readonly RepeatSettings _repeatSettings = new();
    private readonly AppSettings _appSettings;
    private readonly PersistenceService _persistence;
    private CancellationTokenSource? _runCts;
    private UpdateInfo? _pendingUpdate;
    private CancellationTokenSource? _downloadCts;

    public MainWindow(PersistenceService persistence, IEnumerable<KnockNode> loadedNodes, RepeatSettings loadedRepeat, AppSettings appSettings)
    {
        _persistence = persistence;
        _appSettings = appSettings;

        InitializeComponent();
        NodesListBox.ItemsSource = _nodes;

        foreach (var node in loadedNodes)
        {
            _nodes.Add(node);
        }

        _repeatSettings.Count = loadedRepeat.Count;
        _repeatSettings.IntervalMs = loadedRepeat.IntervalMs;
        RepeatCountTextBox.DataContext = _repeatSettings;
        RepeatIntervalTextBox.DataContext = _repeatSettings;

        _persistence.Attach(_nodes, _repeatSettings, _appSettings);

        if (_nodes.Count > 0)
        {
            NodesListBox.SelectedIndex = 0;
        }

        UpdateThemeToggleButton();
        Log("برنامه آماده است.");
        Log($"نسخه فعلی: {UpdateService.CurrentVersion}");
        Log($"ذخیره خودکار: {_persistence.ConfigPath}");

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await CheckForUpdatesAsync();
    }

    private void CopyrightLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/ho3inzahedi/KnockingTool") { UseShellExecute = true });
        e.Handled = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        _persistence.Dispose();
        base.OnClosed(e);
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _appSettings.IsDarkTheme = !_appSettings.IsDarkTheme;
        ThemeManager.SetTheme(_appSettings.IsDarkTheme);
        UpdateThemeToggleButton();
        _persistence.SaveNow();
    }

    private void UpdateThemeToggleButton()
    {
        ThemeToggleButton.Content = _appSettings.IsDarkTheme ? "☀️" : "🌙";
        ThemeToggleButton.ToolTip = _appSettings.IsDarkTheme ? "تم روشن" : "تم تیره";
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Clear();
    }

    private void NodesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NodesListBox.SelectedItem is KnockNode node)
        {
            NodeNameTextBox.DataContext = node;
            NodeIpTextBox.DataContext = node;
            StepsDataGrid.ItemsSource = node.Steps;
        }
        else
        {
            NodeNameTextBox.DataContext = null;
            NodeIpTextBox.DataContext = null;
            StepsDataGrid.ItemsSource = null;
        }
    }

    private void AddNode_Click(object sender, RoutedEventArgs e)
    {
        var node = new KnockNode
        {
            Name = $"Node {_nodes.Count + 1}",
            DestinationIp = "192.168.1.1"
        };
        _nodes.Add(node);
        NodesListBox.SelectedItem = node;
    }

    private void DeleteNode_Click(object sender, RoutedEventArgs e)
    {
        if (NodesListBox.SelectedItem is not KnockNode node)
        {
            return;
        }

        _nodes.Remove(node);
    }

    private void AddStep_Click(object sender, RoutedEventArgs e)
    {
        if (NodesListBox.SelectedItem is not KnockNode node)
        {
            MessageBox.Show("ابتدا یک نود انتخاب کنید.", "هشدار", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var nextOrder = node.Steps.Count == 0 ? 1 : node.Steps.Max(s => s.Order) + 1;
        node.Steps.Add(new KnockStep { Order = nextOrder, Protocol = KnockProtocol.Tcp, Port = 80, DelayMs = 500 });
        StepsDataGrid.Items.Refresh();
    }

    private void DeleteStep_Click(object sender, RoutedEventArgs e)
    {
        if (NodesListBox.SelectedItem is not KnockNode node || StepsDataGrid.SelectedItem is not KnockStep step)
        {
            return;
        }

        node.Steps.Remove(step);
        StepsDataGrid.Items.Refresh();
    }

    private async void RunSelectedNode_Click(object sender, RoutedEventArgs e)
    {
        if (NodesListBox.SelectedItem is not KnockNode node)
        {
            MessageBox.Show("ابتدا یک نود انتخاب کنید.", "هشدار", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await RunNodesAsync([node]);
    }

    private async void RunAllNodes_Click(object sender, RoutedEventArgs e)
    {
        if (_nodes.Count == 0)
        {
            MessageBox.Show("هیچ نودی تعریف نشده است.", "هشدار", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await RunNodesAsync(_nodes.ToList());
    }

    private async Task RunNodesAsync(IReadOnlyList<KnockNode> nodes)
    {
        _runCts?.Cancel();
        _runCts = new CancellationTokenSource();
        var token = _runCts.Token;

        var repeatCount = Math.Max(0, _repeatSettings.Count);
        var repeatInterval = Math.Max(0, _repeatSettings.IntervalMs);
        var isInfinite = repeatCount == 0;

        SetRunningState(true);

        try
        {
            var iteration = 0;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                iteration++;

                Log(isInfinite
                    ? $"--- تکرار {iteration} (بی‌نهایت) ---"
                    : $"--- تکرار {iteration}/{repeatCount} ---");

                foreach (var node in nodes)
                {
                    await SequenceRunner.RunNodeAsync(node, Log, token);
                }

                if (!isInfinite && iteration >= repeatCount)
                {
                    break;
                }

                if (repeatInterval > 0)
                {
                    Log($"انتظار {repeatInterval}ms تا تکرار بعدی ...");
                    await Task.Delay(repeatInterval, token);
                }
            }

            Log("همه توالی‌ها با موفقیت اجرا شدند.");
        }
        catch (OperationCanceledException)
        {
            Log("اجرا توسط کاربر متوقف شد.");
        }
        catch (Exception ex)
        {
            Log($"خطای کلی: {ex.Message}");
        }
        finally
        {
            SetRunningState(false);
        }
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _runCts?.Cancel();
    }

    private void SaveConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            FileName = "knocking-config.json"
        };

        if (dialog.ShowDialog() == true)
        {
            ConfigService.Save(dialog.FileName, _nodes, _repeatSettings, _appSettings);
            Log($"پیکربندی ذخیره شد: {dialog.FileName}");
        }
    }

    private void LoadConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var (loadedNodes, loadedRepeat, loadedApp) = ConfigService.Load(dialog.FileName);
            _nodes.Clear();
            foreach (var node in loadedNodes)
            {
                _nodes.Add(node);
            }

            _repeatSettings.Count = loadedRepeat.Count;
            _repeatSettings.IntervalMs = loadedRepeat.IntervalMs;

            _appSettings.IsDarkTheme = loadedApp.IsDarkTheme;
            ThemeManager.SetTheme(_appSettings.IsDarkTheme);
            UpdateThemeToggleButton();

            if (_nodes.Count > 0)
            {
                NodesListBox.SelectedIndex = 0;
            }

            _persistence.SaveNow();
            Log($"پیکربندی بارگذاری شد: {dialog.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Dispatcher.Invoke(() =>
        {
            LogTextBox.AppendText(line + Environment.NewLine);
            LogTextBox.ScrollToEnd();
        });
    }

    private void SetRunningState(bool isRunning)
    {
        Dispatcher.Invoke(() =>
        {
            StopButton.IsEnabled = isRunning;
            RunSelectedButton.IsEnabled = !isRunning;
            RunAllButton.IsEnabled = !isRunning;
        });
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var update = await UpdateService.CheckForUpdateAsync();
            if (update is null)
            {
                return;
            }

            if (string.Equals(_appSettings.DismissedUpdateVersion, update.TagName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _pendingUpdate = update;
            Dispatcher.Invoke(() =>
            {
                UpdateBannerTitle.Text = $"نسخه جدید {update.Version} در دسترس است";
                UpdateBannerSubtitle.Text =
                    $"نسخه فعلی شما: {UpdateService.CurrentVersion} — برای دریافت آخرین بهبودها دانلود کنید.";
                UpdateBanner.Visibility = Visibility.Visible;
            });
            Log($"به‌روزرسانی جدید یافت شد: {update.TagName}");
        }
        catch (Exception ex)
        {
            Log($"بررسی به‌روزرسانی ناموفق بود: {ex.Message}");
        }
    }

    private async void DownloadUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingUpdate is null)
        {
            return;
        }

        _downloadCts?.Cancel();
        _downloadCts = new CancellationTokenSource();
        var token = _downloadCts.Token;

        DownloadUpdateButton.IsEnabled = false;
        DismissUpdateButton.IsEnabled = false;
        UpdateProgressBar.Visibility = Visibility.Visible;
        UpdateProgressBar.Value = 0;

        var progress = new Progress<double>(value =>
        {
            Dispatcher.Invoke(() =>
            {
                UpdateProgressBar.Value = value;
                UpdateBannerSubtitle.Text = $"در حال دانلود... {value:P0}";
            });
        });

        try
        {
            var filePath = await UpdateService.DownloadUpdateAsync(_pendingUpdate, progress, token);
            Log($"نسخه جدید دانلود شد: {filePath}");

            var result = MessageBox.Show(
                $"نسخه {_pendingUpdate.Version} با موفقیت دانلود شد.\n\n" +
                "برای نصب، برنامه را ببندید و فایل جدید را جایگزین نسخه فعلی کنید.\n\n" +
                "آیا پوشه دانلود باز شود؟",
                "دانلود کامل شد",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = true });
            }

            UpdateBanner.Visibility = Visibility.Collapsed;
            _pendingUpdate = null;
        }
        catch (OperationCanceledException)
        {
            Log("دانلود به‌روزرسانی لغو شد.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دانلود:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            Log($"خطا در دانلود به‌روزرسانی: {ex.Message}");
        }
        finally
        {
            DownloadUpdateButton.IsEnabled = true;
            DismissUpdateButton.IsEnabled = true;
            UpdateProgressBar.Visibility = Visibility.Collapsed;
            if (_pendingUpdate is not null)
            {
                UpdateBannerSubtitle.Text =
                    $"نسخه فعلی شما: {UpdateService.CurrentVersion} — برای دریافت آخرین بهبودها دانلود کنید.";
            }
        }
    }

    private void DismissUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingUpdate is not null)
        {
            _appSettings.DismissedUpdateVersion = _pendingUpdate.TagName;
            _persistence.SaveNow();
        }

        UpdateBanner.Visibility = Visibility.Collapsed;
    }
}
