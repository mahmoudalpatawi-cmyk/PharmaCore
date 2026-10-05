using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Inventory
{
    public class InventoryTransaction : BaseEntity
    {
        public int BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public InventoryTransactionType TransactionType { get; set; }

        public int Quantity { get; set; }

        public ReferenceType? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }

        public DateTime Date { get; set; }

        public string? Notes { get; set; }
    }
}
