using System;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Shifts;
using PharmaCore.Domain.Enums;

namespace PharmaCore.Domain.Entities.Sales
{
    public class Payment : BaseEntity, IMustHaveTenant, ISoftDelete
    {
        // حقول السيستم (Public Set لتوافق الإنترفيس والـ EF Core)
        public Guid TenantId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // حقول البزنس (Private Set لمنع التلاعب المحاسبي بعد الدفع)
        public int? SaleId { get; private set; }
        public Sale? Sale { get; private set; }

        public int ShiftId { get; private set; }
        public Shift? Shift { get; private set; }

        public PaymentMethod PaymentMethod { get; private set; }
        public decimal Amount { get; private set; }
        public string? ReferenceNumber { get; private set; }
        public DateTime PaymentDate { get; private set; }
        public string? Notes { get; private set; }

        // 1. Constructor البناء الإجباري
        public Payment(Guid tenantId, int shiftId, PaymentMethod paymentMethod, decimal amount, int? saleId = null, string? referenceNumber = null, string? notes = null)
        {
            if (amount <= 0)
                throw new ArgumentException("Payment amount must be greater than zero.");

            TenantId = tenantId;
            ShiftId = shiftId;
            PaymentMethod = paymentMethod;
            Amount = amount;
            SaleId = saleId;
            ReferenceNumber = referenceNumber;
            Notes = notes;
            PaymentDate = DateTime.UtcNow; // يتم تسجيل وقت الدفع أوتوماتيكياً وقت الإنشاء
        }

        // 2. Constructor فارغ لعمل مكتبة Entity Framework
        private Payment() { }

        // 3. الدالة الوحيدة المسموح بتعديلها بعد الدفع هي الملاحظات
        public void UpdateNotes(string newNotes)
        {
            Notes = newNotes;
        }
    }
}