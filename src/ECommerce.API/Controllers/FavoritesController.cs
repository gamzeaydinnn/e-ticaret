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

        // ================================================================
        // KAYITLI KULLANICI ENDPOİNT'LERİ
        // ================================================================

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

        // ================================================================
        // MİSAFİR KULLANICI ENDPOİNT'LERİ
        // ================================================================

        /// <summary>
        /// Misafir kullanıcının favorilerini getirir
        /// </summary>
        [HttpGet("guest")]
        [AllowAnonymous]
        public async Task<IActionResult> GetGuestFavorites()
        {
            try
            {
                var guestToken = Request.Headers["X-Favorites-Token"].ToString();
                if (string.IsNullOrEmpty(guestToken))
                {
                    return Ok(new { success = true, data = Array.Empty<object>() });
                }

                var favorites = await _favoriteService.GetGuestFavoritesAsync(guestToken);
                return Ok(new { success = true, data = favorites });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Misafir kullanıcının favorilerine ürün ekler/çıkarır
        /// </summary>
        [HttpPost("guest/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ToggleGuestFavorite(int productId)
        {
            try
            {
                var guestToken = Request.Headers["X-Favorites-Token"].ToString();
                if (string.IsNullOrEmpty(guestToken))
                {
                    return BadRequest(new { success = false, message = "X-Favorites-Token header gerekli" });
                }

                var result = await _favoriteService.ToggleGuestFavoriteAsync(guestToken, productId);
                return Ok(new { success = true, message = "Favori durumu güncellendi", action = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Misafir kullanıcının favorisinden ürün siler
        /// </summary>
        [HttpDelete("guest/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveGuestFavorite(int productId)
        {
            try
            {
                var guestToken = Request.Headers["X-Favorites-Token"].ToString();
                if (string.IsNullOrEmpty(guestToken))
                {
                    return BadRequest(new { success = false, message = "X-Favorites-Token header gerekli" });
                }

                await _favoriteService.RemoveGuestFavoriteAsync(guestToken, productId);
                return Ok(new { success = true, message = "Favori silindi" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Misafir favorilerini kullanıcı hesabına aktarır (login sonrası)
        /// </summary>
        [HttpPost("merge")]
        public async Task<IActionResult> MergeGuestFavorites([FromBody] MergeGuestFavoritesRequest request)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Kullanıcı doğrulanamadı" });
                }

                if (string.IsNullOrEmpty(request?.GuestToken))
                {
                    return Ok(new { success = true, mergedCount = 0, message = "Token sağlanmadı" });
                }

                var mergedCount = await _favoriteService.MergeGuestFavoritesToUserAsync(request.GuestToken, userId);
                return Ok(new { success = true, mergedCount, message = $"{mergedCount} favori aktarıldı" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    public class MergeGuestFavoritesRequest
    {
        public string GuestToken { get; set; } = string.Empty;
    }
}
