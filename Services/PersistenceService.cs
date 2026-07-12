using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using KnockingTool.Models;

namespace KnockingTool.Services;

public sealed class PersistenceService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly DispatcherTimer _saveTimer;
    private readonly string _configPath;
    private ObservableCollection<KnockNode>? _nodes;
    private RepeatSettings? _repeatSettings;
    private AppSettings? _appSettings;
    private bool _isLoading;

    public PersistenceService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "KnockingTool");
        Directory.CreateDirectory(folder);
        _configPath = Path.Combine(folder, "config.json");

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            SaveNow();
        };
    }

    public string ConfigPath => _configPath;

    public void Attach(ObservableCollection<KnockNode> nodes, RepeatSettings repeatSettings, AppSettings appSettings)
    {
        Detach();

        _nodes = nodes;
        _repeatSettings = repeatSettings;
        _appSettings = appSettings;

        _nodes.CollectionChanged += OnNodesCollectionChanged;
        foreach (var node in _nodes)
        {
            AttachNode(node);
        }

        _repeatSettings.PropertyChanged += OnSettingsChanged;
    }

    public void Detach()
    {
        if (_nodes is null)
        {
            return;
        }

        _nodes.CollectionChanged -= OnNodesCollectionChanged;
        foreach (var node in _nodes)
        {
            DetachNode(node);
        }

        if (_repeatSettings is not null)
        {
            _repeatSettings.PropertyChanged -= OnSettingsChanged;
        }

        _nodes = null;
        _repeatSettings = null;
        _appSettings = null;
    }

    public (ObservableCollection<KnockNode> Nodes, RepeatSettings Repeat, AppSettings App) LoadAll()
    {
        _isLoading = true;
        try
        {
            if (!File.Exists(_configPath))
            {
                return ([], new RepeatSettings(), new AppSettings());
            }

            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<KnockConfig>(json, JsonOptions);
            if (config is null)
            {
                return ([], new RepeatSettings(), new AppSettings());
            }

            var nodes = new ObservableCollection<KnockNode>();
            foreach (var nodeDto in config.Nodes)
            {
                var node = new KnockNode
                {
                    Name = nodeDto.Name,
                    DestinationIp = nodeDto.DestinationIp
                };

                foreach (var stepDto in nodeDto.Steps.OrderBy(s => s.Order))
                {
                    node.Steps.Add(new KnockStep
                    {
                        Order = stepDto.Order,
                        Protocol = stepDto.Protocol,
                        Port = stepDto.Port,
                        PayloadSize = stepDto.PayloadSize,
                        PayloadMode = stepDto.PayloadMode,
                        PayloadContent = stepDto.PayloadContent,
                        IncludeIpHeader = stepDto.IncludeIpHeader,
                        DelayMs = stepDto.DelayMs
                    });
                }

                nodes.Add(node);
            }

            return (nodes, config.Repeat ?? new RepeatSettings(), config.App ?? new AppSettings());
        }
        finally
        {
            _isLoading = false;
        }
    }

    public void SaveNow()
    {
        if (_isLoading || _nodes is null || _repeatSettings is null || _appSettings is null)
        {
            return;
        }

        var config = new KnockConfig
        {
            Nodes = _nodes.Select(node => new KnockNodeDto
            {
                Name = node.Name,
                DestinationIp = node.DestinationIp,
                Steps = node.Steps.Select(step => new KnockStepDto
                {
                    Order = step.Order,
                    Protocol = step.Protocol,
                    Port = step.Port,
                    PayloadSize = step.PayloadSize,
                    PayloadMode = step.PayloadMode,
                    PayloadContent = step.PayloadContent,
                    IncludeIpHeader = step.IncludeIpHeader,
                    DelayMs = step.DelayMs
                }).ToList()
            }).ToList(),
            Repeat = new RepeatSettings
            {
                Count = _repeatSettings.Count,
                IntervalMs = _repeatSettings.IntervalMs
            },
            App = new AppSettings
            {
                IsDarkTheme = _appSettings.IsDarkTheme
            }
        };

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json);
    }

    public void ScheduleSave()
    {
        if (_isLoading)
        {
            return;
        }

        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (KnockNode node in e.OldItems)
            {
                DetachNode(node);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (KnockNode node in e.NewItems)
            {
                AttachNode(node);
            }
        }

        ScheduleSave();
    }

    private void AttachNode(KnockNode node)
    {
        node.PropertyChanged += OnNodeChanged;
        node.Steps.CollectionChanged += OnStepsCollectionChanged;
        foreach (var step in node.Steps)
        {
            step.PropertyChanged += OnStepChanged;
        }
    }

    private void DetachNode(KnockNode node)
    {
        node.PropertyChanged -= OnNodeChanged;
        node.Steps.CollectionChanged -= OnStepsCollectionChanged;
        foreach (var step in node.Steps)
        {
            step.PropertyChanged -= OnStepChanged;
        }
    }

    private void OnStepsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (KnockStep step in e.OldItems)
            {
                step.PropertyChanged -= OnStepChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (KnockStep step in e.NewItems)
            {
                step.PropertyChanged += OnStepChanged;
            }
        }

        ScheduleSave();
    }

    private void OnNodeChanged(object? sender, PropertyChangedEventArgs e) => ScheduleSave();
    private void OnStepChanged(object? sender, PropertyChangedEventArgs e) => ScheduleSave();
    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e) => ScheduleSave();

    public void Dispose()
    {
        _saveTimer.Stop();
        SaveNow();
        Detach();
    }
}
