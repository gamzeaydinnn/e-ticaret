using ECommerce.Entities.Concrete;
namespace ECommerce.Entities.Concrete
{
public class Courier
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}
}