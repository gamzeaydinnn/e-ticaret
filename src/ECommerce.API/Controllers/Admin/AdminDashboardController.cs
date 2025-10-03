using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;

namespace ECommerce.API.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
public class AdminDashboardController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IProductService _productService;
    private readonly IOrderService _orderService;
    private readonly ICategoryService _categoryService;
    private readonly ICartService _cartService;
    private readonly IFavoriteService _favoriteService;
    private readonly ICourierService _courierService;
    private readonly IPaymentService _paymentService;

    public AdminDashboardController(
        IUserService userService,
        IProductService productService,
        IOrderService orderService,
        ICategoryService categoryService,
        ICartService cartService,
        IFavoriteService favoriteService,
        ICourierService courierService,
        IPaymentService paymentService
    )
    {
        _userService = userService;
        _productService = productService;
        _orderService = orderService;
        _categoryService = categoryService;
        _cartService = cartService;
        _favoriteService = favoriteService;
        _courierService = courierService;
        _paymentService = paymentService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var stats = new
        {
            TotalUsers = await _userService.GetUserCountAsync(),
            TotalProducts = await _productService.GetProductCountAsync(),
            TotalOrders = await _orderService.GetOrderCountAsync(),
            TotalCategories = await _categoryService.GetCategoryCountAsync(),
            TotalCarts = await _cartService.GetCartCountAsync(),
            TotalFavorites = await _favoriteService.GetFavoriteCountAsync(),
            TotalCouriers = await _courierService.GetCourierCountAsync(),
            TotalPayments = await _paymentService.GetPaymentCountAsync(),
            TodayOrders = await _orderService.GetTodayOrderCountAsync(),
            Revenue = await _orderService.GetTotalRevenueAsync()
        };

        return Ok(stats);
    }
}
