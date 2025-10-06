using System;

namespace ECommerce.Core.DTOs.Review
{
    public class ReviewCreateDto
    {
        public int ProductId { get; set; }
        public int Rating { get; set; } // 1..5
        public string Comment { get; set; } = string.Empty;
    }

    public class ReviewDto : ReviewCreateDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
