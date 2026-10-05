using PharmaCore.Domain.Common;

namespace PharmaCore.Domain.Entities.Catalog;

public class ActiveIngredient : BaseEntity, IMustHaveTenant, ISoftDelete
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Contraindications { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<MedicineActiveIngredient> MedicineActiveIngredients { get; set; } = new List<MedicineActiveIngredient>();
}
