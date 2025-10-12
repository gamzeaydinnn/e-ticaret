using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System;

namespace ECommerce.Entities.Concrete
{
    public class ProductImage : BaseEntity
    {
        public int ProductId { get; set; }
        public string Url { get; set; } = string.Empty;       // boş string ile başlatıldı
        public string FileName { get; set; } = string.Empty;  // boş string ile başlatıldı
        public bool IsMain { get; set; }
        public string SizeTag { get; set; } = string.Empty;   // boş string ile başlatıldı
        public Product Product { get; set; } = null!;
    }
}