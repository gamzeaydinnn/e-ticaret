using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
    
using ECommerce.Data.Repositories;
using ECommerce.Core.Interfaces;
using ECommerce.Core.DTOs.Auth;

namespace ECommerce.Business.Services.Managers
{
    public class UserManager : IUserService
    {
        private readonly IUserRepository _userRepository;

        // Constructor ile repository’yi inject ediyoruz
        public UserManager(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _userRepository.ExistsAsync(email);
        }

        public async Task AddAsync(User user)
        {
            await _userRepository.AddAsync(user);
        }

        public void Update(User user)
        {
            _userRepository.Update(user);
        }

        public void Delete(User user)
        {
            _userRepository.Delete(user);
        }

        public Task UpdateAsync(User user)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(User user)
        {
            throw new NotImplementedException();

        }
        public async Task<int> GetUserCountAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Count();
        }
        public async Task<bool> ChangePasswordAsync(ChangePasswordDto dto)
{
    // TODO: Şifre değiştirme işlemi burada yapılacak
    return await Task.FromResult(true);
}

public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto dto)
{
    // TODO: Şifre sıfırlama maili gönderme işlemi burada yapılacak
    return await Task.FromResult(true);
}

public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
{
    // TODO: Yeni şifre kaydetme işlemi burada yapılacak
    return await Task.FromResult(true);
}

    }
}
