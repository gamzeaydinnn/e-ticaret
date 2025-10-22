using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // IProductService

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrerenderController : ControllerBase
    {
        private readonly IProductService _productService;

        public PrerenderController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: api/prerender/routes
        [HttpGet("routes")]
        public async Task<IActionResult> Routes()
        {
            try
            {
                // get recent active products (first 200)
                var products = await _productService.GetActiveProductsAsync(1, 200);
                var routes = new List<string> { "/", "/category/meyve-sebze" };
                routes.AddRange(products.Select(p => $"/product/{Slugify(p.Name ?? (p.Id.ToString()))}"));
                return Ok(routes.Distinct());
            }
            catch
            {
                // fallback to safe defaults
                var routes = new List<string> { "/", "/category/meyve-sebze", "/product/domates" };
                return Ok(routes);
            }
        }

        // GET: api/prerender/routes-with-meta
        [HttpGet("routes-with-meta")]
        public async Task<IActionResult> RoutesWithMeta()
        {
            try
            {
                var products = await _productService.GetActiveProductsAsync(1, 200);
                var items = products.Select(p => new {
                    route = $"/product/{Slugify(p.Name ?? (p.Id.ToString()))}",
                    title = p.Name,
                    description = p.Description,
                    image = p.ImageUrl
                }).ToList();
                // also include a category page
                items.Insert(0, new { route = "/category/meyve-sebze", title = (string?)"Meyve & Sebze", description = (string?)"Taze meyve ve sebze kategorisi", image = (string?)"/images/og-default.jpg" });
                return Ok(items);
            }
            catch
            {
                var items = new[] {
                    new { route = "/category/meyve-sebze", title = (string?)"Meyve & Sebze", description = (string?)"Taze meyve ve sebze kategorisi", image = (string?)"/images/og-default.jpg" },
                    new { route = "/product/domates", title = (string?)"Domates - Taze", description = (string?)"Günlük hasat domates", image = (string?)"/images/products/domates.jpg" }
                };
                return Ok(items);
            }
        }

        private static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            // basic slug: lowercase, replace spaces with '-', remove invalid chars
            var s = value.ToLowerInvariant();
            var sb = new System.Text.StringBuilder();
            foreach (var c in s)
            {
                if (char.IsLetterOrDigit(c) || c == '-') sb.Append(c);
                else if (char.IsWhiteSpace(c) || c == '_') sb.Append('-');
            }
            var outp = sb.ToString();
            while (outp.Contains("--")) outp = outp.Replace("--", "-");
            return outp.Trim('-');
        }
    }
}
