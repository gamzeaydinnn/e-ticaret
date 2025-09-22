using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Data.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            // TODO: Add Category entity configuration
            // - Table name
            // - Primary key
            // - Property configurations
            // - Self-referencing relationship
            // - Indexes
        }
    }
}