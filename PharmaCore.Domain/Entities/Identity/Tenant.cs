using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Catalog;
using PharmaCore.Domain.Entities.Operations;
using PharmaCore.Domain.Entities.Partners;
using System;
using System.Collections.Generic;
using System.Text;

namespace PharmaCore.Domain.Entities.Identity
{
    public class Tenant : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public ICollection<Branch> Branches { get; set; } = new List<Branch>();
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
        public ICollection<InsuranceCompany> InsuranceCompanies { get; set; } = new List<InsuranceCompany>();
        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
