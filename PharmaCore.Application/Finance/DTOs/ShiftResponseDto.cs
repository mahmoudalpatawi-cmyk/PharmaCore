using System;

namespace PharmaCore.Application.Finance.DTOs;

public class ShiftResponseDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int? OpenedByUserId { get; set; }
    public int? ClosedByUserId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal ClosingCashActual { get; set; }
    public decimal ClosingCashSystem { get; set; }
    public decimal CashDifference { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ClosingNotes { get; set; }
}
