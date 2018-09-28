using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace IdentitySample.DefaultUI.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        [ProtectedPersonalData]
        public string Name { get; set; }
        
        [PersonalData]
        public int Age { get; set; }
    }
}
