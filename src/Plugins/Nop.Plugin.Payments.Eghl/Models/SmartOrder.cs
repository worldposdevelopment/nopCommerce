using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Payments.Eghl.Models
{
  public  class SmartOrder
    {
        public string OrderNo { get; set; }
        public string OrderDate { get; set; }
        public int TotalItems { get; set; }
        public int Seq { get; set; }
        public string ItemBrand { get; set; }
        public string ItemDescription { get; set; }
        public string ItemSize { get; set; }
        public int LineTotal { get; set; }

    }
}
