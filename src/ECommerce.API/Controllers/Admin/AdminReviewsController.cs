using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/reviews")]
    [Authorize(Roles = Roles.AdminLike)]
    public class AdminReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public AdminReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var pending = await _reviewService.GetPendingReviewsAsync();
            return Ok(pending);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            await _reviewService.ApproveReviewAsync(id);
            return NoContent();
        }

        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            await _reviewService.RejectReviewAsync(id);
            return NoContent();
        }
    }
}
