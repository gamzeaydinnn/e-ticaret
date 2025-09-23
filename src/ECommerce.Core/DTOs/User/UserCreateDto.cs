namespace ECommerce.Core.DTOs.User

{
public class UserCreateDto
{
 
public string Email { get; set; } = null!;   // 🔹 eksikse ekle
public string Password { get; set; } = null!; 
public string Name { get; set; }
public string Description { get; set; }
public decimal Price { get; set; }
public int StockQuantity { get; set; }
public int CategoryId { get; set; }

}
}
