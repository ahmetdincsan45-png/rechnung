using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using UglyToad.PdfPig;
using Tesseract;

namespace Örnek.Services;

public sealed class LocalOcrService
{
    private sealed class OcrCandidate
    {
        public string Text { get; init; } = string.Empty;
        public float Confidence { get; init; }
        public int TextLength => Text.Length;
        public double Score => (Confidence * 100d) + Math.Min(TextLength, 400) / 10d;
    }

    public async Task<string> ExtractTextAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return string.Empty;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => await Task.Run(() => ExtractFromPdf(filePath)),
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tif" or ".tiff" => await Task.Run(() => ExtractFromImage(filePath)),
            _ => string.Empty
        };
    }

    public string GetTessdataPath()
    {
        var appBase = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appBase, "tessdata");
    }

    public void EnsureTessdataFolder()
    {
        Directory.CreateDirectory(GetTessdataPath());
    }

    public bool HasLanguageData()
    {
        var tessdata = GetTessdataPath();
        return File.Exists(Path.Combine(tessdata, "deu.traineddata")) ||
               File.Exists(Path.Combine(tessdata, "tur.traineddata")) ||
               File.Exists(Path.Combine(tessdata, "eng.traineddata"));
    }

    private string ExtractFromPdf(string filePath)
    {
        var sb = new StringBuilder();
        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            var text = page.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (sb.Length > 0)
                    sb.AppendLine().AppendLine();
                sb.Append(text);
            }
        }

        return sb.ToString();
    }

    private string ExtractFromImage(string filePath)
    {
        if (!HasLanguageData())
            throw new InvalidOperationException("OCR-Sprachdateien fehlen. Legen Sie tessdata mit deu.traineddata oder tur.traineddata im Ausgabeverzeichnis ab.");

        var languages = BuildLanguageString();
        using var engine = new TesseractEngine(GetTessdataPath(), languages, EngineMode.Default);
        using var image = Pix.LoadFromFile(filePath);
        using var page = engine.Process(image);
        return page.GetText()?.Trim() ?? string.Empty;
    }

    private string BuildLanguageString()
    {
        var tessdata = GetTessdataPath();
        var languages = new List<string>();

        if (File.Exists(Path.Combine(tessdata, "deu.traineddata")))
            languages.Add("deu");
        if (File.Exists(Path.Combine(tessdata, "tur.traineddata")))
            languages.Add("tur");
        if (File.Exists(Path.Combine(tessdata, "eng.traineddata")))
            languages.Add("eng");

        return string.Join("+", languages);
    }
}
