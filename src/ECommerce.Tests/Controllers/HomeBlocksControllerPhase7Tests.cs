using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.API.Controllers;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.HomeBlock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Controllers
{
    public class HomeBlocksControllerPhase7Tests
    {
        [Fact]
        public async Task GetActiveBlocks_ShouldReturnOk_WithListOfBlocks()
        {
            var serviceMock = new Mock<IHomeBlockService>();
            var loggerMock = new Mock<ILogger<HomeBlocksController>>();

            var blocks = new List<HomeProductBlockDto>
            {
                new()
                {
                    Id = 1,
                    Name = "Fırsatlar",
                    PosterClickTarget = "https://example.com/banner",
                    ClickCount = 10,
                    ViewCount = 100,
                    ClickThroughRate = 10.0m
                }
            };

            serviceMock.Setup(s => s.GetAllActiveBlocksAsync())
                .ReturnsAsync(blocks);

            var controller = new HomeBlocksController(serviceMock.Object, loggerMock.Object);

            var actionResult = await controller.GetActiveBlocks();

            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var returnedBlocks = Assert.IsType<List<HomeProductBlockDto>>(okResult.Value);
            Assert.Single(returnedBlocks);
            Assert.Equal("https://example.com/banner", returnedBlocks[0].PosterClickTarget);
            Assert.Equal(10.0m, returnedBlocks[0].ClickThroughRate);
        }

        [Fact]
        public async Task TrackClick_ShouldReturnNoContent()
        {
            var serviceMock = new Mock<IHomeBlockService>();
            var loggerMock = new Mock<ILogger<HomeBlocksController>>();

            serviceMock.Setup(s => s.TrackPosterClickAsync(1))
                .Returns(Task.CompletedTask);

            var controller = new HomeBlocksController(serviceMock.Object, loggerMock.Object);

            var result = await controller.TrackClick(1);

            Assert.IsType<NoContentResult>(result);
            serviceMock.Verify(s => s.TrackPosterClickAsync(1), Times.Once);
        }

        [Fact]
        public async Task TrackView_ShouldReturnNoContent()
        {
            var serviceMock = new Mock<IHomeBlockService>();
            var loggerMock = new Mock<ILogger<HomeBlocksController>>();

            serviceMock.Setup(s => s.TrackBlockViewAsync(1))
                .Returns(Task.CompletedTask);

            var controller = new HomeBlocksController(serviceMock.Object, loggerMock.Object);

            var result = await controller.TrackView(1);

            Assert.IsType<NoContentResult>(result);
            serviceMock.Verify(s => s.TrackBlockViewAsync(1), Times.Once);
        }

        [Fact]
        public async Task CreateBlock_ShouldReturnBadRequest_WhenTargetCategoryIsInvalid()
        {
            var serviceMock = new Mock<IHomeBlockService>();
            var loggerMock = new Mock<ILogger<HomeBlocksController>>();

            var dto = new CreateHomeBlockDto
            {
                Name = "Kategori Bloğu",
                BlockType = "category",
                TargetCategoryId = 999
            };

            serviceMock.Setup(s => s.ValidateTargetCategoryAsync(999))
                .ReturnsAsync((false, "Seçilen kategori bulunamadı."));

            var controller = new HomeBlocksController(serviceMock.Object, loggerMock.Object);

            var actionResult = await controller.CreateBlock(dto);

            Assert.IsType<BadRequestObjectResult>(actionResult);
            serviceMock.Verify(s => s.CreateBlockAsync(It.IsAny<CreateHomeBlockDto>()), Times.Never);
        }

        [Fact]
        public async Task UpdateBlock_ShouldReturnBadRequest_WhenTargetCategoryIsInvalid()
        {
            var serviceMock = new Mock<IHomeBlockService>();
            var loggerMock = new Mock<ILogger<HomeBlocksController>>();

            var dto = new UpdateHomeBlockDto
            {
                Id = 5,
                Name = "Kategori Bloğu",
                BlockType = "category",
                TargetCategoryId = 999
            };

            serviceMock.Setup(s => s.ValidateTargetCategoryAsync(999))
                .ReturnsAsync((false, "Seçilen kategori pasif durumda."));

            var controller = new HomeBlocksController(serviceMock.Object, loggerMock.Object);

            var actionResult = await controller.UpdateBlock(5, dto);

            Assert.IsType<BadRequestObjectResult>(actionResult);
            serviceMock.Verify(s => s.UpdateBlockAsync(It.IsAny<int>(), It.IsAny<UpdateHomeBlockDto>()), Times.Never);
        }
    }
}
