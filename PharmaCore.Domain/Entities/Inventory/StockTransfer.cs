using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Inventory
{
    public class StockTransfer : BaseEntity
    {
        public int FromBranchId { get; set; }
        public Branch FromBranch { get; set; } = null!;

        public int ToBranchId { get; set; }
        public Branch ToBranch { get; set; } = null!;

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public DateTime TransferDate { get; set; }

        public StockTransferStatus Status { get; set; }

        public string? Notes { get; set; }

        public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
    }
}
