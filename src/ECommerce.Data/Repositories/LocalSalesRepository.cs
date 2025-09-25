using ECommerce.Core.Entities.Concrete;
using ECommerce.Data.Context;
//Amaç: Market içi satışları ve stok güncellemelerini kaydetmek
namespace ECommerce.Data.Repositories
{
    public class LocalSalesRepository
    {
        private readonly ECommerceDbContext _context;

        public LocalSalesRepository(ECommerceDbContext context)
        {
            _context = context;
        }

        public void AddSale(int productId, int quantity)
        {
            var product = _context.Products.Find(productId);
            if (product != null)
            {
                product.StockQuantity -= quantity;
                _context.SaveChanges();
            }
        }
    }
}
