# 🚀 PharmaCore (SaaS ERP & POS) - Development Tracker

## Phase 1: Project Setup & Architecture (Completed ✅)
- [x] **Database Design (ERD):** Designed 15 modules covering customers, suppliers, inventory, ledger, shifts, and invoices.
- [x] **GitHub Repository Setup:** Created public 'PharmaCore' repository with README and .gitignore.
- [x] **Clean Architecture Scaffolding:** Initialized Domain, Application, Infrastructure, and API projects with proper references.

## Phase 2: Domain Layer (Core Entities) (Completed ✅)
- [x] **BaseEntity Implementation:** Added `Id`, `CreatedAt`, `UpdatedAt`, `IsActive` in the Common folder.
- [x] **Domain Enums:** Created separate enum files for `PaymentMethod`, `ShiftStatus`, `TransactionType`, etc.
- [x] **Entity Models:** Programmed Pure POCO entities (Tenant, Branch, Medicine, Batch, Sale, etc.) strictly without Data Annotations.
- [ ] **Git Commit & Push:** Commit Domain Entities and merge with the main branch via Pull Request.

## Phase 3: Infrastructure Layer & Database ⏳
- [ ] **Install EF Core Packages:** Add Entity Framework Core, SQL Server, and Tools to Infrastructure and API projects.
- [ ] **ApplicationDbContext Setup:** Register all entities as `DbSet` within the Context.
- [ ] **Fluent API Configurations:** Define Primary/Foreign Keys, precision (decimal 18,2), and Global Query Filters (`IsActive`).
- [ ] **Database Migrations:** Run `Add-Migration` and `Update-Database` to generate SQL tables.

## Phase 4: Application Layer (Business Logic)
- [ ] **Interfaces (Contracts):** Implement `IGenericRepository` and `IUnitOfWork`.
- [ ] **Repositories Implementation:** Write the concrete data access code in the Infrastructure layer.
- [ ] **DTOs & Validation:** Create DTOs and apply validation rules (Data Annotations or FluentValidation).
- [ ] **AutoMapper Setup:** Configure `MappingProfiles` for Entity-to-DTO mapping.
- [ ] **Core Business Services:** Implement accounting/inventory logic (stock deduction, shift closing, customer ledger settlement).

## Phase 5: API Layer
- [ ] **Dependency Injection (DI):** Register services and repositories in `Program.cs`.
- [ ] **Controllers Setup:** Build RESTful endpoints for Medicines, Sales, Shifts, and Inventory.
- [ ] **Global Exception Handling:** Implement custom Middleware for centralized JSON error responses.
- [ ] **Swagger Customization:** Configure Swagger UI for JWT documentation and endpoint descriptions.

## Phase 6: Authentication & Authorization
- [ ] **ASP.NET Core Identity Integration:** Link `ApplicationUser` to the Identity framework.
- [ ] **JWT Token Service:** Generate encrypted tokens containing `TenantId` and Roles.
- [ ] **Endpoint Security:** Apply `[Authorize]` and implement Tenant-based data isolation.

## Phase 7: Advanced Features (AI & Background Jobs)
- [ ] **Smart Restock Algorithm:** Calculate average sales and lead times to suggest automatic purchase orders.
- [ ] **Hangfire / Quartz.NET Setup:** Schedule background tasks to check for expiring medicines daily.
- [ ] **Audit Logging System:** Implement an EF Core Interceptor to automatically track data modifications.

## Phase 8: Testing & Deployment
- [ ] **Unit Testing:** Write tests for critical business logic and services using xUnit and Moq.
- [ ] **Production Database Preparation:** Seed default data (Roles, default Admin user).
- [ ] **Deployment:** Publish the application to a live server (SmarterASP, Azure, or VPS).
- [ ] **Front-end Integration:** Configure CORS policies to allow connections from React/Angular apps.