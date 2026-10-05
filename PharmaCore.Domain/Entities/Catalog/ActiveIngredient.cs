using PharmaCore.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Catalog
{
    public class ActiveIngredient : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public ICollection<MedicineActiveIngredient> MedicineActiveIngredients { get; set; } = new List<MedicineActiveIngredient>();
    }
}
