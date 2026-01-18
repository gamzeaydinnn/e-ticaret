namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Unit of Work pattern - tüm repository'leri merkezi olarak yönetir.
    /// Transaction'ları koordine eder ve tek bir SaveChanges çağrısı ile
    /// tüm değişiklikleri atomic olarak kaydeder.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Mevcut repository'ler
        IProductRepository Products { get; }
        ICategoryRepository Categories { get; }
        IOrderRepository Orders { get; }
        IUserRepository Users { get; }
        ICartRepository Cart { get; }
        
        // XML/Variant sistemi için yeni repository'ler
        IProductVariantRepository ProductVariants { get; }
        IProductOptionRepository ProductOptions { get; }
        IXmlFeedSourceRepository XmlFeedSources { get; }
        
        Task<int> SaveChangesAsync();
        int SaveChanges();
    }
}