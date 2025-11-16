using System;

namespace ECommerce.Entities.Concrete
{
    public class CampaignRule : BaseEntity
    {
        public int CampaignId { get; set; }
        public string ConditionJson { get; set; } = string.Empty;

        public virtual Campaign Campaign { get; set; } = null!;
    }
}

