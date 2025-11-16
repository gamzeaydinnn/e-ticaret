using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Promotions;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Managers
{
    public class CampaignManager : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepository;

        public CampaignManager(ICampaignRepository campaignRepository)
        {
            _campaignRepository = campaignRepository;
        }

        public Task<List<Campaign>> GetActiveCampaignsAsync(DateTime? now = null)
        {
            return _campaignRepository.GetActiveCampaignsAsync(now);
        }

        public async Task<List<Campaign>> GetAllAsync()
        {
            var campaigns = await _campaignRepository.GetAllAsync();
            return campaigns.ToList();
        }

        public Task<Campaign?> GetByIdAsync(int id)
        {
            return _campaignRepository.GetByIdAsync(id);
        }

        public async Task<Campaign> CreateAsync(CampaignSaveDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var campaign = new Campaign
            {
                Name = dto.Name,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive
            };

            if (!string.IsNullOrWhiteSpace(dto.ConditionJson))
            {
                campaign.Rules.Add(new CampaignRule
                {
                    ConditionJson = dto.ConditionJson.Trim(),
                    Campaign = campaign
                });
            }

            campaign.Rewards.Add(new CampaignReward
            {
                RewardType = string.IsNullOrWhiteSpace(dto.RewardType) ? "Percent" : dto.RewardType,
                Value = dto.RewardValue,
                Campaign = campaign
            });

            await _campaignRepository.AddAsync(campaign);
            return await _campaignRepository.GetByIdAsync(campaign.Id) ?? campaign;
        }

        public async Task<Campaign> UpdateAsync(int id, CampaignSaveDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var campaign = await _campaignRepository.GetByIdAsync(id);
            if (campaign == null)
            {
                throw new KeyNotFoundException("Kampanya bulunamadı.");
            }

            campaign.Name = dto.Name;
            campaign.Description = dto.Description;
            campaign.StartDate = dto.StartDate;
            campaign.EndDate = dto.EndDate;
            campaign.IsActive = dto.IsActive;

            var condition = dto.ConditionJson?.Trim();
            if (!string.IsNullOrEmpty(condition))
            {
                var rule = campaign.Rules.FirstOrDefault();
                if (rule == null)
                {
                    rule = new CampaignRule { CampaignId = campaign.Id };
                    campaign.Rules.Add(rule);
                }
                rule.ConditionJson = condition;
            }
            else
            {
                var rule = campaign.Rules.FirstOrDefault();
                if (rule != null)
                {
                    rule.ConditionJson = string.Empty;
                }
            }

            var reward = campaign.Rewards.FirstOrDefault();
            if (reward == null)
            {
                reward = new CampaignReward { CampaignId = campaign.Id };
                campaign.Rewards.Add(reward);
            }
            reward.RewardType = string.IsNullOrWhiteSpace(dto.RewardType) ? "Percent" : dto.RewardType;
            reward.Value = dto.RewardValue;

            await _campaignRepository.UpdateAsync(campaign);
            return campaign;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var campaign = await _campaignRepository.GetByIdAsync(id);
            if (campaign == null)
            {
                return false;
            }

            await _campaignRepository.DeleteAsync(campaign);
            return true;
        }
    }
}
