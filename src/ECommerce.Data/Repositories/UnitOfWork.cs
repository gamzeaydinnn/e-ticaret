using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Data.Repositories;
using System;
using System.Threading.Tasks;


namespace ECommerce.Data.Repositories
{
    /// <summary>
    /// Unit of Work pattern implementasyonu.
    /// Tüm repository'leri merkezi olarak yönetir ve transaction'ları koordine eder.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ECommerceDbContext _context;
        
        // Mevcut repository'ler
        private IProductRepository? _products;
        private ICategoryRepository? _categories;
        private IOrderRepository? _orders;
        private IUserRepository? _users;
        private ICartRepository? _cart;
        
        // XML/Variant sistemi için yeni repository'ler
        private IProductVariantRepository? _productVariants;
        private IProductOptionRepository? _productOptions;
        private IXmlFeedSourceRepository? _xmlFeedSources;

        public UnitOfWork(ECommerceDbContext context)
        {
            _context = context;
        }

        // Mevcut repository property'leri
        public IProductRepository Products =>
            _products ??= new ProductRepository(_context);

        public ICategoryRepository Categories =>
            _categories ??= new CategoryRepository(_context);

        public IOrderRepository Orders =>
            _orders ??= new OrderRepository(_context);

        public IUserRepository Users =>
            _users ??= new UserRepository(_context);

        public ICartRepository Cart =>
            _cart ??= new CartRepository(_context);

        // XML/Variant sistemi için yeni repository property'leri
        public IProductVariantRepository ProductVariants =>
            _productVariants ??= new ProductVariantRepository(_context);

        public IProductOptionRepository ProductOptions =>
            _productOptions ??= new ProductOptionRepository(_context);

        public IXmlFeedSourceRepository XmlFeedSources =>
            _xmlFeedSources ??= new XmlFeedSourceRepository(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}