using PharmaCore.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Catalog
{
    public class MedicineActiveIngredient : BaseEntity
    {
        public int MedicineId { get; set; }
        public Medicine Medicine { get; set; } = null!;

        public int ActiveIngredientId { get; set; }
        public ActiveIngredient ActiveIngredient { get; set; } = null!;

        public string? Dosage { get; set; }
    }
}
