using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Inventory;

namespace PharmaCore.Domain.Entities.Sales;

public class SaleItem : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int SaleId { get; set; }
    public Sale? Sale { get; set; }

    public int BatchId { get; set; }
    public Batch? Batch { get; set; }

    public int Quantity { get; set; }
    public string UnitSold { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
}
