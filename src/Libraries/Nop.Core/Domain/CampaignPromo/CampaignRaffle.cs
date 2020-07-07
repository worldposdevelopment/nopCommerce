using System;
using System.Collections.Generic;
using System.Text;


namespace Nop.Core.Domain.CampaignPromo
{
    public partial class CampaignRaffle : BaseEntity
    {
        public int CampaignId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerMobile { get; set; }
        public int SelectedAttribute { get; set; }
        public int SelectedAttributeValue { get; set; }
        public int ProductId { get; set; }
        public bool? IsWinner { get; set; }
        public int HasClaimed { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int StoreId { get; set; }
    }
}