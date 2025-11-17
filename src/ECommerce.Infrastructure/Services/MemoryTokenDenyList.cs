using System;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.Infrastructure.Services
{
    // Simple in-memory deny list using IMemoryCache.
    public class MemoryTokenDenyList : ITokenDenyList
    {
        private readonly IMemoryCache _cache;

        public MemoryTokenDenyList(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task AddAsync(string jti, DateTimeOffset expiration)
        {
            if (string.IsNullOrWhiteSpace(jti)) return Task.CompletedTask;

            var options = new MemoryCacheEntryOptions();
            var now = DateTimeOffset.UtcNow;
            if (expiration <= now)
            {
                // already expired; nothing to store
                return Task.CompletedTask;
            }

            options.SetAbsoluteExpiration(expiration);
            // store a simple flag
            _cache.Set(GetKey(jti), true, options);
            return Task.CompletedTask;
        }

        public Task<bool> IsDeniedAsync(string jti)
        {
            if (string.IsNullOrWhiteSpace(jti)) return Task.FromResult(false);
            var exists = _cache.TryGetValue(GetKey(jti), out _);
            return Task.FromResult(exists);
        }

        private static string GetKey(string jti) => $"deny_jti::{jti}";
    }
}
