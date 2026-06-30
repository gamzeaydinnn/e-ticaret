using ECommerce.Business.Helpers;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    public class ProductOrderLimitSettingsManager : IProductOrderLimitSettingsService
    {
        private readonly ECommerceDbContext _context;
        private readonly ILogger<ProductOrderLimitSettingsManager> _logger;

        public ProductOrderLimitSettingsManager(
            ECommerceDbContext context,
            ILogger<ProductOrderLimitSettingsManager> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<ProductOrderLimitSettingsDto> GetActiveSettingsAsync(CancellationToken cancellationToken = default) =>
            GetSettingsInternalAsync(cancellationToken);

        public Task<ProductOrderLimitSettingsDto> GetSettingsForAdminAsync(CancellationToken cancellationToken = default) =>
            GetSettingsInternalAsync(cancellationToken);

        public async Task<ProductOrderLimitSettingsDto> UpdateSettingsAsync(
            ProductOrderLimitSettingsUpdateDto updateDto,
            int updatedByUserId,
            string updatedByUserName,
            CancellationToken cancellationToken = default)
        {
            var entity = await GetOrCreateEntityAsync(cancellationToken);

            if (updateDto.DefaultMaxQuantityPiece.HasValue)
                entity.DefaultMaxQuantityPiece = Math.Max(1, updateDto.DefaultMaxQuantityPiece.Value);

            if (updateDto.DefaultMinQuantityPiece.HasValue)
                entity.DefaultMinQuantityPiece = Math.Max(1, updateDto.DefaultMinQuantityPiece.Value);

            if (updateDto.DefaultQuantityStepPiece.HasValue)
                entity.DefaultQuantityStepPiece = Math.Max(0.01m, updateDto.DefaultQuantityStepPiece.Value);

            if (updateDto.DefaultMaxWeightKg.HasValue)
                entity.DefaultMaxWeightKg = Math.Max(0.01m, updateDto.DefaultMaxWeightKg.Value);

            if (updateDto.DefaultMinWeightKg.HasValue)
                entity.DefaultMinWeightKg = Math.Max(0.01m, updateDto.DefaultMinWeightKg.Value);

            if (updateDto.DefaultWeightStepKg.HasValue)
                entity.DefaultWeightStepKg = Math.Max(0.01m, updateDto.DefaultWeightStepKg.Value);

            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedByUserId = updatedByUserId;
            entity.UpdatedByUserName = string.IsNullOrWhiteSpace(updatedByUserName)
                ? entity.UpdatedByUserName
                : updatedByUserName.Trim();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[ProductOrderLimitSettings] Güncellendi. PieceMax={PieceMax}, WeightMaxKg={WeightMaxKg}",
                entity.DefaultMaxQuantityPiece,
                entity.DefaultMaxWeightKg);

            return Map(entity);
        }

        private async Task<ProductOrderLimitSettingsDto> GetSettingsInternalAsync(CancellationToken cancellationToken)
        {
            var entity = await GetOrCreateEntityAsync(cancellationToken);
            return Map(entity);
        }

        private async Task<ProductOrderLimitSetting> GetOrCreateEntityAsync(CancellationToken cancellationToken)
        {
            var entity = await _context.ProductOrderLimitSettings
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity != null)
            {
                return entity;
            }

            entity = new ProductOrderLimitSetting
            {
                DefaultMaxQuantityPiece = 5,
                DefaultMinQuantityPiece = 1,
                DefaultQuantityStepPiece = 1m,
                DefaultMaxWeightKg = 10m,
                DefaultMinWeightKg = 0.25m,
                DefaultWeightStepKg = 0.25m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ProductOrderLimitSettings.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        private static ProductOrderLimitSettingsDto Map(ProductOrderLimitSetting entity) =>
            new()
            {
                Id = entity.Id,
                DefaultMaxQuantityPiece = entity.DefaultMaxQuantityPiece,
                DefaultMinQuantityPiece = entity.DefaultMinQuantityPiece,
                DefaultQuantityStepPiece = entity.DefaultQuantityStepPiece,
                DefaultMaxWeightKg = entity.DefaultMaxWeightKg,
                DefaultMinWeightKg = entity.DefaultMinWeightKg,
                DefaultWeightStepKg = entity.DefaultWeightStepKg,
                UpdatedAt = entity.UpdatedAt,
                UpdatedByUserName = entity.UpdatedByUserName
            };
    }
}
