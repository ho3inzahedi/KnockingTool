using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using KnockingTool.Models;

namespace KnockingTool.Services;

public static class UpdateService
{
    private const string RepoOwner = "ho3inzahedi";
    private const string RepoName = "KnockingTool";
    private const string ExeAssetName = "KnockingTool.exe";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders = { { "User-Agent", "KnockingTool-Updater" } }
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

    public static string UpdatesFolder
    {
        get
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KnockingTool",
                "Updates");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    public static async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
        await using var stream = await Http.GetStreamAsync(url, cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, JsonOptions, cancellationToken);

        if (release is null || string.IsNullOrWhiteSpace(release.TagName))
        {
            return null;
        }

        var latestVersion = ParseVersion(release.TagName);
        if (latestVersion <= CurrentVersion)
        {
            return null;
        }

        var asset = release.Assets?.FirstOrDefault(a =>
            string.Equals(a.Name, ExeAssetName, StringComparison.OrdinalIgnoreCase));

        if (asset is null || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
        {
            return null;
        }

        return new UpdateInfo
        {
            Version = latestVersion,
            TagName = release.TagName,
            DownloadUrl = asset.BrowserDownloadUrl,
            ReleaseNotes = release.Body
        };
    }

    public static async Task<string> DownloadUpdateAsync(
        UpdateInfo update,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var fileName = $"KnockingTool-{update.TagName.TrimStart('v', 'V')}.exe";
        var destination = Path.Combine(UpdatesFolder, fileName);

        if (File.Exists(destination))
        {
            File.Delete(destination);
        }

        using var response = await Http.GetAsync(
            update.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = File.Create(destination);

        var buffer = new byte[81920];
        long bytesRead = 0;
        int read;

        while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            bytesRead += read;

            if (totalBytes is > 0)
            {
                progress?.Report((double)bytesRead / totalBytes.Value);
            }
        }

        progress?.Report(1);
        return destination;
    }

    private static Version ParseVersion(string tag)
    {
        var normalized = tag.Trim().TrimStart('v', 'V');
        if (Version.TryParse(normalized, out var version))
        {
            return version;
        }

        var parts = normalized.Split('.', '-', '+');
        if (parts.Length >= 3
            && int.TryParse(parts[0], out var major)
            && int.TryParse(parts[1], out var minor)
            && int.TryParse(parts[2], out var build))
        {
            return new Version(major, minor, build);
        }

        return new Version(0, 0);
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }
}
