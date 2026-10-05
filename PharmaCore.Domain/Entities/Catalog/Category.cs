using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Catalog
{
    public class Category : BaseEntity
    {
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
    }
}
