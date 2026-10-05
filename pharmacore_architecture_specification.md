# 🏥 PharmaCore - Enterprise Multi-Tenant Pharmacy ERP & POS Architecture Specification

---

## 1. Project Overview & Business Vision

**PharmaCore** is a modern, multi-tenant SaaS Pharmacy Enterprise Resource Planning (ERP) and Point of Sale (POS) system built using **ASP.NET Core Web API (.NET 8/9)**. 

### Core Value Propositions:
1. **Multi-Tenant SaaS Foundation:** Built to be leased or sold to multiple independent pharmacies/chains. Every tenant has strictly isolated data, settings, and branches.
2. **AI-Powered Inventory & Demand Forecasting:** Proactive replenishment analysis, stockout prediction, and automated purchase recommendations based on historical sales velocity and seasonal patterns.
3. **Comprehensive Operational & Financial Dashboards:** Real-time visibility into shift balances, daily/monthly target achievements, gross profit calculations, low stock notifications, and near-expiry alerts.
4. **Strict Architectural Integrity:** Implemented under **Clean Architecture**, **Domain-Driven Design (DDD)**, and **SOLID** principles.

---

## 2. Architecture & Layering Paradigm (Clean Architecture)

```
PharmaCore/
├── src/
│   ├── Core/
│   │   ├── PharmaCore.Domain/          # Pure POCOs, Enums, Value Objects, Domain Events, Zero Dependencies
│   │   └── PharmaCore.Application/     # CQRS (MediatR), DTOs, FluentValidation, Business Services Interfaces
│   ├── Infrastructure/
│   │   └── PharmaCore.Infrastructure/  # EF Core, Migrations, Repositories, Identity, External APIs, Background Workers
│   └── Presentation/
│       └── PharmaCore.API/             # Controllers/Minimal APIs, Middlewares, DI Wiring, Swagger, Filters
└── tests/
    ├── PharmaCore.UnitTests/
    └── PharmaCore.IntegrationTests/
```

### Dependency Flow:
`API` $\rightarrow$ `Infrastructure` $\rightarrow$ `Application` $\rightarrow$ `Domain`
* *Rule:* Inner layers must **NEVER** know anything about outer layers.

---

## 3. Core Architectural Constraints & Patterns

### 3.1 Multi-Tenancy Strategy
- **Isolation Pattern:** Logical isolation (Shared Database, Shared Schema) using a discriminator column `TenantId (Guid)`.
- **Enforcement:**
  - Entities belonging to a tenant must implement `IMustHaveTenant { Guid TenantId { get; set; } }`.
  - EF Core `ApplicationDbContext` enforces a **Global Query Filter** automatically for all entities implementing `IMustHaveTenant`:
    ```csharp
    builder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _currentTenantService.TenantId);
    ```
  - `ITenantService` resolves `TenantId` per HTTP request from:
    1. JWT Claims (`tenant_id`).
    2. Request Header (`X-Tenant-Id`).
    3. Custom Domain/Subdomain fallback.

### 3.2 Auditability & Soft Deletes
- All core entities inherit from `BaseEntity<TId>`:
  - `Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsActive`.
- All soft-deletable entities implement `ISoftDelete`:
  - `IsDeleted`, `DeletedAt`.
  - Filtered automatically via EF Core Global Query Filter (`!e.IsDeleted`).
- EF Core `SaveChangesInterceptor` automatically populates audit metadata and enforces soft deletes.

### 3.3 CQRS & Request Pipeline
- Application operations are segregated into **Commands** and **Queries** via **MediatR**.
- Cross-cutting concerns are handled via MediatR Pipeline Behaviors:
  - `ValidationBehavior<TRequest, TResponse>`: Executes FluentValidation rules before handlers.
  - `LoggingBehavior<TRequest, TResponse>`: Performance profiling and operational logging.
  - `TransactionBehavior<TRequest, TResponse>`: Automated atomic transactions for state-mutating commands.

---

## 4. Domain Data Model & Entity Specifications

### 4.1 Tenancy & Organization Module
- **Tenant:** `Id`, `Name`, `SubscriptionPlan`, `MaxBranches`, `IsActive`, `SubscriptionExpiryDate`.
- **Branch:** `Id`, `TenantId`, `Name`, `Address`, `Phone`, `IsMainBranch`.
- **ApplicationUser:** Extends ASP.NET Identity `IdentityUser`. Linked with `TenantId`, `BranchId`, `FullName`, `UserRole` (SuperAdmin, PharmacyAdmin, Pharmacist, Cashier).

### 4.2 Medicine Catalog & Scientific Data
- **ActiveIngredient:** `Id`, `TenantId`, `Name`, `Description`, `MaxSafeDailyDosage`.
- **Category:** `Id`, `TenantId`, `Name`, `Description`, `ParentCategoryId`.
- **Medicine:**
  - `Id`, `TenantId`, `TradeName`, `GenericName`, `Barcode`, `CategoryId`.
  - `ReorderLevel` (Minimum threshold for alerts).
  - `UnitOfMeasure` (Strip, Box, Bottle, Ampoule).
  - Many-to-Many relationship with `ActiveIngredient` via `MedicineActiveIngredient`.
- **Batch (تشغيلة):**
  - Crucial for pharmaceuticals: Inventory is managed **at the batch level**, not just the medicine level.
  - `Id`, `TenantId`, `BranchId`, `MedicineId`, `BatchNumber`, `ExpiryDate`.
  - `PurchasePrice`, `SellingPrice`.
  - `QuantityAvailable`, `QuantityReserved`.

### 4.3 Supplier & Purchasing Module
- **Supplier:** `Id`, `TenantId`, `Name`, `Phone`, `Email`, `Address`, `TaxRegistrationNumber`, `CurrentBalance`.
- **PurchaseInvoice:**
  - `Id`, `TenantId`, `BranchId`, `SupplierId`, `InvoiceNumber`, `InvoiceDate`, `TotalAmount`, `DiscountAmount`, `PaidAmount`, `RemainingBalance`, `PaymentStatus`.
- **PurchaseInvoiceItem:**
  - `Id`, `PurchaseInvoiceId`, `MedicineId`, `BatchNumber`, `ExpiryDate`, `Quantity`, `UnitCost`, `SubTotal`.

### 4.4 Sales, Shifts & POS Module
- **Customer:** `Id`, `TenantId`, `Name`, `Phone`, `Address`, `CreditLimit`, `CurrentBalance`.
- **Shift (الوردية):**
  - `Id`, `TenantId`, `BranchId`, `CashierId`, `StartTime`, `EndTime`, `InitialCash`, `ActualEndCash`, `TotalSalesCash`, `TotalSalesVisa`, `DifferenceAmount`, `Status` (Open, Closed).
- **SaleInvoice:**
  - `Id`, `TenantId`, `BranchId`, `ShiftId`, `CustomerId` (optional for walk-ins), `InvoiceNumber`, `InvoiceDate`.
  - `InvoiceType` (Sale, Return).
  - `TotalAmount`, `TaxAmount`, `DiscountAmount`, `NetAmount`, `PaymentMethod` (Cash, Visa, Credit, Mixed).
- **SaleInvoiceItem:**
  - `Id`, `SaleInvoiceId`, `MedicineId`, `BatchId`, `Quantity`, `UnitPrice`, `ItemDiscount`, `SubTotal`.

### 4.5 Financial Transactions & Auditing
- **PaymentTransaction:** `Id`, `TenantId`, `ShiftId`, `TransactionType` (Income, Expense), `Category`, `Amount`, `Notes`, `ReferenceId`.
- **AuditLog:** `Id`, `TenantId`, `UserId`, `TableName`, `Action` (Insert, Update, Delete), `OldValues`, `NewValues`, `Timestamp`.

---

## 5. Specialized Modules

### 5.1 Real-Time Dashboard & Analytics Module
The dashboard serves as the central hub:
1. **Financial KPIs:**
   - Real-time Shift Gross Sales and Net Cash flow.
   - Daily, Monthly, and Annual Sales Target calculations (`Actual vs. Target %`).
   - Net Profit Margin calculation:
     $$\text{Gross Profit} = \sum (\text{SellingPrice} - \text{PurchasePrice}) \times \text{Quantity}$$
2. **Operational Alerts:**
   - **Low-Stock Alert:** Items where $\text{Total Stock} \le \text{ReorderLevel}$.
   - **Near-Expiry Alert:** Batches where $\text{ExpiryDate} \le (\text{CurrentDate} + \text{ConfiguredDays})$.

### 5.2 Artificial Intelligence & Demand Forecasting Module
- **Purpose:** Analyze historical batch sales, consumption velocity, and seasonal spikes to predict stockouts and recommend optimal replenishment quantities.
- **Key Algorithm Parameters:**
  - **Daily Velocity ($V_d$):** Average quantity sold per day over sliding windows (7, 30, 90 days).
  - **Supplier Lead Time ($L$):** Average time from purchase order placement to receipt.
  - **Estimated Depletion Time:**
    $$\text{Days To Stockout} = \frac{\text{Current Available Quantity}}{V_d}$$
  - **Reorder Quantity Recommendation:**
    $$\text{Recommended Order} = (V_d \times L) + \text{Safety Stock} - \text{Current Stock}$$
- **Execution Architecture:**
  - Background tasks (Hangfire or Quartz.NET) run periodically (e.g., daily at midnight) to compute demand scores and generate `PurchaseRecommendation` entities without locking the POS database.

---

## 6. Coding Standards & Guidelines for AI Code Generation

When generating code for this repository:
1. **Pure Domain Entities:** No EF Core annotations (`[Key]`, `[ForeignKey]`, `[Table]`) inside `PharmaCore.Domain`. Use **Fluent API** configurations in `PharmaCore.Infrastructure.Persistence.Configurations`.
2. **Encapsulation:** Protect entity state with private or internal setters where applicable. Domain mutation logic should be encapsulated inside entity methods.
3. **Decoupled Business Logic:** Use CQRS (`MediatR`) for application logic. Controllers must remain thin, delegating requests directly to the mediator.
4. **Validation:** Use `FluentValidation` validators in `PharmaCore.Application`. Do not place validation logic inside controllers.
5. **Modern C# Standards:** Always use C# 12+ idioms:
   - File-scoped namespaces (`namespace PharmaCore.Domain.Entities;`).
   - Primary constructors for services and command handlers.
   - Nullable reference types enabled (`#nullable enable`).