using System;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;

namespace ECommerce.Entities.Concrete
{
    public class Notification : BaseEntity
    {
        public int? UserId { get; set; } // null => broadcast
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public string? Url { get; set; } // optional link
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual User? User { get; set; }
    }
}
