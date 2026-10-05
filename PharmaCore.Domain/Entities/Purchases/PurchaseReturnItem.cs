using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Inventory;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Purchases
{
    public class PurchaseReturnItem : BaseEntity
    {
        public int PurchaseReturnId { get; set; }
        public PurchaseReturn PurchaseReturn { get; set; } = null!;

        public int BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        public int Quantity { get; set; }
    }
}
