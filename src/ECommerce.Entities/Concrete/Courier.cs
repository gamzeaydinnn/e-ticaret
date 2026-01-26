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

        // Dispatcher panel için eklenen alanlar
        /// <summary>
        /// Kurye aktif mi? (sisteme kayıtlı ve çalışabilir durumda)
        /// BaseEntity'den miras alınan IsActive'i override eder
        /// </summary>
        public new bool IsActive { get; set; } = true;

        /// <summary>
        /// Kurye şu an online mı? (uygulamaya giriş yapmış ve sipariş alabilir)
        /// </summary>
        public bool IsOnline { get; set; } = false;

        /// <summary>
        /// Araç tipi: motorcycle, car, bicycle, on_foot
        /// </summary>
        public string? VehicleType { get; set; } = "motorcycle";

        /// <summary>
        /// Son görülme zamanı
        /// </summary>
        public DateTime? LastSeenAt { get; set; }
        
        // Navigation properties
        public ICollection<Order> AssignedOrders { get; set; } = new List<Order>();
    }
}