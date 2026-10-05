using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Entities.Purchases;
using PharmaCore.Domain.Entities.Sales;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PharmaCore.Domain.Entities.Catalog
{
    public class Medicine : BaseEntity
    {
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public string? Barcode { get; set; }

        public string PrimaryUnit { get; set; } = string.Empty;    // e.g. Box
        public string SecondaryUnit { get; set; } = string.Empty;  // e.g. Strip
        public int ConversionFactor { get; set; }                  // 1 Box = N Strips

        public decimal SellingPrice { get; set; }
        public int MinStockLevel { get; set; }

        public bool IsScheduleDrug { get; set; }

        public ICollection<MedicineActiveIngredient> MedicineActiveIngredients { get; set; } = new List<MedicineActiveIngredient>();
        public ICollection<Batch> Batches { get; set; } = new List<Batch>();
        public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
        public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
    }
}
