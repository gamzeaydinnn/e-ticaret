using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Cart;
using ECommerce.Core.DTOs.Inventory;
using ECommerce.Core.Helpers;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Config;
using ECommerce.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services
{
    public class LocalInventoryRestoreTests
    {
        private static ECommerceDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ECommerceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new ECommerceDbContext(options);
        }

        private static InventoryManager CreateInventory(ECommerceDbContext db)
        {
            var productRepo = new Mock<IProductRepository>();
            productRepo
                .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => db.Products.FirstOrDefault(p => p.Id == id));
            productRepo
                .Setup(r => r.UpdateAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask);

            var emailSettings = Options.Create(new EmailSettings
            {
                FromEmail = "no-reply@test.local",
                FromName = "Test",
                UsePickupFolder = true,
                PickupDirectory = "TestEmails"
            });
            var env = new Mock<IHostEnvironment>();
            env.SetupGet(e => e.ContentRootPath).Returns(Environment.CurrentDirectory);
            var emailSender = new EmailSender(emailSettings, env.Object);
            var inventorySettings = Options.Create(new InventorySettings { CriticalStockThreshold = 1 });
            var configuration = new Mock<IConfiguration>();
            var log = new Mock<IInventoryLogService>();

            return new InventoryManager(
                productRepo.Object,
                db,
                emailSender,
                inventorySettings,
                configuration.Object,
                log.Object);
        }

        [Fact]
        public void Faz0_LocalInventoryPolicy_LocksMasterAndNoOutboundOnRestore()
        {
            Assert.False(LocalInventoryPolicy.PushOutboundOnOrderStockRestore);
            Assert.True(LocalInventoryPolicy.UseSiparisDocumentForOnlineSaleStock);
            Assert.True(LocalInventoryPolicy.IsCashOnDelivery("cash_on_delivery"));
            Assert.False(LocalInventoryPolicy.HoldsCommittedSellableStock(
                Entities.Enums.OrderStatus.Pending, isInventoryCommitted: false));
            Assert.True(LocalInventoryPolicy.HoldsCommittedSellableStock(
                Entities.Enums.OrderStatus.Paid, isInventoryCommitted: true));
            Assert.False(LocalInventoryPolicy.HoldsCommittedSellableStock(
                Entities.Enums.OrderStatus.Cancelled, isInventoryCommitted: true));
            Assert.True(LocalInventoryPolicy.IsPaymentSuccessStatus(Entities.Enums.OrderStatus.PreAuthorized));
        }

        [Fact]
        public async Task RestoreOrderStockAsync_IncreasesProductStockQuantity()
        {
            using var db = CreateDb();
            var product = new Product { Name = "Peynir", StockQuantity = 7 };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var inventory = CreateInventory(db);
            await inventory.RestoreOrderStockAsync(
                new[]
                {
                    new OrderStockRestoreLineDto
                    {
                        ProductId = product.Id,
                        Quantity = 3
                    }
                },
                LocalInventoryPolicy.LogActionRefund,
                "ORD-TEST");

            var reloaded = await db.Products.FindAsync(product.Id);
            Assert.Equal(10, reloaded!.StockQuantity);
        }

        [Fact]
        public async Task RestoreOrderStockAsync_AlsoRestoresVariantAndWarehouseStock()
        {
            using var db = CreateDb();
            var product = new Product { Name = "Zeytin", StockQuantity = 1 };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var variant = new ProductVariant
            {
                ProductId = product.Id,
                Title = "500g",
                Stock = 1,
                IsActive = true
            };
            db.ProductVariants.Add(variant);
            await db.SaveChangesAsync();

            db.Stocks.Add(new Stocks
            {
                ProductVariantId = variant.Id,
                WarehouseId = 1,
                Quantity = 1
            });
            await db.SaveChangesAsync();

            var inventory = CreateInventory(db);
            await inventory.RestoreOrderStockAsync(
                new[]
                {
                    new OrderStockRestoreLineDto
                    {
                        ProductId = product.Id,
                        ProductVariantId = variant.Id,
                        Quantity = 4
                    }
                },
                LocalInventoryPolicy.LogActionOrderCancelled,
                "ORD-VAR");

            Assert.Equal(5, (await db.Products.FindAsync(product.Id))!.StockQuantity);
            Assert.Equal(5, (await db.ProductVariants.FindAsync(variant.Id))!.Stock);
            Assert.Equal(5, (await db.Stocks.SingleAsync(s => s.ProductVariantId == variant.Id)).Quantity);
        }

        [Fact]
        public async Task CommitReservationAsync_DecreasesProductAndVariantStock()
        {
            using var db = CreateDb();
            var product = new Product { Name = "Bal", StockQuantity = 10 };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var variant = new ProductVariant
            {
                ProductId = product.Id,
                Title = "1kg",
                Stock = 10,
                IsActive = true
            };
            db.ProductVariants.Add(variant);
            await db.SaveChangesAsync();

            db.Stocks.Add(new Stocks { ProductVariantId = variant.Id, WarehouseId = 1, Quantity = 10 });
            await db.SaveChangesAsync();

            var clientOrderId = Guid.NewGuid();
            var order = new Order
            {
                OrderNumber = "ORD-COMMIT",
                ClientOrderId = clientOrderId,
                Status = Entities.Enums.OrderStatus.Pending,
                OrderDate = DateTime.UtcNow
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ProductVariantId = variant.Id,
                Quantity = 2,
                UnitPrice = 100
            });
            await db.SaveChangesAsync();

            var inventory = CreateInventory(db);
            var reserved = await inventory.ReserveStockAsync(
                clientOrderId,
                new List<CartItemDto>
                {
                    new() { ProductId = product.Id, Quantity = 2 }
                });
            Assert.True(reserved);

            await inventory.CommitReservationAsync(clientOrderId);

            Assert.Equal(8, (await db.Products.FindAsync(product.Id))!.StockQuantity);
            Assert.Equal(8, (await db.ProductVariants.FindAsync(variant.Id))!.Stock);
            Assert.Equal(8, (await db.Stocks.SingleAsync(s => s.ProductVariantId == variant.Id)).Quantity);
        }
    }
}
