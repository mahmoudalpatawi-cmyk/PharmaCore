using PharmaCore.Domain.Common;

namespace PharmaCore.Domain.Entities.Catalog;

public class MedicineActiveIngredient : BaseEntity, IMustHaveTenant
{
    public Guid TenantId { get; set; }

    public int MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    public int ActiveIngredientId { get; set; }
    public ActiveIngredient? ActiveIngredient { get; set; }

    public string Dosage { get; set; } = string.Empty;
}
