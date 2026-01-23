using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Pricing;
using ECommerce.Core.DTOs.Promotions;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    /// <summary>
    /// CampaignManager için unit testler
    /// Kampanya hesaplama mantığını doğrular
    /// </summary>
    public class CampaignManagerTests
    {
        private readonly Mock<ICampaignRepository> _campaignRepositoryMock;
        private readonly Mock<ILogger<CampaignManager>> _loggerMock;

        public CampaignManagerTests()
        {
            _campaignRepositoryMock = new Mock<ICampaignRepository>();
            _loggerMock = new Mock<ILogger<CampaignManager>>();
        }

        private CampaignManager CreateManager()
        {
            return new CampaignManager(
                _campaignRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        /// <summary>
        /// CartItemForCampaign nesnesi oluşturur
        /// </summary>
        private CartItemForCampaign CreateCartItem(int productId, int quantity, decimal unitPrice, int categoryId)
        {
            return new CartItemForCampaign
            {
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                CategoryId = categoryId
            };
        }

        #region Yüzde İndirim Testleri

        [Fact]
        public async Task CalculateCampaignDiscount_Percentage_ShouldApplyCorrectDiscount()
        {
            // Arrange
            var campaign = new Campaign
            {
                Id = 1,
                Name = "Test %20 İndirim",
                Type = CampaignType.Percentage,
                TargetType = CampaignTargetType.All,
                DiscountValue = 20,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _campaignRepositoryMock
                .Setup(r => r.GetActiveCampaignsAsync(It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<Campaign> { campaign });

            var manager = CreateManager();

            var items = new List<CartItemForCampaign>
            {
                CreateCartItem(1, 2, 100m, 1) // 2 x 100 = 200 TL
            };

            // Act
            var result = await manager.CalculateCampaignDiscountsAsync(items, 200m);

            // Assert
            Assert.NotNull(result);
            // %20 indirim: 200 * 0.20 = 40 TL
            Assert.Equal(40m, result.TotalCampaignDiscount);
        }

        [Fact]
        public async Task CalculateCampaignDiscount_Percentage_ShouldRespectMaxDiscount()
        {
            // Arrange
            var campaign = new Campaign
            {
                Id = 1,
                Name = "Test %50 İndirim (Max 30 TL)",
                Type = CampaignType.Percentage,
                TargetType = CampaignTargetType.All,
                DiscountValue = 50,
                MaxDiscountAmount = 30m, // Maksimum 30 TL
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _campaignRepositoryMock
                .Setup(r => r.GetActiveCampaignsAsync(It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<Campaign> { campaign });

            var manager = CreateManager();

            var items = new List<CartItemForCampaign>
            {
                CreateCartItem(1, 1, 200m, 1) // %50 = 100 TL ama max 30 TL
            };

            // Act
            var result = await manager.CalculateCampaignDiscountsAsync(items, 200m);

            // Assert
            Assert.NotNull(result);
            // %50 = 100 TL ama max 30 TL
            Assert.Equal(30m, result.TotalCampaignDiscount);
        }

        #endregion

        #region Sabit Tutar İndirim Testleri

        [Fact]
        public async Task CalculateCampaignDiscount_FixedAmount_ShouldApplyCorrectDiscount()
        {
            // Arrange
            var campaign = new Campaign
            {
                Id = 1,
                Name = "Test 50 TL İndirim",
                Type = CampaignType.FixedAmount,
                TargetType = CampaignTargetType.All,
                DiscountValue = 50,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _campaignRepositoryMock
                .Setup(r => r.GetActiveCampaignsAsync(It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<Campaign> { campaign });

            var manager = CreateManager();

            var items = new List<CartItemForCampaign>
            {
                CreateCartItem(1, 1, 200m, 1)
            };

            // Act
            var result = await manager.CalculateCampaignDiscountsAsync(items, 200m);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(50m, result.TotalCampaignDiscount);
        }

        #endregion

        #region X Al Y Öde Testleri

        [Fact]
        public async Task CalculateCampaignDiscount_BuyXPayY_3Al2Ode_ShouldApplyCorrectDiscount()
        {
            // Arrange
            var campaign = new Campaign
            {
                Id = 1,
                Name = "3 Al 2 Öde",
                Type = CampaignType.BuyXPayY,
                TargetType = CampaignTargetType.All,
                BuyQty = 3,
                PayQty = 2,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _campaignRepositoryMock
                .Setup(r => r.GetActiveCampaignsAsync(It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<Campaign> { campaign });

            var manager = CreateManager();

            var items = new List<CartItemForCampaign>
            {
                CreateCartItem(1, 3, 100m, 1) // 3 adet, en ucuzu bedava
            };

            // Act
            var result = await manager.CalculateCampaignDiscountsAsync(items, 300m);

            // Assert
            Assert.NotNull(result);
            // 3 al 2 öde: 1 adet bedava = 100 TL indirim
            Assert.Equal(100m, result.TotalCampaignDiscount);
        }

        [Fact]
        public async Task CalculateCampaignDiscount_BuyXPayY_NotEnoughQuantity_ShouldNotApply()
        {
            // Arrange
            var campaign = new Campaign
            {
                Id = 1,
                Name = "3 Al 2 Öde",
                Type = CampaignType.BuyXPayY,
                TargetType = CampaignTargetType.All,
                BuyQty = 3,
                PayQty = 2,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _campaignRepositoryMock
                .Setup(r => r.GetActiveCampaignsAsync(It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<Campaign> { campaign });

            var manager = CreateManager();

            var items = new List<CartItemForCampaign>
            {
                CreateCartItem(1, 2, 100m, 1) // 3 değil, kampanya uygulanmaz
            };

            // Act
            var result = await manager.CalculateCampaignDiscountsAsync(items, 200m);

            // Assert
            Assert.NotNull(result);
            // Yeterli adet yok, indirim 0
            Assert.Equal(0m, result.TotalCampaignDiscount);
        }

        [Fact]
        public async Task CalculateCampaignDiscount_BuyXPayY_6Items_ShouldApplyTwice()
        {
            // Arrange
            var campaign = new Campaign
            {
                Id = 1,
                Name = "3 Al 2 Öde",
                Type = CampaignType.BuyXPayY,
                TargetType = CampaignTargetType.All,
                BuyQty = 3,
                PayQty = 2,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _campaignRepositoryMock
                .Setup(r => r.GetActiveCampaignsAsync(It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<Campaign> { campaign });

            var manager = CreateManager();

            var items = new List<CartItemForCampaign>
            {
                CreateCartItem(1, 6, 100m, 1) // 2 set = 2 adet bedava
            };

            // Act
            var result = await manager.CalculateCampaignDiscountsAsync(items, 600m);

            // Assert
            Assert.NotNull(result);
            // 6 adet = 2 set, 2 adet bedava = 200 TL indirim
            Assert.Equal(200m, result.TotalCampaignDiscount);
        }

        #endregion

        #region Boş Sepet ve Edge Case Testleri

        [Fact]
        public async Task CalculateCampaignDiscount_EmptyCart_ShouldReturnZero()
        {
            // Arrange
            var campaign = new Campaign
            {
                Id = 1,
                Name = "%20 İndirim",
                Type = CampaignType.Percentage,
                TargetType = CampaignTargetType.All,
                DiscountValue = 20,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _campaignRepositoryMock
                .Setup(r => r.GetActiveCampaignsAsync(It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<Campaign> { campaign });

            var manager = CreateManager();

            var items = new List<CartItemForCampaign>(); // Boş sepet

            // Act
            var result = await manager.CalculateCampaignDiscountsAsync(items, 0m);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0m, result.TotalCampaignDiscount);
        }

        [Fact]
        public async Task CalculateCampaignDiscount_NoCampaigns_ShouldReturnZero()
        {
            // Arrange
            _campaignRepositoryMock
                .Setup(r => r.GetActiveCampaignsAsync(It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<Campaign>()); // Kampanya yok

            var manager = CreateManager();

            var items = new List<CartItemForCampaign>
            {
                CreateCartItem(1, 1, 100m, 1)
            };

            // Act
            var result = await manager.CalculateCampaignDiscountsAsync(items, 100m);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0m, result.TotalCampaignDiscount);
        }

        #endregion

        #region En İyi İndirim Seçimi Testleri

        [Fact]
        public async Task CalculateCampaignDiscount_MultipleCampaigns_ShouldSelectBestDiscount()
        {
            // Arrange
            var campaign1 = new Campaign
            {
                Id = 1,
                Name = "%10 İndirim",
                Type = CampaignType.Percentage,
                TargetType = CampaignTargetType.All,
                DiscountValue = 10,
                Priority = 100,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            var campaign2 = new Campaign
            {
                Id = 2,
                Name = "%25 İndirim",
                Type = CampaignType.Percentage,
                TargetType = CampaignTargetType.All,
                DiscountValue = 25,
                Priority = 50,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _campaignRepositoryMock
                .Setup(r => r.GetActiveCampaignsAsync(It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<Campaign> { campaign1, campaign2 });

            var manager = CreateManager();

            var items = new List<CartItemForCampaign>
            {
                CreateCartItem(1, 1, 100m, 1)
            };

            // Act
            var result = await manager.CalculateCampaignDiscountsAsync(items, 100m);

            // Assert
            Assert.NotNull(result);
            // En yüksek indirim seçilmeli: %25 = 25 TL
            Assert.Equal(25m, result.TotalCampaignDiscount);
        }

        #endregion

        #region Kategori Bazlı Kampanya Testleri

        [Fact]
        public async Task CalculateCampaignDiscount_CategoryBased_ShouldOnlyApplyToMatchingCategory()
        {
            // Arrange
            var campaign = new Campaign
            {
                Id = 1,
                Name = "Elektronik %10 İndirim",
                Type = CampaignType.Percentage,
                TargetType = CampaignTargetType.Category,
                DiscountValue = 10,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                Targets = new List<CampaignTarget>
                {
                    new CampaignTarget { TargetId = 1, TargetKind = CampaignTargetKind.Category } // Elektronik = 1
                }
            };

            _campaignRepositoryMock
                .Setup(r => r.GetActiveCampaignsAsync(It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<Campaign> { campaign });

            var manager = CreateManager();

            var items = new List<CartItemForCampaign>
            {
                CreateCartItem(1, 1, 1000m, 1), // Elektronik - kampanya uygulanır
                CreateCartItem(2, 1, 500m, 2)   // Giyim - kampanya uygulanmaz
            };

            // Act
            var result = await manager.CalculateCampaignDiscountsAsync(items, 1500m);

            // Assert
            Assert.NotNull(result);
            // Sadece elektronik kategorisine %10 = 100 TL
            Assert.Equal(100m, result.TotalCampaignDiscount);
        }

        #endregion
    }
}
