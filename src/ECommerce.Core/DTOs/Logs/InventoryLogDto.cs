using System;

namespace ECommerce.Core.DTOs.Logs
{
    public class InventoryLogDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Action { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int OldStock { get; set; }
        public int NewStock { get; set; }
        public string? ReferenceId { get; set; }
        public string? ProductName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
