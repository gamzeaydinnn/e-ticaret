namespace ECommerce.Core.DTOs.Coupon
{
    public class CouponDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public bool IsPercentage { get; set; }
        public decimal Value { get; set; }
        public DateTime ExpirationDate { get; set; }
    }

    public class CouponCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public bool IsPercentage { get; set; }
        public decimal Value { get; set; }
        public DateTime ExpirationDate { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public int UsageLimit { get; set; } = 1;
    }
}
