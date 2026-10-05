using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Sales;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Operations
{
    public class Shift : BaseEntity
    {
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public int? ClosedByUserId { get; set; }
        public ApplicationUser? ClosedByUser { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public decimal OpeningCash { get; set; }
        public decimal? ExpectedCash { get; set; }
        public decimal? ClosingCash { get; set; }
        public decimal? CashDifference { get; set; }

        public ShiftStatus Status { get; set; }

        public string? ClosingNotes { get; set; }

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<SalesReturn> SalesReturns { get; set; } = new List<SalesReturn>();
    }
}
