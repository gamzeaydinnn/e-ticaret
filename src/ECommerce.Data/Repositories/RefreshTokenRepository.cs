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
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ECommerceDbContext _context;

        public RefreshTokenRepository(ECommerceDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            return await _context.RefreshTokens
                                 .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task AddAsync(RefreshToken refreshToken)
        {
            refreshToken.CreatedAt = DateTime.UtcNow;
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            refreshToken.UpdatedAt = DateTime.UtcNow;
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserAsync(int userId)
        {
            return await _context.RefreshTokens
                                 .Where(rt => rt.UserId == userId &&
                                              !rt.RevokedAt.HasValue &&
                                              rt.ExpiresAt > DateTime.UtcNow)
                                 .ToListAsync();
        }
    }
}
