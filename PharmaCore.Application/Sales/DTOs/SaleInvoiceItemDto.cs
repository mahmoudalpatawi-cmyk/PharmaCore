namespace PharmaCore.Application.Sales.DTOs;

public class SaleInvoiceItemDto
{
    public int BatchId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
}
