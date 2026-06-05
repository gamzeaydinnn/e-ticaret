using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.API.Controllers;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs;
using ECommerce.Core.DTOs.HomeBlock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Controllers
{
    public class HomeBlocksControllerTests
    {
        [Fact]
        public async Task GetBlockProductsPaged_ShouldReturnPagedResult()
        {
            var service = new Mock<IHomeBlockService>();
            var logger = new Mock<ILogger<HomeBlocksController>>();

            var paged = new PagedResult<HomeBlockProductItemDto>(
                new List<HomeBlockProductItemDto>
                {
                    new() { Id = 1, Name = "Ürün 1", Price = 100m, StockQuantity = 5 }
                },
                total: 13,
                skip: 12,
                take: 12);

            service
                .Setup(x => x.GetBlockBySlugPagedAsync("et-urunleri", 2, 12))
                .ReturnsAsync(paged);

            var controller = new HomeBlocksController(service.Object, logger.Object);

            var result = await controller.GetBlockProductsPaged("et-urunleri", 2, 12);
            var ok = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsType<PagedResult<HomeBlockProductItemDto>>(ok.Value);

            Assert.Equal(13, model.Total);
            Assert.Single(model.Items);
            Assert.Equal(12, model.Skip);
            Assert.Equal(12, model.Take);
        }
    }
}
