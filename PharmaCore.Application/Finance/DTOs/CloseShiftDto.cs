namespace PharmaCore.Application.Finance.DTOs;

public class CloseShiftDto
{
    public int ShiftId { get; set; }
    public decimal EndingCash { get; set; }
    public string? Notes { get; set; }
}
