namespace ECommerce.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Products { get; }
        ICategoryRepository Categories { get; }
        IOrderRepository Orders { get; }
        IUserRepository Users { get; }
        ICartRepository Cart { get; }
        
        Task<int> SaveChangesAsync();
        int SaveChanges();
    }
}
