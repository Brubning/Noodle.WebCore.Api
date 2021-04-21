using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Noodle.WebCore.Api.Data;

namespace Noodle.WebCore.Api.Models
{
    public class RegisterRequest
    {
        public PortalUser PortalUser { get; set; }
        
        public string Password { get; set; }

        public string ConfirmPassword { get; set; }
    }
}
