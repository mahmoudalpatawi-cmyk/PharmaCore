using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Sales;

namespace PharmaCore.Domain.Entities.Clinical;

public class Prescription : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public string DoctorName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;

    public string? ApprovalNumber { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime PrescriptionDate { get; set; } = DateTime.UtcNow;

    public int? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }

    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
