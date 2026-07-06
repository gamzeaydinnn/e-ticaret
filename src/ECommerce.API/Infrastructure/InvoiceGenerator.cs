using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ECommerce.Core.DTOs.Order;

namespace ECommerce.API.Infrastructure
{
    public static class InvoiceGenerator
    {
        public static byte[] Generate(OrderDetailDto order)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var orderLabel = string.IsNullOrWhiteSpace(order.TrackingNumber)
                ? $"#{order.Id}"
                : order.TrackingNumber;
            var finalTotal = order.FinalPrice > 0 ? order.FinalPrice : order.TotalPrice;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);

                    page.Header().Column(column =>
                    {
                        column.Item().Text("Gölköy Gurme — Satış Faturası").FontSize(20).Bold();
                        column.Item().PaddingTop(6).Text($"Sipariş: {orderLabel}").FontSize(12);
                        column.Item().Text($"Tarih: {order.OrderDate:dd.MM.yyyy HH:mm}").FontSize(11).FontColor(Colors.Grey.Darken2);
                        column.Item().PaddingTop(4).Text($"Durum: {order.Status}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(5);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Ürün").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Adet").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("Birim").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("Tutar").Bold();
                            });

                            foreach (var item in order.OrderItems)
                            {
                                var lineTotal = item.LineTotal > 0
                                    ? item.LineTotal
                                    : item.UnitPrice * item.Quantity;

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text(item.ProductName ?? "Ürün");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text(item.Quantity.ToString("0.##"));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight()
                                    .Text($"{item.UnitPrice:C}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight()
                                    .Text($"{lineTotal:C}");
                            }
                        });

                        column.Item().PaddingTop(16).AlignRight().Column(totals =>
                        {
                            if (order.DiscountAmount > 0)
                            {
                                totals.Item().Text($"Ara Toplam: {order.TotalPrice:C}").FontSize(11);
                                totals.Item().Text($"İndirim: -{order.DiscountAmount:C}").FontSize(11).FontColor(Colors.Red.Medium);
                            }

                            if (order.VatAmount > 0)
                            {
                                totals.Item().Text($"KDV: {order.VatAmount:C}").FontSize(11);
                            }

                            totals.Item().PaddingTop(4).Text($"Genel Toplam: {finalTotal:C}").FontSize(14).Bold();
                        });
                    });

                    page.Footer().AlignCenter().DefaultTextStyle(style =>
                        style.FontSize(8).FontColor(Colors.Grey.Medium)).Text(text =>
                    {
                        text.Span("Bu belge bilgilendirme amaçlıdır. Resmi e-fatura Mikro ERP üzerinden kesilir.");
                        text.Span(" | ");
                        text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
