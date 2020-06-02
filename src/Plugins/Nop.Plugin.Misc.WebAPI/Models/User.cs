using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.WebAPI.Models
{
    public class User : IdentityUser<int>
    {
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string KnownAs { get; set; }

        public string ProfileImg { get; set; }

        public DateTime Created { get; set; }
        public DateTime LastActive {get;set;}
        public virtual ICollection<UserRole> UserRoles { get; set; }


    }
}
