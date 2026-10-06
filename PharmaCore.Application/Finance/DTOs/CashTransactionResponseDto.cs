namespace PharmaCore.Application.Finance.DTOs;

public class CashTransactionResponseDto
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
}
