namespace KnockingTool.Models;

public sealed class UpdateInfo
{
    public required Version Version { get; init; }

    public required string TagName { get; init; }

    public required string DownloadUrl { get; init; }

    public string? ReleaseNotes { get; init; }
}
