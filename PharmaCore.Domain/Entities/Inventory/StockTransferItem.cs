using PharmaCore.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Inventory
{
    public class StockTransferItem : BaseEntity
    {
        public int TransferId { get; set; }
        public StockTransfer Transfer { get; set; } = null!;

        public int BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        public int Quantity { get; set; }
    }
}
