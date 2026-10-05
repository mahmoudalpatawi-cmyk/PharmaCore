using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Identity
{
    public class ApplicationUser : BaseEntity
    {
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public ICollection<Shift> OpenedShifts { get; set; } = new List<Shift>();
        public ICollection<Shift> ClosedShifts { get; set; } = new List<Shift>();
    }
}
