using System;
using Microsoft.Extensions.Caching.Memory;
using ECommerce.Business.Services.Interfaces;

namespace ECommerce.Business.Services.Managers
{
    public class LoginRateLimitService : ILoginRateLimitService
    {
        private readonly IMemoryCache _cache;
        private readonly int _threshold = 5;
        private readonly TimeSpan _blockDuration = TimeSpan.FromMinutes(15);
        private readonly TimeSpan _attemptsTtl = TimeSpan.FromMinutes(15);

        public LoginRateLimitService(IMemoryCache cache)
        {
            _cache = cache;
        }

        private string AttemptsKey(string email) => $"login_attempts:{email?.ToLowerInvariant()}";
        private string BlockKey(string email) => $"login_blocked:{email?.ToLowerInvariant()}";

        public int IncrementFailedAttempt(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return 0;

            var attemptsKey = AttemptsKey(email);
            var blockKey = BlockKey(email);

            var attempts = 0;
            if (!_cache.TryGetValue<int>(attemptsKey, out attempts))
            {
                attempts = 0;
            }

            attempts++;

            // store attempts with TTL so stale attempts eventually expire
            _cache.Set(attemptsKey, attempts, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _attemptsTtl
            });

            if (attempts >= _threshold)
            {
                var blockedUntil = DateTime.UtcNow.Add(_blockDuration);
                _cache.Set(blockKey, blockedUntil, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _blockDuration
                });
            }

            return attempts;
        }

        public bool IsBlocked(string email, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(email)) return false;

            var blockKey = BlockKey(email);
            if (_cache.TryGetValue<DateTime>(blockKey, out var blockedUntil))
            {
                var now = DateTime.UtcNow;
                if (blockedUntil > now)
                {
                    remaining = blockedUntil - now;
                    return true;
                }
                // expired but still in cache? let cache expiration handle removal; treat as not blocked
            }
            return false;
        }

        public void ResetAttempts(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return;
            _cache.Remove(AttemptsKey(email));
            _cache.Remove(BlockKey(email));
        }

        public int GetAttempts(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return 0;
            if (_cache.TryGetValue<int>(AttemptsKey(email), out var attempts)) return attempts;
            return 0;
        }
    }
}
