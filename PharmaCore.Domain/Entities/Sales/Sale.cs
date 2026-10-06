using System;
using System.Collections.Generic;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Identity;
using PharmaCore.Domain.Entities.Insurance;
using PharmaCore.Domain.Entities.MultiTenancy;
using PharmaCore.Domain.Entities.Shifts;

namespace PharmaCore.Domain.Entities.Sales
{
    public class Sale : BaseEntity, IMustHaveTenant, ISoftDelete
    {
        // 1. حقول السيستم (Public Set)
        public Guid TenantId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // 2. الروابط الأساسية (Private Set - تُحدد وقت الإنشاء فقط)
        public int ShiftId { get; private set; }
        public Shift? Shift { get; private set; }

        public int BranchId { get; private set; }
        public Branch? Branch { get; private set; }

        public int? CustomerId { get; private set; }
        public Customer? Customer { get; private set; }

        public int? InsuranceCompanyId { get; private set; }
        public InsuranceCompany? InsuranceCompany { get; private set; }

        public int? CreatedByUserId { get; private set; }
        public ApplicationUser? CreatedByUser { get; private set; }

        // 3. الخصائص المالية (Private Set - ممنوع التلاعب بها)
        public decimal TotalAmount { get; private set; }
        public decimal InsuranceCoverageAmount { get; private set; }
        public decimal Discount { get; private set; }
        public decimal NetAmount { get; private set; }

        public int PointsRedeemed { get; private set; }
        public int PointsEarned { get; private set; }

        public DateTime OrderDate { get; private set; }
        public string? Notes { get; private set; }

        // 4. القوائم المرتبطة
        public ICollection<SaleItem> Items { get; private set; } = new List<SaleItem>();
        public ICollection<Payment> Payments { get; private set; } = new List<Payment>();
        public ICollection<SalesReturn> SalesReturns { get; private set; } = new List<SalesReturn>();

        // Constructor: الإجبار على إدخال البيانات الحيوية لبناء فاتورة سليمة
        public Sale(Guid tenantId, int shiftId, int branchId, int? createdByUserId, int? customerId = null, int? insuranceCompanyId = null, string? notes = null)
        {
            TenantId = tenantId;
            ShiftId = shiftId;
            BranchId = branchId;
            CreatedByUserId = createdByUserId;
            CustomerId = customerId;
            InsuranceCompanyId = insuranceCompanyId;
            Notes = notes;
            OrderDate = DateTime.UtcNow;

            // تصفير العدادات المالية عند فتح الفاتورة
            TotalAmount = 0;
            InsuranceCoverageAmount = 0;
            Discount = 0;
            NetAmount = 0;
            PointsRedeemed = 0;
            PointsEarned = 0;
        }

        // Constructor فارغ للـ EF Core
        private Sale() { }

        // --- دوال البيزنس (Business Logic Methods) ---

        /// <summary>إضافة خصم وإعادة حساب الصافي</summary>
        public void ApplyDiscount(decimal discountAmount)
        {
            if (discountAmount < 0 || discountAmount > TotalAmount)
                throw new ArgumentException("Invalid discount amount.");

            Discount = discountAmount;
            CalculateNetAmount(); // السيستم بيحسب الصافي بنفسه
        }

        /// <summary>تطبيق تحمل شركة التأمين وإعادة حساب الصافي</summary>
        public void ApplyInsuranceCoverage(decimal coverageAmount)
        {
            if (coverageAmount < 0 || coverageAmount > TotalAmount)
                throw new ArgumentException("Invalid insurance coverage amount.");

            InsuranceCoverageAmount = coverageAmount;
            CalculateNetAmount(); // السيستم بيحسب الصافي بنفسه
        }

        /// <summary>دالة داخلية لحساب صافي الفاتورة بدقة</summary>
        private void CalculateNetAmount()
        {
            // الصافي = الإجمالي - الخصم - ما ستدفعه شركة التأمين
            NetAmount = TotalAmount - Discount - InsuranceCoverageAmount;
        }

        // ملاحظة: لاحقاً عند برمجة SaleItem، سنقوم بعمل دالة AddItem هنا 
        // لتقوم بزيادة الـ TotalAmount أوتوماتيكياً كلما أضفنا دواء للفاتورة.
    }
}