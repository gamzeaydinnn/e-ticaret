using ECommerce.Business.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Extensions;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFavorites([FromQuery] int? userId = null)
        {
            try
            {
                // Kimlik doğrulamadan gelen kullanıcı id'si kullanılmalı
                var effectiveUserId = User.GetUserId();
                if (effectiveUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Kullanıcı doğrulanamadı" });
                }

                var favorites = await _favoriteService.GetFavoritesAsync(effectiveUserId);
                return Ok(new { success = true, data = favorites });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{productId}")]
        public async Task<IActionResult> ToggleFavorite(int productId, [FromQuery] int? userId = null)
        {
            try
            {
                var effectiveUserId = User.GetUserId();
                if (effectiveUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Kullanıcı doğrulanamadı" });
                }

                await _favoriteService.ToggleFavoriteAsync(effectiveUserId, productId);
                return Ok(new { success = true, message = "Favori durumu güncellendi" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFavorite(int productId, [FromQuery] int? userId = null)
        {
            try
            {
                var effectiveUserId = User.GetUserId();
                if (effectiveUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Kullanıcı doğrulanamadı" });
                }

                await _favoriteService.RemoveFavoriteAsync(effectiveUserId, productId);
                return Ok(new { success = true, message = "Favori silindi" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
