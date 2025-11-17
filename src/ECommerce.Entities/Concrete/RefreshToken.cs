using System;

namespace ECommerce.Entities.Concrete
{
    public class RefreshToken : BaseEntity
    {
        public int UserId { get; set; }
        // Raw token value is returned to the client but should not be persisted.
        // The actual value stored in DB is `HashedToken`.
        public string Token { get; set; } = string.Empty;
        public string HashedToken { get; set; } = string.Empty;
        public string JwtId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? CreatedIp { get; set; }
        public virtual User User { get; set; } = null!;

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt.HasValue;
        public bool IsActiveToken => !IsExpired && !IsRevoked;
    }
}
