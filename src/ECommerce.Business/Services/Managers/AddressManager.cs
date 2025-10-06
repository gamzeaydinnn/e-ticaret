using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Concrete
{
    public class AddressManager : IAddressService
    {
        private readonly IAddressRepository _addressRepository;

        public AddressManager(IAddressRepository addressRepository)
        {
            _addressRepository = addressRepository;
        }

        public async Task<IEnumerable<Address>> GetAllAsync() => await _addressRepository.GetAllAsync();
        public async Task<Address?> GetByIdAsync(int id) => await _addressRepository.GetByIdAsync(id);
        public async Task AddAsync(Address address) => await _addressRepository.AddAsync(address);
        public async Task UpdateAsync(Address address) => await _addressRepository.UpdateAsync(address);
        public async Task DeleteAsync(int id)
        {
            var existing = await _addressRepository.GetByIdAsync(id);
            if (existing == null) return;
            await _addressRepository.DeleteAsync(existing);
        }
        public async Task<IEnumerable<Address>> GetByUserIdAsync(int userId) => await _addressRepository.GetByUserIdAsync(userId);
   
    }
}
