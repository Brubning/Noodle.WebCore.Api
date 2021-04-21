using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noodle.WebCore.Api.Data
{
    /// <summary>
    /// DbContext for Identity.
    /// </summary>
    public class PortalUserDbContext : IdentityDbContext<PortalUser, PortalRole, long>
    {
        public PortalUserDbContext(DbContextOptions<PortalUserDbContext> options) : base(options)
        {
        }
    }
}
