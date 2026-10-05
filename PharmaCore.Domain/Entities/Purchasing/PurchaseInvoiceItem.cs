using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;

namespace PharmaCore.Domain.Entities.Purchasing;

public class PurchaseInvoiceItem : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }

    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpirationDate { get; set; }
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal SubTotal { get; set; }
}
