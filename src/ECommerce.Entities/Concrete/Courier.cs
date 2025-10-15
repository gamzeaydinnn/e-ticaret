using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System;

namespace ECommerce.Entities.Concrete
{
    public class Courier : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        
        // Ek kurye bilgileri
        public string? Phone { get; set; }
        public string? Vehicle { get; set; } // Motosiklet, Bisiklet, Araba
        public string Status { get; set; } = "offline"; // active, busy, offline, break
        public string? Location { get; set; }
        public decimal Rating { get; set; } = 0;
        public int ActiveOrders { get; set; } = 0;
        public int CompletedToday { get; set; } = 0;
        public DateTime? LastActiveAt { get; set; }
        
        // Navigation properties
        public ICollection<Order> AssignedOrders { get; set; } = new List<Order>();
    }
}