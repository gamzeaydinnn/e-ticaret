using System;

namespace ECommerce.Core.DTOs.Logs
{
    public class InventoryLogDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Action { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal OldStock { get; set; }
        public decimal NewStock { get; set; }
        public string? ReferenceId { get; set; }
        public string? ProductName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
