namespace Örnek.Models;

public sealed class UpdateCheckResult
{
    public bool IsUpdateAvailable { get; init; }
    public Version? CurrentVersion { get; init; }
    public Version? AvailableVersion { get; init; }
    public string DownloadUrl { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}
