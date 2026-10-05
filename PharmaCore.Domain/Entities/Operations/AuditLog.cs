using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Operations
{
    public class AuditLog : BaseEntity
    {
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public string Action { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;
        public int? EntityId { get; set; }

        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        public string? IpAddress { get; set; }
    }
}
