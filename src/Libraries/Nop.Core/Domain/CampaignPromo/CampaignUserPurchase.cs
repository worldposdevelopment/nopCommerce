using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Core.Domain.CampaignPromo
{
    public partial class CampaignUserPurchase : BaseEntity
    {
        public int CampaignId { get; set; }
        public int UserId { get; set; }
        public int CampaignPurchaseLimit { get; set; }
        public int CampaignPurchases { get; set; }
        
    }
}
