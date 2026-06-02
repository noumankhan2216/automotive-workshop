using System.Globalization;
using AutomotiveWorkshop.Application.Documents;
using AutomotiveWorkshop.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutomotiveWorkshop.Infrastructure.Services;

public class PdfDocumentService : IPdfService
{
    private const string Ink = "#111827";
    private const string Muted = "#6b7280";
    private const string Soft = "#4b5563";
    private const string HeaderBar = "#374151";
    private const string Line = "#e5e7eb";
    private const string Accent = "#2563eb";

    static PdfDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(DocumentModel d)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(9).FontColor(Soft).FontFamily(Fonts.Calibri));

                page.Header().Element(c => ComposeHeader(c, d));
                page.Content().Element(c => ComposeContent(c, d));
                page.Footer().Element(c => ComposeFooter(c, d));
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, DocumentModel d)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(brand =>
                {
                    brand.Item().Text(d.Shop.Name).FontSize(20).Bold().FontColor(Ink);
                    if (!string.IsNullOrWhiteSpace(d.Shop.Address))
                        brand.Item().Text(d.Shop.Address).FontSize(8).FontColor(Muted);
                    var contact = string.Join("   ", new[] { d.Shop.Phone, d.Shop.Email }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrWhiteSpace(contact))
                        brand.Item().Text(contact).FontSize(8).FontColor(Muted);
                });

                row.ConstantItem(200).Column(right =>
                {
                    right.Item().AlignRight().Text(d.DocumentType).FontSize(24).Light().FontColor(Soft);
                    right.Item().AlignRight().Text($"# {d.DocumentNumber}").Bold().FontColor(Ink);
                    right.Item().AlignRight().PaddingTop(2).Text(d.StatusLabel).FontSize(8).FontColor(Accent).Bold();
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Line);
        });
    }

    private static void ComposeContent(IContainer container, DocumentModel d)
    {
        container.PaddingVertical(12).Column(col =>
        {
            col.Spacing(14);

            col.Item().Row(row =>
            {
                row.RelativeItem().Column(bill =>
                {
                    bill.Item().Text("BILL TO").FontSize(8).Bold().FontColor(Muted);
                    bill.Item().PaddingTop(2).Text(d.Customer.Name).FontSize(11).Bold().FontColor(Ink);
                    if (!string.IsNullOrWhiteSpace(d.Customer.Address))
                        bill.Item().Text(d.Customer.Address);
                    var line = string.Join("   ", new[] { d.Customer.Phone, d.Customer.Email }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (!string.IsNullOrWhiteSpace(line))
                        bill.Item().Text(line);
                });

                row.ConstantItem(220).Column(meta =>
                {
                    MetaRow(meta, "Date", d.IssuedAt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
                    if (d.SecondaryDate.HasValue)
                        MetaRow(meta, d.SecondaryDateLabel, d.SecondaryDate.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
                    if (d.Vehicle is { } v)
                    {
                        MetaRow(meta, "Vehicle", v.Description);
                        if (!string.IsNullOrWhiteSpace(v.LicensePlate))
                            MetaRow(meta, "Plate", v.LicensePlate!);
                        if (!string.IsNullOrWhiteSpace(v.Vin))
                            MetaRow(meta, "VIN", v.Vin!);
                    }
                });
            });

            col.Item().Element(c => ComposeTable(c, d));

            col.Item().AlignRight().Width(260).Element(c => ComposeTotals(c, d));

            if (!string.IsNullOrWhiteSpace(d.Notes))
            {
                col.Item().PaddingTop(6).Column(notes =>
                {
                    notes.Item().Text("NOTES").FontSize(8).Bold().FontColor(Muted);
                    notes.Item().PaddingTop(2).Text(d.Notes!);
                });
            }
        });
    }

    private static void MetaRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(r =>
        {
            r.RelativeItem().Text($"{label} :").FontColor(Muted);
            r.RelativeItem().AlignRight().Text(value).FontColor(Ink);
        });
    }

    private static void ComposeTable(IContainer container, DocumentModel d)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(26);
                c.RelativeColumn();
                c.ConstantColumn(50);
                c.ConstantColumn(70);
                c.ConstantColumn(80);
            });

            table.Header(header =>
            {
                HeaderCell(header, "#");
                HeaderCell(header, "Item & Description");
                HeaderCell(header, "Qty", true);
                HeaderCell(header, "Rate", true);
                HeaderCell(header, "Amount", true);
            });

            foreach (var l in d.Lines)
            {
                BodyCell(table, l.Index.ToString());
                BodyCell(table, l.Description, false, Ink, true);
                BodyCell(table, l.Quantity.ToString("0.##", CultureInfo.InvariantCulture), true);
                BodyCell(table, Money(l.UnitPrice, d.Shop.CurrencyCode), true);
                BodyCell(table, Money(l.LineTotal, d.Shop.CurrencyCode), true);
            }
        });
    }

    private static void HeaderCell(TableCellDescriptor header, string text, bool right = false)
    {
        var cell = header.Cell().Background(HeaderBar).PaddingVertical(6).PaddingHorizontal(6);
        var t = cell.Text(text).FontColor("#ffffff").FontSize(8).Bold();
        if (right) t.AlignRight();
    }

    private static void BodyCell(TableDescriptor table, string text, bool right = false, string? color = null, bool bold = false)
    {
        var cell = table.Cell().BorderBottom(1).BorderColor(Line).PaddingVertical(6).PaddingHorizontal(6);
        var t = cell.Text(text).FontSize(9).FontColor(color ?? Soft);
        if (bold) t.SemiBold();
        if (right) t.AlignRight();
    }

    private static void ComposeTotals(IContainer container, DocumentModel d)
    {
        container.Column(col =>
        {
            TotalRow(col, "Sub Total", Money(d.SubTotal, d.Shop.CurrencyCode));
            TotalRow(col, $"Tax ({(d.TaxRate * 100).ToString("0.##", CultureInfo.InvariantCulture)}%)", Money(d.TaxAmount, d.Shop.CurrencyCode));
            col.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Line);
            TotalRow(col, "Total", Money(d.Total, d.Shop.CurrencyCode), true);
            if (d.BalanceDue > 0)
                col.Item().Background("#f3f4f6").Padding(6).Row(r =>
                {
                    r.RelativeItem().Text("Balance Due").Bold().FontColor(Ink);
                    r.RelativeItem().AlignRight().Text(Money(d.BalanceDue, d.Shop.CurrencyCode)).Bold().FontColor(Ink);
                });
        });
    }

    private static void TotalRow(ColumnDescriptor col, string label, string value, bool strong = false)
    {
        col.Item().PaddingVertical(2).Row(r =>
        {
            var l = r.RelativeItem().Text(label);
            var v = r.RelativeItem().AlignRight().Text(value);
            if (strong)
            {
                l.Bold().FontColor(Ink);
                v.Bold().FontColor(Ink);
            }
        });
    }

    private static void ComposeFooter(IContainer container, DocumentModel d)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor(Line);
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text($"Thank you for choosing {d.Shop.Name}.").FontSize(8).FontColor(Muted);
                row.ConstantItem(120).AlignRight().Text(t =>
                {
                    t.Span("Page ").FontSize(8).FontColor(Muted);
                    t.CurrentPageNumber().FontSize(8).FontColor(Muted);
                    t.Span(" / ").FontSize(8).FontColor(Muted);
                    t.TotalPages().FontSize(8).FontColor(Muted);
                });
            });
        });
    }

    private static string Money(decimal amount, string currencyCode)
    {
        var symbol = currencyCode switch
        {
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            "PKR" => "Rs ",
            _ => currencyCode + " "
        };
        return $"{symbol}{amount.ToString("N2", CultureInfo.InvariantCulture)}";
    }
}
