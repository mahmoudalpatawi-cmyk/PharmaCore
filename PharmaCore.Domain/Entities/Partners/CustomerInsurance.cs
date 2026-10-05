using PharmaCore.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Partners
{
    public class CustomerInsurance : BaseEntity
    {
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public int InsuranceCompanyId { get; set; }
        public InsuranceCompany InsuranceCompany { get; set; } = null!;

        public string MemberNumber { get; set; } = string.Empty;
        public decimal CoveragePercentage { get; set; }
    }
}
