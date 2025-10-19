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
        // CreatedAt already exists in BaseEntity

        public virtual User? User { get; set; }
    }
}
