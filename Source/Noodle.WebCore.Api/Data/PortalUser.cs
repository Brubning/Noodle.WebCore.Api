using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noodle.WebCore.Api.Data
{
    /// <summary>
    /// Core PortalUser derived from Identity.User
    /// </summary>
    public class PortalUser : Microsoft.AspNetCore.Identity.IdentityUser<long>
    {
    }
}
