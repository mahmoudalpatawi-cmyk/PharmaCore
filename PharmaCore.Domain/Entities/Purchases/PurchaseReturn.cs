using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Operations;
using PharmaCore.Domain.Entities.Partners;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Purchases
{
    public class PurchaseReturn : BaseEntity
    {
        public int? PurchaseOrderId { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }

        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public decimal TotalAmount { get; set; }

        public DateTime ReturnDate { get; set; }

        public string? Reason { get; set; }

        public ICollection<PurchaseReturnItem> Items { get; set; } = new List<PurchaseReturnItem>();
    }
}
