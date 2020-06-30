using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Core.Domain.CampaignPromo
{
     public partial class CampaignProduct : BaseEntity
    {
        public int Id { get; set; }
        public int ProductID { get; set; }
        public string? AttributeDescription { get; set; }
        public int? Quantity { get; set; }
        public decimal? SellPrice { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public string CreatedBy { get; set; }
        public string CampaignUid { get; set; }
        public int? AttributeID { get; set; }
    
}
}
