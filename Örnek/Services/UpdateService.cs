using System.Net.Http;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Örnek.Models;

namespace Örnek.Services;

public sealed class UpdateService
{
    private static readonly HttpClient HttpClient = new();

    public async Task<UpdateCheckResult?> CheckForUpdateAsync(string manifestUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifestUrl))
            return null;

        try
        {
            if (!Uri.TryCreate(manifestUrl, UriKind.Absolute, out var manifestUri))
                return null;

            using var response = await HttpClient.GetAsync(manifestUri, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (manifest is null || !Version.TryParse(manifest.Version, out var availableVersion))
                return null;

            var downloadUrl = ResolveDownloadUrl(manifestUri, manifest.DownloadUrl);
            var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
            return new UpdateCheckResult
            {
                IsUpdateAvailable = availableVersion > currentVersion,
                CurrentVersion = currentVersion,
                AvailableVersion = availableVersion,
                DownloadUrl = downloadUrl,
                Notes = manifest.Notes ?? string.Empty
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> DownloadInstallerAsync(string downloadUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var downloadUri))
            throw new InvalidOperationException("Geçersiz güncelleme bağlantısı.");

        var fileName = Path.GetFileName(downloadUri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "Rechnung-Setup.exe";

        var targetDirectory = Path.Combine(Path.GetTempPath(), "Rechnung", "Updates");
        Directory.CreateDirectory(targetDirectory);

        var targetPath = Path.Combine(targetDirectory, fileName);

        using var response = await HttpClient.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var output = File.Create(targetPath);
        await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);

        return targetPath;
    }

    public void StartInstaller(string installerPath)
    {
        if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath))
            throw new FileNotFoundException("Installer dosyası bulunamadı.", installerPath);

        Process.Start(new ProcessStartInfo(installerPath)
        {
            UseShellExecute = true
        });
    }

    private static string ResolveDownloadUrl(Uri manifestUri, string? downloadUrl)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl))
            return string.Empty;

        if (Uri.TryCreate(downloadUrl, UriKind.Absolute, out var absoluteUri))
            return absoluteUri.ToString();

        return new Uri(manifestUri, downloadUrl).ToString();
    }
}
