using System.Collections.Generic;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Application.Purchasing.DTOs;

public class CreatePurchaseInvoiceDto
{
    public int SupplierId { get; set; }
    public int BranchId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    
    public List<PurchaseInvoiceItemDto> Items { get; set; } = new List<PurchaseInvoiceItemDto>();
}
