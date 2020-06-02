using System;
using System.Collections.Generic;
using System.Text;
using Nop.Core;

namespace Nop.Plugin.Misc.WebAPI.Domain
{
   public class PhoneActivation :BaseEntity
    {
      
        public virtual  String PhoneNo { get; set; }
        public virtual  String ActivationCode { get; set; }
        public virtual  DateTime DateTime { get; set; }
        public virtual  DateTime Expiry { get; set; }
        public virtual  int Activated { get; set; }
    }
}
