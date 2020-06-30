using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Core.Domain.CampaignPromo
{
    public partial class CampaignMaster : BaseEntity
    {
        public string CampaignType { get; set; }
        public string CampaignName { get; set; }
        public string CampaignImage { get; set; }
        public string TNC { get; set; }
        public DateTime StartEffectiveStamp { get; set; }
        public DateTime EndEffectiveStamp { get; set; }
        public DateTime StartNotifyDate { get; set; }
        public string NotifyImage { get; set; }
        public string ReminderImage { get; set; }
        public decimal Amount { get; set; }
        public int? TotalChop { get; set; }
        public string? Claimable { get; set; }
        public string RecordStatus { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedOnUtc { get; set; }
        public string? UpdatedBy { get; set; }
        public string CampaignUid { get; set; }
        public string CampaignDescription { get; set; }
        public int? DiscountID { get; set; }
        public string? ChopImage { get; set; }
        public string? UnchopImage { get; set; }
        public string? RewardImage { get; set; }
        public bool GenerateWinner { get; set; }
        public int? MaxPurchase { get; set; }
    }
}
