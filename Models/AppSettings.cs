namespace KnockingTool.Models;

public class AppSettings
{
    public bool IsDarkTheme { get; set; }

    /// <summary>Tag of the latest release banner dismissed by the user (e.g. v1.0.3).</summary>
    public string? DismissedUpdateVersion { get; set; }
}
