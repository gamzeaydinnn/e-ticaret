using ECommerce.Core.Entities.Concrete;
using ECommerce.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Entities.Concrete; // <- Burayı kullan
using System.Threading.Tasks;
using System.Collections.Generic;



[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ECommerceDbContext _context;
    public ProductController(ECommerceDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _context.Products.ToListAsync());

    [HttpPost]
public async Task<IActionResult> Post(ECommerce.Entities.Concrete.Product product) // <--- doğru sınıf
{
    await _context.Products.AddAsync(product);
    await _context.SaveChangesAsync();
    return Ok(product);
}

}
