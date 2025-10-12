using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace ECommerce.Data.Repositories
{
    public class AddressRepository : BaseRepository<Address>, IAddressRepository
    {
        public AddressRepository(ECommerceDbContext context) : base(context) { }

        public async Task<IEnumerable<Address>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }
    }
}
