using System;
using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.ProductReview
{
    // Kullanıcı yeni yorum eklerken kullanılacak (POST)
    public class ProductReviewCreateDto
    {
        public int ProductId { get; set; }
        public int Rating { get; set; } // 1..5
        public string Comment { get; set; } = string.Empty;
    }

    // API üzerinden dönen yorum modeli
    public class ProductReviewDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
