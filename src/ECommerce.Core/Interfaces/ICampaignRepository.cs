using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    public interface ICampaignRepository : IRepository<Campaign>
    {
        Task<List<Campaign>> GetActiveCampaignsAsync(DateTime? now = null);
    }
}

