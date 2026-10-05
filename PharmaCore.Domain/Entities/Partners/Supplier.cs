using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Entities.Purchases;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Partners
{
    public class Supplier : BaseEntity
    {
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }

        // Cached current balance. Movements live in SupplierAccountTransaction.
        public decimal Balance { get; set; }

        public ICollection<Batch> Batches { get; set; } = new List<Batch>();
        public ICollection<SupplierAccountTransaction> AccountTransactions { get; set; } = new List<SupplierAccountTransaction>();
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public ICollection<PurchaseReturn> PurchaseReturns { get; set; } = new List<PurchaseReturn>();
    }
}
