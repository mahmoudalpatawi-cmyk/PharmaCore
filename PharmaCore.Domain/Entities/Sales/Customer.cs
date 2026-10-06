
using System;
using System.Collections.Generic;
using PharmaCore.Domain.Entities.Finance;
using PharmaCore.Domain.Common;
using PharmaCore.Domain.Entities.Insurance;

namespace PharmaCore.Domain.Entities.Sales
{
    public class Customer : BaseEntity, IMustHaveTenant, ISoftDelete
    {
        // تم إغلاق الـ set لتكون private لحماية البيانات من التعديل العشوائي
        public Guid TenantId { get; set; }
        public string Name { get; private set; } = string.Empty;
        public string? Phone { get; private set; }
        public string? Address { get; private set; }

        // حماية مالية صارمة: لا يمكن تعديل الرصيد أو النقاط إلا عبر الدوال المخصصة
        public decimal Balance { get; private set; }
        public int LoyaltyPoints { get; private set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // العلاقات
        public ICollection<Sale> Sales { get; private set; } = new List<Sale>();
        public ICollection<SaleInvoice> Invoices { get; private set; } = new List<SaleInvoice>();
        public ICollection<CustomerInsurance> CustomerInsurances { get; private set; } = new List<CustomerInsurance>();
        public ICollection<CustomerAccountTransaction> AccountTransactions { get; private set; } = new List<CustomerAccountTransaction>();

        // 1. Constructor للبناء الإجباري: يمنع إنشاء عميل بدون اسم أو رقم صيدلية
        public Customer(Guid tenantId, string name, string? phone = null, string? address = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Customer name cannot be empty.");

            TenantId = tenantId;
            Name = name;
            Phone = phone;
            Address = address;
            Balance = 0; 
            LoyaltyPoints = 0;
        }


        private Customer() { }

        public void UpdateDetails(string name, string? phone, string? address)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Customer name cannot be empty.");

            Name = name;
            Phone = phone;
            Address = address;
        }

        // 3. دوال مالية صارمة (تمثل العمليات الحقيقية في الصيدلية)

        /// <summary>زيادة مديونية العميل (عند الشراء الآجل)</summary>
        public void AddDebt(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            Balance += amount;
        }

        /// <summary>سداد مديونية العميل</summary>
        public void PayDebt(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            Balance -= amount; // سيقل الرصيد (المديونية)
        }

        /// <summary>إضافة نقاط ولاء للعميل عند الشراء</summary>
        public void AddLoyaltyPoints(int points)
        {
            if (points > 0)
                LoyaltyPoints += points;
        }

        /// <summary>خصم نقاط الولاء عند استبدالها بخصم مالي</summary>
        public void RedeemLoyaltyPoints(int points)
        {
            if (points <= 0 || points > LoyaltyPoints)
                throw new ArgumentException("Invalid points redemption.");

            LoyaltyPoints -= points;
        }
    }
}
