using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using KnockingTool.Models;

namespace KnockingTool.Services;

public static class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Save(string filePath, ObservableCollection<KnockNode> nodes, RepeatSettings? repeat = null, AppSettings? app = null)
    {
        var config = new KnockConfig
        {
            Nodes = nodes.Select(node => new KnockNodeDto
            {
                Name = node.Name,
                DestinationIp = node.DestinationIp,
                Steps = node.Steps.Select(step => new KnockStepDto
                {
                    Order = step.Order,
                    Protocol = step.Protocol,
                    Port = step.Port,
                    PayloadSize = step.PayloadSize,
                    IncludeIpHeader = step.IncludeIpHeader,
                    DelayMs = step.DelayMs
                }).ToList()
            }).ToList(),
            Repeat = repeat ?? new RepeatSettings(),
            App = app ?? new AppSettings()
        };

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public static (ObservableCollection<KnockNode> Nodes, RepeatSettings Repeat, AppSettings App) Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<KnockConfig>(json, JsonOptions)
            ?? throw new InvalidOperationException("فایل پیکربندی نامعتبر است");

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
                    IncludeIpHeader = stepDto.IncludeIpHeader,
                    DelayMs = stepDto.DelayMs
                });
            }

            nodes.Add(node);
        }

        return (nodes, config.Repeat ?? new RepeatSettings(), config.App ?? new AppSettings());
    }
}
