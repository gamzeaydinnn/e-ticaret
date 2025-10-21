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
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Header().Text($"Fatura - Sipariş #{order.Id}").FontSize(20).Bold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Text("Ürün");
                            header.Cell().Text("Adet");
                            header.Cell().Text("Fiyat");
                        });
                        foreach (var item in order.OrderItems)
                        {
                            table.Cell().Text(item.ProductName);
                            table.Cell().Text(item.Quantity.ToString());
                            table.Cell().Text($"{item.UnitPrice:C}");
                        }
                    });
                    page.Footer().AlignRight().Text($"Toplam: {order.TotalPrice:C}").FontSize(14).Bold();
                });
            });
            return document.GeneratePdf();
        }
    }
}
