namespace ECommerce.Core.Interfaces
{
    public interface IInventoryService
    {
        Task<bool> IncreaseStockAsync(int productId, int quantity);
        Task<bool> DecreaseStockAsync(int productId, int quantity);
        Task<int> GetStockLevelAsync(int productId);
    }
}
//stok artır/azalt.