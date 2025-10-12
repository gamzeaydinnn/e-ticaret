using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace ECommerce.Data.Repositories
{
    public class BrandRepository : BaseRepository<Brand>, IBrandRepository
    {
        public BrandRepository(ECommerceDbContext context) : base(context) { }

        public async Task<Brand?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(b => b.Name == name && b.IsActive);
        }
    }
}
