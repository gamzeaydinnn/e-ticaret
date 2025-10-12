using System.Collections.Generic;
using ECommerce.Entities.Concrete;
using System;

namespace ECommerce.Entities.Concrete
{
    public class Address : BaseEntity
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;
    }
}
