using System.Collections.Generic;
using ECommerce.Entities.Concrete;
using System;

namespace ECommerce.Entities.Concrete
{
    public class Brand : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }      // eklendi
        public string? LogoUrl { get; set; }          // eklendi
        public virtual ICollection<Product> Products { get; set; } = new HashSet<Product>();

    }
}
