using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Operations;
using PharmaCore.Domain.Entities.Partners;
using System;
using System.Collections.Generic;
using System.Net.ServerSentEvents;
using System.Text;

namespace PharmaCore.Domain.Entities.Sales
{
    public class Sale : BaseEntity
    {
        public int ShiftId { get; set; }
        public Shift Shift { get; set; } = null!;

        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public int? InsuranceCompanyId { get; set; }
        public InsuranceCompany? InsuranceCompany { get; set; }

        public int CreatedByUserId { get; set; }
        public ApplicationUser CreatedByUser { get; set; } = null!;

        public decimal TotalAmount { get; set; }
        public decimal InsuranceCoverageAmount { get; set; }
        public decimal Discount { get; set; }

        public int PointsRedeemed { get; set; }
        public int PointsEarned { get; set; }

        public decimal NetAmount { get; set; }

        public DateTime OrderDate { get; set; }

        public string? Notes { get; set; }

        public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<SalesReturn> SalesReturns { get; set; } = new List<SalesReturn>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }
}
