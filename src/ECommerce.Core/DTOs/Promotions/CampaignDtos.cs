using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.Promotions
{
    public class CampaignListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class CampaignDetailDto : CampaignListDto
    {
        public List<string> RulesSummaries { get; set; } = new();
        public List<string> RewardsSummaries { get; set; } = new();
        public string? ConditionJson { get; set; }
        public string RewardType { get; set; } = "Percent";
        public decimal RewardValue { get; set; }
    }

    public class CampaignSaveDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string? ConditionJson { get; set; }
        [Required]
        public string RewardType { get; set; } = "Percent";
        public decimal RewardValue { get; set; }
    }
}
