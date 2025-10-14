using ECommerce.Business.Services.Interfaces; // IUserService
using ECommerce.Entities.Concrete;           // User, diğer entityler
using ECommerce.Core.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;


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

        public async Task UpdateAsync(User user)
        {
            _userRepository.Update(user);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(User user)
        {
            _userRepository.Delete(user);
            await Task.CompletedTask;
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
