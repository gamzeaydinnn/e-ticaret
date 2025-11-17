using System;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface ITokenDenyList
    {
        Task AddAsync(string jti, DateTimeOffset expiration);
        Task<bool> IsDeniedAsync(string jti);
    }
}
