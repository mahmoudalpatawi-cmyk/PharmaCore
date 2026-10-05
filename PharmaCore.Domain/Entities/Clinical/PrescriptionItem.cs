using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;

namespace PharmaCore.Domain.Entities.Clinical;

public class PrescriptionItem : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int PrescriptionId { get; set; }
    public Prescription? Prescription { get; set; }

    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    public int PrescribedQuantity { get; set; }
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Duration { get; set; }
    public string? Instructions { get; set; }
}
