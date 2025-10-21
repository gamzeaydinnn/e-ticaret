using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IBannerService
    {
        Task<IEnumerable<BannerDto>> GetAllAsync();
        Task<BannerDto?> GetByIdAsync(int id);
        Task AddAsync(BannerDto dto);
        Task UpdateAsync(BannerDto dto);
        Task DeleteAsync(int id);
    }
}