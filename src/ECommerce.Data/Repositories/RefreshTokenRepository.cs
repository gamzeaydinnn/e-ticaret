using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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
            var hashed = ComputeSha256Hash(token);
            return await _context.RefreshTokens
                                 .FirstOrDefaultAsync(rt => rt.HashedToken == hashed);
        }

        public async Task AddAsync(RefreshToken refreshToken)
        {
            refreshToken.CreatedAt = DateTime.UtcNow;
            // Compute SHA256 hash of the provided raw token and store only the hash in DB.
            if (!string.IsNullOrWhiteSpace(refreshToken.Token))
            {
                refreshToken.HashedToken = ComputeSha256Hash(refreshToken.Token);
                // Do not persist the raw token
                refreshToken.Token = string.Empty;
            }

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            refreshToken.UpdatedAt = DateTime.UtcNow;
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }

        private static string ComputeSha256Hash(string raw)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(raw);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
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
