using System.Threading;
using System.Threading.Tasks;
using PharmaCore.Application.Finance.DTOs;

namespace PharmaCore.Application.Finance.Interfaces;

public interface IFinanceService
{
    Task<ShiftResponseDto> OpenShiftAsync(OpenShiftDto dto, CancellationToken cancellationToken = default);
    Task<ShiftResponseDto> CloseShiftAsync(CloseShiftDto dto, CancellationToken cancellationToken = default);
    Task<CashTransactionResponseDto> AddCashTransactionAsync(CashTransactionDto dto, CancellationToken cancellationToken = default);
    Task<ShiftResponseDto?> GetCurrentActiveShiftAsync(int branchId, CancellationToken cancellationToken = default);
}
