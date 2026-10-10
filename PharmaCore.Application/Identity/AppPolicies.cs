using PharmaCore.Domain.Entities.Identity;

namespace PharmaCore.Application.Identity;

/// <summary>
/// Centralized authorization policy names used to secure API endpoints.
/// </summary>
public static class AppPolicies
{
    // Single-role restrictive policies
    public const string OwnerOnly = "OwnerOnly";
    public const string AdminOnly = "AdminOnly";
    public const string PharmacistOnly = "PharmacistOnly";
    public const string CashierOnly = "CashierOnly";

    // Operational composite policies
    public const string RequireOwnerOrAdmin = "RequireOwnerOrAdmin";
    public const string RequirePharmacyStaff = "RequirePharmacyStaff"; // Owner, Admin, Pharmacist
    public const string RequirePosAccess = "RequirePosAccess";         // Owner, Admin, Cashier

    // Rate Limiting Policy Names
    public const string LoginRateLimitPolicy = "LoginRateLimitPolicy";
}
