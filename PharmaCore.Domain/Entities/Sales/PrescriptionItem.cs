using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Sales
{
    public class PrescriptionItem : BaseEntity
    {
        public int PrescriptionId { get; set; }
        public Prescription Prescription { get; set; } = null!;

        public int MedicineId { get; set; }
        public Medicine Medicine { get; set; } = null!;

        public int? PrescribedQuantity { get; set; }

        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }

        public string? Instructions { get; set; }
    }
}
