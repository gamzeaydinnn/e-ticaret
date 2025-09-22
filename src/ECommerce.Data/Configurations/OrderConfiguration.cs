using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // TODO: Add Order entity configuration
            // - Table name
            // - Primary key
            // - Property configurations
            // - Relationships with User and OrderItems
            // - Indexes
        }
    }
}