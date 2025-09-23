using System.Threading.Tasks;
namespace ECommerce.Core.Interfaces
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
    }
}
