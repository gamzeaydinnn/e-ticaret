using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Data.Repositories;
using System;
using System.Threading.Tasks;


namespace ECommerce.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ECommerceDbContext _context;
        private IProductRepository? _products;
        private ICategoryRepository? _categories;
        private IOrderRepository? _orders;
        private IUserRepository? _users;
        private ICartRepository? _cart;

        public UnitOfWork(ECommerceDbContext context)
        {
            _context = context;
        }

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