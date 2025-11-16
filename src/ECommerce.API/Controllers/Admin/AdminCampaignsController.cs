using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;
using ECommerce.Core.DTOs.Promotions;
using ECommerce.Entities.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Admin
{
    [Authorize(Roles = Roles.AdminLike)]
    [ApiController]
    [Route("api/admin/campaigns")]
    public class AdminCampaignsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;

        public AdminCampaignsController(ICampaignService campaignService)
        {
            _campaignService = campaignService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CampaignListDto>>> GetCampaigns()
        {
            var campaigns = await _campaignService.GetAllAsync();
            var result = campaigns.Select(MapToListDto).ToList();
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CampaignDetailDto>> GetCampaignById(int id)
        {
            var campaign = await _campaignService.GetByIdAsync(id);
            if (campaign == null)
            {
                return NotFound(new { message = "Kampanya bulunamadı." });
            }

            return Ok(MapToDetailDto(campaign));
        }

        [HttpPost]
        public async Task<ActionResult<CampaignDetailDto>> CreateCampaign([FromBody] CampaignSaveDto dto)
        {
            if (dto == null || !ModelState.IsValid)
            {
                return BadRequest(new { message = "Geçersiz kampanya verisi." });
            }

            if (dto.EndDate < dto.StartDate)
            {
                return BadRequest(new { message = "Bitiş tarihi başlangıç tarihinden önce olamaz." });
            }

            var created = await _campaignService.CreateAsync(dto);
            var detailDto = MapToDetailDto(created);
            return CreatedAtAction(nameof(GetCampaignById), new { id = detailDto.Id }, detailDto);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CampaignDetailDto>> UpdateCampaign(int id, [FromBody] CampaignSaveDto dto)
        {
            if (dto == null || !ModelState.IsValid)
            {
                return BadRequest(new { message = "Geçersiz kampanya verisi." });
            }

            if (dto.EndDate < dto.StartDate)
            {
                return BadRequest(new { message = "Bitiş tarihi başlangıç tarihinden önce olamaz." });
            }

            var existing = await _campaignService.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = "Kampanya bulunamadı." });
            }

            var updated = await _campaignService.UpdateAsync(id, dto);
            return Ok(MapToDetailDto(updated));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCampaign(int id)
        {
            var deleted = await _campaignService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = "Kampanya bulunamadı." });
            }

            return NoContent();
        }

        private static CampaignListDto MapToListDto(Campaign campaign)
        {
            return new CampaignListDto
            {
                Id = campaign.Id,
                Name = campaign.Name,
                Description = campaign.Description,
                StartDate = campaign.StartDate,
                EndDate = campaign.EndDate,
                IsActive = campaign.IsActive
            };
        }

        private static CampaignDetailDto MapToDetailDto(Campaign campaign)
        {
            var firstRule = campaign.Rules?.FirstOrDefault();
            var firstReward = campaign.Rewards?.FirstOrDefault();

            var detail = new CampaignDetailDto
            {
                Id = campaign.Id,
                Name = campaign.Name,
                Description = campaign.Description,
                StartDate = campaign.StartDate,
                EndDate = campaign.EndDate,
                IsActive = campaign.IsActive,
                ConditionJson = firstRule?.ConditionJson,
                RewardType = firstReward?.RewardType ?? "Percent",
                RewardValue = firstReward?.Value ?? 0
            };

            if (campaign.Rules != null)
            {
                detail.RulesSummaries = campaign.Rules
                    .Where(r => !string.IsNullOrWhiteSpace(r.ConditionJson))
                    .Select(r => r.ConditionJson)
                    .ToList();
            }

            if (campaign.Rewards != null)
            {
                detail.RewardsSummaries = campaign.Rewards
                    .Select(BuildRewardSummary)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            return detail;
        }

        private static string BuildRewardSummary(CampaignReward reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            var culture = new CultureInfo("tr-TR");
            return reward.RewardType?.ToLowerInvariant() switch
            {
                "amount" => $"₺{reward.Value.ToString("0.##", culture)} İndirim",
                "freeshipping" => "Ücretsiz Kargo",
                _ => $"%{reward.Value.ToString("0.##", culture)} İndirim"
            };
        }
    }
}
