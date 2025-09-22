using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // TODO: Add User entity configuration
            // - Table name
            // - Primary key
            // - Property configurations
            // - Relationships
            // - Indexes
        }
    }
}