using ECommerce.Entities.Concrete;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface IBrandRepository : IRepository<Brand>
    {
        Task<Brand?> GetByNameAsync(string name);
    }
}
