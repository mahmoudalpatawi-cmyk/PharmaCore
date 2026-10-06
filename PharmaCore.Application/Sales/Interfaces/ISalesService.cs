using System.Threading;
using System.Threading.Tasks;
using PharmaCore.Application.Sales.DTOs;

namespace PharmaCore.Application.Sales.Interfaces;

public interface ISalesService
{
    Task<SaleInvoiceResponseDto> CreateSaleInvoiceAsync(CreateSaleInvoiceDto dto, CancellationToken cancellationToken = default);
    Task<SaleInvoiceResponseDto> GetSaleInvoiceByIdAsync(int id, CancellationToken cancellationToken = default);
}
