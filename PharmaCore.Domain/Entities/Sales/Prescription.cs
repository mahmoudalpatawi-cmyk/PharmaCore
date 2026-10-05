using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Sales
{
    public class Prescription : BaseEntity
    {
        public int? SaleId { get; set; }
        public Sale? Sale { get; set; }

        public string? DoctorName { get; set; }
        public string PatientName { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public DateTime PrescriptionDate { get; set; }

        public int CreatedByUserId { get; set; }
        public ApplicationUser CreatedByUser { get; set; } = null!;

        public string? Notes { get; set; }

        public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
    }
}
