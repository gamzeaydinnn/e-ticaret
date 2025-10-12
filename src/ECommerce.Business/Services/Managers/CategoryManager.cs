using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // ICategoryService
using ECommerce.Entities.Concrete;            // Category
using ECommerce.Core.Interfaces;              // ICategoryRepository


namespace ECommerce.Business.Services.Managers
{
    public class CategoryManager : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryManager(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task AddAsync(Category category)
        {
            await _categoryRepository.AddAsync(category);
        }

        public async Task DeleteAsync(Category category)
        {
            await _categoryRepository.DeleteAsync(category);
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _categoryRepository.GetAllAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _categoryRepository.GetByIdAsync(id);
        }

        public async Task<Category?> GetBySlugAsync(string slug)
        {
            return await _categoryRepository.GetBySlugAsync(slug);
        }

        public Task<int> GetCategoryCountAsync()
        {
            throw new NotImplementedException();
        }

        public async Task UpdateAsync(Category category)
        {
            await _categoryRepository.UpdateAsync(category);
        }
    }
}
