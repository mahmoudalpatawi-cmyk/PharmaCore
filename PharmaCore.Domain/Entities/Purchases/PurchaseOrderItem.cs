using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Purchases
{
    public class PurchaseOrderItem : BaseEntity
    {
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = null!;

        public int MedicineId { get; set; }
        public Medicine Medicine { get; set; } = null!;

        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }
}
