using System;
using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface IProductAdminOverrideSettingsService
    {
        Task<ProductAdminOverrideSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
        Task<ProductAdminOverrideSettingsDto> GetSettingsForAdminAsync(CancellationToken cancellationToken = default);
        Task<ProductAdminOverrideSettingsDto> UpdateSettingsAsync(
            ProductAdminOverrideSettingsUpdateDto updateDto,
            int? updatedByUserId,
            string? updatedByUserName,
            CancellationToken cancellationToken = default);
    }

    public class ProductAdminOverrideSettingsDto
    {
        public int Id { get; set; }
        public bool DefaultAdminOverrideName { get; set; }
        public bool DefaultAdminOverridePrice { get; set; }
        public bool DefaultAdminOverrideCategory { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    public class ProductAdminOverrideSettingsUpdateDto
    {
        public bool? DefaultAdminOverrideName { get; set; }
        public bool? DefaultAdminOverridePrice { get; set; }
        public bool? DefaultAdminOverrideCategory { get; set; }
    }
}