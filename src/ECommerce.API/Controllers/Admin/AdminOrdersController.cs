using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;

namespace ECommerce.API.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
public class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var orders = await _orderService.GetAllOrdersAsync(page, size);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        return Ok(order);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string status)
    {
        await _orderService.UpdateOrderStatusAsync(id, status);
        return NoContent();
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentOrders()
    {
        var orders = await _orderService.GetRecentOrdersAsync(10);
        return Ok(orders);
    }
}