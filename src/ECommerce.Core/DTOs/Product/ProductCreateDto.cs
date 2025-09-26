namespace ECommerce.Core.DTOs.Product
{
    public class ProductCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }



        public string? ImageUrl { get; set; }
         public string? Brand { get; set; }
    }
        

       
    
}
