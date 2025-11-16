using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Promotions;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Interfaces
{
    public interface ICampaignService
    {
        Task<List<Campaign>> GetActiveCampaignsAsync(DateTime? now = null);
        Task<List<Campaign>> GetAllAsync();
        Task<Campaign?> GetByIdAsync(int id);
        Task<Campaign> CreateAsync(CampaignSaveDto dto);
        Task<Campaign> UpdateAsync(int id, CampaignSaveDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
