namespace KnockingTool.Models;

public class KnockConfig
{
    public List<KnockNodeDto> Nodes { get; set; } = [];
    public RepeatSettings Repeat { get; set; } = new();
    public AppSettings App { get; set; } = new();
}

public class KnockNodeDto
{
    public string Name { get; set; } = string.Empty;
    public string DestinationIp { get; set; } = string.Empty;
    public List<KnockStepDto> Steps { get; set; } = [];
}

public class KnockStepDto
{
    public int Order { get; set; }
    public KnockProtocol Protocol { get; set; }
    public int Port { get; set; }
    public int PayloadSize { get; set; }
    public PayloadMode PayloadMode { get; set; }
    public string PayloadContent { get; set; } = string.Empty;
    public bool IncludeIpHeader { get; set; }
    public int DelayMs { get; set; }
}
