using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Operations;
using PharmaCore.Domain.Entities.Partners;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Purchases
{
    public class PurchaseOrder : BaseEntity
    {
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public decimal TotalAmount { get; set; }

        public DateTime Date { get; set; }

        public PurchaseOrderStatus Status { get; set; }

        public string? Notes { get; set; }

        public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
        public ICollection<PurchaseReturn> PurchaseReturns { get; set; } = new List<PurchaseReturn>();
    }
}
