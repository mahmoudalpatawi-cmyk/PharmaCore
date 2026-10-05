using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Sales
{
    public class Payment : BaseEntity
    {
        public int SaleId { get; set; }
        public Sale Sale { get; set; } = null!;

        public int ShiftId { get; set; }
        public Shift Shift { get; set; } = null!;

        public PaymentMethod PaymentMethod { get; set; }

        public decimal Amount { get; set; }

        public string? ReferenceNumber { get; set; }

        public DateTime PaymentDate { get; set; }

        public string? Notes { get; set; }
    }
}
