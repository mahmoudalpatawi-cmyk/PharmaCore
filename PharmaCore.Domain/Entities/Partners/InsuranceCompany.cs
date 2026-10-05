using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Sales;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Partners
{
    public class InsuranceCompany : BaseEntity
    {
        public int TenantId { get; set; }
        public Tenant Tenant { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        public ICollection<CustomerInsurance> CustomerInsurances { get; set; } = new List<CustomerInsurance>();
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
