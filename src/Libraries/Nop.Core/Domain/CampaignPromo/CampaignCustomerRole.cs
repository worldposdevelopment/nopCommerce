using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Core.Domain.CampaignPromo
{
    public partial class CampaignCustomerRole:BaseEntity
    {
        public int CustomerRoleId { get; set; }
        public string CampaignUid { get; set; }
    }
}
