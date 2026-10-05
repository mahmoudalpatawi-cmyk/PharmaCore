using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Inventory;
using PharmaCore.Domain.Entities.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Sales
{
    public class SalesReturn : BaseEntity
    {
        public int? SaleId { get; set; }
        public Sale? Sale { get; set; }

        public int ShiftId { get; set; }
        public Shift Shift { get; set; } = null!;

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public decimal TotalAmount { get; set; }

        public DateTime ReturnDate { get; set; }

        public string? Reason { get; set; }

        public ICollection<SalesReturnItem> Items { get; set; } = new List<SalesReturnItem>();
    }
}
