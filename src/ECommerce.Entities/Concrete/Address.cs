using System;

namespace ECommerce.Entities.Concrete
{
    public class Address : BaseEntity
    {
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty; // Ev, İş vb.
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public bool IsDefault { get; set; } = false;

        // Navigation
        public virtual User? User { get; set; }
    }
}
