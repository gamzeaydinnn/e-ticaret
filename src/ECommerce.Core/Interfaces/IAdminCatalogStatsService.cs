using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.Interfaces;

namespace ECommerce.Core.Interfaces
{
    public interface IAdminCatalogStatsService
    {
        Task<IReadOnlyList<AdminCatalogProductSnapshot>> GetProductSnapshotsAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<int, int>> GetActiveProductCountsByCategoryAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Yerel DB üzerinden hızlı ürün sayımı (Mikro ERP birleştirme yok).
        /// Admin kategori listesi/ağacı için kullanılır.
        /// </summary>
        Task<IReadOnlyDictionary<int, int>> GetLocalActiveProductCountsByCategoryAsync(
            CancellationToken cancellationToken = default);
    }
}
