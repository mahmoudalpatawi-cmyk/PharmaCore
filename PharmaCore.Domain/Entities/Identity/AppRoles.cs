namespace PharmaCore.Domain.Entities.Identity;

/// <summary>
/// Canonical role names used across PharmaCore for role-based authorization.
/// </summary>
public static class AppRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Pharmacist = "Pharmacist";
    public const string Cashier = "Cashier";
}
