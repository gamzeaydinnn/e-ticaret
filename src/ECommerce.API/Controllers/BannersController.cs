using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    public class BannersController : ControllerBase
    {
        private readonly IBannerService _bannerService;
        private readonly ILogger<BannersController> _logger;
        
        public BannersController(IBannerService bannerService, ILogger<BannersController> logger)
        {
            _bannerService = bannerService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("🔍 BannersController.GetAll çağrıldı");
            var banners = await _bannerService.GetAllAsync();
            _logger.LogInformation($"✅ {(banners as System.Collections.Generic.IEnumerable<BannerDto>)?.Count() ?? 0} banner döndürüldü");
            return Ok(banners);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var banner = await _bannerService.GetByIdAsync(id);
            if (banner == null) return NotFound();
            return Ok(banner);
        }

        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Add([FromBody] BannerDto dto)
        {
            await _bannerService.AddAsync(dto);
            return Ok();
        }

        [HttpPut("{id}")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Update(int id, [FromBody] BannerDto dto)
        {
            dto.Id = id;
            await _bannerService.UpdateAsync(dto);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _bannerService.DeleteAsync(id);
            return Ok();
        }
    }
}
