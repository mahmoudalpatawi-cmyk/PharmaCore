using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Partners
{
    public class CustomerAccountTransaction : BaseEntity
    {
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public CustomerAccountTransactionType TransactionType { get; set; }

        public decimal Amount { get; set; }

        public ReferenceType? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }

        public DateTime Date { get; set; }

        public string? Notes { get; set; }
    }
}
