using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Inventory;
using ECommerce.Core.Helpers;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Interfaces.Jobs;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Ödeme sonrası yerel stok settle + Mikro sipariş/iade kuyruğu.
    /// </summary>
    public class OrderInventorySettlementService : IOrderInventorySettlementService
    {
        private readonly ECommerceDbContext _db;
        private readonly IInventoryService _inventoryService;
        private readonly IInventoryLogService _inventoryLogService;
        private readonly ILogger<OrderInventorySettlementService> _logger;

        public OrderInventorySettlementService(
            ECommerceDbContext db,
            IInventoryService inventoryService,
            IInventoryLogService inventoryLogService,
            ILogger<OrderInventorySettlementService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _inventoryLogService = inventoryLogService ?? throw new ArgumentNullException(nameof(inventoryLogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SettlePaymentSuccessAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("[InventorySettle] Order bulunamadı. OrderId={OrderId}", orderId);
                return;
            }

            if (!order.IsInventoryCommitted)
            {
                var committedViaReservation = false;
                if (order.ClientOrderId.HasValue)
                {
                    var hadReservation = await _db.StockReservations.AnyAsync(
                        r => r.ClientOrderId == order.ClientOrderId.Value && !r.IsReleased,
                        cancellationToken);

                    if (hadReservation)
                    {
                        await _inventoryService.CommitReservationAsync(order.ClientOrderId.Value);
                        committedViaReservation = true;
                    }
                }

                if (!committedViaReservation)
                {
                    await CommitFromOrderLinesAsync(order);
                }

                order.IsInventoryCommitted = true;
                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "[InventorySettle] Stok commit. OrderId={OrderId}, Status={Status}",
                    orderId, order.Status);
            }

            EnqueueMikroOrderPush(orderId);
        }

        public async Task SettlePaymentFailureAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order == null)
            {
                return;
            }

            var wasCommitted = order.IsInventoryCommitted;

            if (wasCommitted)
            {
                var lines = (order.OrderItems ?? Array.Empty<OrderItem>())
                    .Where(i => i.Quantity > 0)
                    .Select(i => new OrderStockRestoreLineDto
                    {
                        ProductId = i.ProductId,
                        ProductVariantId = i.ProductVariantId,
                        Quantity = i.Quantity
                    })
                    .ToList();

                if (lines.Count > 0)
                {
                    await _inventoryService.RestoreOrderStockAsync(
                        lines,
                        LocalInventoryPolicy.LogActionPaymentFailed,
                        order.OrderNumber ?? order.Id.ToString());
                }

                order.IsInventoryCommitted = false;
            }
            else if (order.ClientOrderId.HasValue)
            {
                await _inventoryService.ReleaseReservationAsync(order.ClientOrderId.Value);
            }

            if (order.Status != OrderStatus.PaymentFailed && order.Status != OrderStatus.Cancelled)
            {
                var previous = order.Status;
                order.Status = OrderStatus.PaymentFailed;
                order.PaymentStatus = PaymentStatus.Failed;
                _db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    PreviousStatus = previous,
                    NewStatus = OrderStatus.PaymentFailed,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = "InventorySettle",
                    Reason = wasCommitted
                        ? "Ödeme başarısız — commit stok geri yüklendi"
                        : "Ödeme başarısız — rezervasyon serbest bırakıldı"
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[InventorySettle] Ödeme fail settle. OrderId={OrderId}, WasCommitted={Committed}",
                orderId, wasCommitted);
        }

        public static void EnqueueMikroRefundInvoice(int orderId, decimal refundAmount)
        {
            if (!LocalInventoryPolicy.UseSiparisDocumentForOnlineSaleStock)
            {
                return;
            }

            try
            {
                BackgroundJob.Enqueue<IFaturaSyncService>(
                    svc => svc.CreateRefundInvoiceAsync(orderId, refundAmount, CancellationToken.None));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InventorySettle] Refund invoice enqueue skipped: {ex.Message}");
            }
        }

        private static void EnqueueMikroOrderPush(int orderId)
        {
            if (!LocalInventoryPolicy.UseSiparisDocumentForOnlineSaleStock)
            {
                return;
            }

            try
            {
                BackgroundJob.Enqueue<ISiparisPushJob>(
                    job => job.PushOrderAsync(orderId, CancellationToken.None));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InventorySettle] Order push enqueue skipped: {ex.Message}");
            }
        }

        private async Task CommitFromOrderLinesAsync(Order order)
        {
            foreach (var group in order.OrderItems
                .Where(i => i.Quantity > 0)
                .GroupBy(i => new { i.ProductId, i.ProductVariantId }))
            {
                var qty = group.Sum(i => i.Quantity);
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == group.Key.ProductId);
                if (product == null)
                {
                    continue;
                }

                var oldStock = product.StockQuantity;
                var newStock = Math.Max(0, oldStock - qty);
                product.StockQuantity = newStock;

                await _inventoryLogService.WriteAsync(
                    product.Id,
                    LocalInventoryPolicy.LogActionCommit,
                    qty,
                    oldStock,
                    newStock,
                    $"Order:{order.OrderNumber ?? order.Id.ToString()}");

                if (group.Key.ProductVariantId.HasValue && group.Key.ProductVariantId > 0)
                {
                    var variant = await _db.ProductVariants.FirstOrDefaultAsync(
                        v => v.Id == group.Key.ProductVariantId.Value && v.ProductId == group.Key.ProductId);
                    if (variant != null)
                    {
                        variant.Stock = Math.Max(0, variant.Stock - qty);
                        var warehouseStock = await _db.Stocks.FirstOrDefaultAsync(
                            s => s.ProductVariantId == variant.Id);
                        if (warehouseStock != null)
                        {
                            warehouseStock.Quantity = Math.Max(0, warehouseStock.Quantity - qty);
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
