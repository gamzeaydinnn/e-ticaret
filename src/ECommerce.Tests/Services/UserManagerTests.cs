using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces;
using ECommerce.Core.DTOs.Auth;
using ECommerce.Entities.Concrete;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    public class UserManagerTests
    {
        private static UserManager CreateUserManager(Mock<IUserRepository> userRepositoryMock)
        {
            return new UserManager(userRepositoryMock.Object);
        }

        [Fact]
        public async Task AddAsync_ShouldSetDefaultRole_AndCallRepository()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            var user = new User
            {
                Email = "newuser@test.com",
                UserName = "newuser@test.com",
                FirstName = "New",
                LastName = "User"
            };

            // Act
            await manager.AddAsync(user);

            // Assert
            userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u == user)), Times.Once);
            Assert.Equal("User", user.Role);
            Assert.True(user.IsActive);
        }

        [Fact]
        public async Task AddAsync_ShouldPropagateBusinessException_FromRepository()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            var user = new User
            {
                Email = "duplicate@test.com",
                UserName = "duplicate@test.com",
            };

            userRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<User>()))
                .ThrowsAsync(new BusinessException("Email already exists"));

            // Act
            var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.AddAsync(user));

            // Assert
            Assert.Equal("Email already exists", exception.Message);
            userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u == user)), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUserFromRepository()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            var user = new User { Id = 1, Email = "user@test.com" };

            userRepositoryMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(user);

            // Act
            var result = await manager.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
            Assert.Equal("user@test.com", result.Email);
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnUserFromRepository()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            var user = new User { Id = 5, Email = "lookup@test.com" };

            userRepositoryMock
                .Setup(r => r.GetByEmailAsync("lookup@test.com"))
                .ReturnsAsync(user);

            // Act
            var result = await manager.GetByEmailAsync("lookup@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result!.Id);
            Assert.Equal("lookup@test.com", result.Email);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllUsersFromRepository()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            var users = new[]
            {
                new User { Id = 1, Email = "user1@test.com" },
                new User { Id = 2, Email = "user2@test.com" }
            }.AsEnumerable();

            userRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(users);

            // Act
            var result = await manager.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task UserExistsAsync_ShouldDelegateToRepositoryExistsAsync()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            userRepositoryMock
                .Setup(r => r.ExistsAsync("exists@test.com"))
                .ReturnsAsync(true);

            // Act
            var exists = await manager.UserExistsAsync("exists@test.com");

            // Assert
            Assert.True(exists);
            userRepositoryMock.Verify(r => r.ExistsAsync("exists@test.com"), Times.Once);
        }

        [Fact]
        public void Update_ShouldCallRepositoryUpdate()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            var user = new User { Id = 10, Email = "update@test.com" };

            // Act
            manager.Update(user);

            // Assert
            userRepositoryMock.Verify(r => r.Update(It.Is<User>(u => u == user)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldCallRepositoryUpdate()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            var user = new User { Id = 11, Email = "updateasync@test.com" };

            // Act
            await manager.UpdateAsync(user);

            // Assert
            userRepositoryMock.Verify(r => r.Update(It.Is<User>(u => u == user)), Times.Once);
        }

        [Fact]
        public void Delete_ShouldCallRepositoryDelete()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            var user = new User { Id = 20, Email = "delete@test.com" };

            // Act
            manager.Delete(user);

            // Assert
            userRepositoryMock.Verify(r => r.Delete(It.Is<User>(u => u == user)), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldCallRepositoryDelete()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            var user = new User { Id = 21, Email = "deleteasync@test.com" };

            // Act
            await manager.DeleteAsync(user);

            // Assert
            userRepositoryMock.Verify(r => r.Delete(It.Is<User>(u => u == user)), Times.Once);
        }

        [Fact]
        public async Task GetUserCountAsync_ShouldReturnCountOfUsers()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            var users = new[]
            {
                new User { Id = 1 },
                new User { Id = 2 },
                new User { Id = 3 }
            }.AsEnumerable();

            userRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(users);

            // Act
            var count = await manager.GetUserCountAsync();

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldCurrentlyReturnTrue_ForAnyDto()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            var dto = new ChangePasswordDto
            {
                Email = "user@test.com",
                OldPassword = "OldPassword123",
                NewPassword = "NewPassword123",
                ConfirmPassword = "NewPassword123"
            };

            // Act
            var result = await manager.ChangePasswordAsync(dto);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ForgotPasswordAsync_ShouldCurrentlyReturnTrue_ForAnyDto()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            var dto = new ForgotPasswordDto
            {
                Email = "forgot@test.com"
            };

            // Act
            var result = await manager.ForgotPasswordAsync(dto);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldCurrentlyReturnTrue_ForAnyDto()
        {
            // Arrange
            var userRepositoryMock = new Mock<IUserRepository>();
            var manager = CreateUserManager(userRepositoryMock);

            var dto = new ResetPasswordDto
            {
                Email = "reset@test.com",
                Token = "token",
                NewPassword = "NewPassword123",
                ConfirmPassword = "NewPassword123"
            };

            // Act
            var result = await manager.ResetPasswordAsync(dto);

            // Assert
            Assert.True(result);
        }
    }
}

