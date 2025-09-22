using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            // TODO: Add Product entity configuration
            // - Table name
            // - Primary key
            // - Property configurations
            // - Relationships
            // - Indexes
        }
    }
}