using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Sales;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Partners
{
    public class Customer : BaseEntity
    {
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }

        // Cached current balance. Movements live in CustomerAccountTransaction.
        public decimal Balance { get; set; }

        public int LoyaltyPoints { get; set; }

        public ICollection<CustomerInsurance> Insurances { get; set; } = new List<CustomerInsurance>();
        public ICollection<CustomerAccountTransaction> AccountTransactions { get; set; } = new List<CustomerAccountTransaction>();
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
