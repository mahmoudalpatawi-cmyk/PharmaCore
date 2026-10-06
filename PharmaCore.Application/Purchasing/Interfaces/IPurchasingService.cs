using System.Threading;
using System.Threading.Tasks;
using PharmaCore.Application.Purchasing.DTOs;

namespace PharmaCore.Application.Purchasing.Interfaces;

public interface IPurchasingService
{
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default);
    Task<PurchaseInvoiceResponseDto> CreatePurchaseInvoiceAsync(CreatePurchaseInvoiceDto dto, CancellationToken cancellationToken = default);
    Task<PurchaseInvoiceResponseDto> GetPurchaseInvoiceByIdAsync(int id, CancellationToken cancellationToken = default);
}
