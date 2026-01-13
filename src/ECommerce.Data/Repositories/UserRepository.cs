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
    public class UserRepository : IUserRepository
    {
        private readonly ECommerceDbContext _context;

        public UserRepository(ECommerceDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        /// <summary>
        /// Yeni kullanıcı ekler ve veritabanına kaydeder.
        /// SaveChangesAsync() çağrılmazsa değişiklikler commit edilmez!
        /// </summary>
        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            // KRİTİK: SaveChangesAsync olmadan EF Core sadece track eder, DB'ye yazmaz
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Kullanıcı bilgilerini günceller ve veritabanına kaydeder.
        /// Async pattern'e uygun hale getirildi.
        /// </summary>
        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Kullanıcıyı siler ve veritabanına kaydeder.
        /// Async pattern'e uygun hale getirildi.
        /// </summary>
        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// [DEPRECATED] Senkron Update - geriye dönük uyumluluk için korundu.
        /// Yeni kodlarda UpdateAsync kullanın.
        /// </summary>
        public void Update(User user)
        {
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        /// <summary>
        /// [DEPRECATED] Senkron Delete - geriye dönük uyumluluk için korundu.
        /// Yeni kodlarda DeleteAsync kullanın.
        /// </summary>
        public void Delete(User user)
        {
            _context.Users.Remove(user);
            _context.SaveChanges();
        }

        public async Task<bool> ExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
    }
}