using System.Text;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Örnek.Models;
using QuestPDF.Fluent;

namespace Örnek.Services
{
    public class RechnungService
    {
        private static string GetBedingungenText(Rechnung rechnung)
        {
            return rechnung.DokumentTyp == DokumentTyp.Angebot
                ? (rechnung.Angebotsbedingungen ?? string.Empty)
                : (rechnung.Zahlungsbedingungen ?? string.Empty);
        }

        public byte[] GenerierePdfBytes(Rechnung rechnung)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            return new RechnungPdfDocument(rechnung).GeneratePdf();
        }

        public byte[] GenerierePdfBytes(Rechnung rechnung, IEnumerable<string>? appendixPdfPaths)
        {
            var basePdf = GenerierePdfBytes(rechnung);
            var validAppendices = appendixPdfPaths?
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (validAppendices == null || validAppendices.Count == 0)
                return basePdf;

            using var output = new PdfDocument();
            AppendPdf(output, basePdf);

            foreach (var appendixPath in validAppendices)
                AppendDocument(output, appendixPath);

            using var stream = new MemoryStream();
            output.Save(stream, false);
            return stream.ToArray();
        }

        private static void AppendDocument(PdfDocument target, string appendixPath)
        {
            var extension = Path.GetExtension(appendixPath);

            if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                var appendixBytes = File.ReadAllBytes(appendixPath);
                AppendPdf(target, appendixBytes);
                return;
            }

            if (IsSupportedImageExtension(extension))
            {
                AppendImage(target, appendixPath);
                return;
            }
        }

        private static void AppendPdf(PdfDocument target, byte[] pdfBytes)
        {
            using var stream = new MemoryStream(pdfBytes);
            using var source = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
            foreach (var page in source.Pages)
                target.AddPage(page);
        }

        private static void AppendImage(PdfDocument target, string imagePath)
        {
            using var image = XImage.FromFile(imagePath);

            var page = target.AddPage();
            page.Width = XUnit.FromMillimeter(210);
            page.Height = XUnit.FromMillimeter(297);

            using var graphics = XGraphics.FromPdfPage(page);

            const double margin = 24d;
            var usableWidth = page.Width.Point - (margin * 2);
            var usableHeight = page.Height.Point - (margin * 2);

            var widthRatio = usableWidth / image.PixelWidth * 72d / image.HorizontalResolution;
            var heightRatio = usableHeight / image.PixelHeight * 72d / image.VerticalResolution;
            var scale = Math.Min(widthRatio, heightRatio);

            var drawWidth = (image.PixelWidth * 72d / image.HorizontalResolution) * scale;
            var drawHeight = (image.PixelHeight * 72d / image.VerticalResolution) * scale;
            var x = (page.Width.Point - drawWidth) / 2;
            var y = (page.Height.Point - drawHeight) / 2;

            graphics.DrawImage(image, x, y, drawWidth, drawHeight);
        }

        private static bool IsSupportedImageExtension(string? extension) =>
            string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase);

        public FlowDocument GeneriereFlowDocument(Rechnung rechnung)
        {
            var culture = CultureInfo.GetCultureInfo("de-DE");

            // A4 page size at 96 DPI (WPF device independent units)
            const double a4Width = 794;  // 8.27in * 96
            const double a4Height = 1123; // 11.69in * 96

            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                PageWidth = a4Width,
                PageHeight = a4Height,
                PagePadding = new Thickness(48),
                ColumnWidth = double.PositiveInfinity,
                LineHeight = 18,
                TextAlignment = TextAlignment.Left
            };

            // Keep content aligned to the printable area (single column)
            doc.ColumnWidth = doc.PageWidth - doc.PagePadding.Left - doc.PagePadding.Right;

            var isAngebot = rechnung.DokumentTyp == DokumentTyp.Angebot;

            var title = new Paragraph(new Run(isAngebot ? "Angebot" : "Rechnung"))
            {
                FontSize = 28,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x36, 0x8A)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(title);

            var meta = new Paragraph { Margin = new Thickness(0, 0, 0, 18) };
            meta.Inlines.Add(new Bold(new Run(isAngebot ? "Angebotsnummer: " : "Rechnungsnummer: ")));
            meta.Inlines.Add(new Run(rechnung.Rechnungsnummer));
            meta.Inlines.Add(new LineBreak());
            meta.Inlines.Add(new Bold(new Run(isAngebot ? "Angebotsdatum: " : "Rechnungsdatum: ")));
            meta.Inlines.Add(new Run(rechnung.Rechnungsdatum.ToString("dd.MM.yyyy", culture)));

            if (isAngebot)
            {
                if (!string.IsNullOrWhiteSpace(rechnung.Lieferzeit))
                {
                    meta.Inlines.Add(new LineBreak());
                    meta.Inlines.Add(new Bold(new Run("Lieferzeit: ")));
                    meta.Inlines.Add(new Run(rechnung.Lieferzeit));
                }
            }
            else
            {
                if (rechnung.Leistungsdatum.HasValue)
                {
                    meta.Inlines.Add(new LineBreak());
                    meta.Inlines.Add(new Bold(new Run("Leistungsdatum: ")));
                    meta.Inlines.Add(new Run(rechnung.Leistungsdatum.Value.ToString("dd.MM.yyyy", culture)));
                }
                if (rechnung.Fälligkeitsdatum.HasValue)
                {
                    meta.Inlines.Add(new LineBreak());
                    meta.Inlines.Add(new Bold(new Run("Fälligkeitsdatum: ")));
                    meta.Inlines.Add(new Run(rechnung.Fälligkeitsdatum.Value.ToString("dd.MM.yyyy", culture)));
                }
            }
            doc.Blocks.Add(meta);

            var headerTable = new Table { CellSpacing = 0 };
            headerTable.Margin = new Thickness(0, 0, 0, 10);
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(20) });
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            headerTable.RowGroups.Add(new TableRowGroup());

            var row = new TableRow();
            headerTable.RowGroups[0].Rows.Add(row);

            row.Cells.Add(CreateBoxCell("Absender", rechnung.Absender.ToString() +
                (string.IsNullOrWhiteSpace(rechnung.Steuernummer) ? "" : $"\nSteuernummer: {rechnung.Steuernummer}") +
                (string.IsNullOrWhiteSpace(rechnung.UstIdNr) ? "" : $"\nUSt-IdNr: {rechnung.UstIdNr}")));

            row.Cells.Add(new TableCell(new Paragraph()) { Padding = new Thickness(0), BorderThickness = new Thickness(0) });

            var empfaengerText = new StringBuilder();
            empfaengerText.Append(rechnung.Empfänger.ToString());
            if (!string.IsNullOrWhiteSpace(rechnung.Objektname))
                empfaengerText.Append($"\n\nObjekt: {rechnung.Objektname}");

            row.Cells.Add(CreateBoxCell("Empfänger", empfaengerText.ToString()));

            doc.Blocks.Add(headerTable);
            doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 0, 0, 16) });

            var positionsTitle = new Paragraph(new Run("Positionen"))
            {
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            doc.Blocks.Add(positionsTitle);

            var table = new Table { CellSpacing = 0 };
            table.Margin = new Thickness(0, 0, 0, 8);
            table.Columns.Add(new TableColumn { Width = new GridLength(40) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(70) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(90) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(110) });
            table.Columns.Add(new TableColumn { Width = new GridLength(120) });
            table.RowGroups.Add(new TableRowGroup());

            var header = new TableRow();
            table.RowGroups[0].Rows.Add(header);
            AddHeaderCell(header, "#");
            AddHeaderCell(header, "Beschreibung");
            AddHeaderCell(header, "Menge");
            AddHeaderCell(header, "Einheit");
            AddHeaderCell(header, "Preis");
            AddHeaderCell(header, "MwSt.%");
            AddHeaderCell(header, "Netto");
            AddHeaderCell(header, "Brutto");

            var altBg = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));
            var i = 0;
            foreach (var p in rechnung.Positionen)
            {
                var r = new TableRow();
                table.RowGroups[0].Rows.Add(r);

                var bg = (i++ % 2 == 1) ? altBg : null;

                AddBodyCell(r, p.Nummer.ToString(culture), TextAlignment.Right, bg);
                var desc = string.IsNullOrWhiteSpace(p.Notiz)
                    ? p.Beschreibung
                    : $"{p.Beschreibung}\n{p.Notiz}";
                AddBodyCell(r, desc, TextAlignment.Left, bg);
                AddBodyCell(r, p.Menge.ToString("0.##", culture), TextAlignment.Right, bg);
                AddBodyCell(r, p.Einheit, TextAlignment.Left, bg);
                AddBodyCell(r, p.EinzelPreis.ToString("C", culture), TextAlignment.Right, bg);
                AddBodyCell(r, p.Steuersatz.ToString("0.##", culture), TextAlignment.Right, bg);
                AddBodyCell(r, p.Betrag.ToString("C", culture), TextAlignment.Right, bg);
                AddBodyCell(r, p.GesamtBetrag.ToString("C", culture), TextAlignment.Right, bg);
            }

            doc.Blocks.Add(table);

            // Totals
            doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 12, 0, 0) });
            var totals = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 10) };
            totals.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            totals.Columns.Add(new TableColumn { Width = new GridLength(170) });
            totals.RowGroups.Add(new TableRowGroup());

            AddTotalRow(totals.RowGroups[0], "Summe Netto", rechnung.GesamtNettoBetrag.ToString("C", culture), false);
            foreach (var g in rechnung.PositionenNachSteuersatz.OrderBy(g => g.Key))
            {
                var steuer = g.Sum(x => x.Steuer);
                AddTotalRow(totals.RowGroups[0], $"MwSt. ({g.Key:0.##}%)", steuer.ToString("C", culture), false);
            }
            AddTotalRow(totals.RowGroups[0], "Gesamtbetrag", rechnung.GesamtBruttoBetrag.ToString("C", culture), true);
            doc.Blocks.Add(totals);

            var bedingungen = GetBedingungenText(rechnung);
            if (!string.IsNullOrWhiteSpace(bedingungen))
            {
                var label = isAngebot ? "Bedingungen" : "Zahlungsbedingungen";
                doc.Blocks.Add(new Paragraph(new Run($"{label}: {bedingungen}"))
                {
                    Margin = new Thickness(0, 6, 0, 0)
                });
            }

            if (!isAngebot && !string.IsNullOrWhiteSpace(rechnung.IBAN))
            {
                var bank = new Paragraph { Margin = new Thickness(0, 6, 0, 0) };
                bank.Inlines.Add(new Bold(new Run("Bankverbindung")));
                bank.Inlines.Add(new LineBreak());
                if (!string.IsNullOrWhiteSpace(rechnung.Kontoinhaber))
                {
                    bank.Inlines.Add(new Run($"Kontoinhaber: {rechnung.Kontoinhaber}"));
                    bank.Inlines.Add(new LineBreak());
                }
                bank.Inlines.Add(new Run($"IBAN: {rechnung.IBAN}"));
                if (!string.IsNullOrWhiteSpace(rechnung.BIC))
                {
                    bank.Inlines.Add(new LineBreak());
                    bank.Inlines.Add(new Run($"BIC: {rechnung.BIC}"));
                }
                doc.Blocks.Add(bank);
            }

            doc.Blocks.Add(new Paragraph(new Run(isAngebot
                ? "Vielen Dank für Ihr Interesse. Wir freuen uns auf Ihren Auftrag."
                : "Vielen Dank für Ihren Auftrag."))
            {
                Margin = new Thickness(0, 18, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))
            });

            return doc;
        }

        private static TableCell CreateBoxCell(string header, string body)
        {
            var headerPara = new Paragraph(new Bold(new Run(header)))
            {
                Margin = new Thickness(0, 0, 0, 6),
                FontSize = 13
            };
            var bodyPara = CreateMultilineParagraph(body);

            var section = new Section();
            section.Blocks.Add(headerPara);
            section.Blocks.Add(bodyPara);

            var cell = new TableCell();
            cell.Blocks.Add(section);
            cell.Padding = new Thickness(12);
            cell.BorderBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
            cell.BorderThickness = new Thickness(1);
            return cell;
        }

        private static Paragraph CreateMultilineParagraph(string text)
        {
            var p = new Paragraph { Margin = new Thickness(0) };

            var parts = (text ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\r", "\n", StringComparison.Ordinal)
                .Split('\n');

            for (var i = 0; i < parts.Length; i++)
            {
                p.Inlines.Add(new Run(parts[i]));
                if (i < parts.Length - 1)
                    p.Inlines.Add(new LineBreak());
            }

            return p;
        }

        private static void AddHeaderCell(TableRow row, string text)
        {
            row.Cells.Add(new TableCell(new Paragraph(new Bold(new Run(text)))
            {
                Margin = new Thickness(0),
                Foreground = Brushes.White
            })
            {
                Padding = new Thickness(6, 8, 6, 8),
                Background = new SolidColorBrush(Color.FromRgb(0x00, 0x36, 0x8A)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(0, 0, 1, 0)
            });
        }

        private static void AddBodyCell(TableRow row, string text, TextAlignment align, Brush? background)
        {
            row.Cells.Add(new TableCell(new Paragraph(new Run(text))
            {
                Margin = new Thickness(0),
                TextAlignment = align
            })
            {
                Padding = new Thickness(6, 6, 6, 6),
                Background = background,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            });
        }

        private static void AddTotalRow(TableRowGroup group, string label, string value, bool emphasize)
        {
            var row = new TableRow();
            group.Rows.Add(row);

            var bg = emphasize ? new SolidColorBrush(Color.FromRgb(0xF0, 0xF6, 0xFF)) : null;

            row.Cells.Add(new TableCell(new Paragraph(new Run(label))
            {
                Margin = new Thickness(0),
                FontWeight = emphasize ? FontWeights.SemiBold : FontWeights.Normal
            })
            {
                Padding = new Thickness(6, 6, 6, 6),
                Background = bg,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            });

            row.Cells.Add(new TableCell(new Paragraph(new Run(value))
            {
                Margin = new Thickness(0),
                TextAlignment = TextAlignment.Right,
                FontWeight = emphasize ? FontWeights.SemiBold : FontWeights.Normal
            })
            {
                Padding = new Thickness(6, 6, 6, 6),
                Background = bg,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            });
        }

        public string GenerierePlaintext(Rechnung rechnung)
        {
            var sb = new StringBuilder();
            
            // Header
            sb.AppendLine("═══════════════════════════════════════════════════════════════════");
            var headerText = rechnung.DokumentTyp == DokumentTyp.Angebot ? "ANGEBOT" : "RECHNUNG";
            sb.AppendLine(headerText.PadRight(40) + rechnung.Rechnungsnummer.PadLeft(33));
            sb.AppendLine("═══════════════════════════════════════════════════════════════════\n");

            if (rechnung.DokumentTyp == DokumentTyp.Angebot)
            {
                if (rechnung.GueltigBis.HasValue)
                    sb.AppendLine($"Gültig bis: {rechnung.GueltigBis.Value:dd.MM.yyyy}");
                if (!string.IsNullOrWhiteSpace(rechnung.Lieferzeit))
                    sb.AppendLine($"Lieferzeit: {rechnung.Lieferzeit}");
                sb.AppendLine();
            }
            
            // Absender
            sb.AppendLine(rechnung.Absender.ToString());
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(rechnung.Absender.Telefon))
                sb.AppendLine($"Tel: {rechnung.Absender.Telefon}");
            if (!string.IsNullOrEmpty(rechnung.Absender.Email))
                sb.AppendLine($"E-Mail: {rechnung.Absender.Email}");
            if (!string.IsNullOrEmpty(rechnung.Steuernummer))
                sb.AppendLine($"Steuernummer: {rechnung.Steuernummer}");
            if (!string.IsNullOrEmpty(rechnung.UstIdNr))
                sb.AppendLine($"USt-ID Nr.: {rechnung.UstIdNr}");
            
            sb.AppendLine("\n" + new string('─', 70) + "\n");
            
            // Empfänger
            sb.AppendLine(rechnung.DokumentTyp == DokumentTyp.Angebot ? "ANGEBOTSEMPFÄNGER:" : "RECHNUNGSEMPFÄNGER:");
            sb.AppendLine(rechnung.Empfänger.ToString());
            
            sb.AppendLine("\n" + new string('─', 70) + "\n");
            
            // Daten
            if (rechnung.DokumentTyp == DokumentTyp.Angebot)
            {
                sb.AppendLine($"Angebotsdatum:      {rechnung.Rechnungsdatum:dd.MM.yyyy}");
                if (!string.IsNullOrWhiteSpace(rechnung.Lieferzeit))
                    sb.AppendLine($"Lieferzeit:         {rechnung.Lieferzeit}");
            }
            else
            {
                sb.AppendLine($"Rechnungsdatum:     {rechnung.Rechnungsdatum:dd.MM.yyyy}");
                if (rechnung.Leistungsdatum.HasValue)
                    sb.AppendLine($"Leistungsdatum:     {rechnung.Leistungsdatum:dd.MM.yyyy}");
                if (rechnung.Fälligkeitsdatum.HasValue)
                    sb.AppendLine($"Fälligkeitsdatum:   {rechnung.Fälligkeitsdatum:dd.MM.yyyy}");
            }
            
            sb.AppendLine("\n" + new string('─', 70) + "\n");
            
            // Positionen
            sb.AppendLine("POS │ BESCHREIBUNG                    │    MENGE │ EINHEIT │   PREIS │   BETRAG");
            sb.AppendLine(new string('─', 70));

            foreach (var position in rechnung.Positionen)
            {
                var betrag = rechnung.IstKleinunternehmer ? position.Betrag : position.GesamtBetrag;
                sb.AppendLine($"{position.Nummer:D3} │ {position.Beschreibung,-30} │ {position.Menge,8:F2} │ {position.Einheit,-7} │ {position.EinzelPreis,7:F2} € │ {betrag,8:F2} €");
            }

            sb.AppendLine(new string('─', 70));

            if (rechnung.IstKleinunternehmer)
            {
                var sum = rechnung.Positionen.Sum(p => p.Betrag);
                sb.AppendLine($"\nSumme:                                             {sum,14:F2} €");
                sb.AppendLine(new string('─', 70));
                sb.AppendLine($"GESAMTBETRAG:                                       {sum,14:F2} €");
                sb.AppendLine(new string('═', 70));
                sb.AppendLine("\nHinweis: Gemäß § 19 UStG wird keine Umsatzsteuer berechnet.");
            }
            else
            {
                // Steuerzusammenfassung
                var steuernachSatz = rechnung.PositionenNachSteuersatz.OrderBy(g => g.Key).ToList();

                sb.AppendLine($"\nSumme Netto:                                        {rechnung.GesamtNettoBetrag,14:F2} €");

                foreach (var steuersatzgruppe in steuernachSatz)
                {
                    var steueriMSatz = steuersatzgruppe.Sum(p => p.Steuer);
                    sb.AppendLine($"MwSt. ({steuersatzgruppe.Key}%):                                       {steueriMSatz,14:F2} €");
                }

                sb.AppendLine(new string('─', 70));
                sb.AppendLine($"GESAMTBETRAG:                                       {rechnung.GesamtBruttoBetrag,14:F2} €");
                sb.AppendLine(new string('═', 70));
            }
            
            // Bedingungen
            var bedingungen = GetBedingungenText(rechnung);
            if (!string.IsNullOrWhiteSpace(bedingungen))
            {
                var label = rechnung.DokumentTyp == DokumentTyp.Angebot ? "Bedingungen" : "Zahlungsbedingungen";
                sb.AppendLine($"\n{label}: {bedingungen}");
            }
            
            // Bankverbindung (Rechnung)
            if (rechnung.DokumentTyp != DokumentTyp.Angebot && !string.IsNullOrEmpty(rechnung.IBAN))
            {
                sb.AppendLine("\nBankverbindung:");
                if (!string.IsNullOrEmpty(rechnung.Kontoinhaber))
                    sb.AppendLine($"Kontoinhaber: {rechnung.Kontoinhaber}");
                sb.AppendLine($"IBAN: {rechnung.IBAN}");
                if (!string.IsNullOrEmpty(rechnung.BIC))
                    sb.AppendLine($"BIC: {rechnung.BIC}");
            }
            
            // Notizen
            if (!string.IsNullOrEmpty(rechnung.Notizen))
            {
                sb.AppendLine($"\nNotizen:\n{rechnung.Notizen}");
            }
            
            return sb.ToString();
        }

        public IReadOnlyList<string> Validierungsfehler(Rechnung rechnung)
        {
            var fehler = new List<string>();

            var isAngebot = rechnung.DokumentTyp == DokumentTyp.Angebot;
            var nummerLabel = isAngebot ? "Angebotsnummer" : "Rechnungsnummer";
            
            if (string.IsNullOrWhiteSpace(rechnung.Rechnungsnummer))
                fehler.Add($"{nummerLabel} ist erforderlich");
            
            if (string.IsNullOrWhiteSpace(rechnung.Absender.Firmenname))
                fehler.Add("Absender Firmenname ist erforderlich");
            
            if (string.IsNullOrWhiteSpace(rechnung.Empfänger.Firmenname))
                fehler.Add("Empfänger Firmenname ist erforderlich");
            
            if (rechnung.Positionen.Count == 0)
                fehler.Add("Mindestens eine Position ist erforderlich");
            
            if (rechnung.DokumentTyp == DokumentTyp.Angebot)
            {
                // Angebot: tax id fields can be empty
            }
            else if (rechnung.IstKleinunternehmer)
            {
                if (string.IsNullOrWhiteSpace(rechnung.Steuernummer))
                    fehler.Add("Steuernummer ist erforderlich (Kleinunternehmerregelung)");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(rechnung.Steuernummer) && string.IsNullOrWhiteSpace(rechnung.UstIdNr))
                    fehler.Add("Steuernummer oder USt-ID Nr. ist erforderlich");
            }

            return fehler;
        }

        public bool ValidierRechnung(Rechnung rechnung) => Validierungsfehler(rechnung).Count == 0;
    }
}
