using System.Collections.Generic;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Application.Sales.DTOs;

public class CreateSaleInvoiceDto
{
    public int BranchId { get; set; }
    public int ShiftId { get; set; }
    public string? CustomerName { get; set; }
    public decimal PaidAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    
    public List<SaleInvoiceItemDto> Items { get; set; } = new List<SaleInvoiceItemDto>();
}
