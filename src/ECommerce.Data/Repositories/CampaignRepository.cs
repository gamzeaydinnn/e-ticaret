using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Data.Repositories
{
    public class CampaignRepository : BaseRepository<Campaign>, ICampaignRepository
    {
        public CampaignRepository(ECommerceDbContext context) : base(context)
        {
        }

        public override async Task<IEnumerable<Campaign>> GetAllAsync()
        {
            return await _context.Campaigns
                .Include(c => c.Rules)
                .Include(c => c.Rewards)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public override async Task<Campaign?> GetByIdAsync(int id)
        {
            return await _context.Campaigns
                .Include(c => c.Rules)
                .Include(c => c.Rewards)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Campaign>> GetActiveCampaignsAsync(DateTime? now = null)
        {
            var current = now ?? DateTime.UtcNow;

            return await _context.Campaigns
                .Include(c => c.Rules)
                .Include(c => c.Rewards)
                .Where(c => c.IsActive
                            && c.StartDate <= current
                            && c.EndDate >= current)
                .ToListAsync();
        }
    }
}
