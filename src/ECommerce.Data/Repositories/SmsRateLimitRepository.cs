using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;

namespace ECommerce.Data.Repositories
{
    /// <summary>
    /// SMS rate limiting repository implementasyonu.
    /// SOLID: Single Responsibility - Sadece rate limit veri erişimi
    /// </summary>
    public class SmsRateLimitRepository : BaseRepository<SmsRateLimit>, ISmsRateLimitRepository
    {
        public SmsRateLimitRepository(ECommerceDbContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public async Task<SmsRateLimit?> GetByPhoneAsync(string phoneNumber)
        {
            return await _dbSet
                .Where(x => x.PhoneNumber == phoneNumber && x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<SmsRateLimit?> GetByIpAsync(string ipAddress)
        {
            return await _dbSet
                .Where(x => x.IpAddress == ipAddress && x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<SmsRateLimit> GetOrCreateAsync(string phoneNumber, string? ipAddress = null)
        {
            var existing = await GetByPhoneAsync(phoneNumber);
            
            if (existing != null)
            {
                // Sayaçları sıfırla gerekirse
                var now = DateTime.UtcNow;
                
                if (existing.ShouldResetDaily)
                {
                    existing.DailyCount = 0;
                    existing.DailyResetAt = now.Date.AddDays(1); // Yarın gece yarısı
                }
                
                if (existing.ShouldResetHourly)
                {
                    existing.HourlyCount = 0;
                    existing.HourlyResetAt = now.AddHours(1);
                }

                existing.UpdatedAt = now;
                await _context.SaveChangesAsync();
                
                return existing;
            }

            // Yeni kayıt oluştur
            var now2 = DateTime.UtcNow;
            var newLimit = new SmsRateLimit
            {
                PhoneNumber = phoneNumber,
                IpAddress = ipAddress,
                DailyCount = 0,
                HourlyCount = 0,
                LastSentAt = DateTime.MinValue,
                DailyResetAt = now2.Date.AddDays(1),
                HourlyResetAt = now2.AddHours(1),
                CreatedAt = now2,
                IsActive = true
            };

            await _dbSet.AddAsync(newLimit);
            await _context.SaveChangesAsync();

            return newLimit;
        }

        /// <inheritdoc />
        public async Task IncrementCountersAsync(string phoneNumber, string? ipAddress = null)
        {
            var limit = await GetOrCreateAsync(phoneNumber, ipAddress);
            var now = DateTime.UtcNow;

            limit.DailyCount++;
            limit.HourlyCount++;
            limit.LastSentAt = now;
            limit.UpdatedAt = now;

            // IP adresini güncelle (varsa)
            if (!string.IsNullOrEmpty(ipAddress) && limit.IpAddress != ipAddress)
            {
                limit.IpAddress = ipAddress;
            }

            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<int> ResetDailyCountersAsync()
        {
            var now = DateTime.UtcNow;

            var expiredRecords = await _dbSet
                .Where(x => x.DailyResetAt <= now && x.IsActive)
                .ToListAsync();

            foreach (var record in expiredRecords)
            {
                record.DailyCount = 0;
                record.DailyResetAt = now.Date.AddDays(1);
                record.UpdatedAt = now;
            }

            return await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<int> ResetHourlyCountersAsync()
        {
            var now = DateTime.UtcNow;

            var expiredRecords = await _dbSet
                .Where(x => x.HourlyResetAt <= now && x.IsActive)
                .ToListAsync();

            foreach (var record in expiredRecords)
            {
                record.HourlyCount = 0;
                record.HourlyResetAt = now.AddHours(1);
                record.UpdatedAt = now;
            }

            return await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task BlockPhoneAsync(string phoneNumber, TimeSpan duration, string reason)
        {
            var limit = await GetOrCreateAsync(phoneNumber);
            var now = DateTime.UtcNow;

            limit.IsBlocked = true;
            limit.BlockedUntil = now.Add(duration);
            limit.BlockReason = reason;
            limit.UpdatedAt = now;

            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task UnblockPhoneAsync(string phoneNumber)
        {
            var limit = await GetByPhoneAsync(phoneNumber);
            
            if (limit != null)
            {
                limit.IsBlocked = false;
                limit.BlockedUntil = null;
                limit.BlockReason = null;
                limit.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
        }

        /// <inheritdoc />
        public async Task RecordFailedAttemptAsync(string phoneNumber)
        {
            var limit = await GetOrCreateAsync(phoneNumber);
            
            limit.TotalFailedAttempts++;
            limit.UpdatedAt = DateTime.UtcNow;

            // Çok fazla başarısız deneme varsa otomatik blokla
            if (limit.TotalFailedAttempts >= 10)
            {
                limit.IsBlocked = true;
                limit.BlockedUntil = DateTime.UtcNow.AddHours(1);
                limit.BlockReason = "Çok fazla başarısız deneme";
            }

            await _context.SaveChangesAsync();
        }
    }
}
