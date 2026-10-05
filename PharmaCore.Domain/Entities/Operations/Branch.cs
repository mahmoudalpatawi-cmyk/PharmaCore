using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Entities.Purchases;
using PharmaCore.Domain.Entities.Sales;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PharmaCore.Domain.Entities.Operations
{
    public class Branch : BaseEntity
    {
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }

        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
        public ICollection<Batch> Batches { get; set; } = new List<Batch>();
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
        public ICollection<StockTransfer> OutgoingTransfers { get; set; } = new List<StockTransfer>();
        public ICollection<StockTransfer> IncomingTransfers { get; set; } = new List<StockTransfer>();
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public ICollection<PurchaseReturn> PurchaseReturns { get; set; } = new List<PurchaseReturn>();
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
