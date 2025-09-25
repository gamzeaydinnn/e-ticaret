using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ECommerceDbContext _context;

        public UserRepository(ECommerceDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Email ile kullanıcı getirir.
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        /// Id ile kullanıcı getirir ve ilişkili siparişleri yükler.
        /// </summary>
        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// Email ile kullanıcı var mı kontrol eder.
        /// </summary>
        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email);
        }

        /// <summary>
        /// Tüm kullanıcıları getirir.
        /// </summary>
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        /// <summary>
        /// Yeni kullanıcı ekler.
        /// </summary>
        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Var olan kullanıcıyı günceller.
        /// </summary>
        public async Task Update(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Kullanıcıyı siler.
        /// </summary>
        public async Task Delete(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        void IUserRepository.Update(User user)
        {
            throw new NotImplementedException();
        }

        void IUserRepository.Delete(User user)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(string email)
        {
            throw new NotImplementedException();
        }
    }
}
