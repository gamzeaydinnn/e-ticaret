using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;

namespace ECommerce.API.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
public class AdminDashboardController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IProductService _productService;
    private readonly IOrderService _orderService;

    public AdminDashboardController(IUserService userService, IProductService productService, IOrderService orderService)
    {
        _userService = userService;
        _productService = productService;
        _orderService = orderService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var stats = new
        {
            TotalUsers = await _userService.GetUserCountAsync(),
            TotalProducts = await _productService.GetProductCountAsync(),
            TotalOrders = await _orderService.GetOrderCountAsync(),
            TodayOrders = await _orderService.GetTodayOrderCountAsync(),
            Revenue = await _orderService.GetTotalRevenueAsync()
        };
        return Ok(stats);
    }
}