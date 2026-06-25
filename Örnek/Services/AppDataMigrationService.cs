using System.IO;

namespace Örnek.Services;

public sealed class AppDataMigrationService
{
    private readonly string _baseAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    private string LegacyAppFolder => Path.Combine(_baseAppDataPath, "Örnek");
    private string CurrentAppFolder => Path.Combine(_baseAppDataPath, "Rechnung");
    private string MigrationMarkerPath => Path.Combine(CurrentAppFolder, "migration-complete.marker");

    public void MigrateLegacyDataIfNeeded()
    {
        try
        {
            if (!Directory.Exists(LegacyAppFolder))
                return;

            Directory.CreateDirectory(CurrentAppFolder);

            if (File.Exists(MigrationMarkerPath))
                return;

            CopyDirectoryIfMissing(LegacyAppFolder, CurrentAppFolder);
            File.WriteAllText(MigrationMarkerPath, DateTime.Now.ToString("O"));
        }
        catch
        {
            // ignore migration issues and let the app continue
        }
    }

    private static void CopyDirectoryIfMissing(string sourceRoot, string targetRoot)
    {
        foreach (var directory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, directory);
            Directory.CreateDirectory(Path.Combine(targetRoot, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, file);
            var targetPath = Path.Combine(targetRoot, relativePath);
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            if (!File.Exists(targetPath))
                File.Copy(file, targetPath);
        }
    }
}
