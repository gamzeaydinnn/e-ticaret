using System;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;

namespace ECommerce.Entities.Concrete
{
    public class Coupon : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public bool IsPercentage { get; set; }
        public decimal Value { get; set; }
        public DateTime ExpirationDate { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public int UsageLimit { get; set; } = 1;
        public bool IsActive { get; set; } = true; // BaseEntity’de zaten var, ama gerekirse override edebilirsin
    }
}
