using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Data.Repositories
{
    /// <summary>
    /// SMS doğrulama repository implementasyonu.
    /// SOLID: Single Responsibility - Sadece SMS doğrulama veri erişimi
    /// </summary>
    public class SmsVerificationRepository : BaseRepository<SmsVerification>, ISmsVerificationRepository
    {
        public SmsVerificationRepository(ECommerceDbContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public async Task<SmsVerification?> GetActiveByPhoneAsync(string phoneNumber, SmsVerificationPurpose purpose)
        {
            var now = DateTime.UtcNow;

            return await _dbSet
                .Where(x => x.PhoneNumber == phoneNumber
                         && x.Purpose == purpose
                         && x.Status == SmsVerificationStatus.Pending
                         && x.ExpiresAt > now
                         && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SmsVerification>> GetByPhoneAsync(string phoneNumber)
        {
            return await _dbSet
                .Where(x => x.PhoneNumber == phoneNumber && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SmsVerification>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Where(x => x.UserId == userId && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SmsVerification>> GetExpiredAsync()
        {
            var now = DateTime.UtcNow;

            return await _dbSet
                .Where(x => x.ExpiresAt < now 
                         && x.Status == SmsVerificationStatus.Pending
                         && x.IsActive)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SmsVerification>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(x => x.CreatedAt >= startDate 
                         && x.CreatedAt <= endDate
                         && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<SmsVerification?> GetByPhoneAndCodeAsync(
            string phoneNumber, 
            string code, 
            SmsVerificationPurpose purpose)
        {
            var now = DateTime.UtcNow;

            return await _dbSet
                .Where(x => x.PhoneNumber == phoneNumber
                         && x.Code == code
                         && x.Purpose == purpose
                         && x.Status == SmsVerificationStatus.Pending
                         && x.ExpiresAt > now
                         && x.IsActive)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<int> CleanupExpiredAsync()
        {
            var now = DateTime.UtcNow;
            // 24 saatten eski expired kayıtları temizle
            var cutoffDate = now.AddHours(-24);

            var expiredRecords = await _dbSet
                .Where(x => x.ExpiresAt < cutoffDate 
                         && x.Status == SmsVerificationStatus.Pending
                         && x.IsActive)
                .ToListAsync();

            foreach (var record in expiredRecords)
            {
                record.Status = SmsVerificationStatus.Expired;
                record.IsActive = false;
                record.UpdatedAt = now;
            }

            return await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task CancelPendingByPhoneAsync(string phoneNumber, SmsVerificationPurpose purpose)
        {
            var now = DateTime.UtcNow;

            var pendingRecords = await _dbSet
                .Where(x => x.PhoneNumber == phoneNumber
                         && x.Purpose == purpose
                         && x.Status == SmsVerificationStatus.Pending
                         && x.IsActive)
                .ToListAsync();

            foreach (var record in pendingRecords)
            {
                record.Status = SmsVerificationStatus.Cancelled;
                record.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<int> GetSentCountSinceAsync(string phoneNumber, DateTime since)
        {
            return await _dbSet
                .CountAsync(x => x.PhoneNumber == phoneNumber
                              && x.CreatedAt >= since
                              && x.SmsSent);
        }
    }
}
