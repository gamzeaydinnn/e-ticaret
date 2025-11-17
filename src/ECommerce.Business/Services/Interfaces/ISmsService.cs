using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    public interface ISmsService
    {
        Task SendAsync(string phoneNumber, string message);
    }
}
