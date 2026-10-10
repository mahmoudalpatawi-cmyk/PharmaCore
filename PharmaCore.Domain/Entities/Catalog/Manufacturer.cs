using PharmaCore.Domain.Common;

namespace PharmaCore.Domain.Entities.Catalog;

public class Manufacturer : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? ContactInfo { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
}
