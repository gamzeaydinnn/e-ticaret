using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    public class HomeBlockManagerTests
    {
        private static ECommerceDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ECommerceDbContext(options);
        }

        private static HomeBlockManager CreateManager(ECommerceDbContext context, Mock<IHomeBlockRepository>? repositoryMock = null)
        {
            repositoryMock ??= new Mock<IHomeBlockRepository>();
            var loggerMock = new Mock<ILogger<HomeBlockManager>>();
            return new HomeBlockManager(repositoryMock.Object, context, loggerMock.Object);
        }

        [Fact]
        public async Task GetProductsByBlockTypeAsync_ShouldOrderBestsellersBySoldQuantity()
        {
            using var context = GetInMemoryDbContext();

            var category = new Category { Name = "Kategori", Slug = "kategori", IsActive = true };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var productA = new Product { Name = "A", Price = 10m, CategoryId = category.Id, IsActive = true };
            var productB = new Product { Name = "B", Price = 10m, CategoryId = category.Id, IsActive = true };
            var productC = new Product { Name = "C", Price = 10m, CategoryId = category.Id, IsActive = true };

            context.Products.AddRange(productA, productB, productC);
            await context.SaveChangesAsync();

            var deliveredOrder = new Order
            {
                OrderNumber = "ORD-1",
                Status = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Pending,
                OrderDate = DateTime.UtcNow.AddDays(-2)
            };
            var completedOrder = new Order
            {
                OrderNumber = "ORD-2",
                Status = OrderStatus.Completed,
                PaymentStatus = PaymentStatus.Pending,
                OrderDate = DateTime.UtcNow.AddDays(-1)
            };
            var paidOrder = new Order
            {
                OrderNumber = "ORD-3",
                Status = OrderStatus.Paid,
                PaymentStatus = PaymentStatus.Paid,
                OrderDate = DateTime.UtcNow
            };
            var cancelledOrder = new Order
            {
                OrderNumber = "ORD-4",
                Status = OrderStatus.Cancelled,
                PaymentStatus = PaymentStatus.Paid,
                OrderDate = DateTime.UtcNow
            };

            context.Orders.AddRange(deliveredOrder, completedOrder, paidOrder, cancelledOrder);
            await context.SaveChangesAsync();

            context.OrderItems.AddRange(
                new OrderItem { OrderId = deliveredOrder.Id, ProductId = productA.Id, Quantity = 2, UnitPrice = 10m },
                new OrderItem { OrderId = completedOrder.Id, ProductId = productB.Id, Quantity = 5, UnitPrice = 10m },
                new OrderItem { OrderId = paidOrder.Id, ProductId = productC.Id, Quantity = 1, UnitPrice = 10m },
                new OrderItem { OrderId = cancelledOrder.Id, ProductId = productA.Id, Quantity = 99, UnitPrice = 10m }
            );
            await context.SaveChangesAsync();

            var manager = CreateManager(context);

            var result = (await manager.GetProductsByBlockTypeAsync("bestseller", null, 3)).ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal(productB.Id, result[0].Id);
            Assert.Equal(productA.Id, result[1].Id);
            Assert.Equal(productC.Id, result[2].Id);
        }

        [Fact]
        public async Task GetProductsByBlockTypeAsync_ShouldFallbackToNewest_WhenNoQualifiedSalesExist()
        {
            using var context = GetInMemoryDbContext();

            var category = new Category { Name = "Kategori", Slug = "kategori", IsActive = true };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var olderProduct = new Product
            {
                Name = "Older",
                Price = 10m,
                CategoryId = category.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };
            var newestProduct = new Product
            {
                Name = "Newest",
                Price = 10m,
                CategoryId = category.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Products.AddRange(olderProduct, newestProduct);
            await context.SaveChangesAsync();

            var cancelledOrder = new Order
            {
                OrderNumber = "ORD-X",
                Status = OrderStatus.Cancelled,
                PaymentStatus = PaymentStatus.Paid,
                OrderDate = DateTime.UtcNow
            };

            context.Orders.Add(cancelledOrder);
            await context.SaveChangesAsync();

            context.OrderItems.Add(new OrderItem
            {
                OrderId = cancelledOrder.Id,
                ProductId = olderProduct.Id,
                Quantity = 10,
                UnitPrice = 10m
            });
            await context.SaveChangesAsync();

            var manager = CreateManager(context);

            var result = (await manager.GetProductsByBlockTypeAsync("bestseller", null, 2)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(newestProduct.Id, result[0].Id);
            Assert.Equal(olderProduct.Id, result[1].Id);
        }

        [Fact]
        public async Task GetActiveBlocksForHomepageAsync_ShouldHideOutOfStockProducts_ButKeepBlockActive()
        {
            using var context = GetInMemoryDbContext();

            var category = new Category { Name = "Kategori", Slug = "kategori", IsActive = true };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var inStockProduct = new Product
            {
                Name = "Stokta Var",
                Price = 10m,
                CategoryId = category.Id,
                IsActive = true,
                StockQuantity = 5
            };
            var outOfStockProduct = new Product
            {
                Name = "Stokta Yok",
                Price = 12m,
                CategoryId = category.Id,
                IsActive = true,
                StockQuantity = 0
            };

            context.Products.AddRange(inStockProduct, outOfStockProduct);
            await context.SaveChangesAsync();

            var block = new HomeProductBlock
            {
                Id = 1,
                Name = "Ana Sayfa Manuel Blok",
                Title = "Ana Sayfa Manuel Blok",
                Slug = "ana-sayfa-manuel-blok",
                BlockType = "manual",
                IsActive = true,
                MaxProductCount = 10,
                BlockProducts =
                {
                    new HomeBlockProduct
                    {
                        ProductId = inStockProduct.Id,
                        Product = inStockProduct,
                        DisplayOrder = 0,
                        IsActive = true,
                    },
                    new HomeBlockProduct
                    {
                        ProductId = outOfStockProduct.Id,
                        Product = outOfStockProduct,
                        DisplayOrder = 1,
                        IsActive = true,
                    }
                }
            };

            var repositoryMock = new Mock<IHomeBlockRepository>();
            repositoryMock
                .Setup(repository => repository.GetActiveBlocksWithProductsAsync())
                .ReturnsAsync(new[] { block });

            var manager = CreateManager(context, repositoryMock);

            var result = (await manager.GetActiveBlocksForHomepageAsync()).ToList();

            var mappedBlock = Assert.Single(result);
            var mappedProduct = Assert.Single(mappedBlock.Products);
            Assert.Equal(inStockProduct.Id, mappedProduct.Id);
        }

        [Fact]
        public async Task GetBlockBySlugAsync_ShouldHideOutOfStockAndZeroPriceProducts_ForFullCollectionView()
        {
            using var context = GetInMemoryDbContext();

            var category = new Category { Name = "Kategori", Slug = "kategori", IsActive = true };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var inStockProduct = new Product
            {
                Name = "Stokta Var",
                Price = 10m,
                CategoryId = category.Id,
                IsActive = true,
                StockQuantity = 5
            };
            var outOfStockProduct = new Product
            {
                Name = "Stokta Yok",
                Price = 12m,
                CategoryId = category.Id,
                IsActive = true,
                StockQuantity = 0
            };

            var zeroPriceProduct = new Product
            {
                Name = "Fiyatı Sıfır",
                Price = 0m,
                CategoryId = category.Id,
                IsActive = true,
                StockQuantity = 7
            };

            context.Products.AddRange(inStockProduct, outOfStockProduct, zeroPriceProduct);
            await context.SaveChangesAsync();

            var block = new HomeProductBlock
            {
                Id = 1,
                Name = "Manual Block",
                Title = "Manual Block",
                Slug = "manual-block",
                BlockType = "manual",
                IsActive = true,
                MaxProductCount = 10,
                BlockProducts =
                {
                    new HomeBlockProduct
                    {
                        ProductId = inStockProduct.Id,
                        Product = inStockProduct,
                        DisplayOrder = 0,
                        IsActive = true,
                    },
                    new HomeBlockProduct
                    {
                        ProductId = outOfStockProduct.Id,
                        Product = outOfStockProduct,
                        DisplayOrder = 1,
                        IsActive = true,
                    },
                    new HomeBlockProduct
                    {
                        ProductId = zeroPriceProduct.Id,
                        Product = zeroPriceProduct,
                        DisplayOrder = 2,
                        IsActive = true,
                    }
                }
            };

            var repositoryMock = new Mock<IHomeBlockRepository>();
            repositoryMock
                .Setup(repository => repository.GetBySlugAsync("manual-block"))
                .ReturnsAsync(block);

            var manager = CreateManager(context, repositoryMock);

            var result = await manager.GetBlockBySlugAsync("manual-block");

            Assert.NotNull(result);
            var product = Assert.Single(result!.Products);
            Assert.Equal(inStockProduct.Id, product.Id);
        }
    }
}
