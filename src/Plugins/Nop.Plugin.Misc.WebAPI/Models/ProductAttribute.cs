using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Misc.WebAPI.Models
{
   public class ProductAttributeMobile
    {
        public int ProductId { get; set; }
        public int qty { get; set; }
        public string ProductAttributeId { get; set; }
        public string ProductAttributeValue { get; set; }
        public Boolean Selected { get; set; }
           public Boolean HasWarning { get; set; }
    }
}
