
using Nop.Core.Domain.Media;
using Nop.Plugin.Misc.WebAPI.Models.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.WebAPI.Models.Promotions { 
    public class HomeSlider
    {
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public int ListingOrder { get; set; }
        public string Picture { get; set; }
        public string Createddt { get; set; }
        public string Updateddt { get; set; }
    }
}
