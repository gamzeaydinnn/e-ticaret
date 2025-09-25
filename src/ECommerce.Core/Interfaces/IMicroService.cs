namespace ECommerce.Core.Interfaces
{
    public interface IMicroService
    {
        void UpdateProduct(ECommerce.Core.DTOs.Micro.MicroProductDto productDto);
        void UpdateStock(ECommerce.Core.DTOs.Micro.MicroStockDto stockDto);
        // İhtiyaca göre diğer metodlar eklenebilir
    }
}
