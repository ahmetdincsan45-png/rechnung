using System.Text.Json;
using System.IO;
using Örnek.Models;

namespace Örnek.Services;

public sealed class FirmaProfilService
{
    private static string AppFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Rechnung");

    private static string ProfilPath => Path.Combine(AppFolder, "firma-profil.json");

    public FirmaProfil LoadOrDefault()
    {
        try
        {
            if (!File.Exists(ProfilPath))
                return CreateDefault();

            var json = File.ReadAllText(ProfilPath);
            var profil = JsonSerializer.Deserialize<FirmaProfil>(json);
            return profil ?? CreateDefault();
        }
        catch
        {
            return CreateDefault();
        }
    }

    public void Save(FirmaProfil profil)
    {
        Directory.CreateDirectory(AppFolder);
        var json = JsonSerializer.Serialize(profil, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ProfilPath, json);
    }

    public string? SaveLogoCopy(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return null;

        Directory.CreateDirectory(AppFolder);

        var ext = Path.GetExtension(sourcePath);
        var dest = Path.Combine(AppFolder, "logo" + ext);
        File.Copy(sourcePath, dest, overwrite: true);
        return dest;
    }

    private static FirmaProfil CreateDefault()
    {
        return new FirmaProfil
        {
            Adresse = new Adresse
            {
                Firmenname = "Meine Firma GmbH",
                Strasse = "Hauptstrasse",
                Hausnummer = "123",
                Postleitzahl = "10115",
                Stadt = "Berlin",
                Telefon = "+49 (0) 30 12345678",
                Email = "info@meinefirma.de",
                Webseite = "www.meinefirma.de"
            },
            Steuernummer = "12 345 678 901",
            UstIdNr = "DE 123 456 789",
            Zahlungsbedingungen = "Zahlbar innerhalb von 14 Tagen nach Rechnungsdatum",
            Angebotsbedingungen = "",
            AngebotEinleitungText = "",
            AngebotHaftungText = "",
            AngebotAuftragText = "",
            AngebotWiderrufText = "",
            Kontoinhaber = "Meine Firma GmbH",
            IBAN = "DE89370400440532013000",
            BIC = "COBADEFFXXX"
        };
    }
}
