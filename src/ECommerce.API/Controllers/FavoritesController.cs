using ECommerce.Business.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFavorites([FromQuery] Guid userId)
        {
            var favorites = await _favoriteService.GetFavoritesAsync(userId);
            return Ok(favorites);
        }

        [HttpPost("{productId}")]
        public async Task<IActionResult> ToggleFavorite([FromQuery] Guid userId, int productId)
        {
            await _favoriteService.ToggleFavoriteAsync(userId, productId);
            return Ok();
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFavorite([FromQuery] Guid userId, int productId)
        {
            await _favoriteService.RemoveFavoriteAsync(userId, productId);
            return Ok();
        }
    }
}
