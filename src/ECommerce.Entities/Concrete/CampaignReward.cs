using System;

namespace ECommerce.Entities.Concrete
{
    public class CampaignReward : BaseEntity
    {
        public int CampaignId { get; set; }
        public string RewardType { get; set; } = "Percent"; // Percent, Amount, FreeShipping
        public decimal Value { get; set; }

        public virtual Campaign Campaign { get; set; } = null!;
    }
}

