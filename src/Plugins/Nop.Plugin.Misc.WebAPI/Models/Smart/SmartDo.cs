using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nop.Plugin.Misc.WebAPI.Models.Smart
{

    public class SmartDo
    {
        public SmartDo()
        {
            DocumentDetails = new List<Documentdetail>();
        }
        public Documentheader DocumentHeader { get; set; }
        public List<Documentdetail> DocumentDetails { get; set; }
    }

    public class Documentheader
    {
        public string AcCusDeliveryOrderMID { get; set; }
        public string AcCustomerID { get; set; }
        public string AcLocationID { get; set; }
        public string DocumentDate { get; set; }
        public string DeliveryDate { get; set; }
        public decimal DocumentNetAmount { get; set; }
        public decimal DocumentCentBalance { get; set; }
        public decimal DocumentFinalAmount { get; set; }
        public string RefDocumentNo { get; set; }
        public string DocumentRemark { get; set; }
        public string ExtraRemark1 { get; set; }
        public string ExtraRemark2 { get; set; }
        public string ExtraRemark3 { get; set; }
        public string ExtraRemark4 { get; set; }
        public string DocumentYourReference { get; set; }
        public string ShipToRemark { get; set; }
        public string ShipToShipVia { get; set; }
        public string ShipToAddress1 { get; set; }
        public string ShipToAddress2 { get; set; }
        public string ShipToAddress3 { get; set; }
        public string ShipToAddress4 { get; set; }
        public string ShipToPhone1 { get; set; }
        public string ShipToPhone2 { get; set; }
        public string ShipToFax { get; set; }
        public string ShipToContact1 { get; set; }
        public string ShipToContact2 { get; set; }
        public string ShipToAttention { get; set; }
    }

    public class Documentdetail
    {
        public string AcCusDeliveryOrderMID { get; set; }
        public string ItemNo { get; set; }
        public string AcStockID { get; set; }
        public string AcStockUOMID { get; set; }
        public decimal ItemUnitPrice { get; set; }
        public int ItemQuantity { get; set; }
        public decimal ItemGrossTotal { get; set; }
        public decimal ItemDiscountAmount { get; set; }
        public decimal ItemTotalPrice { get; set; }
        public string ItemRemark1 { get; set; }
    }
}
