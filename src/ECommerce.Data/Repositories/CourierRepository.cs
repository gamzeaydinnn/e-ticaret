using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Data.Repositories
{
    public class CourierRepository : BaseRepository<Courier>, ICourierRepository
    {
        public CourierRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<Courier?> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
        }

        public async Task<IEnumerable<Courier>> GetActiveCouriersAsync()
        {
            return await _dbSet
                .Include(c => c.User)
                .Where(c => c.IsActive && c.Status == "active")
                .ToListAsync();
        }

        public async Task<IEnumerable<Courier>> GetByStatusAsync(string status)
        {
            return await _dbSet
                .Include(c => c.User)
                .Where(c => c.IsActive && c.Status == status)
                .ToListAsync();
        }

        public async Task<Courier?> GetByEmailAsync(string email)
        {
            return await _dbSet
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.User.Email == email && c.IsActive);
        }

        public async Task<IEnumerable<Order>> GetCourierOrdersAsync(int courierId)
        {
            var courier = await _dbSet
                .Include(c => c.AssignedOrders)
                    .ThenInclude(o => o.User)
                .FirstOrDefaultAsync(c => c.Id == courierId);

            return courier?.AssignedOrders ?? new List<Order>();
        }

        public async Task UpdateLocationAsync(int courierId, string location)
        {
            var courier = await GetByIdAsync(courierId);
            if (courier != null)
            {
                courier.Location = location;
                await UpdateAsync(courier);
            }
        }

        public async Task UpdateStatusAsync(int courierId, string status)
        {
            var courier = await GetByIdAsync(courierId);
            if (courier != null)
            {
                courier.Status = status;
                if (status == "active")
                {
                    courier.LastActiveAt = System.DateTime.UtcNow;
                }
                await UpdateAsync(courier);
            }
        }
    }
}