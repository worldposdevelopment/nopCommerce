using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nop.Plugin.Misc.WebAPI.DTO
{
    public class PaymentDetailsDTO 
    {

        public string mobileno { get; set; }
        public string ordernumber { get; set; }
        public string total { get; set; }
        public bool status { get; set; }
    }
}
