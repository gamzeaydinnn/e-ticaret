using ECommerce.Core.DTOs.Product;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductListDto>> GetProductsAsync(
            string query = null, int? categoryId = null, int page = 1, int pageSize = 20);

        Task<ProductListDto?> GetByIdAsync(int id);    // Nullable, null dönebilir
        Task<ProductListDto> CreateAsync(ProductCreateDto dto);
        Task UpdateAsync(int id, ProductUpdateDto dto);
        Task DeleteAsync(int id);
    }
}
