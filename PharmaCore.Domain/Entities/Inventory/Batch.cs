using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;
using PharmaCore.Domain.Entities.Operations;
using PharmaCore.Domain.Entities.Partners;
using PharmaCore.Domain.Entities.Purchases;
using PharmaCore.Domain.Entities.Sales;
using System;
using System.Collections.Generic;
using System.Net.ServerSentEvents;
using System.Text;

namespace PharmaCore.Domain.Entities.Inventory
{
    public class Batch : BaseEntity
    {
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        public int MedicineId { get; set; }
        public Medicine Medicine { get; set; } = null!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }

        // Current stock in the smallest unit
        public int Quantity { get; set; }

        public decimal CostPrice { get; set; }

        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
        public ICollection<StockTransferItem> StockTransferItems { get; set; } = new List<StockTransferItem>();
        public ICollection<PurchaseReturnItem> PurchaseReturnItems { get; set; } = new List<PurchaseReturnItem>();
        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
        public ICollection<SalesReturnItem> SalesReturnItems { get; set; } = new List<SalesReturnItem>();
    }
}
