using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using PharmaCore.Application.Finance.DTOs;
using PharmaCore.Application.Finance.Interfaces;
using PharmaCore.Application.Identity.Interfaces;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Finance;
using PharmaCore.Domain.Entities.Sales;
using PharmaCore.Domain.Entities.Shifts;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Application.Finance.Services;

public class FinanceService : IFinanceService
{
    private readonly IRepository<Shift> _shiftRepository;
    private readonly IRepository<CashTransaction> _cashTransactionRepository;
    private readonly IRepository<SaleInvoice> _saleInvoiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public FinanceService(
        IRepository<Shift> shiftRepository,
        IRepository<CashTransaction> cashTransactionRepository,
        IRepository<SaleInvoice> saleInvoiceRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _shiftRepository = shiftRepository ?? throw new ArgumentNullException(nameof(shiftRepository));
        _cashTransactionRepository = cashTransactionRepository ?? throw new ArgumentNullException(nameof(cashTransactionRepository));
        _saleInvoiceRepository = saleInvoiceRepository ?? throw new ArgumentNullException(nameof(saleInvoiceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<ShiftResponseDto> OpenShiftAsync(OpenShiftDto dto, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.CanAccessBranch(dto.BranchId))
        {
            throw new UnauthorizedAccessException($"User is not authorized to open a shift for branch {dto.BranchId}.");
        }

        var tenantId = _tenantProvider.GetTenantId();

        var existingShifts = await _shiftRepository.ListAsync(s => s.TenantId == tenantId && s.BranchId == dto.BranchId && s.Status == ShiftStatus.Open, cancellationToken);
        if (existingShifts.Any())
            throw new InvalidOperationException("An active shift already exists for this branch.");

        var shift = new Shift
        {
            TenantId = tenantId,
            BranchId = dto.BranchId,
            OpeningCash = dto.StartingCash,
            Status = ShiftStatus.Open,
            StartTime = DateTime.UtcNow
        };

        await _shiftRepository.AddAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ShiftResponseDto>(shift);
    }

    public async Task<ShiftResponseDto> CloseShiftAsync(CloseShiftDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        
        var shift = await _shiftRepository.GetByIdAsync(dto.ShiftId, cancellationToken);
        if (shift == null || shift.TenantId != tenantId)
            throw new UnauthorizedAccessException("Shift not found or unauthorized.");

        if (!_currentUserService.CanAccessBranch(shift.BranchId))
            throw new UnauthorizedAccessException($"User is not authorized to close shift for branch {shift.BranchId}.");

        if (shift.Status != ShiftStatus.Open)
            throw new InvalidOperationException("Shift is already closed.");

        var cashTransactions = await _cashTransactionRepository.ListAsync(t => t.TenantId == tenantId && t.ShiftId == shift.Id, cancellationToken);
        decimal totalIncome = cashTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        decimal totalExpense = cashTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var invoices = await _saleInvoiceRepository.ListAsync(i => i.TenantId == tenantId && i.ShiftId == shift.Id, cancellationToken);
        decimal totalCashSales = invoices.Where(i => i.PaymentMethod == PaymentMethod.Cash).Sum(i => i.PaidAmount);
        decimal totalSales = invoices.Sum(i => i.TotalAmount);

        decimal closingCashSystem = shift.OpeningCash + totalIncome - totalExpense + totalCashSales;

        shift.ClosingCashSystem = closingCashSystem;
        shift.ClosingCashActual = dto.EndingCash;
        shift.CashDifference = dto.EndingCash - closingCashSystem;
        shift.TotalSales = totalSales;
        shift.TotalExpenses = totalExpense;
        
        shift.Status = ShiftStatus.Closed;
        shift.EndTime = DateTime.UtcNow;
        shift.ClosingNotes = dto.Notes;

        await _shiftRepository.UpdateAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ShiftResponseDto>(shift);
    }

    public async Task<CashTransactionResponseDto> AddCashTransactionAsync(CashTransactionDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var shift = await _shiftRepository.GetByIdAsync(dto.ShiftId, cancellationToken);
        if (shift == null || shift.TenantId != tenantId)
            throw new UnauthorizedAccessException("Shift not found or unauthorized.");

        if (!_currentUserService.CanAccessBranch(shift.BranchId))
            throw new UnauthorizedAccessException($"User is not authorized to record cash transactions for branch {shift.BranchId}.");

        if (shift.Status != ShiftStatus.Open)
            throw new InvalidOperationException("Cannot add cash transaction to a closed shift.");

        var transaction = new CashTransaction
        {
            TenantId = tenantId,
            ShiftId = dto.ShiftId,
            BranchId = shift.BranchId,
            Type = dto.TransactionType,
            Amount = dto.Amount,
            Category = dto.Category,
            Description = dto.Reason
        };

        await _cashTransactionRepository.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CashTransactionResponseDto>(transaction);
    }

    public async Task<ShiftResponseDto?> GetCurrentActiveShiftAsync(int branchId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.CanAccessBranch(branchId))
        {
            throw new UnauthorizedAccessException($"User is not authorized to access active shift for branch {branchId}.");
        }

        var tenantId = _tenantProvider.GetTenantId();
        var shifts = await _shiftRepository.ListAsync(s => s.TenantId == tenantId && s.BranchId == branchId && s.Status == ShiftStatus.Open, cancellationToken);
        
        var shift = shifts.FirstOrDefault();
        if (shift == null)
            return null;

        return _mapper.Map<ShiftResponseDto>(shift);
    }
}
