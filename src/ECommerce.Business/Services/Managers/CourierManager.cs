using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using System.Linq;
using System;

namespace ECommerce.Business.Services.Managers
{
    public class CourierManager : ICourierService
    {
        private readonly IRepository<Courier> _courierRepository;

        public CourierManager(IRepository<Courier> courierRepository)
        {
            _courierRepository = courierRepository;
        }

        public async Task<IEnumerable<Courier>> GetAllAsync()
        {
            return await _courierRepository.GetAllAsync();
        }

        public async Task<Courier?> GetByIdAsync(int id)
        {
            return await _courierRepository.GetByIdAsync(id);
        }

        public async Task AddAsync(Courier courier)
        {
            await _courierRepository.AddAsync(courier);
        }

        public async Task UpdateAsync(Courier courier)
{
    await _courierRepository.UpdateAsync(courier);
}

public async Task DeleteAsync(Courier courier)
{
    await _courierRepository.DeleteAsync(courier);
}


        public async Task<int> GetCourierCountAsync()
        {
            var all = await _courierRepository.GetAllAsync();
            return all.Count();
        }
    }
}
