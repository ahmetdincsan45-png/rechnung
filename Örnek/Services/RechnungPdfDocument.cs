using System.Globalization;
using System.IO;
using Örnek.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Örnek.Services;

public sealed class RechnungPdfDocument : IDocument
{
    private readonly Rechnung _rechnung;
    private const string ProfessionellerRechnungstext = "Sehr geehrte Damen und Herren,\n\nfür die erbrachten Leistungen erlauben wir uns, Ihnen wie folgt in Rechnung zu stellen.";

    private string GetDisplayNumber(CultureInfo culture)
    {
        var nummer = _rechnung.Rechnungsnummer ?? string.Empty;

        var targetTyp = _rechnung.DokumentTyp;
        var desiredPrefix = targetTyp == DokumentTyp.Angebot ? "AN-" : "RE-";

        if (nummer.StartsWith(desiredPrefix, StringComparison.OrdinalIgnoreCase))
            return nummer;

        // Try to salvage the sequence part and rebuild with the desired prefix.
        if (NummerService.TryParseSeq(DokumentTyp.Angebot, nummer, out var seqFromAn) ||
            NummerService.TryParseSeq(DokumentTyp.Rechnung, nummer, out seqFromAn))
        {
            return NummerService.Build(targetTyp, _rechnung.Rechnungsdatum, seqFromAn);
        }

        // Fallback: only swap the prefix if it looks like the other type.
        if (nummer.StartsWith("AN-", StringComparison.OrdinalIgnoreCase) ||
            nummer.StartsWith("RE-", StringComparison.OrdinalIgnoreCase))
        {
            return desiredPrefix + nummer[3..];
        }

        return nummer;
    }

    private static string FormatEur(decimal value, CultureInfo culture) => value.ToString("N2", culture);

    public RechnungPdfDocument(Rechnung rechnung)
    {
        _rechnung = rechnung;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");
        var isKlein = _rechnung.IstKleinunternehmer;
        var isAngebot = _rechnung.DokumentTyp == DokumentTyp.Angebot;

        var bedingungenText = isAngebot ? (_rechnung.Angebotsbedingungen ?? string.Empty) : (_rechnung.Zahlungsbedingungen ?? string.Empty);

        byte[]? logoBytes = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(_rechnung.AbsenderLogoPath) && File.Exists(_rechnung.AbsenderLogoPath))
                logoBytes = File.ReadAllBytes(_rechnung.AbsenderLogoPath);
        }
        catch
        {
            logoBytes = null;
        }

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10));

            page.Header().Element(header =>
            {
                header.Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item()
                                .PaddingBottom(2)
                                .Text(isAngebot ? "ANGEBOT" : "RECHNUNG")
                                .FontSize(isAngebot ? 24 : 22)
                                .Bold()
                                .FontColor("#00368A");

                            if (isAngebot)
                            {
                                var metaLine = BuildProjektMetaLine();
                                if (!string.IsNullOrWhiteSpace(metaLine))
                                    left.Item().PaddingTop(2).Text(metaLine).FontSize(9).FontColor("#333333");
                            }
                        });

                        if (logoBytes is { Length: > 0 })
                        {
                            // Keep a fixed logo box and center the image inside for a more "pro" look.
                            row.ConstantItem(200)
                                .Height(80)
                                .AlignCenter()
                                .AlignMiddle()
                                .Image(logoBytes)
                                .FitArea();
                        }
                    });

                    // Put number/date block below the logo row to keep the header visually balanced.
                    col.Item().PaddingTop(6).Column(meta =>
                    {
                        var displayNumber = GetDisplayNumber(culture);

                        meta.Item()
                            .PaddingBottom(4)
                            .DefaultTextStyle(x => x.FontSize(10))
                            .Text(text =>
                            {
                                text.Span(isAngebot ? "Angebotsnummer: " : "Rechnungsnummer: ").SemiBold();
                                text.Span(displayNumber);
                            });

                        meta.Item()
                            .PaddingBottom(4)
                            .DefaultTextStyle(x => x.FontSize(10))
                            .Text(text =>
                            {
                                text.Span(isAngebot ? "Angebotsdatum: " : "Rechnungsdatum: ").SemiBold();
                                text.Span(_rechnung.Rechnungsdatum.ToString("dd.MM.yyyy", culture));
                            });

                        if (isAngebot)
                        {
                            if (!string.IsNullOrWhiteSpace(_rechnung.Lieferzeit))
                            {
                                meta.Item()
                                    .PaddingBottom(4)
                                    .DefaultTextStyle(x => x.FontSize(10))
                                    .Text(text =>
                                    {
                                        text.Span("Lieferzeit: ").SemiBold();
                                        text.Span(_rechnung.Lieferzeit);
                                    });
                            }
                        }
                        else
                        {
                            if (_rechnung.Leistungsdatum.HasValue)
                            {
                                meta.Item()
                                    .PaddingBottom(4)
                                    .DefaultTextStyle(x => x.FontSize(10))
                                    .Text(text =>
                                    {
                                        text.Span("Leistungsdatum: ").SemiBold();
                                        text.Span(_rechnung.Leistungsdatum.Value.ToString("dd.MM.yyyy", culture));
                                    });
                            }

                            if (_rechnung.Fälligkeitsdatum.HasValue)
                            {
                                meta.Item()
                                    .PaddingBottom(4)
                                    .DefaultTextStyle(x => x.FontSize(10))
                                    .Text(text =>
                                    {
                                        text.Span("Fälligkeitsdatum: ").SemiBold();
                                        text.Span(_rechnung.Fälligkeitsdatum.Value.ToString("dd.MM.yyyy", culture));
                                    });
                            }
                        }
                    });
                });
            });

            page.Footer().Column(footer =>
            {
                footer.Item()
                    .PaddingTop(8)
                    .BorderTop(1)
                    .BorderColor("#D9E2F0")
                    .PaddingTop(8)
                    .DefaultTextStyle(x => x.FontSize(7.5f).FontColor("#666666"))
                    .Row(row =>
                    {
                        row.RelativeItem().Element(x => FooterSection(x, "Kontakt", BuildFooterContactLines()));

                        row.ConstantItem(14);

                        row.RelativeItem().Element(x => FooterSection(x, "Bankverbindung", BuildFooterBankLines()));

                        row.ConstantItem(14);

                        row.RelativeItem().Element(x => FooterSection(x, "Steuer", BuildFooterTaxLines()));
                    });

                footer.Item()
                    .PaddingTop(5)
                    .AlignRight()
                    .DefaultTextStyle(x => x.FontSize(8).FontColor("#777777"))
                    .Text(t =>
                    {
                        t.Span("Seite ");
                        t.CurrentPageNumber();
                        t.Span("/");
                        t.TotalPages();
                    });
            });

            page.Content().Element(content =>
            {
                content.Column(col =>
                {
                    col.Spacing(0);

                    if (isAngebot)
                    {
                        var intro = string.IsNullOrWhiteSpace(_rechnung.AngebotEinleitungText)
                            ? "Vielen Dank für Ihr Vertrauen.\nBei Rückfragen stehen wir Ihnen gerne zur Verfügung."
                            : _rechnung.AngebotEinleitungText;

                        col.Item()
                            .PaddingTop(16)
                            .PaddingBottom(20)
                            .Text(intro)
                            .FontSize(11)
                            .LineHeight(1.4f);
                    }

                    col.Item().PaddingBottom(20).Row(row =>
                    {
                        // Common German invoice/offer layouts place the recipient address on the left (e.g., for window envelopes).
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Element(x =>
                            {
                                x.Padding(8).Column(c =>
                                {
                                    c.Spacing(2);

                                    var empfaengerLines = (_rechnung.Empfänger?.ToString() ?? string.Empty)
                                        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                                    for (var i = 0; i < empfaengerLines.Length; i++)
                                    {
                                        var line = empfaengerLines[i];
                                        var item = c.Item();
                                        if (i == 0)
                                            item.Text(line).Bold();
                                        else
                                            item.Text(line);
                                    }

                                    if (!string.IsNullOrWhiteSpace(_rechnung.Objektname))
                                    {
                                        c.Item().PaddingTop(6).Text(t => t.Span("Objekt:").Bold());

                                        foreach (var line in _rechnung.Objektname
                                                     .Split('\n', StringSplitOptions.None)
                                                     .Select(l => l.TrimEnd()))
                                        {
                                            if (string.IsNullOrWhiteSpace(line))
                                                continue;

                                            c.Item().Text(line);
                                        }
                                    }
                                });
                            });
                        });
                        row.ConstantItem(20);
                        // Keep text left-aligned, but shift the whole sender block to the right.
                        row.ConstantItem(280).AlignRight().Element(x =>
                        {
                            x.Padding(8).Column(c =>
                            {
                                c.Spacing(2);

                                var absenderLines = BuildAbsenderText()
                                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                                for (var i = 0; i < absenderLines.Length; i++)
                                {
                                    var line = absenderLines[i];
                                    var item = c.Item();
                                    if (i == 0)
                                        item.Text(line).Bold();
                                    else
                                        item.Text(line);
                                }
                            });
                        });
                    });

                    if (!isAngebot && _rechnung.ProfessionellerRechnungstextAktiv)
                    {
                        col.Item()
                            .PaddingBottom(20)
                            .Text(ProfessionellerRechnungstext)
                            .FontSize(11)
                            .LineHeight(1.4f);
                    }

                    if (!string.IsNullOrWhiteSpace(_rechnung.Notizen))
                    {
                        col.Item()
                            .PaddingBottom(14)
                            .Text(_rechnung.Notizen)
                            .FontSize(10)
                            .LineHeight(1.35f)
                            .FontColor("#333333");
                    }

                    col.Item()
                        .PaddingBottom(10)
                        .Text("Positionen")
                        .FontSize(13)
                        .Bold();

                    col.Item().PaddingBottom(16).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn();
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(70);

                            if (isKlein)
                            {
                                columns.ConstantColumn(90);
                            }
                            else
                            {
                                columns.ConstantColumn(45);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(70);
                            }
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("#");
                            header.Cell().Element(HeaderCell).Text("Beschreibung");
                            header.Cell().Element(HeaderCell).AlignCenter().Text("Menge");
                            header.Cell().Element(HeaderCell).AlignCenter().Text("Einheit");
                            header.Cell().Element(HeaderCell).AlignCenter().Text("Einzelpreis");

                            if (isKlein)
                            {
                                header.Cell().Element(HeaderCell).AlignCenter().Text("Gesamt");
                            }
                            else
                            {
                                header.Cell().Element(HeaderCell).AlignCenter().Text("MwSt.%");
                                header.Cell().Element(HeaderCell).AlignCenter().Text("Netto");
                                header.Cell().Element(HeaderCell).AlignCenter().Text("Brutto");
                            }
                        });
                        var i = 0;
                        foreach (var p in _rechnung.Positionen)
                        {
                            var zebra = (i++ % 2 == 1) ? "#FAFAFA" : "#FFFFFF";

                            table.Cell().Element(c => BodyCell(c, zebra)).AlignRight().Text(p.Nummer.ToString(culture));
                            table.Cell().Element(c => BodyCell(c, zebra)).Text(t =>
                            {
                                if (string.IsNullOrWhiteSpace(p.Notiz))
                                {
                                    t.Span(p.Beschreibung);
                                    return;
                                }

                                t.Span(p.Beschreibung + "\n");
                                t.Span(p.Notiz).FontSize(9).FontColor("#444444");
                            });
                            table.Cell().Element(c => BodyCell(c, zebra)).AlignCenter().Text(p.Menge.ToString("0.##", culture));
                            table.Cell().Element(c => BodyCell(c, zebra)).AlignCenter().Text(p.Einheit);
                            table.Cell().Element(c => BodyCell(c, zebra)).AlignCenter().Text(FormatEur(p.EinzelPreis, culture));

                            if (isKlein)
                            {
                                table.Cell().Element(c => BodyCell(c, zebra)).AlignCenter().Text(FormatEur(p.Betrag, culture));
                            }
                            else
                            {
                                table.Cell().Element(c => BodyCell(c, zebra)).AlignCenter().Text(p.Steuersatz.ToString("0.##", culture));
                                table.Cell().Element(c => BodyCell(c, zebra)).AlignCenter().Text(FormatEur(p.Betrag, culture));
                                table.Cell().Element(c => BodyCell(c, zebra)).AlignCenter().Text(FormatEur(p.GesamtBetrag, culture));
                            }
                        }
                    });

                    col.Item().AlignRight().Element(x => TotalsBox(x, culture, isKlein));

                    if (isKlein)
                    {
                        col.Item().PaddingTop(8).Text("Gemäß § 19 UStG wird keine Umsatzsteuer berechnet.")
                            .FontColor("#555555");
                    }

                    if (!string.IsNullOrWhiteSpace(bedingungenText))
                        col.Item()
                            .PaddingTop(10)
                            .Text($"{(isAngebot ? "Bedingungen" : "Zahlungsbedingungen")}: {bedingungenText}");

                    // Bank details are typically invoice-specific; keep them on invoices to avoid confusion.
                    // Some older/archived records may only have the legacy Bankverbindung field filled.
                    if (!isAngebot &&
                        (!string.IsNullOrWhiteSpace(_rechnung.IBAN) || !string.IsNullOrWhiteSpace(_rechnung.Bankverbindung)))
                    {
                        col.Item().PaddingTop(5).Column(bank =>
                        {
                            bank.Item().Text("Bankverbindung").SemiBold();

                            if (!string.IsNullOrWhiteSpace(_rechnung.Kontoinhaber))
                                bank.Item().Text($"Kontoinhaber: {_rechnung.Kontoinhaber}");

                            if (!string.IsNullOrWhiteSpace(_rechnung.IBAN))
                                bank.Item().Text($"IBAN: {_rechnung.IBAN}");

                            if (!string.IsNullOrWhiteSpace(_rechnung.BIC))
                                bank.Item().Text($"BIC: {_rechnung.BIC}");

                            if (string.IsNullOrWhiteSpace(_rechnung.IBAN) && !string.IsNullOrWhiteSpace(_rechnung.Bankverbindung))
                                bank.Item().Text(_rechnung.Bankverbindung);
                        });
                    }

                    var closingText = isAngebot
                        ? "Vielen Dank für Ihr Vertrauen.\nBei Rückfragen stehen wir Ihnen gerne zur Verfügung."
                        : (_rechnung.ProfessionellerRechnungstextAktiv
                            ? "Wir danken Ihnen für Ihren Auftrag und das entgegengebrachte Vertrauen."
                            : "Vielen Dank für Ihren Auftrag.");

                    // Keep the closing text directly after totals/notes so it flows naturally with long position lists.
                    col.Item()
                        .PaddingTop(20)
                        .Text(closingText)
                        .FontSize(10)
                        .LineHeight(1.3f)
                        .FontColor("#555555");

                    if (isAngebot)
                    {
                        col.Item().PageBreak();
                        col.Item().Element(ComposeOfferAppendix);
                    }
                });
            });
        });
    }

    private string BuildProjektMetaLine()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_rechnung.ProjektObjektNr))
            parts.Add($"Projekt/Objekt-Nr.: {_rechnung.ProjektObjektNr}");
        if (!string.IsNullOrWhiteSpace(_rechnung.Objektname))
            parts.Add($"Objektname: {_rechnung.Objektname}");
        return string.Join("   ", parts);
    }

    private void ComposeOfferAppendix(IContainer container)
    {
        var gueltigBis = _rechnung.Rechnungsdatum.Date.AddDays(14);

        container.Column(col =>
        {
            col.Spacing(10);

            if (!string.IsNullOrWhiteSpace(_rechnung.AngebotHaftungText))
            {
                col.Item().Text("Haftungsbegrenzung").SemiBold().FontSize(11);
                col.Item().Text(_rechnung.AngebotHaftungText).FontSize(10).FontColor("#333333");
            }

            col.Item().PaddingBottom(18).Text("Bestätigung des Angebots").SemiBold().FontSize(11);

            var auftragText = string.IsNullOrWhiteSpace(_rechnung.AngebotAuftragText)
                ? "Hier kann ein Auftrags-/Bestätigungstext stehen. (Bitte im Firmenprofil unter Angebots-Texten anpassen.)"
                : _rechnung.AngebotAuftragText.Replace("[Datum]", gueltigBis.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE")), StringComparison.Ordinal);

            col.Item().PaddingBottom(30).Text(auftragText).FontSize(10).FontColor("#333333");

            col.Item().PaddingTop(8).Element(DottedLine);
            col.Item().Element(DottedLine);
            col.Item().Element(DottedLine);

            if (!string.IsNullOrWhiteSpace(_rechnung.AngebotWiderrufText))
            {
                col.Item().PaddingTop(10).Text("Widerrufsbelehrung").SemiBold().FontSize(11);
                col.Item().Text(_rechnung.AngebotWiderrufText).FontSize(10).FontColor("#333333");
            }

            col.Item().PaddingTop(12).Row(r =>
            {
                r.RelativeItem().Column(c =>
                {
                    c.Item().Text("Datum").FontSize(9).FontColor("#666666");
                    c.Item().PaddingTop(6).Width(200).Element(Underline);
                });
                r.ConstantItem(120);
                r.RelativeItem().Column(c =>
                {
                    c.Item().Text("Unterschrift Auftraggeber").FontSize(9).FontColor("#666666");
                    c.Item().PaddingTop(6).Width(200).Element(Underline);
                });
            });
        });
    }

    private static void DottedLine(IContainer container)
    {
        // QuestPDF doesn't expose a dotted-line primitive in all versions; emulate with dot characters.
        container.PaddingTop(8)
            .Text(new string('·', 160))
            .FontColor("#BBBBBB")
            .FontSize(10);
    }

    private static void Underline(IContainer container)
    {
        container.PaddingTop(14)
            .BorderBottom(1)
            .BorderColor("#444444");
    }

    private string BuildAbsenderText()
    {
        var lines = new List<string> { _rechnung.Absender.ToString() };

        if (!string.IsNullOrWhiteSpace(_rechnung.Absender.Telefon))
            lines.Add($"Tel.: {_rechnung.Absender.Telefon}");
        if (!string.IsNullOrWhiteSpace(_rechnung.Absender.Email))
            lines.Add($"E-Mail: {_rechnung.Absender.Email}");

        if (!string.IsNullOrWhiteSpace(_rechnung.Steuernummer))
            lines.Add($"Steuernummer: {_rechnung.Steuernummer}");

        if (!_rechnung.IstKleinunternehmer && !string.IsNullOrWhiteSpace(_rechnung.UstIdNr))
            lines.Add($"USt-IdNr: {_rechnung.UstIdNr}");
        return string.Join("\n", lines);
    }

    private static void AddressBox(IContainer container, string? title, string body)
    {
        container.Padding(8).Column(col =>
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                col.Item().Text(title).SemiBold().FontSize(11).FontColor("#444444");
                col.Item().PaddingTop(5).Text(body).LineHeight(1.25f).FontColor("#333333");
            }
            else
            {
                col.Item().Text(body).LineHeight(1.25f).FontColor("#333333");
            }
        });
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .Background("#00368A")
            .MinHeight(22)
            .PaddingVertical(4)
            .PaddingHorizontal(6)
            .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White));
    }

    private static IContainer BodyCell(IContainer container, string background)
    {
        return container
            .Background(background)
            .MinHeight(22)
            .BorderBottom(1)
            .BorderColor("#EEEEEE")
            .PaddingVertical(4)
            .PaddingHorizontal(6);
    }

    private List<string> BuildFooterContactLines()
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(_rechnung.Absender?.Firmenname))
            lines.Add(_rechnung.Absender.Firmenname);

        if (!string.IsNullOrWhiteSpace(_rechnung.Absender?.Telefon))
            lines.Add($"Tel.: {_rechnung.Absender.Telefon}");

        if (!string.IsNullOrWhiteSpace(_rechnung.Absender?.Email))
            lines.Add($"E-Mail: {_rechnung.Absender.Email}");

        if (!string.IsNullOrWhiteSpace(_rechnung.Absender?.Webseite))
            lines.Add(_rechnung.Absender.Webseite);

        return lines;
    }

    private static void FooterSection(IContainer container, string title, IReadOnlyCollection<string> lines)
    {
        container.Column(col =>
        {
            col.Spacing(1);
            col.Item().Text(title).SemiBold().FontSize(8).FontColor("#00368A");

            foreach (var line in lines)
                col.Item().Text(line);
        });
    }

    private List<string> BuildFooterBankLines()
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(_rechnung.Kontoinhaber))
            lines.Add($"Kontoinhaber: {_rechnung.Kontoinhaber}");

        if (!string.IsNullOrWhiteSpace(_rechnung.IBAN))
            lines.Add($"IBAN: {_rechnung.IBAN}");

        if (!string.IsNullOrWhiteSpace(_rechnung.BIC))
            lines.Add($"BIC: {_rechnung.BIC}");

        if (lines.Count == 0 && !string.IsNullOrWhiteSpace(_rechnung.Bankverbindung))
            lines.Add(_rechnung.Bankverbindung);

        return lines;
    }

    private List<string> BuildFooterTaxLines()
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(_rechnung.Steuernummer))
            lines.Add($"Steuernummer: {_rechnung.Steuernummer}");

        if (!_rechnung.IstKleinunternehmer && !string.IsNullOrWhiteSpace(_rechnung.UstIdNr))
            lines.Add($"USt-IdNr: {_rechnung.UstIdNr}");

        return lines;
    }

    private void TotalsBox(IContainer container, CultureInfo culture)
    {
        TotalsBox(container, culture, false);
    }

    private void TotalsBox(IContainer container, CultureInfo culture, bool isKlein)
    {
        var totalLabel = _rechnung.DokumentTyp == DokumentTyp.Angebot ? "Angebotssumme" : "Gesamtbetrag";

        container
            .Width(280)
            .Background("#F9FBFD")
            .Border(1)
            .BorderColor("#E3E9F2")
            .Padding(10)
            .Column(col =>
        {
            col.Spacing(6);

            if (isKlein)
            {
                var sum = _rechnung.Positionen.Sum(p => p.Betrag);
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Summe");
                    r.ConstantItem(100).AlignRight().Text(sum.ToString("C", culture));
                });
            }
            else
            {
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Summe Netto");
                    r.ConstantItem(100).AlignRight().Text(_rechnung.GesamtNettoBetrag.ToString("C", culture));
                });

                foreach (var g in _rechnung.PositionenNachSteuersatz.OrderBy(g => g.Key))
                {
                    var steuer = g.Sum(x => x.Steuer);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"MwSt. ({g.Key:0.##}%)");
                        r.ConstantItem(100).AlignRight().Text(steuer.ToString("C", culture));
                    });
                }
            }

            col.Item().PaddingTop(6)
                .BorderTop(1)
                .BorderColor("#DDDDDD")
                .Background("#EEF5FF")
                .PaddingTop(6)
                .Row(r =>
            {
                r.RelativeItem().Text(totalLabel).Bold();
                if (isKlein)
                {
                    var sum = _rechnung.Positionen.Sum(p => p.Betrag);
                    r.ConstantItem(100).AlignRight().Text(sum.ToString("C", culture)).Bold();
                }
                else
                {
                    r.ConstantItem(100).AlignRight().Text(_rechnung.GesamtBruttoBetrag.ToString("C", culture)).Bold();
                }
            });
        });
    }
}
