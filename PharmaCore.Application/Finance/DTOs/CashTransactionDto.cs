using PharmaCore.Domain.Enums;

namespace PharmaCore.Application.Finance.DTOs;

public class CashTransactionDto
{
    public int ShiftId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType TransactionType { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
