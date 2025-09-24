using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;

namespace ECommerce.Data.Repositories
{
    public class BaseRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly ECommerceDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(ECommerceDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.Where(x => x.IsActive).ToListAsync();
        }

        public virtual async Task AddAsync(T entity)
        {
            entity.CreatedDate = DateTime.UtcNow;
            await _dbSet.AddAsync(entity);
        }

        public virtual void Update(T entity)
        {
            entity.UpdatedDate = DateTime.UtcNow;
            _dbSet.Update(entity);
        }

        public virtual void Delete(T entity)
        {
            entity.IsActive = false;
            entity.UpdatedDate = DateTime.UtcNow;
        }

        public virtual void HardDelete(T entity)
        {
            _dbSet.Remove(entity);
        }
    }
}