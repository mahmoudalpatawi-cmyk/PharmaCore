using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Inventory;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Sales
{
    public class SaleItem : BaseEntity
    {
        public int SaleId { get; set; }
        public Sale Sale { get; set; } = null!;

        public int BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        public int Quantity { get; set; }

        public UnitType UnitSold { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
