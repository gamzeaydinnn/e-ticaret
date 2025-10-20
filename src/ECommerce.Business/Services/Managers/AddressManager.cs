using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    public class AddressManager : IAddressService
    {
        private readonly IAddressRepository _repository;

        public AddressManager(IAddressRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Address>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Address?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Address>> GetByUserIdAsync(int userId)
        {
            return await _repository.GetByUserIdAsync(userId);
        }

        public async Task AddAsync(Address address)
        {
            await _repository.AddAsync(address);
        }

        public async Task UpdateAsync(Address address)
        {
            await _repository.UpdateAsync(address);
        }

        public async Task DeleteAsync(int id)
        {
            var address = await _repository.GetByIdAsync(id);
            if (address != null)
            {
                await _repository.DeleteAsync(address);
            }
        }
    }
}
