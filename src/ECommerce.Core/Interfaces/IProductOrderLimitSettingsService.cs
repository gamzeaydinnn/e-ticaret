using System;
using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface IProductOrderLimitSettingsService
    {
        Task<ProductOrderLimitSettingsDto> GetActiveSettingsAsync(CancellationToken cancellationToken = default);
        Task<ProductOrderLimitSettingsDto> GetSettingsForAdminAsync(CancellationToken cancellationToken = default);
        Task<ProductOrderLimitSettingsDto> UpdateSettingsAsync(
            ProductOrderLimitSettingsUpdateDto updateDto,
            int updatedByUserId,
            string updatedByUserName,
            CancellationToken cancellationToken = default);
    }

    public class ProductOrderLimitSettingsDto
    {
        public int Id { get; set; }
        public int DefaultMaxQuantityPiece { get; set; } = 5;
        public int DefaultMinQuantityPiece { get; set; } = 1;
        public decimal DefaultQuantityStepPiece { get; set; } = 1m;
        public decimal DefaultMaxWeightKg { get; set; } = 10m;
        public decimal DefaultMinWeightKg { get; set; } = 0.25m;
        public decimal DefaultWeightStepKg { get; set; } = 0.25m;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class ProductOrderLimitSettingsUpdateDto
    {
        public int? DefaultMaxQuantityPiece { get; set; }
        public int? DefaultMinQuantityPiece { get; set; }
        public decimal? DefaultQuantityStepPiece { get; set; }
        public decimal? DefaultMaxWeightKg { get; set; }
        public decimal? DefaultMinWeightKg { get; set; }
        public decimal? DefaultWeightStepKg { get; set; }
    }
}
