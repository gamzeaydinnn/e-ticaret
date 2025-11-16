using System;
using System.Collections.Generic;

namespace ECommerce.Entities.Concrete
{
    public class Campaign : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<CampaignRule> Rules { get; set; } = new HashSet<CampaignRule>();
        public virtual ICollection<CampaignReward> Rewards { get; set; } = new HashSet<CampaignReward>();
    }
}

