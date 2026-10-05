using PharmaCore.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Identity
{
    public class Role : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}
