using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.API.Controllers;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ECommerce.Tests.Controllers
{
    public class CategoriesControllerTests
    {
        [Fact]
        public async Task GetAllCategories_ShouldReturnOnlyCanonicalStorefrontCategories()
        {
            var categoryService = new Mock<ICategoryService>();
            categoryService
                .Setup(service => service.GetAllAsync())
                .ReturnsAsync(new List<Category>
                {
                    new()
                    {
                        Id = 1,
                        Name = "Et ve Et Ürünleri",
                        Slug = "et-ve-et-urunleri",
                        IsActive = true
                    },
                    new()
                    {
                        Id = 2,
                        Name = "1001",
                        Slug = "1001",
                        IsActive = true
                    },
                    new()
                    {
                        Id = 3,
                        Name = "Diğer",
                        Slug = "diger",
                        IsActive = true
                    }
                });

            var controller = new CategoriesController(categoryService.Object);

            var actionResult = await controller.GetAllCategories();
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var payload = Assert.IsAssignableFrom<IEnumerable<ECommerce.Core.DTOs.Category.CategoryListDto>>(okResult.Value)
                .ToList();

            Assert.Single(payload);
            Assert.Equal("et-ve-et-urunleri", payload[0].Slug);
        }
    }
}