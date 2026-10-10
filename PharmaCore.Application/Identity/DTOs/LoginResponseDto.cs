using System;

namespace PharmaCore.Application.Identity.DTOs;

/// <summary>
/// Data transfer object returned upon successful authentication.
/// Contains the signed JWT access token and user profile context.
/// </summary>
public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public int? BranchId { get; set; }
}
